using Aevatar.Workshop.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.secrets.json", optional: true)
    .AddEnvironmentVariables()
    .Build();
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    Log.Information("Starting Silo");
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
                  .AddJsonFile("appsettings.container.json", optional: true, reloadOnChange: true) // 添加容器化配置
                  .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                  .AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true)
                  .AddEnvironmentVariables(); // 添加环境变量支持
        })
        .ConfigureServices((_, services) => { services.AddApplication<WorkshopHostModule>(); })
        .UseOrleansConfiguration()
        .UseAutofac()
        .UseSerilog();