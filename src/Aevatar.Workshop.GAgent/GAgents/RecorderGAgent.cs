using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.GAgent;

[GenerateSerializer]
public class RecorderGAgentState : StateBase
{
    [Id(0)] public List<RecordMessage> ChatMessages { get; set; } = [];
}

[GenerateSerializer]
public class RecordMessage
{
    [Id(0)] public string Sender { get; set; }
    [Id(1)] public string Message { get; set; }

    public override string ToString()
    {
        return $"【{Sender}】: {Message}\n--------------------------------\n";
    }
}

[GenerateSerializer]
public class RecorderStateLogEvent : StateLogEventBase<RecorderStateLogEvent>;

[GenerateSerializer]
public class NewRecordMessageStateLogEvent : RecorderStateLogEvent
{
    [Id(0)] public RecordMessage Message { get; set; }
}

[GAgent("recorder", "demo")]
public class RecorderGAgent : GAgentBase<RecorderGAgentState, RecorderStateLogEvent>
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent to recording chats between Alice and Bob.");
    }

    [EventHandler]
    public async Task OnChatMessage(RecordEvent recordEvent)
    {
        RaiseEvent(new NewRecordMessageStateLogEvent
        {
            Message = new RecordMessage
            {
                Sender = recordEvent.PublisherGrainId.ToString(),
                Message = recordEvent.Message
            }
        });
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(RecorderGAgentState state,
        StateLogEventBase<RecorderStateLogEvent> @event)
    {
        switch (@event)
        {
            case NewRecordMessageStateLogEvent newChatMessageStateLogEvent:
                state.ChatMessages.Add(newChatMessageStateLogEvent.Message);
                Logger.LogInformation("New chat message recorded: {Sender}: {Message}",
                    newChatMessageStateLogEvent.Message.Sender,
                    newChatMessageStateLogEvent.Message.Message);
                break;
        }
    }
}