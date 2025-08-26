using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities.Agent;

/// <summary>
/// DOCUMENT_AGENT_TOPIC SOURCE에 기록된 내역을 바탕으로 GRAPH API 연동 결과를 메타데이터화하여 기록한다.
/// 메타데이터는 AI SEARCH INDEX 업로드 한다.
/// 해당 테이블은 내역을 추적하여 갱신하기 위함이다.
/// </summary>
public class DOCUMENT_TOPIC_METADATA : DOCUMENT_ENTITY_BASE
{
    public Guid TopicId { get; set; }
    public virtual DOCUMENT_TOPIC Topic { get; set; }
    
    public Guid Id { get; set; }
    
    public string SiteId { get; set; }
    public string DriveId { get; set; }
    public string ItemId { get; set; }
    
    /// <summary>
    /// /drive/root:/HR/2025/복지.pdf
    /// </summary>
    public string Path { get; set; }
    /// <summary>
    /// 비교를 위한 HASH
    /// </summary>
    public string PathHash { get; set; }
    /// <summary>
    /// 작성자
    /// </summary>
    public string Creator { get; set; }
    /// <summary>
    /// 작성일
    /// </summary>
    public DateTime? CreatedDate { get; set; }
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
    
    /// <summary>
    /// 처리 상태 (대기, 처리중, 완료, 에러)
    /// <see cref="TopicMetadataStatus"/>
    /// </summary>
    public string Status { get; set; }
    /// <summary>
    /// 메세지
    /// </summary>
    public string Message { get; set; }
    /// <summary>
    /// 상태별 사유 (에러 메세지)
    /// </summary>
    public string Reason { get; set; }
    
    /// <summary>
    /// index upload된 DOC_ID 
    /// </summary>
    public string IndexDocId { get; set; }

    /// <summary>
    /// 삭제 여부
    /// </summary>
    public bool IsDelete { get; set; }
    
    public Guid? JobId { get; set; }
}

public class DocumentTopicMetadataEntityConfiguration : IEntityTypeConfiguration<DOCUMENT_TOPIC_METADATA>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_TOPIC_METADATA> builder)
    {
        builder.ToTable(nameof(DOCUMENT_TOPIC_METADATA), "dbo");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SiteId).HasMaxLength(200);
        builder.Property(x => x.DriveId).HasMaxLength(200);
        builder.Property(x => x.ItemId).HasMaxLength(200);

        builder.Property(x => x.Path).HasMaxLength(1000);

        builder.Property(x => x.PathHash).HasMaxLength(200);

        builder.Property(x => x.Creator).HasMaxLength(200);
        builder.Property(x => x.Modifier).HasMaxLength(200);   
        
        builder.Property(x => x.JobId)
            .HasColumnName("LastJobId")
            .IsRequired(false);        
        
        builder.HasOne(x => x.Topic)
            .WithMany(x => x.Metadatas)
            .HasForeignKey(x => x.TopicId)
            .OnDelete(DeleteBehavior.Cascade);        
    }
}

public class TopicMetadataStatus
{
    /// <summary>
    /// 대기
    /// </summary>
    public const string READY = nameof(READY);
    
    /// <summary>
    /// 처리중
    /// </summary>
    public const string PROCESSING = nameof(PROCESSING);
    
    /// <summary>
    /// 완료
    /// </summary>
    public const string COMPLETE = nameof(COMPLETE);
    
    /// <summary>
    /// 에러 - 에러에 포함된 MQ는 DROP 대상이며 재실행되지 않는다.
    /// </summary>
    public const string ERROR = nameof(ERROR);

    /// <summary>
    /// 삭제
    /// </summary>
    public const string REMOVE = nameof(REMOVE);
}