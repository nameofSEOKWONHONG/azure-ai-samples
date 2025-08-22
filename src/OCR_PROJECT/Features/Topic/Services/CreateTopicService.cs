using Document.Intelligence.Agent.Infrastructure.Data;

namespace Document.Intelligence.Agent.Features.Topic.Services;

public interface ICreateTopicService
{
    Task<Results<string>> Sample();
} 

public class CreateTopicService : ICreateTopicService
{
    public async Task<Results<string>> Sample()
    {
        return await Results<string>.SuccessAsync("Hello World");
    }
}