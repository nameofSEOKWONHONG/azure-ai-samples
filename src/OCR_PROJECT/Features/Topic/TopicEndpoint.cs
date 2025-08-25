using Document.Intelligence.Agent.Features.Topic.Models;
using Document.Intelligence.Agent.Features.Topic.Services;
using Document.Intelligence.Agent.Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Document.Intelligence.Agent.Features.Topic;

public static class TopicEndpoint
{
    public static void MapTopicEndpoint(this IEndpointRouteBuilder endpoint)
    {
        var group = endpoint.MapGroup("/dia-api/topic")
                .WithTags("Topic")
                .AddEndpointFilter<ErrorFilter>()
            //.RequireAuthorization()
            ;

        group.MapGet("/", async ([AsParameters]FindTopicRequest request, IFindTopicService service, CancellationToken ct) 
            => await service.ExecuteAsync(request, ct))
            .WithSummary("토픽 검색");
        group.MapGet("/{id}", async (Guid id, IGetTopicService service, CancellationToken ct) 
            => await service.ExecuteAsync(id, ct))
            .WithSummary("토픽 조회");
        group.MapPost("/",
                async ([FromBody]CreateTopicRequest request, ICreateTopicService service, CancellationToken ct)
                    => await service.ExecuteAsync(request, ct))
            .WithSummary("토픽 생성")
            .AddEndpointFilter<TransactionFilter>();
        group.MapDelete("/{id}", async (Guid id, IRemoveTopicService service, CancellationToken ct)
            => await service.ExecuteAsync(id, ct))
            .WithSummary("토픽 삭제")
            .AddEndpointFilter<TransactionFilter>();
    }
}