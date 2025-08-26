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
    private readonly IAddTopicJobService _addTopicJobService;

    public CreateTopicService(ILogger<CreateTopicService> logger, IDiaSessionContext session, DiaDbContext dbContext, IAddTopicJobService addTopicJobService) 
        : base(logger, session, dbContext)
    {
        _addTopicJobService = addTopicJobService;
    }

    public override async Task<Results<Guid>> ExecuteAsync(CreateTopicRequest request, CancellationToken ct = default)
    {
        //TODO: CREATE OR MODIFY TOPIC DB
        var exists = await dbContext.Topics.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken: ct);
        if (exists.xIsEmpty())
        {
            exists = new DOCUMENT_TOPIC()
            {
                Id = request.Id.xIsNotEmpty() ? request.Id.Value : Guid.NewGuid(),
                Name = request.TopicName,
                Category = request.Category,
                CreatedId = session.UserId,
                CreatedAt = session.GetNow()
            };
            await dbContext.Topics.AddAsync(exists, ct);
            await dbContext.SaveChangesAsync(ct);
        }
        
        await _addTopicJobService.ExecuteAsync(new AddTopicMetadataRequest(exists.Id, exists.Name, request.ObjectItems), ct);
        return await Results<Guid>.SuccessAsync(exists.Id);
    }
}