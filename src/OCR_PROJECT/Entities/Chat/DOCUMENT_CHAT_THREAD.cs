using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities.Chat;

/*
 * DOCUMENT_THREAD (질의 thread master)
 *         ----> DOCUMENT_QUESTION (질의문)
 *                      ----> DOCUMENT_QUESTION_RESEARCH (AI SEARCH 조사)
 *                      ----> DOCUMENT_ANSWER (LLM 응답)
 *                                 -----> DOCUMENT_CITATION (LLM 응답 근거)
 */

/// <summary>
/// CHAT THREAD 마스터 (히스토리 역활)
/// TODO:페이징 필수
/// </summary>
public class DOCUMENT_CHAT_THREAD: DOCUMENT_ENTITY_BASE
{  
    public Guid Id { get; set; }
    /// <summary>
    /// 챗 제목 (질의시 첫번째 질문으로 제목 생성)
    /// </summary>
    public string Title { get; set; }
    public virtual ICollection<DOCUMENT_CHAT_QUESTION> Questions { get; set; }
}

public class DocumentChatThreadEntityConfiguration: IEntityTypeConfiguration<DOCUMENT_CHAT_THREAD>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_CHAT_THREAD> builder)
    {
        builder.ToTable($"{nameof(DOCUMENT_CHAT_THREAD)}", "dbo");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();
        builder.Property(m => m.CreatedAt)
            .IsRequired();
        builder.Property(m => m.CreatedId)
            .IsRequired();
    }
}