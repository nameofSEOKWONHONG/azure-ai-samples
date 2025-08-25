using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Chat.Services;

public record FindThreadResult(Guid Id, string Title, DateTime CreatedAt);

public interface IFindThreadService: IDiaExecuteServiceBase<DateTime?, Results<IEnumerable<FindThreadResult>>>
{
    
}

public class FindThreadService: DiaExecuteServiceBase<FindThreadService, DiaDbContext, DateTime?, Results<IEnumerable<FindThreadResult>>>, IFindThreadService
{
    public FindThreadService(ILogger<FindThreadService> logger, IDiaSessionContext session,
        DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override async Task<Results<IEnumerable<FindThreadResult>>> ExecuteAsync(DateTime? request, CancellationToken ct = default)
    {
        if (request.xIsEmpty())
        {
            request = DateTime.Now;
        }

        var result = await this.dbContext.ChatThreads.AsNoTracking()
            .Where(m => m.CreatedAt <= request)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new FindThreadResult(m.Id, m.Title, m.CreatedAt))
            .ToListAsync(cancellationToken: ct);

        return await Results<IEnumerable<FindThreadResult>>.SuccessAsync(result);
    }
}

