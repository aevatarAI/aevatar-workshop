using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Aevatar.GAgents.Executor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                    .Configure<ExceptionSerializationOptions>(options =>
                    {
                        options.SupportedNamespacePrefixes.Add("Volo.Abp");
                        options.SupportedNamespacePrefixes.Add("Newtonsoft.Json");
                        options.SupportedNamespacePrefixes.Add("Autofac.Core");
                        options.SupportedNamespacePrefixes.Add("Aevatar");
                    })
                    .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Information).AddConsole(); })
                    .UseAevatar()
                    ;
            })
            .UseConsoleLifetime();
    }
}