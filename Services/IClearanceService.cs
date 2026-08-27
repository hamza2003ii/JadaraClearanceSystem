using JadaraClearance.DTOs.Clearance;

namespace JadaraClearance.Services;

public interface IClearanceService
{
    Task<RequestDetailsDTO> CreateRequestAsync(int studentId);
    Task<RequestDetailsDTO?> GetMyRequestAsync(int studentId);
    Task<List<DepartmentApprovalDTO>> GetDepartmentPendingApprovalsAsync(int departmentId);
    Task<RequestDetailsDTO> UpdateApprovalStatusAsync(int approvalId, UpdateApprovalStatusDTO dto, int officerUserId, int officerDepartmentId);
}
