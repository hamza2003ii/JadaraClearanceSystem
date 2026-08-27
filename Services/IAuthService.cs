using JadaraClearance.DTOs.Auth;

namespace JadaraClearance.Services;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterStudentAsync(RegisterDTO dto);
    Task<AuthResponseDTO> LoginAsync(LoginDTO dto);
}
