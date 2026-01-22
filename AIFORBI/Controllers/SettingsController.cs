
using AICONNECTOR;
using AICONNECTOR.Connectors;
using AIFORBI.Services;
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
}