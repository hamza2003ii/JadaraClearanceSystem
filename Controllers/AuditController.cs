using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JadaraClearance.DTOs;
using JadaraClearance.DTOs.Audit;
using JadaraClearance.Services;

namespace JadaraClearance.Controllers;

/// <summary>
/// Provides administrative access to system audit logs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Retrieves paginated system audit logs with optional filtering criteria.
    /// </summary>
    /// <param name="requestId">Optional filter by Clearance Request ID.</param>
    /// <param name="userId">Optional filter by User ID who performed the action.</param>
    /// <param name="fromDate">Optional start date for filtering.</param>
    /// <param name="toDate">Optional end date for filtering.</param>
    /// <param name="page">Page number for pagination (default: 1).</param>
    /// <param name="pageSize">Page size for pagination (default: 20, max: 100).</param>
    /// <returns>List of audit log entries ordered by timestamp descending.</returns>
    /// <response code="200">Audit logs retrieved successfully.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="403">Forbidden - Admin role required.</response>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(ApiResponse<List<AuditLogDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int? requestId,
        [FromQuery] int? userId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var logs = await _auditService.GetAuditLogsAsync(requestId, userId, fromDate, toDate, page, pageSize);
        return Ok(ApiResponse<List<AuditLogDTO>>.SuccessResponse(logs, "Audit logs retrieved successfully."));
    }
}
