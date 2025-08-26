using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Chat.Services;

public record FindThreadRequest(string Title, int Page, int PageSize);
public record FindThreadResult(Guid Id, string Title);

public interface IFindThreadService : IDiaExecuteServiceBase<FindThreadRequest, PagedResults<FindThreadResult>>;

public class FindThreadService: DiaExecuteServiceBase<FindThreadService, DiaDbContext, FindThreadRequest, PagedResults<FindThreadResult>>
    , IFindThreadService
{
    public FindThreadService(ILogger<FindThreadService> logger, IDiaSessionContext session,
        DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override async Task<PagedResults<FindThreadResult>> ExecuteAsync(FindThreadRequest request, CancellationToken ct = default)
    {
        var queryable = this.dbContext.ChatThreads.AsNoTracking().AsQueryable();

        if (request.Title.xIsNotEmpty())
        {
            queryable = queryable.Where(m => m.Title.Contains(request.Title));
        }

        var total = await queryable.CountAsync(cancellationToken: ct);
        var result = await this.dbContext.ChatThreads.AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new FindThreadResult(m.Id, m.Title))
            .ToArrayAsync(cancellationToken: ct);

        return await PagedResults<FindThreadResult>.SuccessAsync(result, total, request.Page, request.PageSize);
    }
}

