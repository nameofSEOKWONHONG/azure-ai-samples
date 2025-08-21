using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities;

/// <summary>
/// 스캔 및 에이전트를 위한 INDEX 필터 역활 테이블
/// </summary>
public class DOCUMENT_AGENT_TOPIC : DOCUMENT_ENTITY_BASE
{   
    public Guid Id { get; set; }
    /// <summary>
    /// 토픽명
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// 구분명 (인사, 경영, 복지, 보안 등등...), 필터역활
    /// </summary>
    public string Category { get; set; }
    
    /// <summary>
    /// GRAPH API 연동을 위한 폴더 및 파일 기록
    /// </summary>
    public string[] Source { get; set; }
    
    public virtual ICollection<DOCUMENT_AGENT_TOPIC_METADATA> DocumentAgentTopicResources { get; set; }
}

public class DocumentAgentTopicEntityConfiguration: IEntityTypeConfiguration<DOCUMENT_AGENT_TOPIC>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_AGENT_TOPIC> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();

    }
}