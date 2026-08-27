namespace JadaraClearance.DTOs.Clearance;

public class DepartmentApprovalDTO
{
    public int ApprovalId { get; set; }
    public int RequestId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentUniversityId { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public decimal? FineAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
