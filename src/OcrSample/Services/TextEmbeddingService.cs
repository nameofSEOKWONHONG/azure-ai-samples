using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;

namespace OcrSample.Services;

public interface ITextEmbeddingService
{
    Task<float[]> GetEmbeddedText(string text);
}

public class TextEmbeddingService : ITextEmbeddingService
{
    private readonly AzureOpenAIClient _client;
    private readonly IConfiguration _configuration;

    public TextEmbeddingService(AzureOpenAIClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public async Task<float[]> GetEmbeddedText(string text)
    {
        var client = _client.GetEmbeddingClient(_configuration["EMBED_MODEL"]);
        var res = await client.GenerateEmbeddingAsync(text);
        return res.Value.ToFloats().ToArray();
    }
}