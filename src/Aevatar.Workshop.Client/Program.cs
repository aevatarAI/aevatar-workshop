using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.Client;
using Microsoft.AspNetCore.Builder;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.Executor;
using Microsoft.Extensions.Configuration;
using Aevatar.Workshop.Client.Services;

Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

var builder = WebApplication.CreateBuilder(args);

// Add configuration files
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add secrets file with error handling
try
{
    builder.Configuration.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Failed to load appsettings.secrets.json: {ex.Message}");
    Console.WriteLine("The application will continue without this configuration file.");
}

builder.Configuration.AddEnvironmentVariables();

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

// Add configuration sync service
builder.Services.AddHostedService<ConfigSyncService>();

// Add localization service
builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

// Orleans client setup
var serviceProvider = await Startup.RunAsync(args);

// Register Orleans services for dependency injection
builder.Services.AddSingleton(serviceProvider.GetRequiredService<IClusterClient>());
builder.Services.AddSingleton(serviceProvider.GetRequiredService<IGAgentFactory>());
builder.Services.AddSingleton(serviceProvider.GetRequiredService<IGAgentExecutor>());
builder.Services.AddSingleton(serviceProvider.GetRequiredService<IGAgentService>());

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
// Let the application use ASPNETCORE_URLS from environment variables
// app.Urls.Add(url); // Commented out to use Docker environment variable
app.Lifetime.ApplicationStarted.Register(() =>
{
    var urls = app.Urls;
    var url = urls.FirstOrDefault() ?? "http://localhost:5000";

    Console.WriteLine($"Application started. Access the demos at: {url}");

    // Try to open browser (may not work in Docker)
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch
    {
        Console.WriteLine($"Could not launch browser. Please open {url} manually.");
    }
});

await app.RunAsync();