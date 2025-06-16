using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Microsoft.Extensions.Logging;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;

namespace Aevatar.Workshop.GAgent;

[GenerateSerializer]
public class AliceGAgentState : AIGAgentStateBase
{
    [Id(0)] public int SecretNumber { get; set; }
    [Id(1)] public List<ChatMessage> ChatMessages { get; set; } = [];
}

[GenerateSerializer]
public class AliceStateLogEvent : StateLogEventBase<AliceStateLogEvent>;

[GenerateSerializer]
public class SetSecretNumberStateLogEvent : AliceStateLogEvent
{
    [Id(0)] public int SecretNumber { get; set; }
    [Id(1)] public List<ChatMessage> ChatMessages { get; set; }
}

[GenerateSerializer]
public class NewAliceChatStateLogEvent : AliceStateLogEvent
{
    [Id(0)] public ChatMessage ChatMessage { get; set; }
}

public interface IAliceGAgent : IAIGAgent, IGAgent
{
    Task PrepareAsync(int secretNumber);
}

[GAgent("alice", "demo")]
public class AliceGAgent : AIGAgentBase<AliceGAgentState, AliceStateLogEvent>,
    IAliceGAgent
{
    private const string Prompt =
        """
        You are Alice, and you know that the secret number is {SECRET_NUMBER}(don't say it).
        When Bob submits an integer guess between 1-100, please provide feedback according to the following rules:
        - Guessing number is greater than Secret numbers -> "high"
        - Guessing number is lower than secret number -> 'low'
        - Guessing number equals secret number -> 'correct'
        - If the submitted integer is not 1-100 -> 'invalid guessing'

        Apart from these feedbacks, no other information is provided.
        """;

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent stands for Alice.");
    }

    private string GetPrompt()
    {
        return Prompt.Replace("{SECRET_NUMBER}", State.SecretNumber.ToString());
    }

    public async Task PrepareAsync(int secretNumber)
    {
        var prompt = Prompt.Replace("{SECRET_NUMBER}", secretNumber.ToString());
        await PublishAsync(new RecordEvent
        {
            Message = $"[Prompt for Alice]: \n{prompt}",
        });
        var chatResult = await ChatWithHistory(prompt);
        if (chatResult is { Count: > 0 })
        {
            Logger.LogInformation("Alice initialized with prompt: {Prompt}", chatResult[0].Content);
            RaiseEvent(new SetSecretNumberStateLogEvent
            {
                SecretNumber = secretNumber
            });
            RaiseEvent(new NewAliceChatStateLogEvent
            {
                ChatMessage = chatResult[0]
            });
            await ConfirmEvents();
            await PublishAsync(new RecordEvent
            {
                Message = chatResult[0].Content
            });
        }
        else
        {
            Logger.LogError("Failed to initialize Alice with the provided prompt.");
        }
    }

    [EventHandler]
    public async Task HandleGreetingAsync(GreetingEvent eventData)
    {
        Logger.LogInformation($"Alice received greeting: {eventData.Greeting}");
        // Alice replies to Bob
        var reply = $"Hi Bob, Alice received: {eventData.Greeting}";
        await PublishAsync(new ReplyEvent
        {
            Reply = reply
        });
        await PublishAsync(new RecordEvent
        {
            Message = reply
        });
    }

    [EventHandler]
    public async Task GuessingNumberAsync(ChatEvent chatEvent)
    {
        var message = $"Now, Bob's input is: {chatEvent.Content}";
        Logger.LogInformation(message);
        var history = State.ChatMessages.Concat([new ChatMessage
        {
            Content = message,
            ChatRole = ChatRole.User
        }]).ToList();
        RaiseEvent(new NewAliceChatStateLogEvent
        {
            ChatMessage = new ChatMessage
            {
                ChatRole = ChatRole.User,
                Content = message,
            }
        });
        await ConfirmEvents();

        var chatResult = await ChatWithHistory(GetPrompt(), history);
        await PublishAsync(new ChatEvent
        {
            Content = chatResult[0].Content
        });
        await PublishAsync(new RecordEvent
        {
            Message = chatResult[0].Content
        });
    }

    protected override void AIGAgentTransitionState(AliceGAgentState state,
        StateLogEventBase<AliceStateLogEvent> @event)
    {
        switch (@event)
        {
            case SetSecretNumberStateLogEvent setSecretNumberStateLogEvent:
                state.SecretNumber = setSecretNumberStateLogEvent.SecretNumber;
                Logger.LogInformation("Alice's secret number set to: {SecretNumber}", state.SecretNumber);
                break;
            case NewAliceChatStateLogEvent newChatStateLogEvent:
                state.ChatMessages.Add(newChatStateLogEvent.ChatMessage);
                break;
            default:
                Logger.LogWarning("Unhandled event type: {EventType}", @event.GetType().Name);
                break;
        }
    }
}