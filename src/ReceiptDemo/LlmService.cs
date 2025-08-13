using System.Text.Json;
using Azure.AI.OpenAI;
using eXtensionSharp;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using ReceiptDemo.Models;

namespace ReceiptDemo;

public interface IReceiptLlmService
{
    Task<QuerySpec> GetQueryFilterAsync(string query);
    Task<string> ReceiptAskResultAsync(List<QueryResult> results, string question);
}

public class ReceiptLlmService: IReceiptLlmService
{
    private readonly AzureOpenAIClient _client;
    private readonly IConfiguration _configuration;
    private readonly List<ChatMessage> _chatMessages;
    private readonly string _system = @"
                                       역할: 너는 첨부된 자료를 이용해 질의의 결과를 출력한다.
                                       출력 형식: 자연어 형식

                                       목표:
                                       - 첨부된 자료를 활용하여 사용자 잘의에 응답한다.
                                       - 출력은 질의에 관한 사항만 한다.
                                       - 첨부 자료를 활용하여 답변한다.
                                       ";
    public ReceiptLlmService(AzureOpenAIClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
        _chatMessages = new List<ChatMessage>()
        {
            new SystemChatMessage(_system),
        };
    }

    public async Task<QuerySpec> GetQueryFilterAsync(string query)
    {
        var receiptSystemPrompt = """
                     역할: 너는 영수증 검색 질의를 구조화하는 파서다.
                     출력 형식: 반드시 JSON "한 줄"로만 출력한다. 설명/자연어/코드블록/주석 금지.
                     
                     목표:
                     - 사용자의 한국어 자연어 질의를 아래 JSON 스키마의 필드에 매핑한다.
                     - 상대 기간(예: 이번 달, 지난주, 7월, 어제)은 Asia/Seoul(UTC+9) 기준으로 해석하여
                       From/To를 UTC ISO-8601(date-time) 문자열로 변환한다.
                       - 기간 해석 규칙:
                         - "이번 달" → [월 1일 00:00:00 KST, 다음 달 1일 00:00:00 KST) → UTC 변환
                         - "지난주" → (월요일 00:00:00 KST, 다음주 월요일 00:00:00 KST) → UTC 변환
                         - "어제" → [어제 00:00:00 KST, 오늘 00:00:00 KST) → UTC 변환
                         - "7월"처럼 연도 미지정 시, 현재 연도(현재 날짜: 2025-07-31, Asia/Seoul)로 해석
                     - 금액 범위가 있으면 MinWon/MaxWon에 원화 금액(정수)을 넣는다.
                     - 특정 상호/지점은 Brand/Branch에 넣는다.
                     - 품목/키워드는 Keywords 배열에 넣는다(중복 제거, 1~5개 권장).
                     - UseHybrid 기본값 true, TopK 기본값 50.
                     - 모호하거나 언급되지 않은 값은 null 또는 기본값 사용.
                     - 허용 필드 외의 임의 필드는 생성하지 않는다.
                     
                     출력 스키마(JSON Schema):
                     {
                       "type": "object",
                       "properties": {
                         "Brand":    { "type": "string", "nullable": true },
                         "Branch":   { "type": "string", "nullable": true },
                         "From":     { "type": "string", "format": "date-time", "nullable": true },
                         "To":       { "type": "string", "format": "date-time", "nullable": true },
                         "MinWon":   { "type": "integer", "nullable": true },
                         "MaxWon":   { "type": "integer", "nullable": true },
                         "Keywords": { "type": "array", "items": { "type": "string" } },
                         "UseHybrid":{ "type": "boolean" },
                         "TopK":     { "type": "integer" }
                       },
                       "required": []
                     }
                     
                     검증 규칙:
                     - From < To 여야 한다(같거나 역순이면 둘 다 null).
                     - MinWon/MaxWon은 0 이상, MinWon <= MaxWon (아니면 둘 중 유효한 것만).
                     - Keywords는 길이 0~5, 각 항목은 1~30자.
                     - 출력은 반드시 위 스키마를 만족하는 JSON 한 줄.
                     
                     지금 날짜/시간 기준: 2025-07-31T00:00:00+09:00 (Asia/Seoul)
                     """;
        
        ChatClient chatClient = _client.GetChatClient(_configuration["AZURE_OPENAI_GPT_NAME"]);
        var messages = new List<ChatMessage>()
        {
            new SystemChatMessage(receiptSystemPrompt),
            new UserChatMessage(query),
        };
        var chatOptions = new ChatCompletionOptions()
        {
            MaxOutputTokenCount = 4096,
            Temperature = 0.0f,
            TopP = 1.0f,
        };

        var response = await chatClient.CompleteChatAsync(messages, chatOptions);
        var chatMessage = response.Value; // Correctly get the ChatMessage from response.Value
        var json = chatMessage.Content[0].Text;
        var spec = JsonSerializer.Deserialize<QuerySpec>(json); // Use System.Text.Json.JsonSerializer
        return spec;
    }

    public async Task<string> ReceiptAskResultAsync(List<QueryResult> results, string question)
    {
        ChatClient chatClient = _client.GetChatClient(_configuration["AZURE_OPENAI_GPT_NAME"]);
        var o = new
        {
            첨부자료 = results.Select(m => m.ToString()).xJoin(","),
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