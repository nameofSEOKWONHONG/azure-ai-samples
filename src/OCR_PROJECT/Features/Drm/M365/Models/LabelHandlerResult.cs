namespace Document.Intelligence.Agent.Features.Drm.M365.Models;

public class LabelHandlerResult
{
    public bool IsSuccess { get; init; }
    public bool HasProtected { get; init; }
    public string FilePath { get; init; }
    public string Message { get; init; }

    public LabelHandlerResult()
    {
            
    }

    public LabelHandlerResult(bool isSuccess, string message)
    {
        this.IsSuccess = isSuccess;
        Message = message;
    }

    public LabelHandlerResult(bool isSuccess, bool hasProtected, string filePath)
    {
        this.IsSuccess = isSuccess;
        this.HasProtected = hasProtected;
        this.FilePath = filePath;
    }
}