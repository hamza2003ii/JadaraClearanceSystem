namespace JadaraClearance.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int? RequestId { get; set; }
    public int ActionByUserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public virtual ClearanceRequest? Request { get; set; }
    public virtual User ActionByUser { get; set; } = null!;
}
