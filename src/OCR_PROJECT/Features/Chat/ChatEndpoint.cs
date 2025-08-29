using Document.Intelligence.Agent.Features.Chat.Models;
using Document.Intelligence.Agent.Features.Chat.Services;
using Document.Intelligence.Agent.Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Document.Intelligence.Agent.Features.Chat;

public static class ChatEndpoint
{
    public static void MapChatEndpoint(this IEndpointRouteBuilder endpoint)
    {
        var group = endpoint.MapGroup("/dia-api/chat")
                .WithTags("Chat")
                .AddEndpointFilter<ErrorFilter>()
            //.RequireAuthorization()
            ;

        group.MapGet("/",
                async ([AsParameters] FindThreadRequest request, IFindThreadService service, CancellationToken ct)
                    => await service.ExecuteAsync(request, ct))
            .WithDescription("채팅 목록")
            ;

        group.MapGet("/{id}/{page}/{pageSize}", async (Guid id, int page, int pageSize, IGetThreadService service, CancellationToken ct)
            => await service.ExecuteAsync(new GetThreadRequest(id, page, pageSize), ct))
            .WithDescription("채팅 상세")
            ;
        
        group.MapPost("/", async ([FromBody]ChatRequest request, IChatService service, CancellationToken ct) 
            => await service.ExecuteAsync(request, ct))
            .WithDescription("채팅")
            .AddEndpointFilter<TransactionFilter>();

        group.MapPost("/agent",
                async ([FromBody] AgentChatRequest request, IAgentChatService service, CancellationToken ct)
                    => await service.ExecuteAsync(request, ct))
            .WithDescription("AGENT 기반 채팅")
            .AddEndpointFilter<TransactionFilter>();

        group.MapDelete("/{id}", async (Guid? id, IRemoveThreadService service, CancellationToken ct)
                => await service.ExecuteAsync(id, ct))
            .WithDescription("채팅 삭제")
            .AddEndpointFilter<TransactionFilter>()
            ;
    }
}