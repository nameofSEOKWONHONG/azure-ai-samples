using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Document.AiSearch;

public interface IRemoveDocumentService: IDiaExecuteServiceBase<Guid, bool>
{
    
}

/// <summary>
/// AI SEARCH 및 문서 삭제
/// </summary>
public class RemoveDocumentService: DiaExecuteServiceBase<RemoveDocumentService, DiaDbContext, Guid, bool>, IRemoveDocumentService
{
    public RemoveDocumentService(ILogger<RemoveDocumentService> logger, IDiaSessionContext session, DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override async Task<bool> ExecuteAsync(Guid request)
    {
        var exists = await this.dbContext.Threads.FirstOrDefaultAsync(m => m.Id == request);
        if (exists.xIsEmpty()) throw new Exception("not found thread");
        
        //TODO: INDEX 삭제

        this.dbContext.Threads.Remove(exists);
        await this.dbContext.SaveChangesAsync();

        return true;
    }
}