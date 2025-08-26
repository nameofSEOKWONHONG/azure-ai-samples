using Azure.Messaging.ServiceBus;
using Document.Intelligence.Agent.Features.Topic.Models;
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

        try
        {
            var received = msg.Body.ToObjectFromJson<TopicMetadataProcessItem>();
            _logger.LogInformation("TopicId:{TopicId}, TopicName:{topicName}, MetadataId:{metadataId}, DriveId:{driveId}, ItemId:{itemId}, IsFolder:{isFolder}, IsDelete:{isDelete}", 
                received.TopicId, received.TopicName, received.MetadataId, received.DriveId, received.ItemId, received.IsFolder, received.IsDelete);
            
            // simulation code.
            await Task.Delay((1000 * 60) * 3);

            if (received.IsDelete)
            {
                await Remove();
            }
            else
            {
                await Save();    
            }
            
            await arg.CompleteMessageAsync(msg);
        }
        catch (Exception e)
        {
            await arg.AbandonMessageAsync(msg);
            //write log
        }
    }

    private Task Save()
    {
        //TODO: WRITE LOGIC
        // 1. 파일 다운로드
        // 2. drm 해제
        // 3. 문자 추출
        // 4. indexing 형태에 따라 가공
        // 5. index 업로드
        // 6. DB에 index id 기록
        
        return Task.CompletedTask;
    }

    private Task Remove()
    {
        //TODO: WRITE LOGIC
        // 1. DB 조회
        // 2. INDEX 삭제
        // 3. DB 플래그 처리해야 겠네... TOPIC 할당시 조회는 삭제 안보여줌. 로그에서는 보여줌.
        return Task.CompletedTask;
    }

    private Task OnErrorAsync(ProcessErrorEventArgs arg)
    {
        //TODO: WRITE SYSTEM LOG
        _logger.LogError(arg.Exception, "Processor error. Entity={Entity} Source={Source}", arg.EntityPath, arg.ErrorSource);
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