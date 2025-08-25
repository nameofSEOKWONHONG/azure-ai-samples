using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Topic.Services;

public interface IRemoveTopicService : IDiaExecuteServiceBase<Guid, Results<bool>>;

public class RemoveTopicService: DiaExecuteServiceBase<RemoveTopicService, DiaDbContext, Guid, Results<bool>>,
    IRemoveTopicService
{
    public RemoveTopicService(ILogger<RemoveTopicService> logger, IDiaSessionContext session, DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override async Task<Results<bool>> ExecuteAsync(Guid request, CancellationToken ct = default)
    {
        var exists = await this.dbContext.AgentTopics.FirstAsync(m => m.Id == request, cancellationToken: ct);
        if (exists.xIsEmpty()) throw new Exception("Id not exists");

        this.dbContext.Remove(exists);
        await this.dbContext.SaveChangesAsync(ct);

        return await Results<bool>.SuccessAsync(true);
    }
}