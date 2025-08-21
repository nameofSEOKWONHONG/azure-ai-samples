using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities;

/// <summary>
/// 사용자에게 할당될 에이전트 테이블
/// </summary>
public class DOCUMENT_AGENT : DOCUMENT_ENTITY_BASE
{
    public Guid Id { get; set; }
    /// <summary>
    /// 에이전트명
    /// </summary>
    public string Name { get; set; }
    
    public virtual ICollection<DOCUMENT_AGENT_TOPIC> DocumentAgentTopics { get; set; } = new List<DOCUMENT_AGENT_TOPIC>();
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
            .UsingEntity<DOCUMENT_AGENT_TOPIC_MAP>(
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
                    m.HasKey(n => new {n.AgentId, n.TopicId});
                    m.ToTable(nameof(DOCUMENT_AGENT_TOPIC_MAP), "dbo");
                    m.HasIndex(n => n.IsEnabled);
                    m.HasIndex(n => n.AgentId);
                    m.HasIndex(n => n.TopicId).IsUnique();
                }   
            );
    }
}