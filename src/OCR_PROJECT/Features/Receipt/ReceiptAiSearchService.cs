using System.Security.Cryptography;
using System.Text;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Document.Intelligence.Agent.Features.Receipt.Models;
using Document.Intelligence.Agent.Infrastructure.Session;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Receipt;

public interface IReceiptAiSearchIndexInitializeService
{
    Task InitializeIndexAsync();
}

public interface IReceiptAiSearchService
{
    Task<Embedding<float>> UploadReceiptDocument(ReceiptExtract extract);
}

public class ReceiptAiSearchService: ServiceBase<ReceiptAiSearchService>, IReceiptAiSearchService, IReceiptAiSearchIndexInitializeService
{
    private readonly SearchClient _searchClient;
    private readonly SearchIndexClient _searchIndexClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public ReceiptAiSearchService(ILogger<ReceiptAiSearchService> logger, IDiaSessionContext sessionContext,
        [FromKeyedServices(INDEX_CONST.RECEIPT_INDEX)] SearchClient searchClient,
        SearchIndexClient searchIndexClient,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) : base(logger, sessionContext)
    {
        _searchClient = searchClient;
        _searchIndexClient = searchIndexClient;
        _embeddingGenerator = embeddingGenerator;
    }

    /// <summary>
    /// 영수증 인덱스 생성 초기화
    /// </summary>
    public async Task InitializeIndexAsync()
    {
        var indexes = _searchIndexClient.GetIndexes();
        var selectedIndex = indexes.Any(m => m.Name == INDEX_CONST.RECEIPT_INDEX);
        if (!selectedIndex)
        {
            var index = new SearchIndex(INDEX_CONST.RECEIPT_INDEX)
            {
                Fields =
                {
                    new SimpleField("id", SearchFieldDataType.String){ IsKey = true },
                    new SearchableField("content_fulltext"){ IsFilterable = false, IsSortable = false, AnalyzerName = LexicalAnalyzerName.KoMicrosoft},
                    new SearchField("content_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = 1536,
                        VectorSearchProfileName = "vec-profile",
                    },
                    new SearchableField("merchant"){ IsFilterable = true, IsSortable = true, AnalyzerName = LexicalAnalyzerName.KoMicrosoft },
                    new SimpleField("merchant_brand",  SearchFieldDataType.String){ IsFilterable = true, IsFacetable = true, IsSortable = true },
                    new SimpleField("merchant_branch", SearchFieldDataType.String){ IsFilterable = true, IsFacetable = true },
                    
                    new SearchableField("address"){ IsFilterable = true, AnalyzerName = LexicalAnalyzerName.KoMicrosoft },
                    new SimpleField("bizNo",    SearchFieldDataType.String){ IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new SimpleField("trxAt",    SearchFieldDataType.DateTimeOffset){ IsFilterable = true, IsSortable = true },
                    new SimpleField("cardNo",    SearchFieldDataType.String){ IsFilterable = true, IsSortable = true },
                    new SimpleField("totalWon", SearchFieldDataType.Int64){ IsFilterable = true, IsSortable = true }
                },
                VectorSearch = new VectorSearch
                {
                    // 프로파일: 필드에서 참조하는 이름과 동일해야 함
                    Profiles =
                    {
                        // "vec-profile" 이 "hnsw-config" 알고리즘을 사용
                        new VectorSearchProfile(
                            name: "vec-profile",
                            algorithmConfigurationName: "hnsw-config"
                        )
                    },
                    Algorithms =
                    {
                        new HnswAlgorithmConfiguration("hnsw-config")
                    }
                }
            };
            await _searchIndexClient.CreateOrUpdateIndexAsync(index);
        }
    }

    public async Task<Embedding<float>> UploadReceiptDocument(ReceiptExtract extract)
    {
        var naturalKey = $"{extract.Merchant}|{extract.TransactionDateTime:yyyyMMddHHmmss}";
        var id = "receipt-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(naturalKey))).ToLowerInvariant();
        var vector = await _embeddingGenerator.GenerateAsync(extract.ToString());

        var res = await _searchClient.UploadDocumentsAsync([
            new {
                id = id,
                content_fulltext = extract.ToString(),
                content_vector = vector.Vector.ToArray(),
                merchant = extract.Merchant,
                address  = extract.Address,
                trxAt    = extract.TransactionDateTime,
                cardNo   = extract.CardNumberMasked,
                totalWon = extract.TotalAmountWon
            }
        ]);
        
        return vector;
    }
    
}