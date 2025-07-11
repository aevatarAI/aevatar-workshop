using System.Diagnostics;
using Aevatar.Core;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Workshop.Client;
using Microsoft.AspNetCore.Builder;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.Executor;
using Aevatar.Plugins;
using Aevatar.Plugins.DbContexts;
using Aevatar.Plugins.Repositories;
using Microsoft.Extensions.Configuration;

Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

var builder = WebApplication.CreateBuilder(args);

// Add configuration files
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// builder.Services.AddSingleton<IGAgentExecutor, GAgentExecutor>();
// builder.Services.AddSingleton<IGAgentService, GAgentService>();
// builder.Services.AddSingleton<IGAgentManager, GAgentManager>();
// builder.Services.AddSingleton<IPluginGAgentManager, PluginGAgentManager>();
// builder.Services.AddTransient<ITenantPluginCodeRepository, TenantPluginCodeRepository>();
// builder.Services.AddTransient<IPluginCodeStorageRepository, PluginCodeStorageRepository>();
// builder.Services.AddTransient<IPluginLoadStatusRepository, PluginLoadStatusRepository>();
// builder.Services.AddTransient<TenantPluginCodeMongoDbContext>();
// builder.Services.AddTransient<PluginCodeStorageMongoDbContext>();
// builder.Services.AddTransient<PluginLoadStatusMongoDbContext>();

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
// Let the application use ASPNETCORE_URLS from environment variables
// app.Urls.Add(url); // Commented out to use Docker environment variable
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        // Get the actual URL from configuration
        var urls = app.Urls.FirstOrDefault() ?? "http://localhost:80";
        var psi = new ProcessStartInfo
        {
            FileName = urls.Replace("+", "localhost").Replace("*", "localhost").Replace("0.0.0.0", "localhost"),
            UseShellExecute = true
        };
        Process.Start(psi);
    }
    catch { }
});

await app.RunAsync();