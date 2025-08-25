using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Infrastructure.Middleware;

public class ErrorFilter : IEndpointFilter
{
    private readonly ILogger<ErrorFilter> _logger;

    public ErrorFilter(ILogger<ErrorFilter> logger) => _logger = logger;

    public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Minimal API error");
            return Results.Problem("An unexpected error occurred in Minimal API");
        }
    }
}