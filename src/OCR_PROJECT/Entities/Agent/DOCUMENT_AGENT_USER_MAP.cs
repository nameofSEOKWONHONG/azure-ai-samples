using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities.Agent;

/// <summary>
/// 사용자 - AGENT 맵핑 테이블.
/// ROOT 지점으로 직접 JOIN을 걸지않고 AgentId를 찾아서 후속 조회를 한다. 
/// </summary>
public class DOCUMENT_AGENT_USER_MAP : DOCUMENT_ENTITY_BASE
{
    public string UserId { get; set; }
    public Guid DocumentAgentId { get; set; }

    /// <summary>
    /// 근급 상황시 중단용
    /// </summary>
    public bool IsActive { get; set; } = true;
}

public class DocumentAgentUserMapEntityConfiguration: IEntityTypeConfiguration<DOCUMENT_AGENT_USER_MAP>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_AGENT_USER_MAP> builder)
    {
        builder.ToTable(nameof(DOCUMENT_AGENT_USER_MAP), "dbo");
        builder.HasKey(m => new{m.UserId, m.DocumentAgentId});
        builder.HasIndex(m => new { m.UserId, m.IsActive })
            .IsUnique(false);
    }
}