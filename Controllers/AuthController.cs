using Microsoft.AspNetCore.Mvc;
using Invoqs.API.DTOs;
using Invoqs.API.Services;

namespace Invoqs.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// User login
    /// </summary>
    /// <param name="loginDto">Login credentials</param>
    /// <returns>Authentication token and user information</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDTO loginDto)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);

            var result = await _userService.LoginAsync(loginDto);
            if (result == null)
            {
                _logger.LogWarning("Invalid login attempt for email: {Email}", loginDto.Email);
                return Unauthorized(new { error = "Invalid email or password" });
            }

            _logger.LogInformation("Successful login for email: {Email}", loginDto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", loginDto.Email);
            return StatusCode(500, new { error = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Get current user information (requires authentication)
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { error = "An error occurred getting user information" });
        }
    }
}