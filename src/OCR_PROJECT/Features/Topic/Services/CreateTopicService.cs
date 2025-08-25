using Azure.Messaging.ServiceBus;
using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Entities.Agent;
using Document.Intelligence.Agent.Features.Topic.Models;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Topic.Services;

public interface ICreateTopicService : IDiaExecuteServiceBase<CreateTopicRequest, Results<Guid>>;

public class CreateTopicService : DiaExecuteServiceBase<CreateTopicService, DiaDbContext, CreateTopicRequest, Results<Guid>>, ICreateTopicService
{
    private readonly ServiceBusSender _sender;

    public CreateTopicService(ILogger<CreateTopicService> logger, IDiaSessionContext session, DiaDbContext dbContext, ServiceBusSender sender) 
        : base(logger, session, dbContext)
    {
        _sender = sender;
    }

    public override async Task<Results<Guid>> ExecuteAsync(CreateTopicRequest request, CancellationToken ct = default)
    {
        //TODO: CREATE OR MODIFY TOPIC DB
        var exists = await dbContext.AgentTopics.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken: ct);
        if (exists.xIsEmpty())
        {
            exists = new DOCUMENT_AGENT_TOPIC()
            {
                Id = request.Id.xIsNotEmpty() ? request.Id.Value : Guid.NewGuid(),
                Name = request.TopicName,
                Category = request.Category,
                CreatedId = session.UserId.xToGuid(),
                CreatedAt = session.GetNow()
            };
            await dbContext.AgentTopics.AddAsync(exists, ct);
            await dbContext.SaveChangesAsync(ct);
        }
        
        //TODO: SEND MQ, 건별로 처리함.
        var messages = new List<ServiceBusMessage>();
        var metadatas = new List<DOCUMENT_AGENT_TOPIC_METADATA>();
        foreach (var item in request.ObjectItems)
        {
            var sendItem = new MqObjectItem(exists.Id.ToString(), request.TopicName, exists.Id.ToString(), item.Path, item.DriveId, item.ItemId, item.IsFolder);
            var sessionId = $"{item.Path}:{item.DriveId}:{item.ItemId}".xGetHashCode();
            var message = new ServiceBusMessage( BinaryData.FromObjectAsJson(sendItem))
            {
                SessionId = sessionId,
                MessageId = UlidGenerator.Instance.GenerateString()
            }; 
            messages.Add(message);
            
            metadatas.Add(new DOCUMENT_AGENT_TOPIC_METADATA()
            {
                DocumentAgentTopicId = exists.Id,
                
                Id = Guid.NewGuid(),
                DriveId = item.DriveId,
                ItemId = item.ItemId,
                Path = item.Path,
                PathHash = item.Path.xGetHashCode(),
                Status = AgentTopicMetadataStatus.READY,
                CreatedId = session.UserId.xToGuid(),
                CreatedAt = session.GetNow()
            });
        }
        
        await dbContext.AgentTopicMetadatas.AddRangeAsync(metadatas, ct);
        await dbContext.SaveChangesAsync(ct);
        
        await _sender.SendMessagesAsync(messages, ct);
        return await Results<Guid>.SuccessAsync(exists.Id);
    }
}