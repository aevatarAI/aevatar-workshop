using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Microsoft.SemanticKernel;

namespace Aevatar.Workshop;

public class GAgentToolPlugin
{
    private readonly GAgentExecutor _gAgentExecutor;
    private readonly IGAgentFactory _gAgentFactory;

    public GAgentToolPlugin(GAgentExecutor gAgentExecutor, IGAgentFactory gAgentFactory)
    {
        _gAgentExecutor = gAgentExecutor;
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
            return await _gAgentExecutor.ExecuteGAgentEventHandler(grainId, @event);
        }
        catch (Exception ex)
        {
            return $"Error calling GAgent {ns}.{alias}: {ex.Message}";
        }
    }

    private static EventBase DeserializeEvent(string eventTypeName, string eventJson) =>
        new EventDeserializer().DeserializeEvent(eventJson, eventTypeName);
}