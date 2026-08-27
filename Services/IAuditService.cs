using JadaraClearance.DTOs.Audit;

namespace JadaraClearance.Services;

public interface IAuditService
{
    Task<List<AuditLogDTO>> GetAuditLogsAsync(int? requestId, int? userId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
}
