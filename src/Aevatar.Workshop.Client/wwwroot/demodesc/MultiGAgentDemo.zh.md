# 多 GAgent 演示

本演示展示了两个 AI Agent（**Alice** 和 **Bob**）之间的通信，它们协作玩一个"猜数字"游戏。这是一个经典的例子，说明了如何为不同的 Agent 分配独特的角色和指令以实现共同的目标。

### 角色

1.  **Alice（持数者）**：Alice 的角色是秘密持有一个数字（由您提供，默认为 42）并提供反馈。她的指令非常严格，定义在 `Alice.cs` 中：
    > 你是 Alice。你知道秘密数字是 {SECRET_NUMBER}（不要说出来）。
    > 当 Bob 输入一个数字时，根据以下规则提供反馈：
    > - 当输入数字大于秘密数字时，输出"high"
    > - 当输入数字小于秘密数字时，输出"low"
    > - 当输入数字等于秘密数字时，输出"correct"
    > - 如果提交的整数不在 1 到 100 的范围内，输出"invalid guessing"
    >
    > 除了这些反馈之外，不提供其他任何信息。

2.  **Bob（猜测者）**：Bob 的角色是智能地猜测 Alice 持有的数字。他在 `Bob.cs` 中的提示鼓励一种特定的策略：
    > 你是 Bob，你的目标是猜出 Alice 的秘密数字（一个 1-100 之间的整数）。
    > 一次只猜一个整数。
    > Alice 会提供"high"、"low"或"correct"作为反馈。
    >
    > 请遵守：
    > 1. 一次只猜一个数字，格式为：Guess: [数字] (例如 Guess: 50)
    > 2. 使用二分搜索等高效方法（在 7 次内猜对）
    > 3. 根据反馈缩小范围，不要猜测当前可能范围之外的数字
    > 当你收到"correct"时，你就赢了。

您可以在下方的"AI 消息"面板中观看他们游戏的展开过程。

### 如何创建您自己的 AI GAgent

创建您自己的 GAgent 遵循 `Alice` 和 `Bob` 的模式：

1.  **定义状态（可选）**：如果您的 Agent 需要记住事情（就像 Bob 记住他以前的猜测一样），请创建一个继承自 `AIGAgentStateBase` 的状态类。
    ```csharp
    [GenerateSerializer]
    public class YourAgentState : AIGAgentStateBase
    {
        [Id(0)] public List<string> YourData { get; set; } = [];
    }
    ```

2.  **创建 GAgent 类**：您的 Agent 类将继承自 `AIGAgentBase<TState, TLogEvent>`。这使其能够与 LLM 聊天并维护状态。
    ```csharp
    [GAgent("your-agent-name", "your-group")]
    public class YourAgent : AIGAgentBase<YourAgentState, YourLogEvent>
    {
        // ...
    }
    ```

3.  **定义提示**：Agent 的个性和指令在提示字符串中定义。最佳实践是将其保存在一个常量中。
    ```csharp
    private const string Prompt = "你是一个乐于助人的助手，擅长...";
    ```

4.  **定义事件处理程序**：Agent 通过事件对世界和彼此做出反应。事件处理程序是使用 `[EventHandler]` 特性修饰的方法，当特定事件发布时会调用该方法。
    ```csharp
    [EventHandler]
    public async Task HandleCustomEvent(YourCustomEvent @event)
    {
        // 1. 获取历史记录并为 LLM 构建新提示
        var history = State.ChatMessages;
        var prompt = $"根据历史记录和新数据 {@event.Data}，下一步是什么？";

        // 2. 调用 LLM 以获取响应
        var chatResult = await ChatWithHistory(prompt, history);
        var response = chatResult[0].Content;

        // 3. 发布新事件以与另一个 Agent 通信
        await PublishAsync(new AnotherEvent { Data = response });
    }
    ```

通过为 Agent 定义特定的提示并使它们能够通过事件进行通信，您可以构建复杂而智能的多 Agent 系统。 