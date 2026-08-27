using JadaraClearance.Models;

namespace JadaraClearance.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
    Task<List<AuditLog>> GetLogsAsync(int? requestId, int? userId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<int> GetLogsCountAsync(int? requestId, int? userId, DateTime? fromDate, DateTime? toDate);
    Task SaveChangesAsync();
}
