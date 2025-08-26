using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Chat.Services;

public interface IRemoveThreadService : IDiaExecuteServiceBase<Guid?, Results<bool>>;

/// <summary>
/// LLM 대화 중 특정 THREAD 삭제 서비스
/// </summary>
public class RemoveThreadService: DiaExecuteServiceBase<RemoveThreadService, DiaDbContext, Guid?, Results<bool>>, IRemoveThreadService
{
    public RemoveThreadService(ILogger<RemoveThreadService> logger, IDiaSessionContext session, 
        DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override async Task<Results<bool>> ExecuteAsync(Guid? request, CancellationToken ct = default)
    {
        if (request.xIsEmpty()) throw new Exception("Id is empty");
        
        var exists = await this.dbContext.ChatThreads.FirstAsync(m => m.Id == request, cancellationToken: ct);
        
        this.dbContext.ChatThreads.Remove(exists);
        await this.dbContext.SaveChangesAsync(ct);

        return await Results<bool>.SuccessAsync(true);
    }
}