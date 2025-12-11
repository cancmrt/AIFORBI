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
    }
}
