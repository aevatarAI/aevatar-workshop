using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.Client;
using Microsoft.AspNetCore.Builder;
using Aevatar.GAgents.AI.Options;
using Microsoft.Extensions.Configuration;

Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

var builder = WebApplication.CreateBuilder(args);

// Add configuration from both appsettings.json and appsettings.secrets.json
builder.Configuration
    .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Aevatar.Workshop.Host"))
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();

// Configure SystemLLMConfigOptions
builder.Services.Configure<SystemLLMConfigOptions>(options =>
{
    var llmConfigs = new Dictionary<string, LLMConfig>();
    
    // Load from configuration (which now includes both files)
    var section = builder.Configuration.GetSection("SystemLLMConfigs");
    if (section.Exists())
    {
        var configs = section.Get<Dictionary<string, LLMConfig>>();
        if (configs != null)
        {
            foreach (var kvp in configs)
            {
                llmConfigs[kvp.Key] = kvp.Value;
            }
        }
    }
    
    options.SystemLLMConfigs = llmConfigs;
    
    // Log loaded configurations for debugging
    Console.WriteLine($"Loaded {llmConfigs.Count} SystemLLMConfigs:");
    foreach (var kvp in llmConfigs)
    {
        Console.WriteLine($"  - {kvp.Key}: {kvp.Value.ProviderEnum} ({kvp.Value.ModelName})");
    }
});

// Orleans client setup
var serviceProvider = await Startup.RunAsync(args);

// Register Orleans services for dependency injection
builder.Services.AddSingleton(serviceProvider.GetRequiredService<IClusterClient>());
builder.Services.AddSingleton(serviceProvider.GetRequiredService<IGAgentFactory>());

var app = builder.Build();

// Serve static files from wwwroot (index.html)
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

// This will map all the endpoints defined in the controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

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