using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using eXtensionSharp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using ChatMessage = OpenAI.Chat.ChatMessage;

namespace OcrSample;

public class DocumentIntelligenceDemo
{
    private readonly IChatClient _chatClient;
    private readonly SearchIndexClient _searchIndexClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly SearchClient _searchClient;
    private readonly DocumentIntelligenceClient _documentIntelligenceClient;
    private readonly IConfiguration _configuration;
    private readonly string _currentThreadId;
    public DocumentIntelligenceDemo(
        IChatClient chatClient,
        IEmbeddingGenerator<string,Embedding<float>> embeddingGenerator,
        SearchIndexClient searchIndexClient,
        SearchClient searchClient,
        DocumentIntelligenceClient documentIntelligenceClient,
        IConfiguration configuration
        )
    {
        _chatClient = chatClient;
        _searchIndexClient = searchIndexClient;
        _embeddingGenerator = embeddingGenerator;
        _searchClient = searchClient;
        _documentIntelligenceClient = documentIntelligenceClient;
        _configuration = configuration;
        _currentThreadId = Guid.NewGuid().ToString();
    }

    
    public async Task UploadAsync()
    {
        var files = new[] { "E:\\문서\\데이터 표준화 지침.pdf" };
        var list = new List<DocChunk>();
        foreach (var filePath in files)
        {
            var sw = Stopwatch.StartNew();
            await using var stream = File.OpenRead(filePath);
            var binary = await BinaryData.FromStreamAsync(stream);
            var operation = await _documentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", binary);
            var result = operation.Value;
            Console.WriteLine($"Pages: {result.Pages.Count}");
        
            foreach (var documentPage in result.Pages)
            {
                int seq = 0;
                var docId = filePath.xGetFileName().xGetHashCode();
                var paragraphs = DiExtractors.ExtractParagraphs(result, documentPage.PageNumber);
                var chunks = Chunker.ChunkByBoundary(paragraphs, 800, 1200, 200);

                var throttler = new SemaphoreSlim(2);
                var tasks = chunks.Select(async text =>
                {
                    await throttler.WaitAsync();

                    try
                    {
                        var vec = (await _embeddingGenerator.GenerateAsync(text)).Vector.ToArray();
                        return new DocChunk {
                            ChunkId = $"{docId}:{documentPage.PageNumber:D4}:{seq++:D3}",
                            DocId = docId,
                            SourceFileType = "pdf",
                            SourceFilePath = filePath,
                            SourceFileName = filePath.xGetFileName(),
                            Page = documentPage.PageNumber,
                            Content = text,
                            ContentVector = vec
                        };
                    }
                    finally
                    {
                        throttler.Release();
                    }
                });
                var docs = await Task.WhenAll(tasks);
                list.AddRange(docs);
            }            
            sw.Stop();
            Console.WriteLine("time: {0}", sw.Elapsed.TotalMilliseconds);
        }

        var sw2 = Stopwatch.StartNew();
        var objs = list.Select(m => new
        {
            chunk_id = m.ChunkId,
            doc_id = m.DocId,
            source_file_type = m.SourceFileType,
            source_file_path = m.SourceFilePath,
            source_file_name = m.SourceFileName,
            page = m.Page,
            content = m.Content,
            content_vector = m.ContentVector,
        });
        await _searchClient.UploadDocumentsAsync(objs);
        sw2.Stop();
        Console.WriteLine("upload time: {0}", sw2.Elapsed.TotalMilliseconds);
    }

