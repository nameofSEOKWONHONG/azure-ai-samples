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
    internal static void AddTopicService(this IServiceCollection services)
    {
        services.AddScoped<ICreateTopicService, CreateTopicService>();
        services.AddScoped<IFindTopicService, FindTopicService>();
        services.AddScoped<IGetTopicService, GetTopicService>();
        services.AddScoped<IRemoveTopicService, RemoveTopicService>();
    }
}