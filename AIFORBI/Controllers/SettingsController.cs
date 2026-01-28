
using AICONNECTOR;
using AICONNECTOR.Connectors;
using AIFORBI.Services;
using AIFORBI.Models;
using DBCONNECTOR.Connectors;
using DBCONNECTOR.Dtos.Mssql;
using Microsoft.AspNetCore.Mvc;
using Qdrant.Client.Grpc;

namespace AIFORBI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly SettingsService _settingsService;

    public SettingsController(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet("GetDbSummary")]
    public IActionResult GetAndIndexDbSummary(bool UseForceAI = false)
    {
        return Ok(_settingsService.SummaryAndIndexDb(UseForceAI));
    }

    [HttpGet("GetSettings")]
    public IActionResult GetSettings()
    {
        try
        {
            var settings = _settingsService.GetSettings();
            return Ok(settings);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("UpdateSettings")]
    public IActionResult UpdateSettings([FromBody] Models.SettingsDto settings)
    {
        try
        {
            var result = _settingsService.UpdateSettings(settings);
            if (result)
            {
                return Ok(new { message = "Settings updated successfully" });
            }
            return StatusCode(500, new { error = "Failed to update settings" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("IsConfigured")]
    public IActionResult IsConfigured()
    {
        try
        {
            var isConfigured = _settingsService.IsConfigured();
            return Ok(new { isConfigured });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}