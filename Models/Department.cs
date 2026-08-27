namespace JadaraClearance.Models;

public class Department
{
    public int Id { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public bool RequiresPayment { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<ClearanceApproval> ClearanceApprovals { get; set; } = new List<ClearanceApproval>();
}
