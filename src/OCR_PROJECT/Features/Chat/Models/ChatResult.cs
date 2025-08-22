namespace Document.Intelligence.Agent.Features.Chat.Models;

public class DocumentChatResult
{
    public Guid ThreadId { get; set; }
    public Guid QuestionId { get; set; }
    public string Answer { get; set; }
    public PageCitation[] Citations { get; set; } = [];    
}

public class ChatResult
{
    public Guid ThreadId { get; set; }
    public string Answer { get; set; }
    public PageCitation[] Citations { get; set; } = [];
}

public class PageCitation
{
    public string File { get; set; } = ""; // source_file_name (또는 DocId 권장)
    public int? Page { get; set; }          // 1-based page
}