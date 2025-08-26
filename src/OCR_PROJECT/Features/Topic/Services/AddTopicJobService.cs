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
public interface IAddTopicJobService : IDiaExecuteServiceBase<AddTopicMetadataRequest, Results<bool>>;
public class AddTopicJobService: DiaExecuteServiceBase<AddTopicJobService, DiaDbContext, AddTopicMetadataRequest, Results<bool>>,
    IAddTopicJobService
{
    private readonly ServiceBusSender _sender;

    public AddTopicJobService(ILogger<AddTopicJobService> logger, IDiaSessionContext session, DiaDbContext dbContext, ServiceBusSender sender) : base(logger, session, dbContext)
    {
        _sender = sender;
    }

    public override async Task<Results<bool>> ExecuteAsync(AddTopicMetadataRequest request, CancellationToken ct = default)
    {
        var messages = new List<ServiceBusMessage>();
        var job = new DOCUMENT_TOPIC_JOB()
        {
            TopicId = request.TopicId,

            Id = Guid.NewGuid(),
            ProcessedFiles = 0,
            TotalFiles = 0,
            RemovedFiles = 0,
            Status = TopicMetadataStatus.READY
        };
        await this.dbContext.TopicJobs.AddAsync(job, ct);
        await this.dbContext.SaveChangesAsync(ct);
        
        foreach (var item in request.ObjectItems)
        {       
            var sendItem = new TopicMetadataProcessItem(request.TopicId, request.TopicName, job.Id, Guid.Empty, item.Path, item.DriveId, item.ItemId, item.IsFolder, false, session.UserId);
            var sessionId = $"{item.Path}:{item.DriveId}:{item.ItemId}".xGetHashCode();
            var message = new ServiceBusMessage( BinaryData.FromObjectAsJson(sendItem))
            {
                SessionId = sessionId,
                MessageId = UlidGenerator.Instance.GenerateString()
            }; 
            messages.Add(message);
        }
        await _sender.SendMessagesAsync(messages, ct);

        return await Results<bool>.SuccessAsync(true);
    }
}