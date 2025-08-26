using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Topic.Services;

public record GetTopicResult(Guid Id, string Name, string Category, IEnumerable<TopicMetadata> topicMetadatas);

public record TopicMetadata(Guid Id, string Path);

public interface IGetTopicService : IDiaExecuteServiceBase<Guid, Results<GetTopicResult>>;

public class GetTopicService: DiaExecuteServiceBase<GetTopicService, DiaDbContext, Guid, Results<GetTopicResult>>, IGetTopicService
{
    public GetTopicService(ILogger<GetTopicService> logger, IDiaSessionContext session, DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override async Task<Results<GetTopicResult>> ExecuteAsync(Guid request, CancellationToken ct = default)
    {
        var result = await this.dbContext.Topics
            .Include(m => m.DocumentTopicMetadatum)
            .AsNoTracking()
            .Where(m => m.Id == request)
            .Select(m => new GetTopicResult(m.Id, m.Name, m.Category, 
                m.DocumentTopicMetadatum.Select(n => new TopicMetadata(n.Id, n.Path))))
            .FirstAsync(cancellationToken: ct);

        return await Results<GetTopicResult>.SuccessAsync(result);
    }
}