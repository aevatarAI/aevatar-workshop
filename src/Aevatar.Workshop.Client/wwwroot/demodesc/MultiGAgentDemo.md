# Multi-GAgent Demo

This demo showcases communication between two AI Agents, **Alice** and **Bob**, who collaborate to play a "Guess the Number" game. It's a classic example of how different agents can be given distinct roles and instructions to achieve a common goal.

### The Roles

1.  **Alice (The Number Holder)**: Alice's role is to secretly hold a number (which you provide, defaulting to 42) and give feedback. Her instructions are very strict, as defined in `Alice.cs`:
    > You are Alice. You know that the secret number is {SECRET_NUMBER}(don't say it).
    > When Bob submits a number as input, provide feedback according to the following rules:
    > - When input number is greater than secret number, output "high"
    > - When input number is lower than secret number, output "low"
    > - When input number equals to secret number, output "correct"
    > - If the submitted integer is not in the range of 1 to 100, output "invalid guessing"
    >
    > Apart from these feedbacks, no other information is provided.

2.  **Bob (The Guesser)**: Bob's role is to intelligently guess the number held by Alice. His prompt, defined in `Bob.cs`, encourages a specific strategy:
    > You are Bob, and your goal is to guess Alice's secret number (an integer between 1-100).
    > Guess only one integer at a time.
    > Alice will provide feedback as 'high','low', or 'correct'.
    >
    > Please comply with:
    > 1. Guess only one number at a time, format: Guess: [number] (e.g. Guess: 50)
    > 2. Use efficient methods such as binary search (guess correctly within 7 times)
    > 3. Narrow down the range based on feedback and do not guess numbers beyond the current possible range
    > When you receive 'correct', you win.

You can watch their game unfold in the "AI Messages" panel below.

### How to Create Your Own AI GAgent

Creating your own GAgent follows the pattern of `Alice` and `Bob`:

1.  **Define State (Optional)**: If your agent needs to remember things (like Bob remembers his previous guesses), create a state class inheriting from `AIGAgentStateBase`.
    ```csharp
    [GenerateSerializer]
    public class YourAgentState : AIGAgentStateBase
    {
        [Id(0)] public List<string> YourData { get; set; } = [];
    }
    ```

2.  **Create the GAgent Class**: Your agent class will inherit from `AIGAgentBase<TState, TLogEvent>`. This gives it the ability to chat with an LLM and maintain state.
    ```csharp
    [GAgent("your-agent-name", "your-group")]
    public class YourAgent : AIGAgentBase<YourAgentState, YourLogEvent>
    {
        // ...
    }
    ```

3.  **Define a Prompt**: The agent's personality and instructions are defined in a prompt string. It's best practice to keep this in a constant.
    ```csharp
    private const string Prompt = "You are a helpful assistant who is an expert at...";
    ```

4.  **Define Event Handlers**: Agents react to the world and each other through events. An event handler is a method decorated with `[EventHandler]` that gets called when a specific event is published.
    ```csharp
    [EventHandler]
    public async Task HandleCustomEvent(YourCustomEvent @event)
    {
        // 1. Get history and construct a new prompt for the LLM
        var history = State.ChatMessages;
        var prompt = $"Given the history, and the new data {@event.Data}, what is the next step?";

        // 2. Call the LLM to get a response
        var chatResult = await ChatWithHistory(prompt, history);
        var response = chatResult[0].Content;

        // 3. Publish a new event to communicate with another agent
        await PublishAsync(new AnotherEvent { Data = response });
    }
    ```

By defining agents with specific prompts and enabling them to communicate via events, you can build complex and intelligent multi-agent systems. 