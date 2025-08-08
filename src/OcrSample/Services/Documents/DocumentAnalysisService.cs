using Azure.AI.Vision.ImageAnalysis;
using Azure.Search.Documents.Indexes.Models;

namespace OcrSample.Services.Documents;

public interface IDocumentAnalysisService
{
    
}

public class DocumentAnalysisService : IDocumentAnalysisService
{
    private readonly ImageAnalysisClient _client;
    public DocumentAnalysisService(ImageAnalysisClient client)
    {
        _client = client;
    }
}