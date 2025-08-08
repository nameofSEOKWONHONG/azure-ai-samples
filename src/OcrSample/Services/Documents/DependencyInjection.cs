using System.ClientModel;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using eXtensionSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OcrSample.Services.Documents;

public static class DependencyInjection
{
    public static void AddAiDocument(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(_ =>
        {
            var options = new AzureOpenAIClientOptions();
            return new AzureOpenAIClient(new Uri(configuration["AZURE_OPENAI_ENDPOINT"].xValue<string>()),
                new ApiKeyCredential(configuration["AZURE_OPENAI_API_KEY"].xValue<string>()), options);
        });
        services.AddScoped(_ =>
            new SearchIndexClient(new Uri(configuration["AZURE_AI_SEARCH_ENDPOINT"].xValue<string>()), new AzureKeyCredential(configuration["AZURE_AI_SEARCH_API_KEY"].xValue<string>())));
        services.AddScoped<ITextEmbeddingService, TextEmbeddingService>();
        
        services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();
        services.AddScoped<IDocumentIntelligenceService, DocumentIntelligenceService>();
        services.AddScoped<IDocumentSearchService, DocumentSearchService>();
        services.AddScoped<IDocumentLlmService, DocumentLlmService>();
        services.AddKeyedScoped<IAiPipeline, DocumentPipeline>("DOCUMENT");
        services.AddScoped<IDocumentInitializer, DocumentInitializer>();
        services.AddKeyedScoped<SearchClient>("DOCUMENT", (sp, o) => new SearchClient(new Uri(configuration["AZURE_AI_SEARCH_ENDPOINT"].xValue<string>()), 
            "azureblob-index",
            new AzureKeyCredential(configuration["AZURE_AI_SEARCH_API_KEY"].xValue<string>())));
        services.AddKeyedScoped<SearchClient>("")
    }
}

public class DocumentConst
{
    public string PdfIndexName = "";
}