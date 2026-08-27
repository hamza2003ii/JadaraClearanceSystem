namespace JadaraClearance.Models;

public class ClearanceRequest
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public string OverallStatus { get; set; } = "Pending";
    public string? CertificateHash { get; set; }
    public DateTime? CompletedAt { get; set; }

    public virtual User Student { get; set; } = null!;
    public virtual ICollection<ClearanceApproval> ClearanceApprovals { get; set; } = new List<ClearanceApproval>();
    public virtual ICollection<ClearanceAttachment> ClearanceAttachments { get; set; } = new List<ClearanceAttachment>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
