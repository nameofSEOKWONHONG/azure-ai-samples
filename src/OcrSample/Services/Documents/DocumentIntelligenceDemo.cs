using System.Text.Json;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using eXtensionSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;

namespace OcrSample.Services.Documents;

public class DocumentIntelligenceDemo
{
    private readonly SearchIndexClient _searchIndexClient;
    private readonly IConfiguration _configuration;
    private readonly ITextEmbeddingService _textEmbeddingService;
    private readonly SearchClient _searchClient;
    private readonly AzureOpenAIClient _azureOpenAiClient;

    public DocumentIntelligenceDemo(SearchIndexClient searchIndexClient, IConfiguration configuration, ITextEmbeddingService textEmbeddingService,
        [FromKeyedServices(AiFeatureConst.DOCUMENT)]SearchClient searchClient,
        AzureOpenAIClient azureOpenAiClient)
    {
        _searchIndexClient = searchIndexClient;
        _configuration = configuration;
        _textEmbeddingService = textEmbeddingService;
        _searchClient = searchClient;
        _azureOpenAiClient = azureOpenAiClient;
    }

    public async Task UploadAsync()
    {
        var client = new DocumentIntelligenceClient(new Uri(_configuration["AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT"].xValue<string>()),
            new AzureKeyCredential(_configuration["AZURE_DOCUMENT_INTELLIGENCE_KEY"].xValue<string>()));

        var filePath = "E:\\OCR 학습 데이터\\(설명용) 2025 고우아이티 Band 체계 개편.pdf";
        await using var stream = File.OpenRead(filePath);
        var bytes = StreamToByteArray(stream);
        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", new BinaryData(bytes));
        var resp = await operation.WaitForCompletionAsync();
        var result = resp.Value;
        Console.WriteLine($"Pages: {result.Pages.Count}");

        // for (var p = 1; p <= result.Pages.Count; p++)
        // {
        //     // (1) 해당 페이지 문단
        //     var pageParas = result.Paragraphs?
        //         .Where(par => par.BoundingRegions?.Any(br => br.PageNumber == p) == true)
        //         .Select(par => new {
        //             Role = par.Role,                       // title, sectionHeading, pageHeader 등
        //             Text = par.Content
        //         })
        //         .ToList() ?? new();
        //
        //     // (2) 해당 페이지 표
        //     var pageTables = result.Tables?
        //         .Where(t => t.BoundingRegions?.Any(br => br.PageNumber == p) == true)
        //         .Select(t => new {
        //             Rows = t.RowCount,
        //             Cols = t.ColumnCount,
        //             Cells = t.Cells.Select(c => new {
        //                 r = c.RowIndex, c = c.ColumnIndex, text = c.Content
        //             })
        //         })
        //         .ToList() ?? new();
        //
        //     // (3) 페이지 메타(크기/단위 등)
        //     var pageMeta = result.Pages[p - 1];
        //     var export = new {
        //         Page = p,
        //         Size = new { pageMeta.Width, pageMeta.Height, pageMeta.Unit },
        //         Headings = pageParas.Where(x => x.Role == "title" || x.Role == "sectionHeading" || x.Role == "pageHeader")
        //             .Select(x => x.Text).ToArray(),
        //         Paragraphs = pageParas.Where(x => (
        //                 (x.Role == string.Empty || x.Role == null) ||
        //                 x.Role == "pageFooter") == false)
        //             .Select(x => x.Text).ToArray(),
        //         Tables = pageTables
        //     };
        // }
        
        int seq = 0;
        var list = new List<DocChunk>();
        foreach (var documentPage in result.Pages)
        {
            var docId = Guid.CreateVersion7().ToString();
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
                    ContentVector = await _textEmbeddingService.GetEmbeddedText(text)
                };
                list.Add(doc);
            }

            seq = 0;
        }

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
    }

    public async Task CreateIndexAsync()
    {
        if(_searchIndexClient.GetIndexes().Any(m => m.Name == AiFeatureConst.DOCUMENT_INDEX_NAME))
            return;
        
        var index = new SearchIndex(AiFeatureConst.DOCUMENT_INDEX_NAME)
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

    public async Task SearchAsync()
    {
        var chat = _azureOpenAiClient.GetChatClient(_configuration["AZURE_OPENAI_GPT_NAME"]);
        var system = """
                     너는 Azure AI Search 인덱스(doc_id, source_file_type[pdf|pptx|docx], source_file_path, source_file_path_name, page[int], content, content_vector)를 대상으로
                     한국어 사용자 질의를 QueryPlan(JSON 한 줄)로 정규화한다.

                     규칙:
                     - OData 문자열을 직접 만들지 말고, QueryPlan의 필드에만 채워라.
                     - fileTypes는 ["pdf","pptx","docx"]만 허용.
                     - 페이지 범위 표현(예: 5~12페이지)은 PageFrom=5, PageTo=12로.
                     - Select는 꼭 필요한 필드만 (기본: ["chunk_id","doc_id","page","content"]).
                     - VectorFromText가 명시 안되면 Keyword를 벡터 텍스트로 써라.
                     - 하이브리드 요구(“벡터랑 키워드 같이”, “가장 비슷한 내용”)면 UseVector=true, UseKeyword=true.
                     - 단순 필터 탐색(“doc_id=…의 3~5페이지만 보자”)이면 UseKeyword=false, UseVector=false.
                     - TopK 기본 10, 많아야 50을 넘기지 마라.
                     
                     출력 규칙:
                     - 절대 ```json 과 같은 코드블록을 사용하지 마라.
                     - JSON 문자열만 한 줄로 출력하라.
                     - JSON 앞뒤에 설명, 주석, 공백, 텍스트를 붙이지 마라.
                     """;
        
        string userQuery = "직급 체계에 대해 알려줘.";
        
        var resp = await chat.CompleteChatAsync([
            new SystemChatMessage(system),
            new UserChatMessage(userQuery),
        ]);
        
        var jsonLine = resp.Value.Content[0].Text.Trim();
        var plan = JsonSerializer.Deserialize<QueryPlan>(jsonLine, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new QueryPlan();
        
        float[]? embedding = null;

        if (plan.UseVector)
        {
            var embedText = !string.IsNullOrWhiteSpace(plan.VectorFromText)
                ? plan.VectorFromText!
                : (plan.Keyword ?? "");

            embedding = await _textEmbeddingService.GetEmbeddedText(embedText);
        }


        var options = new SearchOptions
        {
            Size = Math.Clamp(plan.TopK, 1, 50),
            QueryType = SearchQueryType.Full
        };

        // Select 필드(없으면 기본)
        var selects = (plan.Select is { Length: > 0 })
            ? plan.Select
            : new[] { "chunk_id", "doc_id", "page", "content" };

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
        await foreach (var hit in searchResult.Value.GetResultsAsync())
        {
            var doc = hit.Document;
            
            
        }
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

        return conds.Count == 0 ? null : string.Join(" and ", conds);
    }

    private static byte[] StreamToByteArray(Stream input)
    {
        using MemoryStream ms = new MemoryStream();
        input.CopyTo(ms);
        return ms.ToArray();
    }
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