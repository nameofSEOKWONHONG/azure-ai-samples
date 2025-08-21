namespace Document.Intelligence.Agent.Features.Document.Models;

public class ChatRequest
{
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