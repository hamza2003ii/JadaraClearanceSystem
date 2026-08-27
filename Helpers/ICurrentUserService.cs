namespace JadaraClearance.Helpers;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    int? DepartmentId { get; }
    bool IsAuthenticated { get; }
}
