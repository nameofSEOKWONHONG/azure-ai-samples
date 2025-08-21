using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Session;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Document.AiSearch;

public interface ICreateDocumentIndexService : IDiaExecuteServiceBase<string, bool>;

/// <summary>
/// 문서 인덱스 생성
/// </summary>
public class CreateDocumentIndexService: DiaExecuteServiceBase<CreateDocumentIndexService, DiaDbContext, string, bool>, ICreateDocumentIndexService
{
    private readonly SearchIndexClient _searchIndexClient;

    public CreateDocumentIndexService(ILogger<CreateDocumentIndexService> logger, IDiaSessionContext session, DiaDbContext dbContext,
        SearchIndexClient searchIndexClient) : base(logger, session, dbContext)
    {
        _searchIndexClient = searchIndexClient;
    }

    public override async Task<bool> ExecuteAsync(string request)
    {
        bool isExcept = false;
        try
        {
            await _searchIndexClient.GetIndexAsync(request);
        }
        catch (Exception e)
        {
            this.logger.LogError(e, "{name} Error: {message}", nameof(CreateDocumentIndexService), e.Message);
            isExcept = true;
        }

        if (!isExcept) return false;
        
        var index = new SearchIndex(request)
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

        return true;
    }
}