using System.Diagnostics;
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
    }

    public async Task UploadAsync()
    {
        var files = _configuration["OCR_SAMPLE_FILES"].xValue<string[]>();
        var list = new List<DocChunk>();
        foreach (var filePath in files)
        {
            var sw = Stopwatch.StartNew();
            await using var stream = File.OpenRead(filePath);
            var bytes = StreamToByteArray(stream);
            var operation = await _documentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", new BinaryData(bytes));
            var resp = await operation.WaitForCompletionAsync();
            var result = resp.Value;
            Console.WriteLine($"Pages: {result.Pages.Count}");
        
            int seq = 0;
            foreach (var documentPage in result.Pages)
            {
                var docId = filePath.xGetFileName().xGetHashCode();
                var paragraphs = DiExtractors.ExtractParagraphs(result, documentPage.PageNumber);
                var chunks = Chunker.ChunkByBoundary(paragraphs, 800, 1200, 200);

                foreach (var text in chunks)
                {
                    var doc = new DocChunk {
                        ChunkId = $"{docId}_{documentPage.PageNumber:D4}_{seq++:D3}",
                        DocId = docId,
                        SourceFileType = "pdf",
                        SourceFilePath = filePath,
                        SourceFileName = filePath.xGetFileName(),
                        Page = documentPage.PageNumber,
                        Content = text,
                        ContentVector = (await _embeddingGenerator.GenerateAsync(text)).Vector.ToArray()
                    };
                    list.Add(doc);
                }

                seq = 0;
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
        if(_searchIndexClient.GetIndexes().Any(m => m.Name == _configuration["AZURE_AI_SEARCH_INDEX_NAME"]))
            return;
        
        var index = new SearchIndex(_configuration["AZURE_AI_SEARCH_INDEX_NAME"])
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
                    IsSearchable = true,
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
            plan.ExcludedChunkIds = plan.ExcludedChunkIds = _documentResults
                .Select(m => m.ChunkId)
                .TakeLast(200)
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
                options.VectorSearch = new()
                {
                    Queries =
                    {
                        new VectorizedQuery(embedding)
                        {
                            Fields = { "content_vector" },
                            KNearestNeighborsCount = plan.TopK
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
            
            var currentHits = new List<SearchDocumentResult>();
            var currentChunkIds = new List<string>();
            
            await foreach (var hit in searchResult.Value.GetResultsAsync())
            {
                var doc = hit.Document;
                
                if (!doc.TryGetValue("chunk_id", out var chunk_id)) continue;
                doc.TryGetValue("content", out var content);
                doc.TryGetValue("source_file_name", out var source_file_name);

                var cid = chunk_id.xValue<string>();
                currentHits.Add(new SearchDocumentResult
                {
                    ChunkId = cid,
                    Content = content.xValue<string>(),
                    SourceFileName = source_file_name.xValue<string>()
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

            var ask = await AskFromGpt(_documentResults.xSerialize(), userQuery, shifted);
            if(ask.Contains("모릅") || ask.Contains("근거가 부족") || ask.Contains("답변하기 어렵")) return;
            _asks.Add(ask);
        }
    }

    private List<string> _asks = new();

    private async Task<string> AskFromGpt(string reference, string question, bool isShift = true)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        var system = """
                     규칙:
                     1) <REF></REF> 안의 내용만 근거로 답한다.
                     2) <REF></REF> 안의 내용중 SourceFileName을 참고하여 년도명이 있을 경우 최신 기준으로 답한다.
                     3) <REF></REF>의 출처(파일명)를 밝힌다.
                     4) 근거가 부족하면 모른다고 말한다.
                     5) 답변은 한국어로 간결·정확·공손하게 작성한다.
                     6) 원문을 장황하게 복사/붙여넣기하지 않는다.
                     """;
        // var messages = new List<ChatMessage>()
        // {
        //     new SystemChatMessage(system),
        //     new UserChatMessage($"<REF>\n{reference.xValue<string>(string.Empty)}\n</REF>"),
        //     new UserChatMessage(question)
        // };
        // messages.AddRange(_asks.Select(m => new AssistantChatMessage(m)));

        var messages = new List<Microsoft.Extensions.AI.ChatMessage>()
        {
            new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, system),
            new Microsoft.Extensions.AI.ChatMessage(ChatRole.User,
                $"<REF>\n{reference.xValue<string>(string.Empty)}\n</REF>"),
            new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, question)
        };
        if (!isShift)
        {
            messages.AddRange(_asks.Select(m => new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, m)));    
        }

        var resp = await _chatClient.GetResponseAsync(messages);
        
        Console.WriteLine(resp.Messages.xJoin());
        return resp.Messages.xJoin();
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
}

public sealed class QueryPlan
{
    // 검색 모드
    public bool UseVector { get; set; } = false;     // 벡터 검색 사용할지
    public bool UseKeyword { get; set; } = true;     // 키워드(BM25) 사용할지
    public int TopK { get; set; } = 10;              // KNN, 또는 Size

    // 키워드
    public string? Keyword { get; set; }             // 사용자 핵심 키워드(없으면 null)

    // 정규화된 필터 파라미터 (여기서만 받아서 OData는 코드가 생성)
    public string? DocId { get; set; }
    public string[]? FileTypes { get; set; }         // 예: ["pdf","pptx","docx"]
    public string? SourcePathEquals { get; set; }    // 전체 경로 정확히 일치
    public int? PageFrom { get; set; }               // 1-base
    public int? PageTo { get; set; }

    // 반환 필드
    public string[]? Select { get; set; }            // 예: ["chunk_id","doc_id","page","content"]
    
    // 벡터 관련
    public string? VectorFromText { get; set; }      // 임베딩 생성에 사용할 텍스트(없으면 Keyword 사용)
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