namespace JadaraClearance.Models;

public class ClearanceApproval
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public int DepartmentId { get; set; }
    public string Status { get; set; } = "Pending";
    public int? ActionByOfficerId { get; set; }
    public string? RejectionReason { get; set; }
    public decimal? FineAmount { get; set; }
    public bool IsPaid { get; set; } = false;
    public DateTime? UpdatedAt { get; set; }

    public virtual ClearanceRequest Request { get; set; } = null!;
    public virtual Department Department { get; set; } = null!;
    public virtual User? ActionByOfficer { get; set; }
    public virtual ICollection<ClearanceAttachment> ClearanceAttachments { get; set; } = new List<ClearanceAttachment>();
}
