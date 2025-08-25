using Document.Intelligence.Agent.Features.Agent.Services;
using Document.Intelligence.Agent.Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Document.Intelligence.Agent.Features.Agent;

public static class AgentEndpoint
{
    public static RouteGroupBuilder MapAgentEndpoint(this IEndpointRouteBuilder endpoint)
    {
        var group = endpoint.MapGroup("/dia-api/agent")
                .WithTags("Agent")
                .AddEndpointFilter<ErrorFilter>()
            //.RequireAuthorization()
            ;

        group.MapGet("/", async (ICreateAgentService service, CancellationToken ct) => await service.Sample());

        return group;
    }
}