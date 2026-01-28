using Microsoft.AspNetCore.Mvc;
using DBCONNECTOR.Interfaces;

namespace AIFORBI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public AuthController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpPost("Login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest("Email and password are required");
        }

        var user = _userRepository.GetByCredentials(request.Email, request.Password);

        if (user == null)
        {
            return Unauthorized("Invalid email or password");
        }

        return Ok(new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            Role = user.UserRole
        });
    }

    [HttpGet("Me")]
    public IActionResult GetCurrentUser([FromQuery] int userId)
    {
        var user = _userRepository.GetById(userId);

        if (user == null)
        {
            return NotFound("User not found");
        }

        return Ok(new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            Role = user.UserRole
        });
    }
}

public class LoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class LoginResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
}
