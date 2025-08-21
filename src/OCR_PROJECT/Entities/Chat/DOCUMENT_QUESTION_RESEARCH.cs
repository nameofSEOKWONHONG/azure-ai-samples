using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities;

/// <summary>
/// 응답에 대한 AI SEARCH 결과
/// </summary>
public class DOCUMENT_QUESTION_RESEARCH
{
    public Guid QuestionId { get; set; }
    
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual DOCUMENT_QUESTION Question { get; set; }
    
    /// <summary>
    /// KEY
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// 문서 ID
    /// </summary>
    public string ChunkId { get; set; }
    /// <summary>
    /// 문서 CONTENT
    /// </summary>
    public string Content { get; set; }
    /// <summary>
    /// 문서의 년도
    /// </summary>
    public int? Year { get; set; }
    /// <summary>
    /// BLOB 파일 경로
    /// </summary>
    public string SourceFileName { get; set; }
}

public class DocumentQuestionResearchEntityConfiguration : IEntityTypeConfiguration<DOCUMENT_QUESTION_RESEARCH>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_QUESTION_RESEARCH> builder)
    {
        builder.ToTable($"{nameof(DOCUMENT_QUESTION_RESEARCH)}", "dbo");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        builder.Property(m => m.ChunkId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.HasOne(m => m.Question)
            .WithMany(m => m.QuestionSearches)
            .HasForeignKey(m => m.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
