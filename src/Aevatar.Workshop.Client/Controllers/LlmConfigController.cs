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
    
    private static readonly string secretsPath =
        Path.Combine(Directory.GetCurrentDirectory(), "../Aevatar.Workshop.Host/appsettings.secrets.json");

    [HttpGet]
    public async Task<IActionResult> GetLlmConfigs()
    {
        var mergedConfigs = new JObject();
        
        // First, load from appsettings.json
        if (System.IO.File.Exists(configPath))
        {
            var json = await System.IO.File.ReadAllTextAsync(configPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("SystemLLMConfigs", out var configs))
            {
                mergedConfigs = JObject.Parse(configs.ToString());
            }
        }
        
        // Then, load from appsettings.secrets.json and merge (overwrite duplicates)
        if (System.IO.File.Exists(secretsPath))
        {
            var json = await System.IO.File.ReadAllTextAsync(secretsPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("SystemLLMConfigs", out var configs))
            {
                var secretConfigs = JObject.Parse(configs.ToString());
                mergedConfigs.Merge(secretConfigs, new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Replace
                });
            }
        }

        return Ok(mergedConfigs.ToString());
    }

    [HttpPost]
    public async Task<IActionResult> SaveLlmConfigs([FromBody] JsonElement newConfigs)
    {
        // Default to saving in appsettings.secrets.json
        JObject jsonObj;
        
        // If secrets file exists, update it; otherwise create it
        if (System.IO.File.Exists(secretsPath))
        {
            var json = await System.IO.File.ReadAllTextAsync(secretsPath);
            jsonObj = JObject.Parse(json);
        }
        else
        {
            // Create a new structure matching appsettings.json format
            jsonObj = JObject.Parse(@"{
                ""Serilog"": {
                    ""Properties"": {
                        ""Application"": ""Aevatar.Workshop.Host"",
                        ""Environment"": ""Development""
                    },
                    ""MinimumLevel"": {
                        ""Default"": ""Information"",
                        ""Override"": {
                            ""Default"": ""Information"",
                            ""System"": ""Warning"",
                            ""Microsoft"": ""Warning"",
                            ""Orleans"": ""Error""
                        }
                    },
                    ""WriteTo"": [
                        {
                            ""Name"": ""Console""
                        },
                        {
                            ""Name"": ""RollingFile"",
                            ""Args"": {
                                ""pathFormat"": ""Logs/log-{Date}.log"",
                                ""outputTemplate"": ""[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}{Offset:zzz}][{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}"",
                                ""rollOnFileSizeLimit"": true,
                                ""rollingInterval"": ""Day"",
                                ""retainedFileCountLimit"": 15
                            }
                        }
                    ]
                }
            }");
        }
        
        jsonObj["SystemLLMConfigs"] = JObject.Parse(newConfigs.ToString());
        await System.IO.File.WriteAllTextAsync(secretsPath, jsonObj.ToString(Newtonsoft.Json.Formatting.Indented));
        
        return Ok("Configuration saved to appsettings.secrets.json");
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckLlmConfig()
    {
        try
        {
            // Check both files
            var hasConfig = false;
            
            if (System.IO.File.Exists(configPath))
            {
                var json = await System.IO.File.ReadAllTextAsync(configPath);
                using var doc = JsonDocument.Parse(json);
                if (CheckConfigsInDocument(doc))
                {
                    hasConfig = true;
                }
            }
            
            if (!hasConfig && System.IO.File.Exists(secretsPath))
            {
                var json = await System.IO.File.ReadAllTextAsync(secretsPath);
                using var doc = JsonDocument.Parse(json);
                if (CheckConfigsInDocument(doc))
                {
                    hasConfig = true;
                }
            }

            return Ok(new { ok = hasConfig });
        }
        catch (Exception ex)
        {
            return Ok(new { ok = false, error = ex.Message });
        }
    }
    
    private bool CheckConfigsInDocument(JsonDocument doc)
    {
        var root = doc.RootElement;
        if (root.TryGetProperty("SystemLLMConfigs", out var llmConfigs))
        {
            foreach (var llm in llmConfigs.EnumerateObject())
            {
                if (llm.Value.TryGetProperty("ApiKey", out var apiKey) &&
                    !string.IsNullOrWhiteSpace(apiKey.GetString()))
                {
                    return true;
                }
            }
        }
        return false;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetLlmList()
    {
        try
        {
            var keys = new HashSet<string>();
            
            // Read from appsettings.json
            if (System.IO.File.Exists(configPath))
            {
                var json = await System.IO.File.ReadAllTextAsync(configPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("SystemLLMConfigs", out var llmConfigs) &&
                    llmConfigs.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in llmConfigs.EnumerateObject())
                    {
                        keys.Add(prop.Name);
                    }
                }
            }
            
            // Also read from appsettings.secrets.json
            if (System.IO.File.Exists(secretsPath))
            {
                var json = await System.IO.File.ReadAllTextAsync(secretsPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("SystemLLMConfigs", out var llmConfigs) &&
                    llmConfigs.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in llmConfigs.EnumerateObject())
                    {
                        keys.Add(prop.Name);
                    }
                }
            }

            return Ok(keys.ToArray());
        }
        catch
        {
            return Ok(Array.Empty<string>());
        }
    }
}