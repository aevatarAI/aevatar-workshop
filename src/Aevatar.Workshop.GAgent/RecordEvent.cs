using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop.GAgent;

[GenerateSerializer]
public class RecordEvent : EventBase
{
    [Id(0)] public string Message { get; set; }
}