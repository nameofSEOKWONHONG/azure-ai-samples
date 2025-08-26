using Azure.Messaging.ServiceBus;
using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Entities.Agent;
using Document.Intelligence.Agent.Features.Topic.Models;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Topic.Services;

public record AddTopicMetadataRequest(Guid TopicId, string TopicName,  ObjectItem[] ObjectItems);
public interface IAddTopicMetadataService : IDiaExecuteServiceBase<AddTopicMetadataRequest, Results<bool>>;
public class AddTopicMetadataService: DiaExecuteServiceBase<AddTopicMetadataService, DiaDbContext, AddTopicMetadataRequest, Results<bool>>,
    IAddTopicMetadataService
{
    private readonly ServiceBusSender _sender;

    public AddTopicMetadataService(ILogger<AddTopicMetadataService> logger, IDiaSessionContext session, DiaDbContext dbContext, ServiceBusSender sender) : base(logger, session, dbContext)
    {
        _sender = sender;
    }

    public override async Task<Results<bool>> ExecuteAsync(AddTopicMetadataRequest request, CancellationToken ct = default)
    {
        var messages = new List<ServiceBusMessage>();
        var metadatas = new List<DOCUMENT_TOPIC_METADATA>();
        foreach (var item in request.ObjectItems)
        {
            if (item.IsFolder)
            {
                //TODO: 그래프 API 조회
            }
            else
            {
                var metadata = new DOCUMENT_TOPIC_METADATA()
                {
                    DocumentTopicId = request.TopicId,

                    Id = Guid.NewGuid(),
                    DriveId = item.DriveId,
                    ItemId = item.ItemId,
                    Path = item.Path,
                    PathHash = item.Path.xGetHashCode(),
                
                    Status = TopicMetadataStatus.READY,
                    CreatedId = session.UserId,
                    CreatedAt = session.GetNow()
                };
                metadatas.Add(metadata);
            }
            
                
            var sendItem = new TopicMetadataProcessItem(request.TopicId.ToString(), request.TopicName, metadata.Id.ToString(), item.Path, item.DriveId, item.ItemId, item.IsFolder, false);
            var sessionId = $"{item.Path}:{item.DriveId}:{item.ItemId}".xGetHashCode();
            var message = new ServiceBusMessage( BinaryData.FromObjectAsJson(sendItem))
            {
                SessionId = sessionId,
                MessageId = UlidGenerator.Instance.GenerateString()
            }; 
            messages.Add(message);
            
        }
        
        await dbContext.TopicMetadatum.AddRangeAsync(metadatas, ct);
        await dbContext.SaveChangesAsync(ct);
        
        await _sender.SendMessagesAsync(messages, ct);

        return await Results<bool>.SuccessAsync(true);
    }
}