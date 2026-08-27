using Microsoft.AspNetCore.Mvc;
using JadaraClearance.DTOs;
using JadaraClearance.DTOs.Auth;
using JadaraClearance.Services;

namespace JadaraClearance.Controllers;

/// <summary>
/// Provides endpoints for user authentication and student registration.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Student self-registration endpoint.
    /// </summary>
    /// <param name="dto">Registration details for student account.</param>
    /// <returns>Authentication response containing JWT token and student details.</returns>
    /// <response code="200">Registration successful, token generated.</response>
    /// <response code="400">Invalid input data.</response>
    /// <response code="409">Email address is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.ErrorResponse("Invalid payload.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var result = await _authService.RegisterStudentAsync(dto);
            return Ok(ApiResponse<AuthResponseDTO>.SuccessResponse(result, "Registration completed successfully."));
        }
        catch (DuplicateEmailException ex)
        {
            return Conflict(ApiResponse.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// User login endpoint for Students, Officers, and Admins.
    /// </summary>
    /// <param name="dto">Login credentials (email and password).</param>
    /// <returns>Authentication response containing JWT token and user info.</returns>
    /// <response code="200">Login successful, token generated.</response>
    /// <response code="401">Invalid email or password.</response>
    /// <response code="403">User account is inactive.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.ErrorResponse("Invalid payload.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(ApiResponse<AuthResponseDTO>.SuccessResponse(result, "Login successful."));
        }
        catch (InvalidCredentialsException ex)
        {
            return Unauthorized(ApiResponse.ErrorResponse(ex.Message));
        }
        catch (InactiveAccountException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.ErrorResponse(ex.Message));
        }
    }
}
