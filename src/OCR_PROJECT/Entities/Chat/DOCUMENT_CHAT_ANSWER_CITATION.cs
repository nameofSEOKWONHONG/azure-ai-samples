using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities.Chat;

/// <summary>
/// 응답에 대한 참조 내역
/// </summary>
public class DOCUMENT_CHAT_ANSWER_CITATION
{
    public Guid AnswerId { get; set; }
    public virtual DOCUMENT_CHAT_ANSWER ChatAnswer { get; set; }
    
    public Guid Id { get; set; }
    public string File { get; set; } = ""; // source_file_name (또는 DocId 권장)
    public int? Page { get; set; }          // 1-based page
}

public class DocumentChatAnswerCitationEntityConfiguration: IEntityTypeConfiguration<DOCUMENT_CHAT_ANSWER_CITATION>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_CHAT_ANSWER_CITATION> builder)
    {
        builder.ToTable($"{nameof(DOCUMENT_CHAT_ANSWER_CITATION)}", "dbo");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        builder.HasOne(m => m.ChatAnswer)
            .WithMany(m => m.Citations)
            .HasForeignKey(m => m.AnswerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
