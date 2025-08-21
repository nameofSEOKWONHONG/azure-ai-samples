using Microsoft.EntityFrameworkCore;

namespace Document.Intelligence.Agent.Entities;

public class DiaDbContext: DbContext
{
    public DiaDbContext(DbContextOptions<DiaDbContext> options): base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Korean_Wansung_CI_AS");

        modelBuilder.ApplyConfiguration(new DocumentThreadEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentQuestionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentQuestionResearchEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentAnswerEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentCitationEntityConfiguration());
    }

    #region [chat]

    public DbSet<DOCUMENT_THREAD> Threads { get; set; }
    public DbSet<DOCUMENT_QUESTION> Questions { get; set; }
    public DbSet<DOCUMENT_QUESTION_RESEARCH> QuestionResearches { get; set; }
    public DbSet<DOCUMENT_ANSWER> Answers { get; set; }
    public DbSet<DOCUMENT_CITATION> Citations { get; set; }    

    #endregion
}