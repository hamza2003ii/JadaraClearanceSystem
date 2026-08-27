using JadaraClearance.DTOs.Auth;
using JadaraClearance.Helpers;
using JadaraClearance.Models;
using JadaraClearance.Repositories;

namespace JadaraClearance.Services;

public class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string message) : base(message) { }
}

public class InactiveAccountException : Exception
{
    public InactiveAccountException(string message) : base(message) { }
}

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException(string message) : base(message) { }
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDTO> RegisterStudentAsync(RegisterDTO dto)
    {
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            throw new DuplicateEmailException("An account with this email address already exists.");
        }

        var studentRole = await _userRepository.GetRoleByNameAsync("Student");
        if (studentRole == null)
        {
            // If Role is not found, fallback to roleId 1 or throw
            studentRole = new Role { Id = 1, RoleName = "Student" };
        }

        var passwordHash = _passwordHasher.HashPassword(dto.Password);

        var newUser = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = passwordHash,
            RoleId = studentRole.Id,
            DepartmentId = dto.DepartmentId,
            UniversityId = dto.UniversityId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Role = studentRole
        };

        await _userRepository.AddAsync(newUser);
        await _userRepository.SaveChangesAsync();

        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(newUser, studentRole.RoleName);

        return new AuthResponseDTO
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = newUser.Id,
            FullName = newUser.FullName,
            Role = studentRole.RoleName,
            DepartmentId = newUser.DepartmentId
        };
    }

    public async Task<AuthResponseDTO> LoginAsync(LoginDTO dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new InactiveAccountException("Your account has been deactivated. Please contact the administrator.");
        }

        var roleName = user.Role?.RoleName ?? "Student";
        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user, roleName);

        return new AuthResponseDTO
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            FullName = user.FullName,
            Role = roleName,
            DepartmentId = user.DepartmentId
        };
    }
}
