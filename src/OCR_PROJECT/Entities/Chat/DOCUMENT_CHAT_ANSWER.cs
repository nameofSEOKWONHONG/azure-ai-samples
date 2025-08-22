using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities.Chat;

/// <summary>
/// LLM 응답 기록
/// TODO: 페이징 필수
/// </summary>
public class DOCUMENT_CHAT_ANSWER
{
    public Guid QuestionId { get; set; }
    public virtual DOCUMENT_CHAT_QUESTION ChatQuestion { get; set; }
    
    public Guid Id { get; set; }
    /// <summary>
    /// LLM 응답
    /// </summary>
    public string Answer { get; set; }
    /// <summary>
    /// 응답에 대한 근거
    /// </summary>
    public virtual ICollection<DOCUMENT_CHAT_ANSWER_CITATION> Citations { get; set; }
}

public class DocumentChatAnswerEntityConfiguration: IEntityTypeConfiguration<DOCUMENT_CHAT_ANSWER>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_CHAT_ANSWER> builder)
    {
        builder.ToTable($"{nameof(DOCUMENT_CHAT_ANSWER)}", "dbo");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();
        
        builder.HasOne(m => m.ChatQuestion)
            .WithMany(m => m.Answers)
            .HasForeignKey(m => m.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

