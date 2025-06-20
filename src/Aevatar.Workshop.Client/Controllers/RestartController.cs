using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Aevatar.Workshop.Client.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RestartController : ControllerBase
    {
        private readonly IHostApplicationLifetime _appLifetime;

        public RestartController(IHostApplicationLifetime appLifetime)
        {
            _appLifetime = appLifetime;
        }

        [HttpPost]
        public IActionResult Restart()
        {
            _appLifetime.StopApplication();
            // This is a simplified restart, a more robust solution would be to use a process manager
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sh",
                    Arguments = "quickstart.sh",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            return Ok("Restarting services...");
        }

        [HttpGet("health")]
        public IActionResult GetHealth()
        {
            return Ok("Service is healthy");
        }
    }
}