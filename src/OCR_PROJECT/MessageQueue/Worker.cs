using Azure.Messaging.ServiceBus;
using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Features.Topic.Models;
using Document.Intelligence.Agent.MessageQueue.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Document.Intelligence.Agent.MessageQueue;

/// <summary>
/// 메인
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<DlqWorker> _logger;
    private readonly ServiceBusClient _client;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ServiceBusOptions _opt;
    
    private ServiceBusSessionProcessor _sessionProcessor;

    public Worker(ILogger<DlqWorker> logger, ServiceBusClient client, IOptions<ServiceBusOptions> opt, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _client = client;
        _serviceScopeFactory = serviceScopeFactory;
        _opt = opt.Value;
    }
    
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Main worker starting. Queue={Queue}", _opt.QUEUE_NAME);

        _sessionProcessor = _client.CreateSessionProcessor(_opt.QUEUE_NAME, new ServiceBusSessionProcessorOptions
        {
            PrefetchCount = _opt.PREFETCH_COUNT,
            MaxConcurrentSessions = Math.Max(1, _opt.MAX_CONCURRENT_CALLS / 2),
            MaxConcurrentCallsPerSession = 1, // 세션 내 순서 보장
            AutoCompleteMessages = false,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(_opt.MAX_AUTO_LOCK_RENEWAL_MINUTES)
        });

        _sessionProcessor.ProcessMessageAsync += OnSessionMessageAsync;
        _sessionProcessor.ProcessErrorAsync += OnErrorAsync;

        await _sessionProcessor.StartProcessingAsync(cancellationToken);

        await base.StartAsync(cancellationToken);
    }
    
    private async Task OnSessionMessageAsync(ProcessSessionMessageEventArgs arg)
    {
        var msg = arg.Message;

        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var saveJobService = scope.ServiceProvider.GetRequiredService<ISaveMqJobService>();
        var removeJobService = scope.ServiceProvider.GetRequiredService<IRemoveMqJobService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<DiaDbContext>();
        try
        {
            var received = msg.Body.ToObjectFromJson<TopicMetadataProcessItem>();
            _logger.LogInformation("Processing - TopicId:{TopicId}, TopicName:{topicName}, JobId:{jobId}, DriveId:{driveId}, ItemId:{itemId}, IsFolder:{isFolder}, IsDelete:{isDelete}", 
                received.TopicId, received.TopicName, received.JobId, received.DriveId, received.ItemId, received.IsFolder, received.IsDelete);
            
            // simulation code.
            await Task.Delay((1000 * 60) * 3);

            var result = false;
            if (!received.IsDelete)
            {
                result = await saveJobService.ExecuteAsync(received, CancellationToken.None);
            }
            else
            {
                result = await removeJobService.ExecuteAsync(received, CancellationToken.None);
            }
            
            _logger.LogInformation("Processed - TopicId: {topicId}, TopicName: {topicName}, JobId: {jobId}, Statue: {status}", received.TopicId, received.TopicName, received.JobId, result);
            await arg.CompleteMessageAsync(msg);
        }
        catch (Exception e)
        {
            await arg.AbandonMessageAsync(msg);
            //write log
            _logger.LogError(e, "Error: {error}", e.Message);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs arg)
    {
        //TODO: WRITE SYSTEM LOG
        
        _logger.LogError(arg.Exception, "Processor error. Identifier={Identifier} Entity={Entity} Source={Source}", arg.Identifier, arg.EntityPath, arg.ErrorSource);
        return Task.CompletedTask;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    public override async Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("Main worker stopping...");
        if (_sessionProcessor is not null)
        {
            await _sessionProcessor.StopProcessingAsync(ct);
            await _sessionProcessor.DisposeAsync();
        }
        await base.StopAsync(ct);
    }

    public override void Dispose()
    {
        if (_sessionProcessor is not null)
        {
            _sessionProcessor.ProcessMessageAsync -= OnSessionMessageAsync;
            _sessionProcessor.ProcessErrorAsync -= OnErrorAsync;
        }
    }
}