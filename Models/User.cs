namespace JadaraClearance.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int? DepartmentId { get; set; }
    public string? UniversityId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Role Role { get; set; } = null!;
    public virtual Department? Department { get; set; }
    public virtual ICollection<ClearanceRequest> ClearanceRequests { get; set; } = new List<ClearanceRequest>();
    public virtual ICollection<ClearanceApproval> ClearanceApprovals { get; set; } = new List<ClearanceApproval>();
    public virtual ICollection<ClearanceAttachment> ClearanceAttachments { get; set; } = new List<ClearanceAttachment>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
