using Document.Intelligence.Agent.Features.Agent;
using Document.Intelligence.Agent.Features.Topic.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Document.Intelligence.Agent.Features.Topic;

public static class TopicEndpoint
{
    public static void MapTopicEndpoint(this IEndpointRouteBuilder endpoint)
    {
        var group = endpoint.MapGroup("/dia-api/topic")
                .WithTags("Topic")
            //.RequireAuthorization()
            ;

        group.MapGet("/", async (ICreateTopicService service, CancellationToken ct) => await service.Sample());        
    }
}