using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Intelligence.Agent.Entities;

/// <summary>
/// 질의에 대한 참조 상세
/// </summary>
public class DOCUMENT_CITATION
{
    public Guid AnswerId { get; set; }
    public virtual DOCUMENT_ANSWER Answer { get; set; }
    
    public Guid Id { get; set; }
    public string File { get; set; } = ""; // source_file_name (또는 DocId 권장)
    public int? Page { get; set; }          // 1-based page
}

public class DocumentCitationEntityConfiguration: IEntityTypeConfiguration<DOCUMENT_CITATION>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_CITATION> builder)
    {
        builder.ToTable($"{nameof(DOCUMENT_CITATION)}", "dbo");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        builder.HasOne(m => m.Answer)
            .WithMany(m => m.Citations)
            .HasForeignKey(m => m.AnswerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
