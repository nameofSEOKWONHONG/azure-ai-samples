using Document.Intelligence.Agent.Features.Chat.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Document.Intelligence.Agent.Features.Chat;

public static class DependencyInjection
{
    internal static void AddChatService(this IServiceCollection services)
    {
        services.AddScoped<IGetThreadService, GetThreadService>();
        services.AddScoped<IFindThreadService, FindThreadService>();
        services.AddScoped<IRemoveThreadService, RemoveThreadService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IQuestionContextSwitchService, QuestionContextSwitchService>();           
    }
}