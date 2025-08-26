namespace Document.Intelligence.Agent.Features.Chat.Models;

public class ChatRequest
{
    /// <summary>
    /// 선택된 AgentId
    /// </summary>
    public Guid? AgentId { get; set; }
    /// <summary>
    /// 서버측에서 최초 발행한 THREAD GUID
    /// </summary>
    public Guid? ThreadId { get; set; }
    /// <summary>
    /// 현재 사용자 질의
    /// </summary>
    public string CurrentQuestion { get; set; }
    /// <summary>
    /// 이전 질의 ID
    /// </summary>
    public Guid? PreviousQuestionId { get; set; } 
}