using Aevatar.Core.Abstractions;
using Aevatar.Workshop.Client;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = await Startup.RunAsync(args);
var gAgentFactory = serviceProvider.GetRequiredService<IGAgentFactory>();

