using System.ClientModel;
using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

var endpoint = new Uri(builder.Configuration["AZURE_OPENAI_ENDPOINT"]);
var apiKey = new ApiKeyCredential(builder.Configuration["AZURE_OPENAI_API_KEY"]);
var ChatDeployment = builder.Configuration["AZURE_OPENAI_GPT_NAME"];
var embedDeployment = builder.Configuration["AZURE_OPENAI_EMBED_MODEL"];

builder.Services.AddSingleton(
    new ChatCompletionsClient(
        endpoint,
        new AzureKeyCredential(builder.Configuration["AZURE_OPENAI_API_KEY"])));

builder.Services.AddSingleton(
    new AzureOpenAIClient(
        endpoint,
        apiKey));

builder.Services.AddChatClient(services => 
        services.GetRequiredService<AzureOpenAIClient>()
            .GetChatClient(ChatDeployment)
            .AsIChatClient())
    //.UseDistributedCache()
    .UseLogging();

builder.Services.AddEmbeddingGenerator(services =>
    services.GetRequiredService<AzureOpenAIClient>()
        .GetEmbeddingClient(embedDeployment)
        .AsIEmbeddingGenerator());

var host = builder.Build();
var chat = host.Services.GetRequiredService<IChatClient>();
var age = await chat.GetResponseAsync<int>("'겸손은 힘들다' 진행자 김어준의 나이는?");
var embed = host.Services.GetRequiredService<IEmbeddingGenerator<string,Embedding<float>>>();
var vector = await embed.GenerateAsync("test");
Console.WriteLine(age);