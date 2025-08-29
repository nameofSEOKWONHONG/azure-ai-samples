using Microsoft.Extensions.DependencyInjection;

namespace Document.Intelligence.Agent.Features.Doc;

public static class DependencyInjection
{
    public static void AddDocService(this IServiceCollection services)
    {
        services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();
    }
}