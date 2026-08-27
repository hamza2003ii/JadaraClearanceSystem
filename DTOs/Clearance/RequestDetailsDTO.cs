namespace JadaraClearance.DTOs.Clearance;

public class RequestDetailsDTO
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentUniversityId { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public string OverallStatus { get; set; } = string.Empty;
    public string? CertificateHash { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<DepartmentApprovalDTO> Approvals { get; set; } = new();
}
