
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
    [HttpGet("GetDbSummary")]
    public IActionResult GetAndIndexDbSummary(bool UseForceAI = false)
    {
        SettingsService setService = new SettingsService();
        return Ok(setService.SummaryAndIndexDb(UseForceAI));
    }
    
}