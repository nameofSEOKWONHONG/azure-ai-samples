using Document.Intelligence.Agent.Features.Agent;
using Document.Intelligence.Agent.Features.Chat.Models;
using Document.Intelligence.Agent.Features.Chat.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Document.Intelligence.Agent.Features.Chat;

public static class ChatEndpoint
{
    public static void MapChatEndpoint(this IEndpointRouteBuilder endpoint)
    {
        var group = endpoint.MapGroup("/dia-api/chat")
                .WithTags("Chat")
            //.RequireAuthorization()
            ;

        group.MapGet("/", async (IChatService service, CancellationToken ct) => await service.ExecuteAsync(new ChatRequest()));
    }
}