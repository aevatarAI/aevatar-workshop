# EventHandler 演示
本演示展示了在 Aevatar 做为一个Multi Agent框架，不同的Agent（我们称之为GAgent）之间如何进行通信。

在 Aevatar 中，开发者可以在一个 GAgent 内部定义事件处理器（Event Handler），以实现多个 GAgent 之间的交流逻辑。当一个事件被发布时，框架会确保它被投递到目标 GAgent 中对应的事件处理器进行处理。

### 代码解析: `EventHandlerDemoGAgent`

在 `EventHandlerDemoGAgent.cs` 文件中，您可以看到三种定义事件处理器以响应 `GreetingEvent` 事件的方式：

1.  **基于约定的处理器**:
    ```csharp
    public Task HandleEventAsync(GreetingEvent eventData)
    {
        // ...
    }
    ```
    根据命名约定，Aevatar 会自动将名为 `HandleEventAsync` 的公共方法识别为事件处理器。

2.  **基于特性的处理器**:
    ```csharp
    [EventHandler]
    public Task ExecuteAsync(GreetingEvent eventData)
    {
        // ...
    }
    ```
    使用 `[EventHandler]` 特性，您可以随意命名处理器方法，提供了更大的灵活性。

3.  **通配符处理器**:
    ```csharp
    [AllEventHandler]
    public Task HandleEventAsync(EventWrapperBase eventData)
    {
        // ...
    }
    ```
    `[AllEventHandler]` 特性指定了一个方法，该方法将接收发送到此 GAgent 的*所有*事件，这对于日志记录、调试或创建通用事件逻辑非常有用。

当您运行此演示时，`GreetingEvent` 事件会被触发。您可以在主机日志（Host Log）中观察到所有这三个处理器是如何接收并处理同一个事件的，这展示了捕获和响应代理间通信的不同方式。 