    public async Task CreateIndexAsync()
    {
        var indexName = _configuration["AZURE_AI_SEARCH_INDEX_NAME"];
        bool isExcept = false;
        try
        {
            await _searchIndexClient.GetIndexAsync(indexName);
        }
        catch
        {
            isExcept = true;
        }
        
        if(!isExcept) return;
        
        var index = new SearchIndex(indexName)
        {
            Fields =
            [
                // key
                new SimpleField("chunk_id", SearchFieldDataType.String)
                {
                    IsKey = true,
                    IsFilterable = true,   // (선택) 키로 직접 필터할 일 많으면 추가
                    IsSortable = true      // (선택)
                },

                new SimpleField("doc_id", SearchFieldDataType.String)
                {
                    IsFilterable = true,
                    IsSortable = true
                },

                new SimpleField("source_file_type", SearchFieldDataType.String)
                {
                    IsFilterable = true
                },

                new SimpleField("source_file_path", SearchFieldDataType.String)
                {
                    IsFilterable = true
                },
                
                new SimpleField("source_file_name", SearchFieldDataType.String)
                {
                    IsFilterable = true
                },
                
                // 페이지는 숫자형이 맞습니다.
                new SimpleField("page", SearchFieldDataType.Int32)
                {
                    IsFilterable = true,
                    IsSortable = true
                },

                // 본문 텍스트
                new SearchField("content", SearchFieldDataType.String)
                {
                    IsSearchable = true,
                    AnalyzerName = LexicalAnalyzerName.KoLucene
                },

                // 벡터 필드 (임베딩 차원은 실제 사용 모델에 맞추세요: 1536 or 3072 등)
                new SearchField("content_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsHidden = true,
                    VectorSearchDimensions = 1536,
                    VectorSearchProfileName = "hnsw"
                }
            ],

            // Vector Search 설정
            VectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("hnsw-config")
                    {
                        Parameters = new HnswParameters
                        {
                            Metric = VectorSearchAlgorithmMetric.Cosine,
                            M = 16,
                            EfConstruction = 200,
                            EfSearch = 256
                        }
                    }
                },
                Profiles =
                {
                    new VectorSearchProfile(name: "hnsw", algorithmConfigurationName: "hnsw-config")
                }
            },

