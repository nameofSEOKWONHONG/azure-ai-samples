using Microsoft.Extensions.Hosting;

namespace Document.Intelligence.Agent.MessageQueue;

/// <summary>
/// DLQ (오류 처리 로그용)
/// </summary>
public class DlqWorker : BackgroundService
{
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return base.StopAsync(cancellationToken);
    }
}