using System.Security.Cryptography;
using System.Text;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using eXtensionSharp;
using Microsoft.Extensions.DependencyInjection;
using OcrSample.Models;

namespace OcrSample.Services.Receipts;

public interface IReceiptSearchService
{
    Task<bool> CreateIndexAndUploadAsync(ReceiptExtract extract, float[] vector);
    Task<List<QueryResult>> SearchAsync(string query, QuerySpec querySpec, float[] queryVector);
}

public class ReceiptSearchService: IReceiptSearchService
{
    private readonly SearchIndexClient _searchIndexClient;
    private readonly SearchClient _searchClient;
    private const string INDEX_NAME = "receipt-v1";

    public ReceiptSearchService(SearchIndexClient searchIndexClient, [FromKeyedServices("RECEIPT")]SearchClient searchClient)
    {
        _searchIndexClient = searchIndexClient;
        _searchClient = searchClient;
    }

    public async Task<bool> CreateIndexAndUploadAsync(ReceiptExtract extract, float[] vector)
    {
        var indexes = _searchIndexClient.GetIndexes();
        var selectedIndex = indexes.Any(m => m.Name == INDEX_NAME);
        if (!selectedIndex)
        {
            var index = new SearchIndex(INDEX_NAME)
            {
                Fields =
                {
                    new SimpleField("id", SearchFieldDataType.String){ IsKey = true },
                    new SearchableField("content_fulltext"){ IsFilterable = false, IsSortable = false, AnalyzerName = LexicalAnalyzerName.KoMicrosoft},
                    new SearchField("content_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = 3072,
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

        var naturalKey = $"{extract.Merchant}|{extract.BusinessNumber}|{extract.TransactionDateTime:yyyyMMddHHmmss}";
        var id = "receipt-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(naturalKey))).ToLowerInvariant();

        var res = await _searchClient.UploadDocumentsAsync(new[]
        {
            new {
                id = id,
                content_fulltext = extract.ToString(),
                content_vector = vector,
                merchant = extract.Merchant,
                merchant_brand = extract.MerchantBrand,
                merchant_branch = extract.MerchantBranch,
                address  = extract.Address,
                bizNo    = extract.BusinessNumber,
                trxAt    = extract.TransactionDateTime,
                cardNo   = extract.CardNumberMasked,
                totalWon = extract.TotalAmountWon
            }
        });
        return res.Value.Results[0].Succeeded;
    }

    public async Task<List<QueryResult>> SearchAsync(string query, QuerySpec querySpec, float[] queryVector)
    {
        var result = new List<QueryResult>();

        try
        {
            var searchOptions = BuildSearchOptions(querySpec, queryVector);
            var resp = await _searchClient.SearchAsync<SearchDocument>(query, searchOptions);


            // 결과 열람
            await foreach (var r in resp.Value.GetResultsAsync())
            {
                var doc = r.Document;
                result.Add(new QueryResult()
                {
                    Id = doc["id"].xValue<string>(),
                    Merchant = doc["merchant"].xValue<string>(),
                    BizNo = doc["bizNo"].xValue<string>(),
                    TrxAt = doc["trxAt"].xValue<DateTime>(),
                    TotalWon = doc["totalWon"].xValue<long>(),
                    CardNo = doc["cardNo"].xValue<string>(),
                    Score = r.Score
                });
            }
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(e.Message);
        }

        return result;
    }

    private SearchOptions BuildSearchOptions(QuerySpec q, float[] queryVector,
        string vectorField = "content_vector",// 인덱스의 벡터 필드명
        bool merchantFullSearchable = true,   // 인덱스에서 searchable인지 여부
        bool contentFulltextSearchable = true // 인덱스에서 searchable인지 여부        
        )
    {
        int size = Math.Clamp(q.TopK ?? 50, 1, 100);
        int page = Math.Max(1, q.Page ?? 1);
        
        var opts = new SearchOptions
        {
            Size = size,
            Skip = (page - 1) * size,
            IncludeTotalCount = true,
            Select = { "id", "merchant", "merchant_brand", "merchant_branch", "trxAt", "cardNo", "bizNo", "totalWon" },
            HighlightFields = { "content_fulltext" }
        };
        
        var filter = BuildFilter(q);
        if (filter.xIsNotEmpty())
            opts.Filter = filter;
        
        switch (q.Sort)
        {
            case SortMode.Relevance:
                opts.OrderBy.Add("search.score desc"); // Select에 score는 넣지 말 것
                break;
            case SortMode.Latest:
                opts.OrderBy.Add("trxAt desc");
                break;
            case SortMode.AmountDesc:
                opts.OrderBy.Add("totalWon desc");
                break;
            case SortMode.AmountAsc:
                opts.OrderBy.Add("totalWon asc");
                break;
        }

        opts.VectorSearch = new()
        {
            Queries =
            {
                new VectorizedQuery(queryVector)
                {
                    Fields = { vectorField },
                    KNearestNeighborsCount = Math.Min(200, size * 4)
                }
            }
        };
        
        opts.SearchFields.Add("merchant");
        opts.SearchFields.Add("content_fulltext");

        opts.QueryType = SearchQueryType.Simple;
        
        return opts;
    }
    
    private string BuildFilter(QuerySpec q)
    {
        var f = new List<string>();

        // 질의별
        if (!string.IsNullOrWhiteSpace(q.Brand))
            f.Add(SearchFilter.Create($"merchant_brand eq {q.Brand}"));
        if (!string.IsNullOrWhiteSpace(q.Branch))
            f.Add(SearchFilter.Create($"merchant_branch eq {q.Branch}"));

        if (q.From is not null)
            f.Add(SearchFilter.Create($"trxAt ge {q.From:yyyy-MM-ddTHH:mm:ssZ}"));
        if (q.To is not null)
            f.Add(SearchFilter.Create($"trxAt lt {q.To:yyyy-MM-ddTHH:mm:ssZ}"));

        if (q.MinWon is not null)
            f.Add(SearchFilter.Create($"totalWon ge {q.MinWon}"));
        if (q.MaxWon is not null)
            f.Add(SearchFilter.Create($"totalWon lt {q.MaxWon}"));

        return string.Join(" and ", f);
    }
    
}

