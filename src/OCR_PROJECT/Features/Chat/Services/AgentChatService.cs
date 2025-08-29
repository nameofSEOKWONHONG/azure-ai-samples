using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Entities.Agent;
using Document.Intelligence.Agent.Features.Chat.Models;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Chat.Services;

public record AgentChatRequest(Guid[] AgentIdList, ChatRequest Request);
public interface IAgentChatService : IDiaExecuteServiceBase<AgentChatRequest, Results<DocumentChatResult>>;
public class AgentChatService: DiaExecuteServiceBase<AgentChatService, DiaDbContext, AgentChatRequest, Results<DocumentChatResult>>, IAgentChatService
{
    private readonly IChatService _chatService;

    public AgentChatService(ILogger<AgentChatService> logger, IDiaSessionContext session, DiaDbContext dbContext,
        IChatService chatService) : base(logger, session, dbContext)
    {
        _chatService = chatService;
    }

    public override async Task<Results<DocumentChatResult>> ExecuteAsync(AgentChatRequest request, CancellationToken ct = default)
    {                
        var agents =  await this.dbContext.Agents
            .AsNoTracking()
            .Include(m => m.AgentPrompts)
            .Include(m => m.AgentTopics)
            .Where(m => request.AgentIdList.Contains(m.Id))
            .ToListAsync(cancellationToken: ct);

        if (agents.xIsEmpty())
        {
            var mapping = await this.dbContext.AgentUserMappings
                .AsNoTracking()
                .Where(m => m.UserId == session.UserId && m.IsActive == true && m.IsDefault == true)
                .FirstOrDefaultAsync(cancellationToken: ct);

            if (mapping.xIsEmpty()) return await Results<DocumentChatResult>.FailAsync("default agent is empty");
                
            agents = await this.dbContext.Agents
                .AsNoTracking()
                .Include(m => m.AgentPrompts)
                .Include(m => m.AgentTopics)
                .Where(m => m.Id == mapping.AgentId)
                .ToListAsync(cancellationToken: ct);
        }

        if (agents.xIsEmpty()) return await Results<DocumentChatResult>.FailAsync("agent is empty");

        foreach (var agent in agents)
        {
            var result = await _chatService.ExecuteAsync(new ChatRequest()
            {
                AgentId = agent.Id,
                CurrentQuestion = request.Request.CurrentQuestion,
                PreviousQuestionId = request.Request.PreviousQuestionId,
                ThreadId = request.Request.ThreadId
            }, ct);

            if (result.IsSucceed) return result;
        }

        return await Results<DocumentChatResult>.FailAsync("don't answer for your question");
    }
}