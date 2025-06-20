using Microsoft.AspNetCore.Mvc;

namespace Aevatar.Workshop.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogController : ControllerBase
{
    [HttpGet("host")]
    public async Task<IActionResult> GetHostLog()
    {
        var logPath = Environment.GetEnvironmentVariable("HOST_LOG_PATH") ?? "host.log";
        if (!System.IO.File.Exists(logPath))
            return NotFound($"host.log not found at {logPath}");
        var lines = await System.IO.File.ReadAllLinesAsync(logPath);
        var lastLines = string.Join("\n", lines.Skip(Math.Max(0, lines.Length - 100)));
        return Content(lastLines, "text/plain");
    }

    [HttpGet("client")]
    public async Task<IActionResult> GetClientLog()
    {
        var logPath = Environment.GetEnvironmentVariable("CLIENT_LOG_PATH") ?? "client.log";
        if (!System.IO.File.Exists(logPath))
            return NotFound($"client.log not found at {logPath}");
        var lines = await System.IO.File.ReadAllLinesAsync(logPath);
        var lastLines = string.Join("\n", lines.Skip(Math.Max(0, lines.Length - 100)));
        return Content(lastLines, "text/plain");
    }

    [HttpGet("chat")]
    public async Task<IActionResult> GetChatLog([FromQuery] string demo)
    {
        if (string.IsNullOrEmpty(demo))
        {
            return BadRequest("Demo name is required.");
        }

        var recorder = Common.GetRecorder(demo);
        if (recorder == null)
        {
            return Ok("");
        }

        var state = await recorder.GetStateAsync();
        var messages = state.ChatMessages.Select(e => e.ToString());
        return Content(string.Join("\n", messages), "text/plain");
    }
}