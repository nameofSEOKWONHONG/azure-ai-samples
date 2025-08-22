namespace Document.Intelligence.Agent.Worker;

public class DocumentIndexingWorker : BackgroundService
{
    private readonly ILogger<DocumentIndexingWorker> _logger;

    public DocumentIndexingWorker(ILogger<DocumentIndexingWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}