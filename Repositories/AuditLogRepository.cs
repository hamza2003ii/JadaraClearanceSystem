using Microsoft.EntityFrameworkCore;
using JadaraClearance.Models;

namespace JadaraClearance.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly JadaraClearanceDbContext _context;

    public AuditLogRepository(JadaraClearanceDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog log)
    {
        await _context.AuditLogs.AddAsync(log);
    }

    private IQueryable<AuditLog> BuildQuery(int? requestId, int? userId, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.AuditLogs
            .Include(a => a.ActionByUser)
            .AsQueryable();

        if (requestId.HasValue)
        {
            query = query.Where(a => a.RequestId == requestId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(a => a.ActionByUserId == userId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= toDate.Value);
        }

        return query;
    }

    public async Task<List<AuditLog>> GetLogsAsync(int? requestId, int? userId, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        return await BuildQuery(requestId, userId, fromDate, toDate)
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetLogsCountAsync(int? requestId, int? userId, DateTime? fromDate, DateTime? toDate)
    {
        return await BuildQuery(requestId, userId, fromDate, toDate).CountAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
