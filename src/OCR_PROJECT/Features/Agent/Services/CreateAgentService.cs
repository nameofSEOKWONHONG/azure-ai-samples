namespace Document.Intelligence.Agent.Features.Agent.Services;

public interface ICreateAgentService
{
    Task<string> Sample();
}
/// <summary>
/// Agent 생성
/// </summary>
public class CreateAgentService : ICreateAgentService
{
    public async Task<string> Sample()
    {
        await Task.Delay(1000);
        return "sample";
    }
}