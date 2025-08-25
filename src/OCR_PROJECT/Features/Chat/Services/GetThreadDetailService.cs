using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Entities.Chat;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Chat.Services;

public record GetThreadDetailRequest
{
    public Guid ThreadId { get; set; }
    public DateTime? Cursor { get; set; }
}

public record GetThreadDetailResult(Guid Id, string Question, string Answer, IEnumerable<DOCUMENT_CHAT_ANSWER_CITATION> Citations);

public interface
    IGetThreadDetailService : IDiaExecuteServiceBase<GetThreadDetailRequest, Results<IEnumerable<GetThreadDetailResult>>>;

public class GetThreadDetailService: DiaExecuteServiceBase<GetThreadDetailService, DiaDbContext, GetThreadDetailRequest, Results<IEnumerable<GetThreadDetailResult>>>, IGetThreadDetailService
{
    public GetThreadDetailService(ILogger<GetThreadDetailService> logger, IDiaSessionContext session,
        DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override async Task<Results<IEnumerable<GetThreadDetailResult>>> ExecuteAsync(GetThreadDetailRequest request, CancellationToken ct = default)
    {
        if (request.Cursor.xIsEmpty())
        {
            request.Cursor = DateTime.Now;
        }

        var result = await this.dbContext.ChatQuestions.AsNoTracking()
            .Include(m => m.Answers)
            .ThenInclude(m => m.Citations)
            .Where(m => m.ThreadId == request.ThreadId)
            .Where(m => m.CreatedAt <= request.Cursor)
            .OrderByDescending(m => m.CreatedAt)
            .Take(30)
            .Select(m =>
                new GetThreadDetailResult(m.Id, m.Question, m.Answers.First().Answer, m.Answers.First().Citations))
            .ToListAsync(cancellationToken: ct);

        return await Results<IEnumerable<GetThreadDetailResult>>.SuccessAsync(result);
    }
}