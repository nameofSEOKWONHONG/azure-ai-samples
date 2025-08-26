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

    public virtual ICollection<DOCUMENT_AGENT_PROMPT> DocumentAgentPrompts { get; set; } =
        new List<DOCUMENT_AGENT_PROMPT>();
    
    public virtual ICollection<DOCUMENT_TOPIC> DocumentAgentTopics { get; set; } = new List<DOCUMENT_TOPIC>();
}

public class DocumentAgentEntityConfiguration : IEntityTypeConfiguration<DOCUMENT_AGENT>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_AGENT> builder)
    {
        builder.ToTable(nameof(DOCUMENT_AGENT), "dbo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        builder.HasMany(m => m.DocumentAgentTopics)
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