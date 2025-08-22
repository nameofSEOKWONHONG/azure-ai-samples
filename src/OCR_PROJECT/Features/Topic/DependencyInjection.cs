using Azure;
using Azure.Search.Documents;
using Document.Intelligence.Agent.Features.Topic.Services;
using eXtensionSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Models;

namespace Document.Intelligence.Agent.Features.Topic;

public static class DependencyInjection
{
    internal static void AddTopicService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKeyedScoped(INDEX_CONST.DOCUMENT_INDEX, (_, _) => new SearchClient(
            new Uri(configuration["OCR:AZURE_AI_SEARCH_ENDPOINT"].xValue<string>()),
            "document-v1",
            new AzureKeyCredential(configuration["OCR:AZURE_AI_SEARCH_API_KEY"].xValue<string>())
        ));
        services.AddScoped<ICreateTopicService, CreateTopicService>();
    }
}