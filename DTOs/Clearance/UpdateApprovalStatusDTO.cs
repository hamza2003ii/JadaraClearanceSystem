using System.ComponentModel.DataAnnotations;

namespace JadaraClearance.DTOs.Clearance;

public class UpdateApprovalStatusDTO
{
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression("^(Approved|Rejected)$", ErrorMessage = "Status must be either 'Approved' or 'Rejected'.")]
    public string Status { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }

    [Range(0, 100000, ErrorMessage = "Fine amount must be a positive value.")]
    public decimal? FineAmount { get; set; }
}
