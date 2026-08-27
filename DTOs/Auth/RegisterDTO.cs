using System.ComponentModel.DataAnnotations;

namespace JadaraClearance.DTOs.Auth;

public class RegisterDTO
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full Name must be between 2 and 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "University ID is required.")]
    public string UniversityId { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
}
