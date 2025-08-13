using System.ClientModel;
using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

var builder = Host.CreateApplicationBuilder(args);

var endpoint = new Uri("YOUR_ENDPOINT"); // e.g. "https://< your hub name >.openai.azure.com/"
var apiKey = new ApiKeyCredential("YOUR_API_KEY");
var deploymentName = "YOUR_DEPLOYMENT_NAME"; // e.g. "gpt-4o-mini"

builder.Services.AddSingleton(
    new ChatCompletionsClient(
        new("https://models.inference.ai.azure.com"),
        new AzureKeyCredential(Environment.GetEnvironmentVariable("GH_TOKEN")!)));

builder.Services.AddSingleton(
    new AzureOpenAIClient(
        new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")),
        apiKey));

builder.Services.AddChatClient(services => 
        services.GetRequiredService<ChatCompletionsClient>()
        .AsIChatClient("gpt-4o-mini"))
    //.UseDistributedCache()
    .UseLogging();

builder.Services.AddEmbeddingGenerator(services =>
    services.GetRequiredService<AzureOpenAIClient>()
        .GetEmbeddingClient("text-embedding-3-small")
        .AsIEmbeddingGenerator());

var host = builder.Build();
var chat = host.Services.GetRequiredService<IChatClient>();
var age = await chat.GetResponseAsync<int>("'겸손은 힘들다' 진행자 김어준의 나이는?");
var embed = host.Services.GetRequiredService<IEmbeddingGenerator<string,Embedding<float>>>();
var vector = await embed.GenerateAsync("test");
Console.WriteLine(age);