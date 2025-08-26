using Azure.Search.Documents;
using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Entities.Agent;
using Document.Intelligence.Agent.Features.Topic.Models;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.MessageQueue.Services;

public interface IRemoveMqJobService : IDiaExecuteServiceBase<TopicMetadataProcessItem, bool>;
public class RemoveMqJobService: DiaExecuteServiceBase<RemoveMqJobService, DiaDbContext, TopicMetadataProcessItem, bool>, IRemoveMqJobService
{
    private readonly SearchClient _client;

    public RemoveMqJobService(ILogger<RemoveMqJobService> logger, IDiaSessionContext session, DiaDbContext dbContext, 
        [FromKeyedServices(INDEX_CONST.DOCUMENT_INDEX)]SearchClient client) : base(logger, session, dbContext)
    {
        _client = client;
    }

    public override async Task<bool> ExecuteAsync(TopicMetadataProcessItem request, CancellationToken ct = default)
    {
        // 1. DB 조회
        // var exists =
        //     await this.dbContext.TopicMetadatum.FirstOrDefaultAsync(m => m.Id == request.MedataId && m.TopicId == request.TopicId,
        //         cancellationToken: ct);
        //
        // Exception exception = null;
        //
        // try
        // {
        //     // 2. INDEX 삭제 - DOC_ID
        //     await _client.DeleteDocumentsAsync(
        //         keyName: "doc_id",
        //         keyValues: [exists.IndexDocId],
        //         cancellationToken: ct);
        //     
        //     // 3. DB 플래그 처리
        //     exists.Status = TopicMetadataStatus.REMOVE;
        //     exists.ModifiedId = request.UserId;
        //     exists.ModifiedAt = session.GetNow();
        //
        //     this.dbContext.Update(exists);
        //     
        //     var existsJob = await dbContext.TopicJobs
        //         .FirstAsync(m => m.Id == request.JobId && m.TopicId == request.TopicId, cancellationToken: ct);
        //     existsJob.RemovedFiles += 1;
        //     dbContext.TopicJobs.Update(existsJob);
        //     
        //     await dbContext.SaveChangesAsync(ct);
        // }
        // catch (Exception e)
        // {
        //     logger.LogError(e, "Error: {error}", e.Message);
        //     exception = e;
        // }
        //
        // if (exception.xIsNotEmpty())
        // {
        //     exists.Status = TopicMetadataStatus.ERROR;
        //     exists.Reason = exception.Message;
        //     exists.ModifiedId = request.UserId;
        //     exists.ModifiedAt = session.GetNow();
        //     this.dbContext.Update(exists);
        //     await this.dbContext.SaveChangesAsync(ct);
        // }

        await Task.Delay(100, ct);
        return true;
    }
}