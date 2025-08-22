using Document.Intelligence.Agent.Features.Agent.Services;
using Document.Intelligence.Agent.Features.AiSearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Models;

namespace Document.Intelligence.Agent.Features.Agent;

public static class DependencyInjection
{
    internal static void AddAgentService(this IServiceCollection services)
    {
        services.AddScoped<ICreateAgentService, CreateAgentService>();
        services.AddScoped<ICreateDocumentIndexService, CreateDocumentIndexService>();
        services.AddScoped<IUploadDocumentService, UploadDocumentService>();          
    }
}