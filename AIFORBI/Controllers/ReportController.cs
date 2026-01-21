using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using AIFORBI.Models;
using AIFORBI.Services;

namespace AIFORBI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        [HttpPost("Ask")]
        public IActionResult Ask(AskModel AskQ)
        {
            ReportService srvR = new ReportService();
            var result = srvR.AskQuestion(AskQ);
            return Ok(result);
        }

        [HttpGet("History")]
        public IActionResult GetHistory(string sessionId = "default-session")
        {
            ReportService srvR = new ReportService();
            try 
            {
                var history = srvR.mscon.GetChatHistory(sessionId);
                return Ok(history);
            }
            catch(Exception ex)
            {
                return BadRequest("Error fetching history: " + ex.Message);
            }
        }

        [HttpGet("Sessions")]
        public IActionResult GetUserSessions(int userId)
        {
            ReportService srvR = new ReportService();
            try 
            {
                var sessions = srvR.mscon.GetUserChatSessions(userId);
                return Ok(sessions);
            }
            catch(Exception ex)
            {
                return BadRequest("Error fetching sessions: " + ex.Message);
            }
        }

        [HttpPost("Sessions")]
        public IActionResult CreateSession([FromBody] CreateSessionRequest request)
        {
            if (request.UserId <= 0)
            {
                return BadRequest("UserId is required");
            }

            ReportService srvR = new ReportService();
            try 
            {
                var sessionId = Guid.NewGuid().ToString();
                var session = srvR.mscon.CreateChatSession(request.UserId, sessionId, request.Title);
                return Ok(session);
            }
            catch(Exception ex)
            {
                return BadRequest("Error creating session: " + ex.Message);
            }
        }

        [HttpPut("Sessions/{sessionId}/Title")]
        public IActionResult UpdateSessionTitle(string sessionId, [FromBody] UpdateTitleRequest request)
        {
            ReportService srvR = new ReportService();
            try 
            {
                srvR.mscon.UpdateChatSessionTitle(sessionId, request.Title ?? "Untitled");
                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest("Error updating title: " + ex.Message);
            }
        }
    }

    public class CreateSessionRequest
    {
        public int UserId { get; set; }
        public string? Title { get; set; }
    }

    public class UpdateTitleRequest
    {
        public string? Title { get; set; }
    }
}
