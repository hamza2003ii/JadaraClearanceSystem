using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JadaraClearance.DTOs;
using JadaraClearance.DTOs.Clearance;
using JadaraClearance.Helpers;
using JadaraClearance.Services;

namespace JadaraClearance.Controllers;

/// <summary>
/// Handles student clearance requests and department officer approvals.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ClearanceController : ControllerBase
{
    private readonly IClearanceService _clearanceService;
    private readonly ICurrentUserService _currentUserService;

    public ClearanceController(IClearanceService clearanceService, ICurrentUserService currentUserService)
    {
        _clearanceService = clearanceService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Creates a new clearance request for the authenticated student.
    /// Automatically generates approval entries for all required departments atomically.
    /// </summary>
    /// <returns>The created clearance request details including approval statuses.</returns>
    /// <response code="200">Clearance request submitted successfully.</response>
    /// <response code="400">Student already has an active request in progress.</response>
    /// <response code="401">Unauthorized.</response>
    [HttpPost("request")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<RequestDetailsDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateRequest()
    {
        var studentId = _currentUserService.UserId;
        if (!studentId.HasValue)
        {
            return Unauthorized(ApiResponse.ErrorResponse("User identity could not be verified from token."));
        }

        try
        {
            var result = await _clearanceService.CreateRequestAsync(studentId.Value);
            return Ok(ApiResponse<RequestDetailsDTO>.SuccessResponse(result, "Clearance request submitted successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Gets the current authenticated student's latest clearance request.
    /// </summary>
    /// <returns>Request details and all department approval statuses.</returns>
    /// <response code="200">Clearance request found.</response>
    /// <response code="404">No clearance request found for the student.</response>
    [HttpGet("my-request")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<RequestDetailsDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyRequest()
    {
        var studentId = _currentUserService.UserId;
        if (!studentId.HasValue)
        {
            return Unauthorized(ApiResponse.ErrorResponse("User identity could not be verified."));
        }

        var result = await _clearanceService.GetMyRequestAsync(studentId.Value);
        if (result == null)
        {
            return NotFound(ApiResponse.ErrorResponse("No clearance request found for this student."));
        }

        return Ok(ApiResponse<RequestDetailsDTO>.SuccessResponse(result));
    }

    /// <summary>
    /// Retrieves all pending clearance approvals for the officer's assigned department.
    /// </summary>
    /// <returns>List of pending department approvals joined with student details.</returns>
    /// <response code="200">List of pending approvals retrieved successfully.</response>
    /// <response code="403">User is not assigned to a valid department.</response>
    [HttpGet("department-pending")]
    [Authorize(Roles = "DepartmentOfficer")]
    [ProducesResponseType(typeof(ApiResponse<List<DepartmentApprovalDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDepartmentPendingApprovals()
    {
        var departmentId = _currentUserService.DepartmentId;
        if (!departmentId.HasValue)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.ErrorResponse("Department Officer does not have an assigned department."));
        }

        var result = await _clearanceService.GetDepartmentPendingApprovalsAsync(departmentId.Value);
        return Ok(ApiResponse<List<DepartmentApprovalDTO>>.SuccessResponse(result));
    }

    /// <summary>
    /// Updates a department approval status (Approved or Rejected) and recalculates overall clearance status.
    /// </summary>
    /// <param name="id">The Department Approval ID.</param>
    /// <param name="dto">Status update payload (Status, RejectionReason, FineAmount).</param>
    /// <returns>Updated parent clearance request details.</returns>
    /// <response code="200">Approval status updated successfully.</response>
    /// <response code="400">Invalid status update payload.</response>
    /// <response code="403">Officer does not belong to the department of this approval.</response>
    /// <response code="404">Approval record not found.</response>
    [HttpPut("approval/{id:int}")]
    [Authorize(Roles = "DepartmentOfficer")]
    [ProducesResponseType(typeof(ApiResponse<RequestDetailsDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateApprovalStatus(int id, [FromBody] UpdateApprovalStatusDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.ErrorResponse("Invalid status payload.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var officerUserId = _currentUserService.UserId;
        var officerDepartmentId = _currentUserService.DepartmentId;

        if (!officerUserId.HasValue || !officerDepartmentId.HasValue)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.ErrorResponse("Officer identity or department claim is missing."));
        }

        try
        {
            var result = await _clearanceService.UpdateApprovalStatusAsync(id, dto, officerUserId.Value, officerDepartmentId.Value);
            return Ok(ApiResponse<RequestDetailsDTO>.SuccessResponse(result, "Approval status updated successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.ErrorResponse(ex.Message));
        }
    }
}
