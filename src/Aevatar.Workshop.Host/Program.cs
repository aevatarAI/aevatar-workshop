using Aevatar.Workshop.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    Log.Information("Starting Aevatar Workshop Host (Orleans Silo)");
    var host = CreateHostBuilder(args).Build();
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

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                  .AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true)
                  .AddEnvironmentVariables();
        })
        .ConfigureServices((context, services) =>
        {
            services.AddApplication<WorkshopHostModule>();
        })
        .UseOrleansConfiguration()
        .UseAutofac()
        .UseSerilog();