using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MathAnalysisAI.Server.Controllers
{
    [ApiController]
    [Route("api/health")]
    [DisableRateLimiting]
    public class HealthController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public HealthController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "ok",
                service = "MathAnalysisAI.Server",
                timestampUtc = DateTime.UtcNow,
                environment = _environment.EnvironmentName
            });
        }
    }
}
