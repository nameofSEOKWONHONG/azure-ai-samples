using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities.Agent;

/// <summary>
/// AGENT 프롬프트 설정.
/// 관리자지정지침과 추천프롬프트를 관리한다.
/// 관리자지점지침은 TITLE: 관리자 지정 지침 
/// 관리자지정지침은 TYPE:ADMIN, 추철프롬프트는 TYPE:RECOMMEND.
/// </summary>
public class DOCUMENT_AGENT_PROMPT
{
    public Guid DocumentAgentId { get; set; }
    public virtual DOCUMENT_AGENT DocumentAgent { get; set; }
    
    /// <summary>
    /// KEY
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// 제목
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// 메세지
    /// </summary>
    public string Prompt { get; set; }
    
    /// <summary>
    /// 프롬프트 타입 (RECOMMEND , ADMIN)
    /// </summary>
    public string Type { get; set; }
}

public class DocumentAgentPromptEntityConfiguration : IEntityTypeConfiguration<DOCUMENT_AGENT_PROMPT>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_AGENT_PROMPT> builder)
    {
        builder.ToTable(nameof(DOCUMENT_AGENT_PROMPT), "dbo");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();
        
        builder.HasOne(m => m.DocumentAgent)
            .WithMany(m => m.DocumentAgentPrompts)
            .HasForeignKey(m => m.DocumentAgentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}