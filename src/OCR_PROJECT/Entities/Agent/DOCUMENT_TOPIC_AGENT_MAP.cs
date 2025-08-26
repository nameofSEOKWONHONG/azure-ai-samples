namespace Document.Intelligence.Agent.Entities.Agent;

/// <summary>
/// AGENT, TOPIC 맵핑 테이블
/// AGENT(1) - TOPIC(N) 관계
/// </summary>
public class DOCUMENT_TOPIC_AGENT_MAP: DOCUMENT_ENTITY_BASE
{
    public Guid AgentId { get; set; }
    public virtual DOCUMENT_AGENT Agent { get; set; }

    public Guid TopicId { get; set; }
    public virtual DOCUMENT_TOPIC Topic { get; set; }

    // 선택: 할당 메타데이터
    public bool IsEnabled { get; set; } = true;
}