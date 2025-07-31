using System;
using System.Linq;
using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Aevatar.GAgents.Executor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;

namespace Aevatar.Workshop.Client;

public static class Startup
{
    public static async Task<IServiceProvider> RunAsync(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .UseOrleansClient(client =>
            {
                var gatewayHost = Environment.GetEnvironmentVariable("ORLEANS_GATEWAY_HOST") ?? "localhost";
                var gatewayPort = int.Parse(Environment.GetEnvironmentVariable("ORLEANS_GATEWAY_PORT") ?? "30000");
                var isDocker = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
                
                if (isDocker && gatewayHost != "localhost")
                {
                    // Container environment: use static cluster configuration to connect to Host container
                    try
                    {
                        var hostEntry = System.Net.Dns.GetHostEntry(gatewayHost);
                        var ipAddress = hostEntry.AddressList.First();
                        client.UseStaticClustering(options =>
                        {
                            options.Gateways.Add(new System.Uri($"gwy.tcp://{ipAddress}:{gatewayPort}"));
                        });
                    }
                    catch (Exception ex)
                    {
                        // Fallback to direct hostname
                        client.UseStaticClustering(options =>
                        {
                            options.Gateways.Add(new System.Uri($"gwy.tcp://{gatewayHost}:{gatewayPort}"));
                        });
                    }
                }
                else
                {
                    // Local development environment or localhost configuration
                    client.UseLocalhostClustering(gatewayPort);
                }
                
                client.AddMemoryStreams(AevatarCoreConstants.StreamProvider)
                    .Configure<ClientMessagingOptions>(options =>
                    {
                        options.ResponseTimeout = TimeSpan.FromMinutes(2);
                        options.ResponseTimeoutWithDebugger = TimeSpan.FromMinutes(2);
                    })
                    .UseAevatar(true);
                client.Services.AddTransient<IGAgentService, GAgentService>();
                client.Services.AddTransient<IGAgentExecutor, GAgentExecutor>();
                client.Services.AddHostedService<Services.ConfigSyncService>();
            })
            .ConfigureLogging(logging => logging.AddConsole())
            .UseConsoleLifetime();

        var host = builder.Build();
        await host.StartAsync();
        return host.Services;
    }
}