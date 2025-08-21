using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Features.Document.Models;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Document.Chat;

public interface IQuestionContextSwitchService : IDiaExecuteServiceBase<SearchDocumentContextSwitchRequest, bool>;

/// <summary>
/// 문맥 전환 체크 서비스
/// </summary>
public class QuestionContextSwitchService: DiaExecuteServiceBase<QuestionContextSwitchService, DiaDbContext, SearchDocumentContextSwitchRequest, bool>,
    IQuestionContextSwitchService
{
    private readonly IChatClient _chatClient;

    public QuestionContextSwitchService(ILogger<QuestionContextSwitchService> logger, 
        IDiaSessionContext session, DiaDbContext dbContext,
        IChatClient chatClient) : base(logger, session, dbContext)
    {
        _chatClient = chatClient;
    }

    public override async Task<bool> ExecuteAsync(SearchDocumentContextSwitchRequest request)
    {
        var previousEntities = ExtractEntities(request.PreviousQuestion);
        var currentEntities = ExtractEntities(request.CurrentQuestion);
        var extractRequest = new ExtractRequest(request.PreviousQuestionVector,
            request.CurrentQuestionVector,
            previousEntities,
            currentEntities, 
            request.PreviousPlanJson.xDeserialize<QueryPlan>(),
            request.CurrentPlanJson.xDeserialize<QueryPlan>(), 
            request.PreviousChunkIds, 
            request.CurrentChunkIds, 
            IsNewTopicHeuristic(request.CurrentQuestion));
        var contextMetrics = extractRequest.Extract();
        var messages = new List<ChatMessage>()
        {
            new ChatMessage(ChatRole.System, LlmConst.QUESTION_CONTEXT_SWITCH_PROMPT),
            new ChatMessage(ChatRole.User, contextMetrics.xSerialize())
        };
        var resp = await _chatClient.GetResponseAsync<ShiftResult>(messages);
        return resp.Result.IsShift;
    }
    
    private static bool IsNewTopicHeuristic(string text)
    {
        // 한국어 트리거 휴리스틱
        string[] triggers = { "딴 얘기", "주제 바꿔", "그건 됐고", "전혀 다른", "이제 " };
        if (string.IsNullOrWhiteSpace(text)) return false;
        return triggers.Any(t => text.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0);
    }
    
    private static string[] ExtractEntities(string text)
    {
        var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "이게","그게","그리고","그러면","어떻게","에는","에서","으로","하는","하다","처럼","등","또","및" };
        return System.Text.RegularExpressions.Regex.Matches(text ?? "", @"[A-Za-z가-힣0-9_]{2,}")
            .Select(m => m.Value)
            .Where(w => !stop.Contains(w))
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .Take(20)
            .ToArray();
    }
    
    private class ShiftResult
    {
        public bool IsShift { get; set; }
        public float Confidence { get; set; }
        public string Reason { get; set; }
        public string Type { get; set; }
    }
}