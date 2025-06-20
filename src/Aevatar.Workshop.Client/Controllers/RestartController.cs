using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Aevatar.Workshop.Client.Controllers;

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
        try
        {
            var logPath = "restart.log";
            var timestamp = DateTime.UtcNow.ToString("o");
            System.IO.File.AppendAllText(logPath, $"[{timestamp}] Restart endpoint called. Stopping application...\n");

            _appLifetime.StopApplication();

            // This is a simplified restart, a more robust solution would be to use a process manager
            // We use 'nohup' and run the script in the background (&).
            // This detaches the script from the current process, allowing the Client
            // to shut down while the script continues running to restart the services.
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sh",
                    Arguments = "-c \"nohup sh ../../quickstart.sh > /dev/null 2>&1 &\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();

            System.IO.File.AppendAllText(logPath, $"[{timestamp}] quickstart.sh process started in background.\n");

            return Ok("Restarting services in background, you can close current page now.");
        }
        catch (Exception ex)
        {
            var logPath = "restart.log";
            var timestamp = DateTime.UtcNow.ToString("o");
            System.IO.File.AppendAllText(logPath, $"[{timestamp}] ERROR during restart: {ex.ToString()}\n");
            return StatusCode(500, "An error occurred during restart, please run `sh quickstart.sh` manually.");
        }
    }

    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok("Service is healthy");
    }
}