using System;
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
                siloBuilder
                    .UseLocalhostClustering()
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
}