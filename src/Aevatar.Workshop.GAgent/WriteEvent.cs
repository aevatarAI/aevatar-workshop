using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop.Events;

[Description("Write something.")]
[GenerateSerializer]
public class WriteEvent : EventBase
{
    [Description("The content to be write.")]
    [Id(0)]
    public string Content { get; set; }
}