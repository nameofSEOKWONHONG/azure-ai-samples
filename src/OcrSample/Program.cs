using System.ClientModel;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.Vision.ImageAnalysis;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using eXtensionSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OcrSample.Receipts;
using OcrSample.Services;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddTransient(_ =>
{
    var options = new AzureOpenAIClientOptions();
    return new AzureOpenAIClient(new Uri(config["AZURE_OPENAI_ENDPOINT"].xValue<string>()),
        new ApiKeyCredential(config["AZURE_OPENAI_API_KEY"].xValue<string>()), options);
});
services.AddTransient(_ =>
    new SearchIndexClient(new Uri(config["AZURE_AI_SEARCH_ENDPOINT"].xValue<string>()), new AzureKeyCredential(config["AZURE_AI_SEARCH_API_KEY"].xValue<string>())));
services.AddTransient(_ => new SearchClient(new Uri(config["AZURE_AI_SEARCH_ENDPOINT"].xValue<string>()), 
    config["AZURE_AI_SEARCH_INDEX_NAME"].xValue<string>(),
    new AzureKeyCredential(config["AZURE_AI_SEARCH_API_KEY"].xValue<string>())));
services.AddTransient(_ =>
    new ImageAnalysisClient(new Uri(config["AZURE_OCR_ENDPOINT"].xValue<string>()), new AzureKeyCredential(config["AZURE_OCR_API_KEY"].xValue<string>())));

services.AddTransient<ILlmService, LlmService>();
services.AddTransient<ITextEmbeddingService, TextEmbeddingService>();
services.AddTransient<IReceiptSearchService, ReceiptSearchService>();

services.AddTransient<IReceiptAnalysisService, ReceiptAnalysisService>();
services.AddTransient<IReceiptConverter, IcReceiptConverter>();
services.AddTransient<IReceiptConverter, KioskReceiptConverter>();
services.AddTransient<ReceiptConvertFactory>();
services.AddSingleton<IMainService, MainService>();

var provider = services.BuildServiceProvider();

var main = provider.GetRequiredService<IMainService>();
await main.RunAsync();
