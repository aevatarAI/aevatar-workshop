using Aevatar.Core.Abstractions;
using Orleans.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Placement;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.Extensions;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.AddMemoryGrainStorage("Default")
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
            .AddMemoryGrainStorage("PubSubStore")
            .AddLogStorageBasedLogConsistencyProvider()
            .UseLocalhostClustering()
            .Configure<SiloOptions>(options =>
            {
                options.SiloName = $"WorkshopSilo-{Guid.NewGuid().ToString("N")[..6]}";
            })
            .ConfigureLogging(logging => logging.AddConsole());
    })
    .UseConsoleLifetime();

builder.ConfigureServices((context, services) =>
{
    services.AddPlacementDirector<SiloNamePatternPlacement, SiloNamePatternPlacementDirector>();
    services.Configure<SystemLLMConfigOptions>(context.Configuration);
    services.AddSemanticKernel();
});

using var host = builder.Build();

await host.RunAsync();