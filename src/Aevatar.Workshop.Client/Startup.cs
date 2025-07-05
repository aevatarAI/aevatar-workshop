using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Aevatar.GAgents.Executor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace Aevatar.Workshop.Client;

public static class Startup
{
    public static async Task<IServiceProvider> RunAsync(string[] args)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .UseOrleansClient(client =>
            {
                client.UseLocalhostClustering()
                    .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
                    .UseAevatar(true);
                client.Services.AddTransient<IGAgentService, GAgentService>();
                client.Services.AddTransient<IGAgentExecutor, GAgentExecutor>();
            })
            .ConfigureLogging(logging => logging.AddConsole())
            .UseConsoleLifetime();

        var host = builder.Build();
        await host.StartAsync();
        return host.Services;
    }
}