            // Semantic 설정: 존재하는 필드만 사용
            SemanticSearch = new SemanticSearch
            {
                DefaultConfigurationName = "sem-config",
                Configurations =
                {
                    new SemanticConfiguration(
                        name: "sem-config",
                        prioritizedFields: new SemanticPrioritizedFields
                        {
                            // TitleField는 선택 사항. 없으면 생략 가능.
                            ContentFields = { new SemanticField("content") }
                        })
                }
            }
        };

        await _searchIndexClient.CreateOrUpdateIndexAsync(index);
    }

    private List<SearchDocumentResult> _documentResults = new();

    private List<string> _questions = new();

    private readonly ContextShiftDetector _detector = new();
    private readonly LinkedList<string> _seenChunkIds = new();
    
    private static IReadOnlyCollection<string> ExtractEntities(string text)
    {
        // 아주 단순한 토크나이즈: 2자+ 토큰에서 불용어 제거
        var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "이게","그게","그리고","그러면","어떻게","에는","에서","으로","하는","하다","처럼","등","또","및" };
        return System.Text.RegularExpressions.Regex.Matches(text ?? "", @"[A-Za-z가-힣0-9_]{2,}")
            .Select(m => m.Value)
            .Where(w => !stop.Contains(w))
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .Take(20)
            .ToArray();
    }
    
    private static bool IsNewTopicHeuristic(string text)
    {
        // 한국어 트리거 휴리스틱
        string[] triggers = { "딴 얘기", "주제 바꿔", "그건 됐고", "전혀 다른", "이제 " };
        if (string.IsNullOrWhiteSpace(text)) return false;
        return triggers.Any(t => text.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    
    public async Task SearchAsync()
    {
        var system = """
                     역할: 너는 Azure AI Search 인덱스(doc_id, source_file_type[pdf|pptx|docx], source_file_path, source_file_name, page[int], content, content_vector)를 대상으로 한국어 사용자 질의를 QueryPlan(JSON 한 줄)로 정규화한다.

                     규칙:
                     - OData 문자열을 직접 만들지 말고, QueryPlan의 필드에만 채워라.
                     - fileTypes는 ["pdf","pptx","docx"]만 허용.
                     - 페이지 범위 표현(예: 5~12페이지)은 PageFrom=5, PageTo=12로.
                     - Select는 꼭 필요한 필드만 (기본: ["chunk_id","doc_id","page","content", "source_file_name"]).
                     - VectorFromText가 명시 안되면 Keyword를 벡터 텍스트로 써라.
                     - 하이브리드 요구(“벡터랑 키워드 같이”, “가장 비슷한 내용”)면 UseVector=true, UseKeyword=true.
                     - 단순 필터 탐색(“doc_id=…의 3~5페이지만 보자”)이면 UseKeyword=false, UseVector=false.
                     - TopK 기본 10, 많아야 50을 넘기지 마라.
                     
                     출력 규칙:
                     - 절대 ```json 과 같은 코드블록을 사용하지 마라.
                     - JSON 문자열만 한 줄로 출력하라.
                     - JSON 앞뒤에 설명, 주석, 공백, 텍스트를 붙이지 마라.
                     """;

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Enter question:");
            var userQuery = Console.ReadLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            
            var entities = ExtractEntities(userQuery);
            float[] detectEmbedding = (await _embeddingGenerator.GenerateAsync(userQuery)).Vector.ToArray(); // 판정용 임베딩
            
            var messages = new List<ChatMessage>()
            {
                new SystemChatMessage(system),
                new UserChatMessage(userQuery),
            };
            
            var resp = await _chatClient.GetResponseAsync<QueryPlan>(new List<Microsoft.Extensions.AI.ChatMessage>()
            {
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, system),
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userQuery)
            });
            var plan = resp.Result;
            var maxExclude = 100;
            plan.ExcludedChunkIds = plan.ExcludedChunkIds = _documentResults
                .Select(m => m.ChunkId)
                .TakeLast(maxExclude)
                .ToArray();
            
            float[]? embedding = null;

            if (plan.UseVector)
            {
                var embedText = !string.IsNullOrWhiteSpace(plan.VectorFromText)
                    ? plan.VectorFromText!
                    : (plan.Keyword ?? "");

                embedding = (await _embeddingGenerator.GenerateAsync(embedText)).Vector.ToArray();
            }
            
            var options = new SearchOptions
            {
                Size = Math.Clamp(plan.TopK, 1, 50),
                QueryType = SearchQueryType.Full
            };

            // Select 필드(없으면 기본)
            var selects = (plan.Select is { Length: > 0 })
                ? plan.Select
                : new[] { "chunk_id", "doc_id", "page", "content", "source_file_name" };

            foreach (var f in selects.Distinct())
                options.Select.Add(f);

            // Filter
            options.Filter = BuildFilter(plan);

            // Vector
            if (plan.UseVector && embedding is { Length: > 0 })
            {
                var knn = Math.Clamp(plan.TopK * 2, 10, 50);
                options.VectorSearch = new()
                {
                    Queries =
                    {
                        new VectorizedQuery(embedding)
                        {
                            Fields = { "content_vector" },
                            KNearestNeighborsCount = knn
                        }
                    }
                };
            }

            // Keyword
            string searchText;
            if (plan.UseKeyword && !string.IsNullOrWhiteSpace(plan.Keyword))
            {
                options.SearchFields.Add("content");
                searchText = plan.Keyword!;
            }
            else
            {
                // 키워드 미사용 시엔 "*"로 필터/벡터만 적용
                searchText = "*";
            }

            var searchResult = await _searchClient.SearchAsync<SearchDocument>(searchText, options);
            var results = new List<(SearchDocument Doc, double Score)>();
            await foreach (var hit in searchResult.Value.GetResultsAsync())
                results.Add((hit.Document, hit.Score ?? 0));

            var filtered = results
                .OrderByDescending(r => r.Score)
                .Take(Math.Clamp(plan.TopK, 1, 50))
                .ToList();
            // 여기서 filtered만 LLM 프롬프트에 사용            
            
            var currentHits = new List<SearchDocumentResult>();
            var currentChunkIds = new List<string>();
            
            foreach (var hit in filtered)
            {
                var doc = hit.Doc;
                
                if (!doc.TryGetValue("chunk_id", out var chunk_id)) continue;
                if (!doc.TryGetValue("content", out var content)) continue;
                if (!doc.TryGetValue("source_file_name", out var source_file_name)) continue;

                var cid = chunk_id.xValue<string>();
                var year = ExtractYear(source_file_name.xValue<string>());
                currentHits.Add(new SearchDocumentResult
                {
                    ChunkId = cid,
                    Year = year,
                    Content = content.ToString(),
                    SourceFileName = source_file_name.ToString()
                });
                currentChunkIds.Add(cid);
            }
            
            var signals = _detector.ComputeSignals(
                curEmbedding: detectEmbedding,
                curEntities: entities,
                intentIsNew: IsNewTopicHeuristic(userQuery),
                curPlan: plan,
                curChunkIds: currentChunkIds
            );
            
            bool shifted = _detector.IsShift(signals);
            if (shifted)
            {
                _documentResults.Clear();
                _asks.Clear();
                _questions.Clear();
                _seenChunkIds.Clear(); // 선택
            }
            
            foreach (var h in currentHits)
            {
                if (_documentResults.Any(x => x.ChunkId == h.ChunkId)) continue;
                _documentResults.Add(h);

                // 선택: 전역 seenChunkIds 유지(필터 길이 제한)
                _seenChunkIds.AddLast(h.ChunkId);
                while (_seenChunkIds.Count > 200) _seenChunkIds.RemoveFirst();
            }
            
            _detector.Commit(detectEmbedding, entities, plan, currentChunkIds, shifted);
            
            Console.WriteLine($"EmbSim={signals.EmbSimToCenter:F3}, EntJ={signals.EntityJaccard:F3}, " +
                              $"IntentNew={signals.IntentIsNew}, Chg={signals.ChangedFilterRatio:F2}, " +
                              $"Overlap={signals.ResultOverlap:F2}, SHIFT={shifted}");

            try
            {
                var ask = await AskFromGpt(_documentResults, userQuery, shifted);
                if (ask.Contains("모릅니다.") || ask.Contains("근거가 부족") || ask.Contains("답변하기 어렵"))
                {
                    continue;
                }
                _asks.Add(ask);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    private List<string> _asks = new();

    private async Task<string> AskFromGpt(IEnumerable<SearchDocumentResult> items, string question, bool isShift = true)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        var system = """
                     역할: 너는 주어진 참조(<REF></REF>)만을 근거로 한국어 답변을 작성하는 엔진이다.
                     
                     규칙:
                     1) **<REF> 내용만 근거로 사용:** 외부 지식, 기억, 또는 과거 Assistant 답변은 참고용일 뿐이며, 최우선 근거는 항상 <REF>로 지정된 내용이다.
                     2) **근거 부족 시 답변 불가:** <REF>가 비었거나 질문과 직접 관련한 근거가 없으면, “근거가 없어 답변할 수 없습니다.”라고만 작성하며, 추정은 금지한다.
                     3) **최신 연도 우선:** 직책, 조직 등 같은 사항에 대해 서로 다른 내용이 있을 경우:
                        - SourceFileName에서 연도(YYYY)를 추출하여 최신 연도 문서를 우선한다.
                        - 연도가 없는 문서는 연도 있는 문서보다 항상 우선순위에서 밀린다.
                     4) **충돌한 경우:** 서로 상충하는 내용이 있다면 최신 연도 근거만을 답하며, 불일치를 한 줄로 명시한다. (예: "과거 문서에는 XXX로 표기").
                     5) **간결하고 공손한 답변:** 답변은 간결하고 정확하며 공손하게 작성한다. 불필요한 수사나 장황한 표현은 금지한다.
                     6) **정보 형식 유지:** 숫자, 코드, 단위, 날짜는 원문 그대로 유지하며, 날짜는 항상 `YYYY-MM-DD` 형식으로 표기한다.
                     7) **절차나 단계 제시:** 답이 절차나 단계라면 번호가 매겨진 리스트 형태로 제시한다.
                     8) **질문 범위 준수:** 질문 범위를 벗어난 내용은 포함하지 않는다.
                     9) **교차검증:** Citations는 <REF>에 실제로 존재하는 파일명과 페이지인지 확인한다.
                     
                     # Steps
                     
                     1. **REF 확인하기:**
                        - <REF> 내용이 있는지 확인한다.
                        - 관련된 근거가 없다면 즉시 "근거가 없어 답변할 수 없습니다."라고 작성한다.
                     2. **최신 연도 선별:**
                        - 같은 속성을 다루는 서로 다른 파일이 있으면 연도를 확인하여 최신 데이터 기반으로 작성한다.
                        - 연도가 없는 문서는 연도 있는 문서보다 항상 우선순위에서 밀린다.
                        - 서로 충돌하는 정보가 있을 경우 최신 연도 데이터만을 사용하되, 불일치를 명시한다.
                     3. **답변 작성:**
                        - 근거가 확인되었으면 간결하고 공손하게 답변한다.
                        - 필요한 경우 정보는 절차나 순서에 따라 번호로 나열한다.
                        - 숫자, 코드, 단위, 날짜는 원문 그대로 유지한다.
                     4. **Citations 검증:**
                        - 답변에 포함된 Citation이 실제 <REF> 안에 존재하는 파일명과 페이지인지 교차검증한다.
                        - 존재하지 않는 Citation은 절대 포함하지 않는다.
                     
                     # Output Format
                     
                     - **일반 답변:** 
                       - 간결·정확한 문장으로 작성. 
                       - 불충분한 근거로 답할 수 없을 경우: “근거가 없어 답변할 수 없습니다.”
                     - **절차/단계:** 
                       - 단계별로 번호 매긴 리스트 사용 (1, 2, 3...).
                     - **Citations 포함 시:** 
                       - (파일명, 페이지) 형식으로 표기한다.
                     
                     # Examples
                     
                     ### Example 1
                     **Input Prompt:**
                     <REF>
                     File1.txt (2021)
                     1. 페이지 2:
                        - "A사는 2021년 기준 B기관 소속이다."
                     2. 페이지 3:
                        - "C사는 B기관에 소속되지 않는다."
                        
                     File2.txt (2019)
                     1. 페이지 1:
                        - "A사는 B기관의 세션 파트너이다."
                     </REF>
                     "‘A사’는 어떤 기관에 소속되어 있는가?"
                     
                     **Expected Output:**
                     A사는 B기관 소속입니다. (File1.txt, 페이지 2)
                     과거 문서에는 B기관의 세션 파트너로 표기되었습니다. (File2.txt, 페이지 1)
                     
                     ---
                     
                     ### Example 2
                     **Input Prompt:**
                     <REF>
                     File1.txt (2020)
                     페이지 1:
                     - "X공정은 3단계로 이루어집니다: 1) 원료 투입, 2) 혼합 작용, 3) 최종 패킹."
                     </REF>
                     "X공정에 대해 설명해 주세요."
                     
                     **Expected Output:**
                     X공정은 총 3단계로 이루어집니다:
                     1. 원료 투입
                     2. 혼합 작용
                     3. 최종 패킹
                     (File1.txt, 페이지 1)
                     
                     ---
                     
                     ### Example 3
                     **Input Prompt:**
                     <REF></REF>
                     "Z사는 어디에 본사를 두고 있나요?"
                     
                     **Expected Output:**
                     근거가 없어 답변할 수 없습니다.
                     
                     # Notes
                     
                     - 만일 명확한 Citations 형식이 준수되지 않으면 전체 답변 실패로 간주.
                     - 질문과 관련된 모든 데이터를 포괄하되, 불필요하거나 벗어난 추가 정보는 피한다.
                     - 연도가 없는 문서를 판단 시 최우선 순위가 될 수 없음을 명심.
                     """;
        // var messages = new List<ChatMessage>()
        // {
        //     new SystemChatMessage(system),
        //     new UserChatMessage($"<REF>\n{reference.xValue<string>(string.Empty)}\n</REF>"),
        //     new UserChatMessage(question)
        // };
        // messages.AddRange(_asks.Select(m => new AssistantChatMessage(m)));

        var opts = new JsonSerializerOptions {
            // 한글 \uXXXX 방지
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, 
            WriteIndented = false
        };
        var reference = JsonSerializer.Serialize(items, opts);
        var messages = new List<Microsoft.Extensions.AI.ChatMessage>()
        {
            new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, system),
            new Microsoft.Extensions.AI.ChatMessage(ChatRole.User,
                $"<REF>{reference}</REF>"),
            new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, question)
        };
        // if (!isShift)
        // {
        //     var previousChats = _asks.TakeLast(10)
        //         .Select(m => new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, m));
        //     messages.AddRange(previousChats);    
        // }

        var options = new ChatOptions()
        {
            MaxOutputTokens = 6553,
            Temperature = 0f,
            TopP = 0.1f
        };
        var resp = await _chatClient.GetResponseAsync<AnswerResult>(messages, options);
        Console.WriteLine($"답변: {resp.Result.Answer}");
        if (resp.Result.Citations.xIsNotEmpty())
        {
            Console.WriteLine($"근거: {resp.Result.Citations.Select(m => $"{m.File.xValue<string>(string.Empty)}:{m.Page.xValue<int>(0)}").xJoin()}");
        }
        return JsonSerializer.Serialize(resp.Result, opts);
    }
    
    int? ExtractYear(string file)
    {
        var m = System.Text.RegularExpressions.Regex.Match(file ?? "", @"\b(20\d{2})\b");
        return m.Success ? int.Parse(m.Value) : (int?)null;
    }

    static string BuildFilter(QueryPlan p)
    {
        static string Quote(string s) => $"'{s.Replace("'", "''")}'";
        var conds = new List<string>();

        if (!string.IsNullOrWhiteSpace(p.DocId))
            conds.Add($"doc_id eq {Quote(p.DocId!)}");

        if (!string.IsNullOrWhiteSpace(p.SourcePathEquals))
            conds.Add($"source_file_path eq {Quote(p.SourcePathEquals!)}");

        if (p.FileTypes != null && p.FileTypes.Length > 0)
        {
            // 화이트리스트(normalize)
            var allow = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "pdf","pptx","docx" };
            var filtered = p.FileTypes
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => allow.Contains(t))
                .ToArray();

            if (filtered.Length > 0)
            {
                var joined = string.Join(",", filtered.Select(x => x.Replace("'", "''")));
                conds.Add($"search.in(source_file_type, '{joined}', ',')");
            }
        }

        if (p.PageFrom.HasValue && p.PageTo.HasValue)
            conds.Add($"page ge {p.PageFrom.Value} and page le {p.PageTo.Value}");
        else if (p.PageFrom.HasValue)
            conds.Add($"page ge {p.PageFrom.Value}");
        else if (p.PageTo.HasValue)
            conds.Add($"page le {p.PageTo.Value}");

        if (p.ExcludedChunkIds.xIsNotEmpty())
        {
            var escaped = p.ExcludedChunkIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Replace("'", "''"));
            var joined = string.Join(",", escaped);
            conds.Add($"not search.in(chunk_id, '{joined}', ',')");
        }

        return conds.Count == 0 ? null : string.Join(" and ", conds);
    }

    private static byte[] StreamToByteArray(Stream input)
    {
        using MemoryStream ms = new MemoryStream();
        input.CopyTo(ms);
        return ms.ToArray();
    }
}

