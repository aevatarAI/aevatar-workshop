using Aevatar.Workshop.Host;
using Aevatar.Workshop.Host.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

// Create a shared runtime configuration provider instance
var runtimeConfigProvider = new RuntimeConfigurationProvider();

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true)
    .Add(new RuntimeConfigurationSource { Provider = runtimeConfigProvider })
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    Log.Information("Starting Aevatar Workshop Host (Orleans Silo)");
    var host = CreateHostBuilder(args, runtimeConfigProvider).Build();
    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly!");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

static IHostBuilder CreateHostBuilder(string[] args, RuntimeConfigurationProvider runtimeConfigProvider) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                  .Add(new RuntimeConfigurationSource { Provider = runtimeConfigProvider })
                  .AddEnvironmentVariables();
        })
        .ConfigureServices((context, services) =>
        {
            // Register the runtime config provider instance
            services.AddSingleton(runtimeConfigProvider);
            services.AddApplication<WorkshopHostModule>();
        })
        .UseOrleansConfiguration()
        .UseAutofac()
        .UseSerilog();