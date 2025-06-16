using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent;

namespace Aevatar.Workshop.Client;

public static class Common
{
    // Add this static field for chat recorder
    public static IStateGAgent<RecorderGAgentState> Recorder;
}