using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.GAgent;

[GenerateSerializer]
public class BobGAgentState : AIGAgentStateBase
{
    [Id(0)] public List<ChatMessage> ChatMessages { get; set; } = [];
}

[GenerateSerializer]
public class BobStateLogEvent : StateLogEventBase<BobStateLogEvent>;

[GenerateSerializer]
public class NewBobChatStateLogEvent : BobStateLogEvent
{
    [Id(0)] public ChatMessage ChatMessage { get; set; }
}

public interface IBobGAgent : IAIGAgent, IGAgent
{
    Task StartGuessingAsync();
}

[GAgent("bob", "demo")]
public class BobGAgent : AIGAgentBase<BobGAgentState, BobStateLogEvent>, IBobGAgent
{
    private const string Prompt =
        """
        You are Bob, and your goal is to guess Alice's secret number (an integer between 1-100).
        Guess only one integer at a time.
        Alice will provide feedback as 'high','low', or 'correct'.

        Please comply with:
        1. Guess only one number at a time, format: Guess: [number] (e.g. Guess: 50)
        2. Use efficient methods such as binary search (guess correctly within 7 times)
        3. Narrow down the range based on feedback and do not guess numbers beyond the current possible range
        When you receive 'correct', you win.

        Please begin your first guess:
        """;

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent stands for Bob.");
    }

    public async Task StartGuessingAsync()
    {
        await PublishAsync(new RecordEvent
        {
            Message = $"[Prompt for Bob]: \n{Prompt}",
        });
        var chatResult = await ChatWithHistory(Prompt);
        if (chatResult is { Count: > 0 })
        {
            RaiseEvent(new NewBobChatStateLogEvent
            {
                ChatMessage = chatResult[0]
            });
            var firstMessage = chatResult[0].Content;
            Logger.LogInformation($"Bob's initial guess: {firstMessage}");
            await PublishAsync(new ChatEvent
            {
                Content = firstMessage
            });
            await PublishAsync(new RecordEvent
            {
                Message = firstMessage
            });
            RaiseEvent(new NewBobChatStateLogEvent
            {
                ChatMessage = new ChatMessage
                {
                    Content = firstMessage,
                    ChatRole = ChatRole.User
                }
            });
            await ConfirmEvents();
        }
        else
        {
            Logger.LogError("Failed to get initial guess from chat.");
        }
    }

    [EventHandler]
    public async Task GuessNumberAsync(ChatEvent chatEvent)
    {
        var message = $"Alice's reply: {chatEvent.Content}";
        Logger.LogInformation($"Bob received message: {message}");
        RaiseEvent(new NewBobChatStateLogEvent
        {
            ChatMessage = new ChatMessage
            {
                Content = message,
                ChatRole = ChatRole.User
            }
        });
        var chatResult = await ChatWithHistory(Prompt, State.ChatMessages);
        await PublishAsync(new ChatEvent
        {
            Content = chatResult[0].Content
        });
        await PublishAsync(new RecordEvent
        {
            Message = chatResult[0].Content
        });
        RaiseEvent(new NewBobChatStateLogEvent
        {
            ChatMessage = chatResult[0]
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public Task HandleReplyAsync(ReplyEvent eventData)
    {
        Logger.LogInformation($"Bob received reply: {eventData.Reply}");
        return Task.CompletedTask;
    }

    protected override void AIGAgentTransitionState(BobGAgentState state, StateLogEventBase<BobStateLogEvent> @event)
    {
        switch (@event)
        {
            case NewBobChatStateLogEvent newBobChatStateLogEvent:
                state.ChatMessages.Add(newBobChatStateLogEvent.ChatMessage);
                break;
            default:
                Logger.LogWarning("Unhandled event type: {EventType}", @event.GetType().Name);
                break;
        }
    }
}