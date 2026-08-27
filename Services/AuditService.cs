using JadaraClearance.DTOs.Audit;
using JadaraClearance.Repositories;

namespace JadaraClearance.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<List<AuditLogDTO>> GetAuditLogsAsync(
        int? requestId, 
        int? userId, 
        DateTime? fromDate, 
        DateTime? toDate, 
        int page, 
        int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var logs = await _auditLogRepository.GetLogsAsync(requestId, userId, fromDate, toDate, page, pageSize);

        return logs.Select(log => new AuditLogDTO
        {
            Id = log.Id,
            RequestId = log.RequestId,
            ActionByUserId = log.ActionByUserId,
            ActionByUserName = log.ActionByUser?.FullName ?? string.Empty,
            ActionType = log.ActionType,
            Description = log.Description,
            Timestamp = log.Timestamp
        }).ToList();
    }
}
