using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities;

/// <summary>
/// 파일 추출 관련 로깅 테이블
/// </summary>
public class DOCUMENT_AGENT_TOPIC_RESOURCE_LOG : DOCUMENT_ENTITY_BASE
{
    /// <summary>
    /// 메타데이터 추출시 기록을 위한 FK
    /// </summary>
    public Guid TopicMetadataId { get; set; }
    public DOCUMENT_AGENT_TOPIC_METADATA DocumentAgentTopicMetadata { get; set; }
    
    public int Id { get; set; }
    public string Message { get; set; }
}

public class DocumentAgentTopicResourceLogEntityConfiguration : IEntityTypeConfiguration<DOCUMENT_AGENT_TOPIC_RESOURCE_LOG>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_AGENT_TOPIC_RESOURCE_LOG> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message)
            .HasMaxLength(2000)
            .IsRequired();
    }
}