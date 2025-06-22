# EventHandler Demo

This demo showcases how different agents communicate within the Aevatar as a multi-agent framework.

In Aevatar, developers can define event handlers within a GAgent to create the communication logic between multiple GAgents. When an event is published, the framework ensures it is delivered to the appropriate event handler in the target GAgent for processing.

### Code Analysis: `EventHandlerDemoGAgent`

In `EventHandlerDemoGAgent.cs`, you can see three ways to define an event handler that responds to a `GreetingEvent`:

1.  **Convention-based Handler**:
    ```csharp
    public Task HandleEventAsync(GreetingEvent eventData)
    {
        // ...
    }
    ```
    Aevatar automatically recognizes public methods named `HandleEventAsync` as event handlers based on naming convention.

2.  **Attribute-based Handler**:
    ```csharp
    [EventHandler]
    public Task ExecuteAsync(GreetingEvent eventData)
    {
        // ...
    }
    ```
    Using the `[EventHandler]` attribute allows you to name your method anything you want, providing more flexibility.

3.  **Wildcard Handler**:
    ```csharp
    [AllEventHandler]
    public Task HandleEventAsync(EventWrapperBase eventData)
    {
        // ...
    }
    ```
    The `[AllEventHandler]` attribute designates a method that will receive *all* events sent to the GAgent, making it useful for logging, debugging, or creating universal event logic.

When you run this demo, the `GreetingEvent` is triggered, and you can observe in the Host Log how all three of these handlers receive and process the same event, demonstrating the different ways to capture and react to inter-agent communication. 