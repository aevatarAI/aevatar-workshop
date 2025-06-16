using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop.GAgent;

[GenerateSerializer]
public class ChatEvent : EventBase
{
    [Id(0)] public string Content { get; set; }
}