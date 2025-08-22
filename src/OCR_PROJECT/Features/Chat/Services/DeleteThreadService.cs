using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Chat.Services;

public interface IDeleteThreadService : IDiaExecuteServiceBase<Guid, bool>;

/// <summary>
/// LLM 대화 중 특정 THREAD 삭제 서비스
/// </summary>
public class DeleteThreadService: DiaExecuteServiceBase<DeleteThreadService, DiaDbContext, Guid, bool>, IDeleteThreadService
{
    public DeleteThreadService(ILogger<DeleteThreadService> logger, IDiaSessionContext session, 
        DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override async Task<Results<bool>> ExecuteAsync(Guid request)
    { 
        var exists = await this.dbContext.ChatThreads.FirstOrDefaultAsync(m => m.Id == request);
        if (exists.xIsEmpty()) throw new NullReferenceException("not found thread");

        this.dbContext.ChatThreads.Remove(exists);
        await this.dbContext.SaveChangesAsync();

        return await Results<bool>.SuccessAsync(true);
    }
}