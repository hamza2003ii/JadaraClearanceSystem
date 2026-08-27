namespace JadaraClearance.DTOs.Audit;

public class AuditLogDTO
{
    public int Id { get; set; }
    public int? RequestId { get; set; }
    public int ActionByUserId { get; set; }
    public string ActionByUserName { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
