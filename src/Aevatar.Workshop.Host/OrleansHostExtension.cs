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
                    // 容器环境：使用特殊配置
                    var advertisedHost = Environment.GetEnvironmentVariable("ORLEANS_ADVERTISED_HOST");

                    if (!string.IsNullOrEmpty(advertisedHost))
                    {
                        // 如果指定了广播地址，使用它
                        siloBuilder.UseLocalhostClustering(siloPort, gatewayPort)
                            .Configure<EndpointOptions>(options =>
                            {
                                options.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Any, gatewayPort);
                                options.SiloListeningEndpoint = new IPEndPoint(IPAddress.Any, siloPort);

                                // 尝试解析广播地址
                                if (IPAddress.TryParse(advertisedHost, out var advertisedIP))
                                {
                                    options.AdvertisedIPAddress = advertisedIP;
                                }
                                else
                                {
                                    // 如果不是IP，尝试DNS解析
                                    try
                                    {
                                        var hostEntry = Dns.GetHostEntry(advertisedHost);
                                        options.AdvertisedIPAddress = hostEntry.AddressList.First(ip =>
                                            ip.AddressFamily == AddressFamily.InterNetwork);
                                    }
                                    catch
                                    {
                                        // 如果解析失败，获取本机IP
                                        options.AdvertisedIPAddress = GetLocalIPAddress();
                                    }
                                }
                            });
                    }
                    else
                    {
                        // 自动检测本机IP
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
                    // 本地开发环境
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
            // 获取所有网络接口
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    // 排除Docker的默认网桥地址
                    if (!ip.ToString().StartsWith("172.17."))
                    {
                        return ip;
                    }
                }
            }

            // 如果没找到合适的IP，返回第一个非环回IPv4地址
            var firstIPv4 = host.AddressList.FirstOrDefault(ip =>
                ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));

            return firstIPv4 ?? IPAddress.Loopback;
        }
        catch
        {
            // 如果获取失败，返回环回地址
            return IPAddress.Loopback;
        }
    }
}