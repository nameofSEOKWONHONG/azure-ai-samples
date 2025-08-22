namespace Document.Intelligence.Agent.Features.Chat.Models;

public sealed class ResearchResult
{
    public string ChunkId { get; set; }
    public string Content { get; set; }
    public string SourceFileName { get; set; }
    public int? Year { get; set; }
}