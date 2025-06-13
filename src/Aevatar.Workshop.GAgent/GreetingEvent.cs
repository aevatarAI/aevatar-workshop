using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop.GAgent;

[GenerateSerializer]
public class GreetingEvent : EventBase
{
    [Id(0)] public string Greeting { get; set; }
}