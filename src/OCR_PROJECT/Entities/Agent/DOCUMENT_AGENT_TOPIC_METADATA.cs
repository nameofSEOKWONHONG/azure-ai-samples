using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities;

/// <summary>
/// DOCUMENT_AGENT_TOPIC SOURCE에 기록된 내역을 바탕으로 GRAPH API 연동 결과를 메타데이터화하여 기록한다.
/// 메타데이터는 AI SEARCH INDEX 업로드 한다.
/// 해당 테이블은 내역을 추적하여 갱신하기 위함이다.
/// </summary>
public class DOCUMENT_AGENT_TOPIC_METADATA : DOCUMENT_ENTITY_BASE
{
    public Guid DocumentAgentTopicId { get; set; }
    public DOCUMENT_AGENT_TOPIC DocumentAgentTopic { get; set; }
    
    public Guid Id { get; set; }
    
    public string SiteId { get; set; }
    public string DriveId { get; set; }
    /// <summary>
    /// 리스트 API 사용 시
    /// </summary>
    public string ListId { get; set; }
    /// <summary>
    /// driveItem id (고유)
    /// </summary>
    public string ItemId { get; set; }
    
    // 부모/경로
    public string ParentDriveId { get; set; }
    public string ParentItemId { get; set; }
    /// <summary>
    /// /drive/root:/HR/2025/복지.pdf
    /// </summary>
    public string Path { get; set; }            
    /// <summary>
    /// /drive/root:/HR/2025
    /// </summary>
    public string ParentReferencePath { get; set; }
    /// <summary>
    /// Path로부터 계산한 깊이
    /// </summary>
    public int Depth { get; set; }
    
    /// <summary>
    /// 파일명
    /// </summary>
    public string FileName { get; set; }
    /// <summary>
    /// 비교를 위한 HASH
    /// </summary>
    public string FileHash { get; set; }
    /// <summary>
    /// 작성자
    /// </summary>
    public string Creator { get; set; }
    /// <summary>
    /// 작성일
    /// </summary>
    public DateTime CreatedDate { get; set; }
    /// <summary>
    /// 수정자
    /// </summary>
    public string Modifier { get; set; }
    /// <summary>
    /// 수정일
    /// </summary>
    public DateTime? ModifiedDate { get; set; }
    /// <summary>
    /// 파일명에서 키워드 추출
    /// </summary>
    public string[] Keyword { get; set; }
    
    public virtual ICollection<DOCUMENT_AGENT_TOPIC_RESOURCE_LOG> DocumentAgentTopicResourceLogs { get; set; }
}

public class DocumentAgentTopicMetadataEntityConfiguration : IEntityTypeConfiguration<DOCUMENT_AGENT_TOPIC_METADATA>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_AGENT_TOPIC_METADATA> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SiteId).HasMaxLength(200);
        builder.Property(x => x.DriveId).HasMaxLength(200);
        builder.Property(x => x.ListId).HasMaxLength(200);
        builder.Property(x => x.ItemId).HasMaxLength(200);

        builder.Property(x => x.Path).HasMaxLength(1000);
        builder.Property(x => x.ParentReferencePath).HasMaxLength(1000);

        builder.Property(x => x.FileName).HasMaxLength(500);
        builder.Property(x => x.FileHash).HasMaxLength(200);

        builder.Property(x => x.Creator).HasMaxLength(200);
        builder.Property(x => x.Modifier).HasMaxLength(200);

        // 관계: Metadata → Log (1:N)
        builder.HasMany(x => x.DocumentAgentTopicResourceLogs)
            .WithOne(x => x.DocumentAgentTopicMetadata)
            .HasForeignKey(x => x.TopicMetadataId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}