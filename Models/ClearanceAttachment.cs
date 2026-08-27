namespace JadaraClearance.Models;

public class ClearanceAttachment
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public int? ApprovalId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public virtual ClearanceRequest Request { get; set; } = null!;
    public virtual ClearanceApproval? Approval { get; set; }
    public virtual User UploadedByUser { get; set; } = null!;
}
