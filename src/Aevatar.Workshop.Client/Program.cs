using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.Client;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Orleans client setup
var serviceProvider = await Startup.RunAsync(args);
var gAgentFactory = serviceProvider.GetRequiredService<IGAgentFactory>();

var app = builder.Build();

// Serve static files from wwwroot (index.html)
app.UseDefaultFiles();
app.UseStaticFiles();

// API endpoint to run demos
app.MapGet("/run", async (HttpContext context) =>
{
    var modeStr = context.Request.Query["mode"].ToString();
    var greeting = context.Request.Query["greeting"].ToString();
    var numberStr = context.Request.Query["number"].ToString();
    var systemLLM = context.Request.Query["systemLLM"].ToString();
    int mode = 0;
    if (!string.IsNullOrEmpty(modeStr) && int.TryParse(modeStr, out var parsedMode))
        mode = parsedMode;
    if (string.IsNullOrEmpty(greeting))
        greeting = "Hello, Aevatar!";
    int number = 42;
    if (!string.IsNullOrEmpty(numberStr) && int.TryParse(numberStr, out var parsedNumber) && parsedNumber >= 1 && parsedNumber <= 100)
        number = parsedNumber;
    if (string.IsNullOrWhiteSpace(systemLLM))
        systemLLM = "OpenAI";
    try
    {
        switch (mode)
        {
            case 0:
                await EventHandlerDemo.RunAsync(gAgentFactory, greeting);
                return Results.Text($"EventHandlerDemo completed with greeting: {greeting}\nYou can refresh host's log to see the event handling details.");
            case 1:
                await MultiGAgentDemo.RunAsync(gAgentFactory, number, systemLLM);
                return Results.Text($"MultiGAgentDemo completed. Secret number was {number}.\nYou can refresh host's log to see the event handling details.");
            case 2:
                await RouterDemo.RunAsync(gAgentFactory, systemLLM);
                return Results.Text("RouterDemo completed.\nYou can refresh host's log to see the event handling details.\nRefresh client's log to see the final report.");
            case 3:
                await YourOwnDemo.RunAsync(gAgentFactory);
                return Results.Text("YourOwnDemo completed.");
            default:
                return Results.Text($"Unknown mode: {mode}");
        }
    }
    catch (Exception ex)
    {
        return Results.Text($"Error: {ex.Message}\n{ex.StackTrace}");
    }
});

// API endpoint to get last 100 lines of host.log
app.MapGet("/hostlog", async (HttpContext _) =>
{
    var logPath = Environment.GetEnvironmentVariable("HOST_LOG_PATH") ?? "host.log";
    if (!File.Exists(logPath))
        return Results.Text($"host.log not found at {logPath}");
    var lines = await File.ReadAllLinesAsync(logPath);
    var lastLines = string.Join("\n", lines.Skip(Math.Max(0, lines.Length - 100)));
    return Results.Text(lastLines, "text/plain");
});

// API endpoint to get last 100 lines of client.log
app.MapGet("/clientlog", async (HttpContext _) =>
{
    var logPath = Environment.GetEnvironmentVariable("CLIENT_LOG_PATH") ?? "client.log";
    if (!File.Exists(logPath))
        return Results.Text($"client.log not found at {logPath}");
    var lines = await File.ReadAllLinesAsync(logPath);
    var lastLines = string.Join("\n", lines.Skip(Math.Max(0, lines.Length - 100)));
    return Results.Text(lastLines, "text/plain");
});

// API endpoint to get MultiGAgentDemo chat messages
app.MapGet("/multichat", async (HttpContext context) =>
{
    try
    {
        if (Common.Recorder == null)
            return Results.Text("No recorder available.");
        var state = await Common.Recorder.GetStateAsync();
        if (state?.ChatMessages == null)
            return Results.Text("No chat records.");
        var messages = string.Join("\n", state.ChatMessages);
        return Results.Text(messages, "text/plain");
    }
    catch (Exception ex)
    {
        return Results.Text($"Error: {ex.Message}\n{ex.StackTrace}");
    }
});

// API endpoint to restart services via quickstart.sh
app.MapPost("/restart", async (HttpContext context) =>
{
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            ArgumentList = { "-c", "nohup sh quickstart.sh > restart.log 2>&1 &" },
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(psi);
        return Results.Text("Restart triggered. Services will restart in a few seconds.");
    }
    catch (Exception ex)
    {
        return Results.Text($"Failed to restart: {ex.Message}");
    }
});

// API endpoint to check if OpenAI ApiKey is configured
app.MapGet("/check-config", async (HttpContext context) =>
{
    try
    {
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "../Aevatar.Workshop.Host/appsettings.json");
        if (!File.Exists(configPath))
            return Results.Json(new { ok = false, error = "Config file not found" });
        var json = await File.ReadAllTextAsync(configPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("SystemLLMConfigs", out var llmConfigs) &&
            llmConfigs.TryGetProperty("OpenAI", out var openai) &&
            openai.TryGetProperty("ApiKey", out var apiKey))
        {
            var key = apiKey.GetString();
            if (!string.IsNullOrWhiteSpace(key))
                return Results.Json(new { ok = true });
        }
        return Results.Json(new { ok = false });
    }
    catch (Exception ex)
    {
        return Results.Json(new { ok = false, error = ex.Message });
    }
});

// API endpoint to get list of available LLM system keys
app.MapGet("/llm-list", async (HttpContext context) =>
{
    Console.WriteLine(100);
    try
    {
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "../Aevatar.Workshop.Host/appsettings.json");
        if (!File.Exists(configPath))
            return Results.Json(Array.Empty<string>());
        var json = await File.ReadAllTextAsync(configPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("SystemLLMConfigs", out var llmConfigs) && llmConfigs.ValueKind == JsonValueKind.Object)
        {
            var keys = llmConfigs.EnumerateObject().Select(p => p.Name).ToArray();
            return Results.Json(keys);
        }
        return Results.Json(Array.Empty<string>());
    }
    catch
    {
        return Results.Json(Array.Empty<string>());
    }
});

// Launch browser on startup
const string url = "http://localhost:5000";
app.Urls.Add(url);
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        };
        Process.Start(psi);
    }
    catch { }
});

await app.RunAsync();