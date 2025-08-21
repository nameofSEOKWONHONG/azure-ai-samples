using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Document.Chat;

public record FindThreadResult(Guid Id, string Title, DateTime CreatedAt);

public interface IFindThreadService: IDiaExecuteServiceBase<DateTime?, IEnumerable<FindThreadResult>>
{
    
}

public class FindThreadService: DiaExecuteServiceBase<FindThreadService, DiaDbContext, DateTime?, IEnumerable<FindThreadResult>>, IFindThreadService
{
    public FindThreadService(ILogger<FindThreadService> logger, IDiaSessionContext session,
        DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override async Task<IEnumerable<FindThreadResult>> ExecuteAsync(DateTime? request)
    {
        if (request.xIsEmpty())
        {
            request = DateTime.Now;
        }

        return await this.dbContext.Threads.AsNoTracking()
            .Where(m => m.CreatedAt <= request)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new FindThreadResult(m.Id, m.Title, m.CreatedAt))
            .ToListAsync();
    }
}

