using ADScimApi.Auth;
using ADScimApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADScimApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, token) = await _authService.AuthenticateAsync(request.Username, request.Password);
        
        if (!success)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }
        
        return Ok(new { token });
    }

    [HttpGet("users")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("users/{username}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUserByUsername(string username)
    {
        var user = await _authService.GetUserByUsernameAsync(username);
        
        if (user == null)
        {
            return NotFound();
        }
        
        return Ok(user);
    }

    [HttpPost("users")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = new ApiUser
        {
            Username = request.Username,
            Role = request.Role,
            IsActive = true
        };
        
        var createdUser = await _authService.CreateUserAsync(user, request.Password);
        return CreatedAtAction(nameof(GetUserByUsername), new { username = createdUser.Username }, createdUser);
    }

    [HttpPut("users/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = new ApiUser
        {
            Username = request.Username,
            Role = request.Role,
            IsActive = request.IsActive
        };
        
        var updatedUser = await _authService.UpdateUserAsync(id, user, request.Password);
        
        if (updatedUser == null)
        {
            return NotFound();
        }
        
        return Ok(updatedUser);
    }

    [HttpDelete("users/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var success = await _authService.DeleteUserAsync(id);
        
        if (!success)
        {
            return NotFound();
        }
        
        return NoContent();
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}