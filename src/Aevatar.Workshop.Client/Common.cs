using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent;
using System.Collections.Concurrent;

namespace Aevatar.Workshop.Client;

public static class Common
{
    // Support multiple named recorders for different demos
    private static readonly ConcurrentDictionary<string, IStateGAgent<RecorderGAgentState>> Recorders = new();

    public static void SetRecorder(string demoKey, IStateGAgent<RecorderGAgentState> recorder)
        => Recorders[demoKey] = recorder;

    public static IStateGAgent<RecorderGAgentState>? GetRecorder(string demoKey)
        => Recorders.TryGetValue(demoKey, out var rec) ? rec : null;
}