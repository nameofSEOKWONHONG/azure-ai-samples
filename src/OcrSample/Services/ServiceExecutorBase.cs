namespace OcrSample.Services;

public abstract class ServiceExecutorBase<TRequest, TResult>
{
    public TRequest Request { get; set; }
    public TResult Result { get; }
    
    protected ServiceExecutorBase()
    {
        
    }

    protected abstract Task PreExecuteAsync();
    protected abstract Task ExecuteAsync();
    protected abstract Task PostExecuteAsync();
}