using System.Net;
using System.Net.Sockets;
using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Aevatar.GAgents.Executor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Networking.Shared;
using Orleans.Serialization;

namespace Aevatar.Workshop.Host;

public static class OrleansHostExtension
{
    public static IHostBuilder UseOrleansConfiguration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleans((context, siloBuilder) =>
            {
                siloBuilder.Services.AddTransient<IGAgentExecutor, GAgentExecutor>();
                siloBuilder.Services.AddTransient<IGAgentService, GAgentService>();
                var siloPort = int.Parse(Environment.GetEnvironmentVariable("ORLEANS_SILO_PORT") ?? "11111");
                var gatewayPort = int.Parse(Environment.GetEnvironmentVariable("ORLEANS_GATEWAY_PORT") ?? "30000");
                var isDocker = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

                if (isDocker)
                {
                    // Container environment: use special configuration
                    var advertisedHost = Environment.GetEnvironmentVariable("ORLEANS_ADVERTISED_HOST");

                    if (!string.IsNullOrEmpty(advertisedHost))
                    {
                        // If broadcast address is specified, use it
                        siloBuilder.UseLocalhostClustering(siloPort, gatewayPort)
                            .Configure<EndpointOptions>(options =>
                            {
                                options.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Any, gatewayPort);
                                options.SiloListeningEndpoint = new IPEndPoint(IPAddress.Any, siloPort);

                                // Try to resolve broadcast address
                                if (IPAddress.TryParse(advertisedHost, out var advertisedIP))
                                {
                                    options.AdvertisedIPAddress = advertisedIP;
                                }
                                else
                                {
                                    // If not IP, try DNS resolution
                                    try
                                    {
                                        var hostEntry = Dns.GetHostEntry(advertisedHost);
                                        options.AdvertisedIPAddress = hostEntry.AddressList.First(ip =>
                                            ip.AddressFamily == AddressFamily.InterNetwork);
                                    }
                                    catch
                                    {
                                        // If resolution fails, get local IP
                                        options.AdvertisedIPAddress = GetLocalIPAddress();
                                    }
                                }
                            });
                    }
                    else
                    {
                        // Auto-detect local IP
                        var localIP = GetLocalIPAddress();
                        siloBuilder.UseLocalhostClustering(siloPort, gatewayPort)
                            .Configure<EndpointOptions>(options =>
                            {
                                options.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Any, gatewayPort);
                                options.SiloListeningEndpoint = new IPEndPoint(IPAddress.Any, siloPort);
                                options.AdvertisedIPAddress = localIP;
                            });
                    }
                }
                else
                {
                    // Local development environment
                    siloBuilder.UseLocalhostClustering();
                }

                siloBuilder
                    .AddMemoryGrainStorage("Default")
                    .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
                    .AddMemoryGrainStorage("PubSubStore")
                    .AddLogStorageBasedLogConsistencyProvider()
                    .Configure<SiloMessagingOptions>(options =>
                    {
                        options.ResponseTimeout = TimeSpan.FromMinutes(2);
                        options.SystemResponseTimeout = TimeSpan.FromMinutes(2);
                    })
                    .Configure<ExceptionSerializationOptions>(options =>
                    {
                        options.SupportedNamespacePrefixes.Add("Volo.Abp");
                        options.SupportedNamespacePrefixes.Add("Newtonsoft.Json");
                        options.SupportedNamespacePrefixes.Add("Autofac.Core");
                        options.SupportedNamespacePrefixes.Add("Aevatar");
                    })
                    .Configure<SocketConnectionOptions>(options =>
                    {
                        options.NoDelay = true; // Disable Nagle algorithm (default, but explicit)
                        options.KeepAlive = false; // Disable keep-alive to prevent socket closure
                        options.KeepAliveTimeSeconds = 90; // Adjust if KeepAlive is enabled
                        options.KeepAliveIntervalSeconds = 30;
                        options.KeepAliveRetryCount = 10;
                    })
                    .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Information).AddConsole(); })
                    .UseAevatar()
                    ;
            })
            .UseConsoleLifetime();
    }

    private static IPAddress GetLocalIPAddress()
    {
        try
        {
            // Get all network interfaces
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    // Exclude Docker's default bridge address
                    if (!ip.ToString().StartsWith("172.17."))
                    {
                        return ip;
                    }
                }
            }

            // If no suitable IP found, return first non-loopback IPv4 address
            var firstIPv4 = host.AddressList.FirstOrDefault(ip =>
                ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));

            return firstIPv4 ?? IPAddress.Loopback;
        }
        catch
        {
            // If acquisition fails, return loopback address
            return IPAddress.Loopback;
        }
    }
}