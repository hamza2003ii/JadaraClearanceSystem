using JadaraClearance.Models;

namespace JadaraClearance.Repositories;

public interface IClearanceApprovalRepository
{
    Task<ClearanceApproval?> GetByIdAsync(int id);
    Task<List<ClearanceApproval>> GetPendingApprovalsByDepartmentIdAsync(int departmentId);
    Task<List<ClearanceApproval>> GetApprovalsByRequestIdAsync(int requestId);
    Task AddAsync(ClearanceApproval approval);
    Task AddRangeAsync(IEnumerable<ClearanceApproval> approvals);
    Task UpdateAsync(ClearanceApproval approval);
    Task SaveChangesAsync();
}
