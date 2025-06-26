using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Microsoft.SemanticKernel;

namespace Aevatar.Workshop;

public class GAgentToolPlugin
{
    private readonly GAgentPlugin _gAgentPlugin;
    private readonly IGAgentFactory _gAgentFactory;

    public GAgentToolPlugin(GAgentPlugin gAgentPlugin, IGAgentFactory gAgentFactory)
    {
        _gAgentPlugin = gAgentPlugin;
        _gAgentFactory = gAgentFactory;
    }

    [KernelFunction("call_gagent")]
    [Description("Call any GAgent in the system by its grain ID")]
    public async Task<string> CallGAgent(
        [Description("The alias of the target GAgent")]
        string alias,
        [Description("The namespace of the target GAgent")]
        string ns,
        [Description("The event type")] string eventTypeName,
        [Description("The event data as JSON string")]
        string eventJson)
    {
        try
        {
            var @event = DeserializeEvent(eventTypeName, eventJson);
            var targetGAgent = await _gAgentFactory.GetGAgentAsync(ns, alias);
            var grainId = targetGAgent.GetGrainId();
            return await _gAgentPlugin.ExecuteGAgentEventHandler(grainId, @event);
        }
        catch (Exception ex)
        {
            return $"Error calling GAgent {ns}.{alias}: {ex.Message}";
        }
    }

    private static EventBase DeserializeEvent(string eventTypeName, string eventJson) =>
        new EventDeserializer().DeserializeEvent(eventJson, eventTypeName);
}