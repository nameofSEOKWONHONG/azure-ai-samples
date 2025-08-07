using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using eXtensionSharp;
using Microsoft.Extensions.DependencyInjection;

namespace OcrSample.Services.Documents;

public interface IDocumentSearchService
{
    Task<bool> CreateIndexAndUploadAsync();
    Task<List<DocumentSearchResult>> SearchAsync(string query, float[] queryVector);
}

public class DocumentSearchService : IDocumentSearchService
{
    private readonly SearchIndexClient _searchIndexClient;
    private readonly SearchClient _searchClient;
    private readonly ITextEmbeddingService _textEmbeddingService;
    private const string INDEX_NAME = "azureblob-index";
    public DocumentSearchService(SearchIndexClient searchIndexClient, [FromKeyedServices("DOCUMENT")]SearchClient searchClient,
        ITextEmbeddingService textEmbeddingService)
    {
        _searchIndexClient = searchIndexClient;
        _searchClient = searchClient;
        _textEmbeddingService = textEmbeddingService;
    }

    public async Task<bool> CreateIndexAndUploadAsync()
    {
        // 이렇게는 안됨... 삭제 및 업데이트를 하기 위해서는 KEY가 필요함.
        // 수작업으로 해야 할 듯함.
        
        // var resp = await _searchClient.SearchAsync<SearchDocument>(string.Empty);
        // var list = new List<dynamic>();
        // await foreach (var r in resp.Value.GetResultsAsync())
        // {
        //     var doc = r.Document;
        //     var content = doc["content"].xValue<string>();
        //     if(content.xIsEmpty()) continue;
        //     
        //     var vector = await _textEmbeddingService.GetEmbeddedText(doc["content"].xValue<string>());
        //     doc["content_vector"] = vector;
        //     await _searchClient.DeleteDocumentsAsync(doc);
        //     list.Add(new
        //     {
        //         content = doc["content"],
        //         content_vector = vector,
        //         metadata_storage_path = doc["metadata_storage_path"],
        //         metadata_storage_file_extension = doc["metadata_storage_file_extension"]
        //     });
        // }
        //
        // await _searchClient.UploadDocumentsAsync(list);
        return true;
    }

    public async Task<List<DocumentSearchResult>> SearchAsync(string query, float[] queryVector)
    {
        var result = new List<DocumentSearchResult>();
        var options = new SearchOptions()
        {
            Select = { "content", "metadata_storage_file_extension"},
            IncludeTotalCount = true
        };
        options.VectorSearch = new()
        {
            Queries =
            {
                new VectorizedQuery(queryVector)
                {
                    Fields = { "content_vector" },
                    KNearestNeighborsCount = Math.Min(200, 500)
                }
            }
        };
        var resp = await _searchClient.SearchAsync<SearchDocument>(query, options);
        double score = 0;
        await foreach (var r in resp.Value.GetResultsAsync())
        {
            if (score < r.Score)
            {
                score = r.Score.xValue<double>();
            }
        }

        score = score / 2;
        await foreach (var r in resp.Value.GetResultsAsync())
        {
            if(r.Score < score) continue;
            var doc = r.Document;
            result.Add(new DocumentSearchResult()
            {
                Content = doc["content"].xValue<string>()
                    .Replace("\t", "")
                    .Replace("\n", " ")
                    .Replace("  ", " ")
                    .Trim(),
                MatchScore = r.Score.xValue<string>(),
                MetadataStorageFileExtension = doc["metadata_storage_file_extension"].xValue<string>(),
            });
        }

        return result;
    }
}

public class DocumentSearchResult
{
    public string Content { get; set; }
    public string MetadataStorageFileExtension { get; set; }
    public string MatchScore { get; set; }

    public override string ToString()
    {
        return $"- 문서 내용:{Content} {Environment.NewLine} - 매칭 점수:{MatchScore} - 파일확장자: {MetadataStorageFileExtension}";
    }
}