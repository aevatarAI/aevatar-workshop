using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace Aevatar.Workshop.Client.Controllers;

[Route("api/llm-configs")]
[ApiController]
public class LlmConfigController : ControllerBase
{
    private static readonly string configPath =
        Path.Combine(Directory.GetCurrentDirectory(), "../Aevatar.Workshop.Host/appsettings.json");

    [HttpGet]
    public async Task<IActionResult> GetLlmConfigs()
    {
        if (!System.IO.File.Exists(configPath))
            return NotFound("Config file not found");

        var json = await System.IO.File.ReadAllTextAsync(configPath);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("SystemLLMConfigs", out var configs))
        {
            return Ok(configs.ToString());
        }

        return Ok("{}");
    }

    [HttpPost]
    public async Task<IActionResult> SaveLlmConfigs([FromBody] JsonElement newConfigs)
    {
        if (!System.IO.File.Exists(configPath))
            return NotFound("Config file not found");

        var json = await System.IO.File.ReadAllTextAsync(configPath);

        var jsonObj = JObject.Parse(json);
        jsonObj["SystemLLMConfigs"] = JObject.Parse(newConfigs.ToString());

        await System.IO.File.WriteAllTextAsync(configPath, jsonObj.ToString());
        return Ok("Configuration saved.");
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckLlmConfig()
    {
        try
        {
            if (!System.IO.File.Exists(configPath))
                return Ok(new { ok = false, error = "Config file not found" });
            var json = await System.IO.File.ReadAllTextAsync(configPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("SystemLLMConfigs", out var llmConfigs))
            {
                foreach (var llm in llmConfigs.EnumerateObject())
                {
                    if (llm.Value.TryGetProperty("ApiKey", out var apiKey) &&
                        !string.IsNullOrWhiteSpace(apiKey.GetString()))
                    {
                        return Ok(new { ok = true });
                    }
                }
            }

            return Ok(new { ok = false });
        }
        catch (Exception ex)
        {
            return Ok(new { ok = false, error = ex.Message });
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetLlmList()
    {
        try
        {
            if (!System.IO.File.Exists(configPath))
                return Ok(Array.Empty<string>());
            var json = await System.IO.File.ReadAllTextAsync(configPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("SystemLLMConfigs", out var llmConfigs) &&
                llmConfigs.ValueKind == JsonValueKind.Object)
            {
                var keys = llmConfigs.EnumerateObject().Select(p => p.Name).ToArray();
                return Ok(keys);
            }

            return Ok(Array.Empty<string>());
        }
        catch
        {
            return Ok(Array.Empty<string>());
        }
    }
}