using Azure.Messaging.ServiceBus;
using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Features.Topic.Models;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Topic.Services;

public record RemoveTopicMetadataRequest(Guid TopicId, Guid MetadataId);

public interface IRemoveTopicJobService : IDiaExecuteServiceBase<RemoveTopicMetadataRequest, Results<bool>>;

public class RemoveTopicJobService : DiaExecuteServiceBase<RemoveTopicJobService, DiaDbContext, RemoveTopicMetadataRequest, Results<bool>>, IRemoveTopicJobService
{
    private readonly ServiceBusSender _sender;

    public RemoveTopicJobService(ILogger<RemoveTopicJobService> logger, IDiaSessionContext session, DiaDbContext dbContext, ServiceBusSender sender) : base(logger, session, dbContext)
    {
        _sender = sender;
    }

    public override async Task<Results<bool>> ExecuteAsync(RemoveTopicMetadataRequest request, CancellationToken ct = default)
    {
        var exists = await this.dbContext.TopicMetadatum.FirstAsync(m =>
            m.Id == request.MetadataId && m.TopicId == request.TopicId, cancellationToken: ct);

        exists.IsDelete = true;
        exists.ModifiedId = this.session.UserId;
        exists.ModifiedAt = this.session.GetNow();

        this.dbContext.TopicMetadatum.Update(exists);
        await this.dbContext.SaveChangesAsync(ct);
        
        var sendItem = new TopicMetadataProcessItem(exists.TopicId, string.Empty, exists.JobId.GetValueOrDefault(), exists.Id,
            exists.Path, exists.DriveId, exists.ItemId, false, true, session.UserId);
        var sessionId = $"{exists.Path}:{exists.DriveId}:{exists.ItemId}".xGetHashCode();
        var message = new ServiceBusMessage( BinaryData.FromObjectAsJson(sendItem))
        {
            SessionId = sessionId,
            MessageId = UlidGenerator.Instance.GenerateString()
        };
        await _sender.SendMessageAsync(message, ct);

        return await Results<bool>.SuccessAsync(true);
    }
}