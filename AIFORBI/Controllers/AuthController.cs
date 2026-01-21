using Microsoft.AspNetCore.Mvc;
using AIFORBI.Services;

namespace AIFORBI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpPost("Login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Email and password are required");
            }

            var srvR = new ReportService();
            var user = srvR.mscon.GetUserByCredentials(request.Email, request.Password);

            if (user == null)
            {
                return Unauthorized("Invalid email or password");
            }

            return Ok(new
            {
                userId = user.Id,
                email = user.Email,
                displayName = user.DisplayName
            });
        }

        [HttpGet("Me")]
        public IActionResult GetCurrentUser(int userId)
        {
            var srvR = new ReportService();
            var user = srvR.mscon.GetUserById(userId);

            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(new
            {
                userId = user.Id,
                email = user.Email,
                displayName = user.DisplayName
            });
        }
    }

    public class LoginRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}
