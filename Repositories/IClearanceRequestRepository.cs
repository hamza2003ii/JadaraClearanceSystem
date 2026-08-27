using Microsoft.EntityFrameworkCore.Storage;
using JadaraClearance.Models;

namespace JadaraClearance.Repositories;

public interface IClearanceRequestRepository
{
    Task<ClearanceRequest?> GetByIdAsync(int id);
    Task<ClearanceRequest?> GetLatestByStudentIdAsync(int studentId);
    Task<ClearanceRequest?> GetActiveByStudentIdAsync(int studentId);
    Task<List<Department>> GetAllDepartmentsAsync();
    Task AddAsync(ClearanceRequest request);
    Task UpdateAsync(ClearanceRequest request);
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task SaveChangesAsync();
}
