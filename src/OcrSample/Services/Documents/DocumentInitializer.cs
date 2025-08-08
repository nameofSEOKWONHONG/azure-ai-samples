using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using eXtensionSharp;
using Microsoft.Extensions.DependencyInjection;

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
        await InitializePdfIndexAsync();
        await InitializePptIndexAsync();
        await InitializeDocIndexAsync();
    }

    private async Task InitializePdfIndexAsync()
    {
        var indexName = "doc-pdf-index";
        var selectedIndex = await _searchIndexClient.GetIndexAsync(indexName);
        if(selectedIndex.xIsNotEmpty()) return;
        
        var index = new SearchIndex(indexName)
        {
            Fields =
            [
                // key
                new SearchField("chunk_id", SearchFieldDataType.String)
                {
                    IsKey = true,
                    IsSearchable = true,
                    IsSortable = true,
                    AnalyzerName = LexicalAnalyzerName.Keyword
                },

                new SearchField("parent_id", SearchFieldDataType.String)
                {
                    IsFilterable = true
                },

                new SearchField("title", SearchFieldDataType.String)
                {
                    IsSearchable = true,
                    AnalyzerName = LexicalAnalyzerName.KoMicrosoft
                },

                new SearchField("chunk", SearchFieldDataType.String)
                {
                    IsSearchable = true,
                    AnalyzerName = LexicalAnalyzerName.KoMicrosoft
                },

                // 벡터 필드: 앱에서 생성한 임베딩 업로드
                new SearchField("text_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = 3072,
                    VectorSearchProfileName = "vprofile"
                },

                new SearchField("sourcePath", SearchFieldDataType.String)
                {
                    IsFilterable = true
                }
            ],

            // 2) VectorSearch: 알고리즘 + 프로필 (앱에서 만든 임베딩을 넣는 방식)
            VectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("hnsw-cosine")
                    {
                        Parameters = new HnswParameters
                        {
                            Metric = VectorSearchAlgorithmMetric.Cosine,
                            M = 16,
                            EfConstruction = 400,
                            EfSearch = 256
                        }
                    }
                },
                Profiles =
                {
                    // 필드에서 참조할 프로필 이름
                    new VectorSearchProfile("vprofile", "hnsw-cosine")
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

    private async Task InitializePptIndexAsync()
    {
        var indexName = "doc-ppt-index";
        var selectedIndex = await _searchIndexClient.GetIndexAsync(indexName);
        if(selectedIndex.xIsNotEmpty()) return;
        
        var index = new SearchIndex(indexName)
        {
            Fields = [
                new SearchField("chunk_id", SearchFieldDataType.String)
                {
                    IsKey = true, IsSearchable = true, IsSortable = true,
                    AnalyzerName = LexicalAnalyzerName.Keyword
                },
                new SearchField("parent_id", SearchFieldDataType.String)
                {
                    IsFilterable = true
                },
                new SearchField("title", SearchFieldDataType.String)
                {
                    IsSearchable = true, AnalyzerName = LexicalAnalyzerName.KoMicrosoft
                },
                new SearchField("chunk", SearchFieldDataType.String)
                {
                    IsSearchable = true, AnalyzerName = LexicalAnalyzerName.KoMicrosoft
                },
                new SearchField("text_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true, VectorSearchDimensions = 3072,
                    VectorSearchProfileName = "vprofile"
                },
                new SearchField("sourcePath", SearchFieldDataType.String)
                {
                    IsFilterable = true
                }
            ],
            VectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("hnsw-cosine")
                    {
                        Parameters = new HnswParameters
                        {
                            Metric = VectorSearchAlgorithmMetric.Cosine,
                            M = 16,
                            EfConstruction = 400,
                            EfSearch = 256
                        }
                    }
                },
                Profiles =
                {
                    new VectorSearchProfile("vprofile", "hnsw-cosine")
                }
            },
            SemanticSearch = new SemanticSearch
            {
                DefaultConfigurationName = "sem-config",
                Configurations =
                {
                    new SemanticConfiguration(
                        "sem-config",
                        new SemanticPrioritizedFields
                        {
                            TitleField = new SemanticField("title"),
                            ContentFields = { new SemanticField("chunk") }
                        })
                }
            }
        };

        await _searchIndexClient.CreateIndexAsync(index);
    }

    private async Task InitializeDocIndexAsync()
    {
        var indexName = "doc-doc-index";
        var selectedIndex = await _searchIndexClient.GetIndexAsync(indexName);
        if(selectedIndex.xIsNotEmpty()) return;
        
        var index = new SearchIndex(indexName)
        {
            Fields = [
                // 키 (청크 단위)
                new SearchField("chunk_id", SearchFieldDataType.String)
                {
                    IsKey = true, IsSearchable = true, IsSortable = true,
                    AnalyzerName = LexicalAnalyzerName.Keyword
                },
                // 부모(문서 단위)
                new SearchField("parent_id", SearchFieldDataType.String)
                {
                    IsFilterable = true
                },
                // 제목(파일명/문서제목)
                new SearchField("title", SearchFieldDataType.String)
                {
                    IsSearchable = true, AnalyzerName = LexicalAnalyzerName.KoMicrosoft
                },
                // 본문 청크
                new SearchField("chunk", SearchFieldDataType.String)
                {
                    IsSearchable = true, AnalyzerName = LexicalAnalyzerName.KoMicrosoft
                },
                // 임베딩 벡터(1536차원)
                new SearchField("text_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = 3072,
                    VectorSearchProfileName = "vprofile"
                },
                // 원본 경로/URL
                new SearchField("sourcePath", SearchFieldDataType.String)
                {
                    IsFilterable = true
                }
            ],
            VectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("hnsw-cosine")
                    {
                        Parameters = new HnswParameters
                        {
                            Metric = VectorSearchAlgorithmMetric.Cosine,
                            M = 16,
                            EfConstruction = 400,
                            EfSearch = 256
                        }
                    }
                },
                Profiles =
                {
                    new VectorSearchProfile("vprofile", "hnsw-cosine")
                }
            },
            SemanticSearch = new SemanticSearch
            {
                DefaultConfigurationName = "sem-config",
                Configurations =
                {
                    new SemanticConfiguration(
                        "sem-config",
                        new SemanticPrioritizedFields
                        {
                            TitleField = new SemanticField("title"),
                            ContentFields = { new SemanticField("chunk") }
                        })
                }
            }
        };

        await _searchIndexClient.CreateIndexAsync(index);
    }

    private async Task InitializeDataAsync()
    {
        var pdfFiles = Directory.GetFiles("E:\\OCR 학습 데이터", "*.pdf");
        foreach (var file in pdfFiles)
        {
               
        }
    }
}