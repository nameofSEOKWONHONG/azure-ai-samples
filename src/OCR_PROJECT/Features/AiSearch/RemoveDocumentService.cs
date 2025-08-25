using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.AiSearch;

public interface IRemoveDocumentService: IDiaExecuteServiceBase<Guid, Results<bool>>
{
    
}

/// <summary>
/// AI SEARCH 및 문서 삭제
/// </summary>
public class RemoveDocumentService: DiaExecuteServiceBase<RemoveDocumentService, DiaDbContext, Guid, Results<bool>>, IRemoveDocumentService
{
    public RemoveDocumentService(ILogger<RemoveDocumentService> logger, IDiaSessionContext session, DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override async Task<Results<bool>> ExecuteAsync(Guid request, CancellationToken ct = default)
    {
        var exists = await this.dbContext.ChatThreads.FirstOrDefaultAsync(m => m.Id == request, cancellationToken: ct);
        if (exists.xIsEmpty()) throw new Exception("not found thread");
        
        //TODO: INDEX 삭제

        this.dbContext.ChatThreads.Remove(exists);
        await this.dbContext.SaveChangesAsync(ct);

        return await Results<bool>.SuccessAsync(true);
    }
}