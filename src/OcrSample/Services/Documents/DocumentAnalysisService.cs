using Azure.AI.Vision.ImageAnalysis;

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