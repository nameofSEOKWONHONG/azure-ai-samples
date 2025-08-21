namespace Document.Intelligence.Agent.Entities;

public abstract class DOCUMENT_ENTITY_BASE
{
    public DateTime CreatedAt { get; set; }
    public Guid CreatedId { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public Guid? ModifiedId { get; set; }
}