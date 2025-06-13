using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.Host;

public static class OrleansHostExtension
{
    public static IHostBuilder UseOrleansConfiguration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleans((context, siloBuilder) =>
            {
                siloBuilder
                    .UseLocalhostClustering()
                    .AddMemoryGrainStorage("Default")
                    .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
                    .AddMemoryGrainStorage("PubSubStore")
                    .AddLogStorageBasedLogConsistencyProvider()
                    .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Information).AddConsole(); })
                    .UseAevatar()
                    ;
            })
            .UseConsoleLifetime();
    }
}