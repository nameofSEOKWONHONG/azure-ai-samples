namespace Document.Intelligence.Agent.Features.Document.Models;

/// <summary>
/// Azure OpenAI, AI Search, OCR, Document Intelligence 관련 설정값
/// </summary>
public class OcrOption
{
    // Azure OpenAI
    public string AZURE_OPENAI_ENDPOINT { get; init; }
    public string AZURE_OPENAI_MODEL_NAME { get; init; }
    public string AZURE_OPENAI_API_KEY { get; init; }
    public string AZURE_OPENAI_EMBED_MODEL { get; init; }

    // Azure AI Search
    public string AZURE_AI_SEARCH_ENDPOINT { get; init; }
    public string AZURE_AI_SEARCH_API_KEY { get; init; }
    public string AZURE_AI_SEARCH_INDEX_NAME { get; init; }

    // Azure Document Intelligence
    public string AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT { get; init; }
    public string AZURE_DOCUMENT_INTELLIGENCE_KEY { get; init; }
    
}