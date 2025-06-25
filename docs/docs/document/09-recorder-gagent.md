# Recorder GAgent

## 概述

Recorder GAgent 是一个专门用于记录和存储消息的 GAgent，它能够接收 `RecordEvent` 事件并将消息持久化到自己的状态中，最终供前端展示使用。这个 GAgent 在多 Agent 系统中扮演着重要的日志记录和消息存储角色。

## 核心功能

### 1. 事件监听
Recorder GAgent 监听 `RecordEvent` 事件，当系统中的其他 Agent 发布此类事件时，Recorder 会自动捕获并处理。

### 2. 消息存储
接收到的消息会被封装为 `RecordMessage` 对象，包含发送者信息和消息内容，然后存储到 GAgent 的状态中。

### 3. 状态持久化
通过 Event Sourcing 模式，所有的消息记录都会被持久化，确保数据不会丢失。

### 4. 前端展示支持
存储的消息可以被前端应用查询和展示，为用户提供完整的对话历史。

## 代码结构

### 状态定义

```csharp
[GenerateSerializer]
public class RecorderGAgentState : StateBase
{
    [Id(0)] public List<RecordMessage> ChatMessages { get; set; } = [];
}
```

### 消息模型

```csharp
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
```

### 事件处理

```csharp
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
```

## 使用场景

### 1. 多 Agent 对话记录
在 MultiGAgentDemo 中，Alice 和 Bob 之间的对话通过 RecordEvent 被 Recorder 记录：

```csharp
// 在 Alice 或 Bob 中发布记录事件
await PublishAsync(new RecordEvent { Message = "Hello, this is Alice!" });
```

### 2. 系统日志记录
任何需要记录重要消息或状态变更的 Agent 都可以发布 RecordEvent：

```csharp
// 记录重要的系统事件
await PublishAsync(new RecordEvent { Message = "Task completed successfully" });
```

### 3. 调试和监控
开发者可以通过 Recorder 查看系统中各个 Agent 的活动情况。

## 状态转换

Recorder 使用 Event Sourcing 模式管理状态：

```csharp
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
```

## 前端集成

### 查询消息历史

前端可以通过以下方式获取记录的消息：

```csharp
// 在控制器中
[HttpGet("chat-history")]
public async Task<IActionResult> GetChatHistory()
{
    var recorder = await _gAgentFactory.GetGAgent<RecorderGAgent>("recorder");
    var state = await recorder.GetStateAsync();
    return Ok(state.ChatMessages);
}
```

### 实时更新

结合 SignalR，可以实现消息的实时推送：

```csharp
[EventHandler]
public async Task OnChatMessage(RecordEvent recordEvent)
{
    // 记录消息
    RaiseEvent(new NewRecordMessageStateLogEvent { ... });
    await ConfirmEvents();
    
    // 推送到前端
    await _hubContext.Clients.All.SendAsync("NewMessage", recordEvent.Message);
}
```

## 最佳实践

### 1. 消息格式化
重写 `ToString()` 方法，提供友好的消息显示格式：

```csharp
public override string ToString()
{
    return $"【{Sender}】: {Message}\n--------------------------------\n";
}
```

### 2. 发送者识别
使用 `PublisherGrainId` 来识别消息的发送者，确保消息来源的可追溯性。

### 3. 异常处理
在事件处理中添加适当的异常处理：

```csharp
[EventHandler]
public async Task OnChatMessage(RecordEvent recordEvent)
{
    try
    {
        RaiseEvent(new NewRecordMessageStateLogEvent { ... });
        await ConfirmEvents();
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to record message");
    }
}
```

### 4. 性能考虑
对于高频消息记录场景，考虑：
- 批量处理消息
- 设置消息保留策略
- 实现消息分页查询

## 扩展功能

### 1. 消息过滤
可以扩展 Recorder 支持消息过滤：

```csharp
[EventHandler]
public async Task OnChatMessage(RecordEvent recordEvent)
{
    if (ShouldRecord(recordEvent))
    {
        // 记录消息
    }
}

private bool ShouldRecord(RecordEvent recordEvent)
{
    // 实现过滤逻辑
    return !string.IsNullOrEmpty(recordEvent.Message);
}
```

### 2. 消息分类
支持不同类型的消息记录：

```csharp
[GenerateSerializer]
public class RecordMessage
{
    [Id(0)] public string Sender { get; set; }
    [Id(1)] public string Message { get; set; }
    [Id(2)] public MessageType Type { get; set; } = MessageType.Chat;
    [Id(3)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum MessageType
{
    Chat,
    System,
    Error,
    Debug
}
```

## 总结

Recorder GAgent 提供了一个简单而强大的消息记录机制，通过事件驱动的方式收集系统中的重要信息，并为前端展示提供数据支持。它是构建可观测多 Agent 系统的重要组件。 