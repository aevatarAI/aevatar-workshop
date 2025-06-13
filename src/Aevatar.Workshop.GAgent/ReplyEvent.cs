using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop.GAgent;

[GenerateSerializer]
public class ReplyEvent : EventBase
{
    [Id(0)] public string Reply { get; set; }
} 