using Document.Intelligence.Agent.Features.Chat.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Models;

namespace Document.Intelligence.Agent.Features.Chat;

public static class DependencyInjection
{
    internal static void AddChatService(this IServiceCollection services)
    {
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IQuestionContextSwitchService, QuestionContextSwitchService>();           
    }
}