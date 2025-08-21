using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities;

public class DOCUMENT_ANSWER
{
    public Guid QuestionId { get; set; }
    public virtual DOCUMENT_QUESTION Question { get; set; }
    
    public Guid Id { get; set; }
    /// <summary>
    /// LLM 응답
    /// </summary>
    public string Answer { get; set; }
    /// <summary>
    /// 응답에 대한 근거
    /// </summary>
    public virtual ICollection<DOCUMENT_CITATION> Citations { get; set; }
}

public class DocumentAnswerEntityConfiguration: IEntityTypeConfiguration<DOCUMENT_ANSWER>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_ANSWER> builder)
    {
        builder.ToTable($"{nameof(DOCUMENT_ANSWER)}", "dbo");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();
        
        builder.HasOne(m => m.Question)
            .WithMany(m => m.Answers)
            .HasForeignKey(m => m.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

