using System.Security.Cryptography;
using System.Text;
using JadaraClearance.DTOs.Clearance;
using JadaraClearance.Models;
using JadaraClearance.Repositories;

namespace JadaraClearance.Services;

public class ClearanceService : IClearanceService
{
    private readonly IClearanceRequestRepository _requestRepository;
    private readonly IClearanceApprovalRepository _approvalRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ClearanceService(
        IClearanceRequestRepository requestRepository,
        IClearanceApprovalRepository approvalRepository,
        IAuditLogRepository auditLogRepository)
    {
        _requestRepository = requestRepository;
        _approvalRepository = approvalRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<RequestDetailsDTO> CreateRequestAsync(int studentId)
    {
        // 1. Business rule check: Student cannot have an existing active/pending clearance request
        var activeRequest = await _requestRepository.GetActiveByStudentIdAsync(studentId);
        if (activeRequest != null)
        {
            throw new InvalidOperationException("You already have an active clearance request in progress.");
        }

        using var transaction = await _requestRepository.BeginTransactionAsync();
        try
        {
            // 2. Create ClearanceRequest entity
            var newRequest = new ClearanceRequest
            {
                StudentId = studentId,
                RequestDate = DateTime.UtcNow,
                OverallStatus = "Pending"
            };

            await _requestRepository.AddAsync(newRequest);
            await _requestRepository.SaveChangesAsync();

            // 3. Retrieve all standard departments (Library, Finance, Registration, Student Affairs)
            var departments = await _requestRepository.GetAllDepartmentsAsync();
            if (!departments.Any())
            {
                // Fallback / standard departments if none found in DB yet
                departments = new List<Department>
                {
                    new() { Id = 1, DepartmentName = "Library", RequiresPayment = false },
                    new() { Id = 2, DepartmentName = "Finance", RequiresPayment = true },
                    new() { Id = 3, DepartmentName = "Registration", RequiresPayment = false },
                    new() { Id = 4, DepartmentName = "Student Affairs", RequiresPayment = false }
                };
            }

            // 4. Automatically create 1 ClearanceApproval record per department
            var approvals = departments.Select(dept => new ClearanceApproval
            {
                RequestId = newRequest.Id,
                DepartmentId = dept.Id,
                Status = "Pending",
                IsPaid = false,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            await _approvalRepository.AddRangeAsync(approvals);
            await _approvalRepository.SaveChangesAsync();

            // 5. Create Audit Log entry
            var auditLog = new AuditLog
            {
                RequestId = newRequest.Id,
                ActionByUserId = studentId,
                ActionType = "RequestCreated",
                Description = $"Clearance request #{newRequest.Id} created by student.",
                Timestamp = DateTime.UtcNow
            };

            await _auditLogRepository.AddAsync(auditLog);
            await _auditLogRepository.SaveChangesAsync();

            await transaction.CommitAsync();

            var reloaded = await _requestRepository.GetByIdAsync(newRequest.Id);
            return MapToDetailsDto(reloaded ?? newRequest);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<RequestDetailsDTO?> GetMyRequestAsync(int studentId)
    {
        var request = await _requestRepository.GetLatestByStudentIdAsync(studentId);
        if (request == null)
        {
            return null;
        }

        return MapToDetailsDto(request);
    }

    public async Task<List<DepartmentApprovalDTO>> GetDepartmentPendingApprovalsAsync(int departmentId)
    {
        var pendingApprovals = await _approvalRepository.GetPendingApprovalsByDepartmentIdAsync(departmentId);
        return pendingApprovals.Select(MapToApprovalDto).ToList();
    }

    public async Task<RequestDetailsDTO> UpdateApprovalStatusAsync(
        int approvalId, 
        UpdateApprovalStatusDTO dto, 
        int officerUserId, 
        int officerDepartmentId)
    {
        var approval = await _approvalRepository.GetByIdAsync(approvalId);
        if (approval == null)
        {
            throw new KeyNotFoundException($"Clearance approval with ID {approvalId} was not found.");
        }

        // Verify officer belongs to the department being approved
        if (approval.DepartmentId != officerDepartmentId)
        {
            throw new UnauthorizedAccessException("You are not authorized to update approvals for another department.");
        }

        // Apply approval update
        approval.Status = dto.Status;
        approval.ActionByOfficerId = officerUserId;
        approval.UpdatedAt = DateTime.UtcNow;

        if (dto.Status == "Rejected")
        {
            approval.RejectionReason = dto.RejectionReason;
        }
        else
        {
            approval.RejectionReason = null;
        }

        if (dto.FineAmount.HasValue)
        {
            if (dto.FineAmount.Value > 0 && !(approval.Department?.RequiresPayment ?? false))
            {
                throw new InvalidOperationException($"Department '{approval.Department?.DepartmentName}' does not collect fees or fines. Fines cannot be applied.");
            }
            approval.FineAmount = dto.FineAmount.Value;
        }

        await _approvalRepository.UpdateAsync(approval);
        await _approvalRepository.SaveChangesAsync();

        // Check roll-up status for the overall request
        var allApprovals = await _approvalRepository.GetApprovalsByRequestIdAsync(approval.RequestId);
        var request = await _requestRepository.GetByIdAsync(approval.RequestId);

        if (request != null)
        {
            if (allApprovals.All(a => a.Status == "Approved"))
            {
                request.OverallStatus = "Completed";
                request.CompletedAt = DateTime.UtcNow;
                request.CertificateHash = GenerateCertificateHash(request.Id, request.CompletedAt.Value);
            }
            else if (allApprovals.Any(a => a.Status == "Rejected"))
            {
                request.OverallStatus = "Rejected";
            }
            else
            {
                request.OverallStatus = "Pending";
            }

            await _requestRepository.UpdateAsync(request);
            await _requestRepository.SaveChangesAsync();
        }

        // Log Audit
        var auditLog = new AuditLog
        {
            RequestId = approval.RequestId,
            ActionByUserId = officerUserId,
            ActionType = dto.Status,
            Description = $"Department approval #{approval.Id} ({approval.Department?.DepartmentName}) updated to '{dto.Status}' by officer #{officerUserId}.",
            Timestamp = DateTime.UtcNow
        };

        await _auditLogRepository.AddAsync(auditLog);
        await _auditLogRepository.SaveChangesAsync();

        var reloadedRequest = await _requestRepository.GetByIdAsync(approval.RequestId);
        return MapToDetailsDto(reloadedRequest!);
    }

    private static string GenerateCertificateHash(int requestId, DateTime completedAt)
    {
        var rawData = $"JADARA-CERTIFICATE-{requestId}-{completedAt:O}";
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes);
    }

    private static RequestDetailsDTO MapToDetailsDto(ClearanceRequest request)
    {
        return new RequestDetailsDTO
        {
            Id = request.Id,
            StudentId = request.StudentId,
            StudentFullName = request.Student?.FullName ?? string.Empty,
            StudentUniversityId = request.Student?.UniversityId ?? string.Empty,
            RequestDate = request.RequestDate,
            OverallStatus = request.OverallStatus,
            CertificateHash = request.CertificateHash,
            CompletedAt = request.CompletedAt,
            Approvals = request.ClearanceApprovals.Select(MapToApprovalDto).ToList()
        };
    }

    private static DepartmentApprovalDTO MapToApprovalDto(ClearanceApproval approval)
    {
        return new DepartmentApprovalDTO
        {
            ApprovalId = approval.Id,
            RequestId = approval.RequestId,
            StudentFullName = approval.Request?.Student?.FullName ?? string.Empty,
            StudentUniversityId = approval.Request?.Student?.UniversityId ?? string.Empty,
            DepartmentName = approval.Department?.DepartmentName ?? string.Empty,
            Status = approval.Status,
            RejectionReason = approval.RejectionReason,
            FineAmount = approval.FineAmount,
            IsPaid = approval.IsPaid,
            UpdatedAt = approval.UpdatedAt
        };
    }
}
