using Microsoft.AspNetCore.Mvc;
using AIFORBI.Models;
using AIFORBI.Services;
using DBCONNECTOR.Interfaces;

namespace AIFORBI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IChatRepository _chatRepository;

    public ReportController(IReportService reportService, IChatRepository chatRepository)
    {
        _reportService = reportService;
        _chatRepository = chatRepository;
    }

    [HttpPost("Ask")]
    public IActionResult Ask([FromBody] AskModel askQ)
    {
        var result = _reportService.AskQuestion(askQ);
        return Ok(result);
    }

    [HttpGet("History")]
    public IActionResult GetHistory([FromQuery] string sessionId = "default-session")
    {
        try
        {
            var history = _chatRepository.GetChatHistory(sessionId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error fetching history: {ex.Message}");
        }
    }

    [HttpGet("Sessions")]
    public IActionResult GetUserSessions([FromQuery] int userId)
    {
        try
        {
            var sessions = _chatRepository.GetUserSessions(userId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error fetching sessions: {ex.Message}");
        }
    }

    [HttpPost("Sessions")]
    public IActionResult CreateSession([FromBody] CreateSessionRequest request)
    {
        if (request.UserId <= 0)
        {
            return BadRequest("UserId is required");
        }

        try
        {
            var sessionId = Guid.NewGuid().ToString();
            var session = _chatRepository.CreateSession(request.UserId, sessionId, request.Title);
            return Ok(session);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error creating session: {ex.Message}");
        }
    }

    [HttpPut("Sessions/{sessionId}/Title")]
    public IActionResult UpdateSessionTitle(string sessionId, [FromBody] UpdateTitleRequest request)
    {
        try
        {
            _chatRepository.UpdateSessionTitle(sessionId, request.Title ?? "Untitled");
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest($"Error updating title: {ex.Message}");
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
