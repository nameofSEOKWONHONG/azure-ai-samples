using Azure.AI.OpenAI;
using eXtensionSharp;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace OcrSample.Services.Documents;

public interface IDocumentLlmService
{
    Task<string> DocumentAskResultAsync(List<DocumentSearchResult> results, string question);
    Task<string> DocumentSummary(DocumentSearchResult result);
}

public class DocumentLlmService : IDocumentLlmService
{
    private readonly AzureOpenAIClient _client;
    private readonly IConfiguration _configuration;
    private readonly List<ChatMessage> _chatMessages;
    private readonly string _system = @"
                                       역할: 너는 문서 분석기야.
                                       출력 형식: 자연어 형식

                                       목표:
                                       - 첨부된 자료를 활용하여 사용자 잘의에 응답한다.
                                       - 출력은 질의에 관한 사항만 한다.
                                       - 첨부 자료를 활용하여 답변한다.
                                       ";

    public DocumentLlmService(AzureOpenAIClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
        _chatMessages = new List<ChatMessage>()
        {
            new SystemChatMessage(_system),
        };
    }

    public async Task<string> DocumentSummary(DocumentSearchResult result)
    {
        ChatClient chatClient = _client.GetChatClient(_configuration["AZURE_OPENAI_GPT_NAME"]);
        var chatMessages = new List<ChatMessage>()
        {
            new SystemChatMessage("너는 문서 요약 전문가다. 질의하는 문서를 요약하자."),
            new UserChatMessage($"첨부된 문서를 요약해줘. - 첨부문서: {result.xSerialize()}")
        };
        var chatOptions = new ChatCompletionOptions()
        {
            MaxOutputTokenCount = 4096,
            Temperature = 0.0f,
            TopP = 1.0f,
        };  
        var response = await chatClient.CompleteChatAsync(chatMessages, chatOptions);
        var chatMessage = response.Value; // Correctly get the ChatMessage from response.Value
        return chatMessage.Content[0].Text;
    }

    public async Task<string> DocumentAskResultAsync(List<DocumentSearchResult> result, string question)
    {
        ChatClient chatClient = _client.GetChatClient(_configuration["AZURE_OPENAI_GPT_NAME"]);
        var o = new
        {
            첨부자료 = result.Select(m => m.ToString()),
            사용자질의 = question
        };
        
        _chatMessages.Add(new UserChatMessage(o.xSerialize()));
        var chatOptions = new ChatCompletionOptions()
        {
            MaxOutputTokenCount = 4096,
            Temperature = 0.0f,
            TopP = 1.0f,
        };  
        var response = await chatClient.CompleteChatAsync(_chatMessages, chatOptions);
        var chatMessage = response.Value; // Correctly get the ChatMessage from response.Value
        _chatMessages.Add(new AssistantChatMessage(chatMessage.Content[0].Text));
        return chatMessage.Content[0].Text;        
    }
}