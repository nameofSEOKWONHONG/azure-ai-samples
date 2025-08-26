using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities.Agent;

/*
 * USER(1) - AGENT(N) = AGENT_USER_MAP
 * AGENT(1) - TOPIC(N) = AGENT_TOPIC_MAP
 */

/// <summary>
/// 사용자에게 할당될 에이전트 테이블
/// TODO: 사용자별 AGENT 맵핑 테이블 필요함.
/// </summary>
public class DOCUMENT_AGENT : DOCUMENT_ENTITY_BASE
{
    public Guid Id { get; set; }
    /// <summary>
    /// 에이전트명
    /// </summary>
    public string Name { get; set; }
    public string Description { get; set; }
    
    /*
     * 정확하고 일관된 답: Temp=0, TopP=1
     * 자연스러운 대화, 균형: Temp≈0.7, TopP≈0.9
     * 창의적 글쓰기, 아이디어 발산: Temp=1.0 이상, TopP=0.8~0.95
     */

    /// <summary>
    /// 최대 출력 토큰값
    /// </summary>
    public int MaxOutputTokens { get; set; }
    /// <summary>
    /// 모델이 출력 확률 분포를 얼마나 평탄하게 만들지 조절하는 값 (0 ~ 2)
    /// Temperature = 0: 결정적(Deterministic) → 항상 확률이 가장 높은 토큰만 선택 (출력이 반복적이고 안정적임).
    /// Temperature ↑ (예: 1.0 이상): 확률이 낮은 토큰도 선택될 가능성이 커져 창의적이고 다양성 높은 응답이 생성됨.
    /// </summary>
    public float Temperature { get; set; } = 0.0f;

    /// <summary>
    /// 확률 누적분포(cumulative probability)를 기준으로 상위 후보군만 남기고 선택(0 ~ 1)
    /// </summary>
    public float TopP { get; set; } = 0.2f;

    public virtual ICollection<DOCUMENT_AGENT_PROMPT> AgentPrompts { get; set; } =
        new List<DOCUMENT_AGENT_PROMPT>();
    
    public virtual ICollection<DOCUMENT_TOPIC> AgentTopics { get; set; } = new List<DOCUMENT_TOPIC>();
}

public class DocumentAgentEntityConfiguration : IEntityTypeConfiguration<DOCUMENT_AGENT>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_AGENT> builder)
    {
        builder.ToTable(nameof(DOCUMENT_AGENT), "dbo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        builder.HasMany(m => m.AgentTopics)
            .WithMany()
            .UsingEntity<DOCUMENT_TOPIC_AGENT_MAP>(
                m => 
                    m.HasOne(n => n.Topic)
                        .WithMany()
                        .HasForeignKey(n => n.TopicId)
                        .OnDelete(DeleteBehavior.Cascade),
                m => m.HasOne(n => n.Agent)
                    .WithMany()
                    .HasForeignKey(n => n.AgentId)
                    .OnDelete(DeleteBehavior.Cascade),
                m =>
                {
                    m.ToTable(nameof(DOCUMENT_TOPIC_AGENT_MAP), "dbo");
                    m.HasKey(n => new {n.AgentId, n.TopicId});
                    m.ToTable(nameof(DOCUMENT_TOPIC_AGENT_MAP), "dbo");
                    m.HasIndex(n => n.IsEnabled);
                    m.HasIndex(n => n.AgentId);
                    m.HasIndex(n => n.TopicId).IsUnique();
                }   
            );
    }
}