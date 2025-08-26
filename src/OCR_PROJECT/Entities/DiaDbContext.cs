using Document.Intelligence.Agent.Entities.Agent;
using Document.Intelligence.Agent.Entities.Chat;
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

        modelBuilder.ApplyConfiguration(new DocumentAgentUserMapEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentAgentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentAgentPromptEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentTopicEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentTopicMetadataEntityConfiguration());

        modelBuilder.ApplyConfiguration(new DocumentChatThreadEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentChatQuestionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentChatQuestionResearchEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentChatAnswerEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentChatAnswerCitationEntityConfiguration());
    }

    #region [agent]

    public DbSet<DOCUMENT_AGENT_USER_MAP> AgentUserMappings { get; set; }
    public DbSet<DOCUMENT_AGENT> Agents { get; set; }
    public DbSet<DOCUMENT_AGENT_PROMPT> AgentPrompts { get; set; }
    
    #endregion

    #region [topic]

    public DbSet<DOCUMENT_TOPIC> Topics { get; set; }
    public DbSet<DOCUMENT_TOPIC_JOB> TopicJobs { get; set; }
    public DbSet<DOCUMENT_TOPIC_METADATA> TopicMetadatum { get; set; }
    public DbSet<DOCUMENT_TOPIC_AGENT_MAP> TopicAgentMaps { get; set; }

    #endregion

    #region [chat]

    public DbSet<DOCUMENT_CHAT_THREAD> ChatThreads { get; set; }
    public DbSet<DOCUMENT_CHAT_QUESTION> ChatQuestions { get; set; }
    public DbSet<DOCUMENT_CHAT_QUESTION_RESEARCH> ChatQuestionResearches { get; set; }
    public DbSet<DOCUMENT_CHAT_ANSWER> ChatAnswers { get; set; }
    public DbSet<DOCUMENT_CHAT_ANSWER_CITATION> ChatAnswerCitations { get; set; }    

    #endregion
}