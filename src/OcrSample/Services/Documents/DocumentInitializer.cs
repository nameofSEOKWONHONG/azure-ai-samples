using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using eXtensionSharp;

namespace OcrSample.Services.Documents;

public interface IDocumentInitializer
{
    Task InitializeAsync();
}

public class DocumentInitializer : IDocumentInitializer
{
    private readonly SearchIndexClient _searchIndexClient;

    public DocumentInitializer(SearchIndexClient searchIndexClient)
    {
        _searchIndexClient = searchIndexClient;
    }

    public async Task InitializeAsync()
    {
        var selectedIndex = await _searchIndexClient.GetIndexAsync(AiFeatureConst.DOCUMENT_INDEX_NAME);
        if(selectedIndex.xIsNotEmpty()) return;
        
        var index = new SearchIndex(AiFeatureConst.DOCUMENT_INDEX_NAME)
        {
            Fields =
            [
                // key
                new SimpleField("chunk_id", SearchFieldDataType.String)
                {
                    IsKey = true,
                },

                new SimpleField("doc_id", SearchFieldDataType.String)
                {
                    IsFilterable = true,
                    IsSortable = true,
                },
                new SimpleField("source_file_type", SearchFieldDataType.String)
                {
                    IsFilterable = true
                },           
                new SimpleField("source_file_path", SearchFieldDataType.String)
                {
                    IsFilterable = true,
                },
                
                new SimpleField("page", SearchFieldDataType.String)
                {
                    IsFilterable = true,
                    IsSortable = true,
                },                   
                
                new SearchField("content", SearchFieldDataType.String)
                {
                    IsSearchable = true,
                    AnalyzerName = LexicalAnalyzerName.KoLucene
                },
                
                new SearchField("content_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = 1536,
                    VectorSearchProfileName = "hnsw"
                },
            ],

            // 2) VectorSearch: 알고리즘 + 프로필 (앱에서 만든 임베딩을 넣는 방식)
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
                            EfSearch = 64
                        }
                    }
                },
                Profiles =
                {
                    // 필드에서 참조할 프로필 이름
                    new VectorSearchProfile("hnsw", "hnsw-config")
                }
            },

            // 3) (선택) Semantic 설정
            SemanticSearch = new SemanticSearch()
            {
                DefaultConfigurationName = "sem-config",
                Configurations =
                {
                    new SemanticConfiguration(
                        name: "sem-config",
                        prioritizedFields: new SemanticPrioritizedFields()
                        {
                            TitleField = new SemanticField("title"),
                            ContentFields = { new SemanticField("chunk") }
                        })
                }
            }
        };

        await _searchIndexClient.CreateOrUpdateIndexAsync(index);      
    }
}