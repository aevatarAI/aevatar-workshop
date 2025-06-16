using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.Client;

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
    int mode = 0;
    if (!string.IsNullOrEmpty(modeStr) && int.TryParse(modeStr, out var parsedMode))
        mode = parsedMode;
    if (string.IsNullOrEmpty(greeting))
        greeting = "Hello, Aevatar!";
    try
    {
        switch (mode)
        {
            case 0:
                await EventHandlerDemo.RunAsync(gAgentFactory, greeting);
                return Results.Text($"EventHandlerDemo completed with greeting: {greeting}\nYou can refresh host's log to see the event handling details.");
            case 1:
                await MultiGAgentDemo.RunAsync(gAgentFactory);
                return Results.Text("MultiGAgentDemo completed.\nYou can refresh host's log to see the event handling details.");
            case 2:
                await RouterDemo.RunAsync(gAgentFactory);
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