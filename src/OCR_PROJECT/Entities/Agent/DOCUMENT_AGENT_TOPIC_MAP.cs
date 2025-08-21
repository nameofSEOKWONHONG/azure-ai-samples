namespace Document.Intelligence.Agent.Entities;

public class DOCUMENT_AGENT_TOPIC_MAP: DOCUMENT_ENTITY_BASE
{
    public Guid AgentId { get; set; }
    public DOCUMENT_AGENT Agent { get; set; }

    public Guid TopicId { get; set; }
    public DOCUMENT_AGENT_TOPIC Topic { get; set; }

    // 선택: 할당 메타데이터
    public bool IsEnabled { get; set; } = true;
}