public sealed class SearchDocumentResult
{
    public string ChunkId { get; set; }
    public string Content { get; set; }
    public string SourceFileName { get; set; }
    public int? Year { get; set; }
}

public sealed class QueryPlan
{
    // 검색 모드
    public bool UseVector { get; set; } = false;     // 벡터 검색 사용할지
    public bool UseKeyword { get; set; } = true;     // 키워드(BM25) 사용할지
    public int TopK { get; set; } = 10;              // KNN, 또는 Size

    // 키워드
    public string Keyword { get; set; }             // 사용자 핵심 키워드(없으면 null)

    // 정규화된 필터 파라미터 (여기서만 받아서 OData는 코드가 생성)
    public string DocId { get; set; }
    public string[] FileTypes { get; set; }         // 예: ["pdf","pptx","docx"]
    public string SourcePathEquals { get; set; }    // 전체 경로 정확히 일치
    public int? PageFrom { get; set; }               // 1-base
    public int? PageTo { get; set; }

    // 반환 필드
    public string[] Select { get; set; }            // 예: ["chunk_id","doc_id","page","content"]
    
    // 벡터 관련
    public string VectorFromText { get; set; }      // 임베딩 생성에 사용할 텍스트(없으면 Keyword 사용)
    public string[] ExcludedChunkIds { get; set; }
}

