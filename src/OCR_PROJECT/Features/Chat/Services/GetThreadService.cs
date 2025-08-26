using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Entities.Chat;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Chat.Services;

public record GetThreadRequest(Guid Id, int Page, int PageSize);

public record GetThreadResult(Guid Id, string Question, string Answer, IEnumerable<DOCUMENT_CHAT_ANSWER_CITATION> Citations);

public interface
    IGetThreadService : IDiaExecuteServiceBase<GetThreadRequest, PagedResults<GetThreadResult>>;

public class GetThreadService: DiaExecuteServiceBase<GetThreadService, DiaDbContext, GetThreadRequest, 
    PagedResults<GetThreadResult>>, IGetThreadService
{
    public GetThreadService(ILogger<GetThreadService> logger, IDiaSessionContext session,
        DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override async Task<PagedResults<GetThreadResult>> ExecuteAsync(GetThreadRequest request, CancellationToken ct = default)
    {
        var queryable = this.dbContext.ChatQuestions.AsNoTracking()
            .Include(m => m.Answers)
            .ThenInclude(m => m.Citations)
            .Where(m => m.ThreadId == request.Id)
            .OrderByDescending(m => m.CreatedAt)
            .AsQueryable();

        var total = await queryable.CountAsync(cancellationToken: ct);
        var result = await queryable
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m =>
                new GetThreadResult(m.Id, m.Question, m.Answers.First().Answer, m.Answers.First().Citations))
            .ToArrayAsync(cancellationToken: ct);

        return await PagedResults<GetThreadResult>.SuccessAsync(result, total, request.Page, request.PageSize);
    }
}