using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Topic.Services;

public record FindTopicRequest(string Keyword, int Page =1, int PageSize = 10);

public record FindTopicResult(Guid Id, string Name, string Category);

public interface IFindTopicService: IDiaExecuteServiceBase<FindTopicRequest, PagedResult<FindTopicResult>>;
public class FindTopicService : DiaExecuteServiceBase<FindTopicService, DiaDbContext, FindTopicRequest, PagedResult<FindTopicResult>>
    , IFindTopicService
{
    public FindTopicService(ILogger<FindTopicService> logger, IDiaSessionContext session, DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override async Task<PagedResult<FindTopicResult>> ExecuteAsync(FindTopicRequest request, CancellationToken ct = default)
    {
        var query = dbContext.Topics
            .AsNoTracking()
            .Include(m => m.DocumentTopicMetadatum)
            .AsQueryable();
        if (request.Keyword.xIsNotEmpty())
        {
            query = query.Where(m => m.Name.Contains(request.Keyword));
        }

        var total = await query.CountAsync(cancellationToken: ct);
        var result = await query.OrderByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new FindTopicResult(m.Id, m.Name, m.Category))
            .ToListAsync(cancellationToken: ct);

        return await PagedResult<FindTopicResult>.SuccessAsync(result, total, request.Page, request.PageSize);
    }
}