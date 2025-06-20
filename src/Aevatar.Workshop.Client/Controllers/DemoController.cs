using Microsoft.AspNetCore.Mvc;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Aevatar.Workshop.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IHostEnvironment _environment;

    public DemoController(IGAgentFactory gAgentFactory, IHostEnvironment environment)
    {
        _gAgentFactory = gAgentFactory;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> RunDemo([FromQuery] int mode, [FromQuery] string? greeting = null,
        [FromQuery] int number = 42, [FromQuery] string? systemLLM = null)
    {
        if (string.IsNullOrEmpty(greeting))
            greeting = "Hello, Aevatar!";
        if (number is <= 0 or > 100)
            number = 42;
        if (string.IsNullOrWhiteSpace(systemLLM))
            systemLLM = "OpenAI";

        try
        {
            switch (mode)
            {
                case 0:
                    await EventHandlerDemo.RunAsync(_gAgentFactory, greeting);
                    return Ok(
                        $"EventHandlerDemo completed with greeting: {greeting}\nYou can refresh host's log to see the event handling details.");
                case 1:
                    await MultiGAgentDemo.RunAsync(_gAgentFactory, number, systemLLM);
                    return Ok(
                        $"MultiGAgentDemo completed. Secret number was {number}.\nYou can refresh host's log to see the event handling details.");
                case 2:
                    await RouterDemo.RunAsync(_gAgentFactory, systemLLM);
                    return Ok(
                        "RouterDemo completed.\nYou can refresh host's log to see the event handling details.\nRefresh client's log to see the final report.");
                case 3:
                    await YourOwnDemo.RunAsync(_gAgentFactory);
                    return Ok("YourOwnDemo completed.");
                default:
                    return BadRequest(new { error = "InvalidMode", message = $"Unknown mode: {mode}" });
            }
        }
        catch (Exception ex)
        {
            var currentEx = ex;
            while (currentEx != null)
            {
                if (currentEx.GetType().Name.Contains("AIHttpOperationException"))
                {
                    var message = currentEx.Message;
                    var friendlyMessageRegex = new System.Text.RegularExpressions.Regex(@"(The request failed because your account has an overdue balance\..*?)\n");
                    var match = friendlyMessageRegex.Match(message);
                    if (match.Success)
                    {
                        return BadRequest(new { error = "AIHttpOperationException", message = match.Groups[1].Value.Trim() });
                    }

                    return BadRequest(new { error = "AIHttpOperationException", message = "The API key configuration might be incorrect. Please check your LLM configuration." });
                }
                currentEx = currentEx.InnerException;
            }

            if (_environment.IsDevelopment())
            {
                return StatusCode(500,
                    new { error = ex.GetType().Name, message = ex.Message, stackTrace = ex.StackTrace });
            }

            return StatusCode(500,
                new
                {
                    error = "InternalServerError", message = "An unexpected error occurred. Please try again later."
                });
        }
    }
}