public class DocChunk
{
    /// <summary>
    /// docId_page_seq
    /// </summary>
    [SimpleField(IsKey = true)]
    public string ChunkId { get; set; }

    /// <summary>
    /// 소스 문서 ID/해시
    /// </summary>
    [SimpleField(IsFilterable = true, IsSortable = true)]
    public string DocId { get; set; }

    /// <summary>
    /// pdf|docx|pptx
    /// </summary>
    [SimpleField(IsFilterable = true)]
    public string SourceFileType { get; set; }
    
    [SimpleField(IsFilterable = true)]
    public string SourceFilePath { get; set; }
    
    [SimpleField(IsFilterable = true)]
    public string SourceFileName { get; set; }

    /// <summary>
    /// 1-based
    /// </summary>
    [SimpleField(IsFilterable = true, IsSortable = true)]
    public int Page { get; set; }

    /// <summary>
    /// 문단 텍스트(슬라이드 본문 포함)
    /// </summary>
    [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.KoLucene)]
    public string Content { get; set; }
    
    /// <summary>
    /// 벡터(임베딩)
    /// </summary>
    [VectorSearchField(VectorSearchDimensions = 1536, VectorSearchProfileName = "hnsw")]
    public float[] ContentVector { get; set; } 
}

public class AnswerResult
{
    public string Answer { get; set; }
    public PageCitation[] Citations { get; set; } = [];
}

public class PageCitation
{
    public string File { get; set; } = ""; // source_file_name (또는 DocId 권장)
    public int? Page { get; set; }          // 1-based page
}