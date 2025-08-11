using System.ClientModel;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.Vision.ImageAnalysis;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using eXtensionSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OcrSample.Services.Documents;

namespace OcrSample.Services.Receipts;

public static class DependencyInjection
{
    public static void AddAiReceipt(this IServiceCollection services, IConfiguration configuration)
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
        
        services.AddKeyedScoped(AiFeatureConst.RECEIPT, (sp, o) => new SearchClient(new Uri(configuration["AZURE_AI_SEARCH_ENDPOINT"].xValue<string>()), 
            AiFeatureConst.RECEIPT_INDEX_NAME,
            new AzureKeyCredential(configuration["AZURE_AI_SEARCH_API_KEY"].xValue<string>())));
        services.AddScoped(_ =>
            new ImageAnalysisClient(new Uri(configuration["AZURE_OCR_ENDPOINT"].xValue<string>()), new AzureKeyCredential(configuration["AZURE_OCR_API_KEY"].xValue<string>())));
        services.AddScoped<IReceiptLlmService, ReceiptLlmService>();
        services.AddScoped<IReceiptSearchService, ReceiptSearchService>();
        services.AddScoped<IReceiptAnalysisService, ReceiptAnalysisService>();
        services.AddScoped<IReceiptConverter, IcReceiptConverter>();
        services.AddScoped<IReceiptConverter, KioskReceiptConverter>();
        services.AddScoped<ReceiptConvertFactory>();
        services.AddKeyedScoped<IAiPipeline, ReceiptPipeline>(AiFeatureConst.RECEIPT);
    }
}