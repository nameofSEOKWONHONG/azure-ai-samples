using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Document.Chat;

public record GetThreadDetailRequest
{
    public Guid ThreadId { get; set; }
    public DateTime? Cursor { get; set; }
}

public record GetThreadDetailResult(Guid Id, string Question, string Answer, IEnumerable<DOCUMENT_CITATION> Citations);

public interface
    IGetThreadDetailService : IDiaExecuteServiceBase<GetThreadDetailRequest, IEnumerable<GetThreadDetailResult>>;

public class GetThreadDetailService: DiaExecuteServiceBase<GetThreadDetailService, DiaDbContext, GetThreadDetailRequest, IEnumerable<GetThreadDetailResult>>, IGetThreadDetailService
{
    public GetThreadDetailService(ILogger<GetThreadDetailService> logger, IDiaSessionContext session,
        DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override async Task<IEnumerable<GetThreadDetailResult>> ExecuteAsync(GetThreadDetailRequest request)
    {
        if (request.Cursor.xIsEmpty())
        {
            request.Cursor = DateTime.Now;
        }

        return await this.dbContext.Questions.AsNoTracking()
            .Include(m => m.Answers)
            .ThenInclude(m => m.Citations)
            .Where(m => m.ThreadId == request.ThreadId)
            .Where(m => m.CreatedAt <= request.Cursor)
            .OrderByDescending(m => m.CreatedAt)
            .Take(30)
            .Select(m =>
                new GetThreadDetailResult(m.Id, m.Question, m.Answers.First().Answer, m.Answers.First().Citations))
            .ToListAsync();
    }
}