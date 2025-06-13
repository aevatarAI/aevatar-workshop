using Aevatar.Workshop.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.secrets.json", optional: true)
    .AddJsonFile("appsettings.json")
    .Build();
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    Log.Information("Starting Silo");
    await CreateHostBuilder(args).RunConsoleAsync();
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
        .ConfigureServices((_, services) => { services.AddApplication<WorkshopHostModule>(); })
        .UseOrleansConfiguration()
        .UseAutofac()
        .UseSerilog();