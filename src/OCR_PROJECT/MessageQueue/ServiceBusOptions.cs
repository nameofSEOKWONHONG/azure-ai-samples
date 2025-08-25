namespace Document.Intelligence.Agent.MessageQueue;

public class ServiceBusOptions
{
    public string CONNECTION_STRING { get; set; }
    public string QUEUE_NAME { get; set; } = "indexing-queue";
    public int PREFETCH_COUNT { get; set; } = 64;
    public int MAX_CONCURRENT_CALLS { get; set; } = 16;
    public int MAX_AUTO_LOCK_RENEWAL_MINUTES { get; set; } = 10;
}