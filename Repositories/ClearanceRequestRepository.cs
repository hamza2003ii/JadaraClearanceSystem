using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using JadaraClearance.Models;

namespace JadaraClearance.Repositories;

public class ClearanceRequestRepository : IClearanceRequestRepository
{
    private readonly JadaraClearanceDbContext _context;

    public ClearanceRequestRepository(JadaraClearanceDbContext context)
    {
        _context = context;
    }

    public async Task<ClearanceRequest?> GetByIdAsync(int id)
    {
        return await _context.ClearanceRequests
            .Include(r => r.Student)
            .Include(r => r.ClearanceApprovals)
                .ThenInclude(a => a.Department)
            .Include(r => r.ClearanceApprovals)
                .ThenInclude(a => a.ActionByOfficer)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<ClearanceRequest?> GetLatestByStudentIdAsync(int studentId)
    {
        return await _context.ClearanceRequests
            .Include(r => r.Student)
            .Include(r => r.ClearanceApprovals)
                .ThenInclude(a => a.Department)
            .Include(r => r.ClearanceApprovals)
                .ThenInclude(a => a.ActionByOfficer)
            .OrderByDescending(r => r.RequestDate)
            .FirstOrDefaultAsync(r => r.StudentId == studentId);
    }

    public async Task<ClearanceRequest?> GetActiveByStudentIdAsync(int studentId)
    {
        return await _context.ClearanceRequests
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.OverallStatus == "Pending");
    }

    public async Task<List<Department>> GetAllDepartmentsAsync()
    {
        return await _context.Departments.ToListAsync();
    }

    public async Task AddAsync(ClearanceRequest request)
    {
        await _context.ClearanceRequests.AddAsync(request);
    }

    public Task UpdateAsync(ClearanceRequest request)
    {
        _context.ClearanceRequests.Update(request);
        return Task.CompletedTask;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
