using Microsoft.EntityFrameworkCore;
using JadaraClearance.Models;

namespace JadaraClearance.Repositories;

public class ClearanceApprovalRepository : IClearanceApprovalRepository
{
    private readonly JadaraClearanceDbContext _context;

    public ClearanceApprovalRepository(JadaraClearanceDbContext context)
    {
        _context = context;
    }

    public async Task<ClearanceApproval?> GetByIdAsync(int id)
    {
        return await _context.ClearanceApprovals
            .Include(a => a.Department)
            .Include(a => a.Request)
                .ThenInclude(r => r.Student)
            .Include(a => a.ActionByOfficer)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<ClearanceApproval>> GetPendingApprovalsByDepartmentIdAsync(int departmentId)
    {
        return await _context.ClearanceApprovals
            .Include(a => a.Department)
            .Include(a => a.Request)
                .ThenInclude(r => r.Student)
            .Where(a => a.DepartmentId == departmentId && a.Status == "Pending")
            .OrderBy(a => a.Request.RequestDate)
            .ToListAsync();
    }

    public async Task<List<ClearanceApproval>> GetApprovalsByRequestIdAsync(int requestId)
    {
        return await _context.ClearanceApprovals
            .Include(a => a.Department)
            .Include(a => a.ActionByOfficer)
            .Where(a => a.RequestId == requestId)
            .ToListAsync();
    }

    public async Task AddAsync(ClearanceApproval approval)
    {
        await _context.ClearanceApprovals.AddAsync(approval);
    }

    public async Task AddRangeAsync(IEnumerable<ClearanceApproval> approvals)
    {
        await _context.ClearanceApprovals.AddRangeAsync(approvals);
    }

    public Task UpdateAsync(ClearanceApproval approval)
    {
        _context.ClearanceApprovals.Update(approval);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
