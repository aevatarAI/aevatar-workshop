using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.Client;
using Microsoft.AspNetCore.Builder;

Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// Orleans client setup
var serviceProvider = await Startup.RunAsync(args);

// Register IGAgentFactory for dependency injection
builder.Services.AddSingleton(serviceProvider.GetRequiredService<IGAgentFactory>());

var app = builder.Build();

// Serve static files from wwwroot (index.html)
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

// This will map all the endpoints defined in the controllers
app.MapControllers();

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