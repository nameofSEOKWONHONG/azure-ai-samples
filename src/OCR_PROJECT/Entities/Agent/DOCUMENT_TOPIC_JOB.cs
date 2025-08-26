using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities.Agent;

public class DOCUMENT_TOPIC_JOB
{
    public Guid TopicId { get; set; }
    public virtual DOCUMENT_TOPIC Topic { get; set; }
    
    public Guid Id { get; set; }

    /// <summary>
    ///  (대기, 처리중, 완료, 실패)
    /// <see cref="TopicMetadataStatus"/>
    /// </summary>
    public string Status { get; set; }   
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int? TotalFiles { get; set; }
    public int? ProcessedFiles { get; set; }
    public int? RemovedFiles { get; set; }
}

public class DocumentTopicJobEntityConfiguration : IEntityTypeConfiguration<DOCUMENT_TOPIC_JOB>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_TOPIC_JOB> builder)
    {
        builder.ToTable(nameof(DOCUMENT_TOPIC_JOB), "dbo");
        builder.HasKey(x => x.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Status).HasMaxLength(50);
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
