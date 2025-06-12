using Aevatar.Core.Abstractions;
using Aevatar.Workshop.Client;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = await Startup.RunAsync(args);
var gAgentFactory = serviceProvider.GetRequiredService<IGAgentFactory>();

int mode = 0;
string greeting = "Hello, Aevatar!";
if (args.Length > 0 && int.TryParse(args[0], out var parsedMode))
{
    mode = parsedMode;
}
if (args.Length > 1)
{
    greeting = args[1];
}

switch (mode)
{
    case 0:
        await EventHandlerDemo.RunAsync(gAgentFactory, greeting);
        break;
    case 1:
        await MultiGAgentDemo.RunAsync(gAgentFactory);
        break;
    case 2:
        await RouterDemo.RunAsync(gAgentFactory);
        break;
    case 3:
        await YourOwnDemo.RunAsync(gAgentFactory);
        break;
    default:
        Console.WriteLine($"Unknown mode: {mode}");
        break;
}

Console.Read();