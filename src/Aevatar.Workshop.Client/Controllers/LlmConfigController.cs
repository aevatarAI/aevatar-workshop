using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Executor;
using Aevatar.Workshop.GAgent;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.Client.Controllers;

[Route("api/llm-configs")]
[ApiController]
public class LlmConfigController : ControllerBase
{
    private static readonly string configPath =
        Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

    private static readonly string secretsPath =
        Path.Combine(Directory.GetCurrentDirectory(), "appsettings.secrets.json");

    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentExecutor _gAgentExecutor;
    private readonly ILogger<LlmConfigController> _logger;

    public LlmConfigController(
        IGAgentFactory gAgentFactory,
        IGAgentExecutor gAgentExecutor,
        ILogger<LlmConfigController> logger)
    {
        _gAgentFactory = gAgentFactory;
        _gAgentExecutor = gAgentExecutor;
        _logger = logger;
    }

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
        try
        {
            // Step 1: Save to local file (appsettings.secrets.json)
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

            _logger.LogInformation("Configuration saved to appsettings.secrets.json");

            // Step 2: Sync to host via ConfigManagerGAgent
            try
            {
                var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>();
                var configJson = newConfigs.ToString();

                var updateEvent = new ConfigUpdateEvent
                {
                    ConfigType = "SystemLLMConfigs",
                    ConfigJson = configJson
                };

                _logger.LogInformation("Sending configuration update to host...");

                var responseJson = await _gAgentExecutor.ExecuteGAgentEventHandler(
                    configManager,
                    updateEvent);

                var response = JsonSerializer.Deserialize<ConfigResponseEvent>(responseJson);
                if (response.Success)
                {
                    _logger.LogInformation("Configuration successfully synced to host");
                    return Ok(new
                    {
                        message = "Configuration saved locally and synced to host",
                        localSave = true,
                        hostSync = true
                    });
                }

                _logger.LogWarning("Failed to sync configuration to host: {Error}", response.ErrorMessage);
                return Ok(new
                {
                    message = $"Configuration saved locally but failed to sync to host: {response.ErrorMessage}",
                    localSave = true,
                    hostSync = false,
                    error = response.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing configuration to host");
                return Ok(new
                {
                    message = $"Configuration saved locally but failed to sync to host: {ex.Message}",
                    localSave = true,
                    hostSync = false,
                    error = ex.Message
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration");
            return StatusCode(500, new { error = ex.Message });
        }
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

    [HttpGet("sync-status")]
    public async Task<IActionResult> GetSyncStatus()
    {
        try
        {
            // Check if we can connect to host
            var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>();

            return Ok(new
            {
                connected = true,
                hostAvailable = configManager != null,
                message = "Connected to Orleans cluster"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to connect to Orleans cluster");
            return Ok(new
            {
                connected = false,
                hostAvailable = false,
                message = $"Unable to connect to Orleans cluster: {ex.Message}"
            });
        }
    }
}