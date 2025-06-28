# ToolAIGAgent 单元测试

## 概述

本测试套件验证了 ToolAIGAgent 的智能工具调用能力，确保 LLM 能够正确识别任务类型并调用相应的专用 GAgent。

## 测试架构

### 核心组件

1. **TestToolAIGAgent** - 继承自 ToolAIGAgentBase 的测试用 AI 代理
2. **MathGAgent** - 专门处理数学计算的工具 GAgent
3. **TimeConverterGAgent** - 专门处理时间转换的工具 GAgent

### 测试用例

#### 1. 基础功能测试

- `MathGAgent_ShouldEvaluateExpressions` - 验证数学计算功能
- `TimeConverterGAgent_ShouldConvertTimeZones` - 验证时间转换功能

#### 2. AI 识别测试

- `TestToolAIGAgent_ShouldRecognizeMathCalculation` - 验证 AI 能识别数学任务
- `TestToolAIGAgent_ShouldRecognizeTimeConversion` - 验证 AI 能识别时间任务

#### 3. 复杂场景测试

- `TestToolAIGAgent_ShouldHandleComplexInstruction` - 测试需要多个工具的复杂指令
- `ComplexScenario_PlanningWithCalculations` - 测试会议时间计算等实际场景

## 运行测试

```bash
# 运行所有 ToolAIGAgent 测试
dotnet test --filter "FullyQualifiedName~ToolAIGAgentTests"

# 运行特定测试
dotnet test --filter "FullyQualifiedName~ToolAIGAgentTests.MathGAgent_ShouldEvaluateExpressions"
```

## 测试设计原则

### 1. 智能任务路由

TestToolAIGAgent 通过增强的提示词引导 LLM：
- 明确列出可用工具及其能力
- 提供使用示例
- 强调任务类型与工具的对应关系

### 2. 事件类型映射

```csharp
protected override async Task<EventBase> CreateEventForGAgentAsync(GAgentInfo gagentInfo, string task)
{
    switch (gagentInfo.Alias.ToLower())
    {
        case "math":
            return new MathCalculateEvent { Expression = task };
        case "timeconverter":
            return new TimeConvertEvent { TimeInput = task };
        default:
            return await base.CreateEventForGAgentAsync(gagentInfo, task);
    }
}
```

### 3. 验证策略

- 对于数学计算：验证结果包含正确的数值
- 对于时间转换：验证结果包含时间格式
- 对于复杂任务：验证响应长度和内容完整性

## 扩展指南

### 添加新的工具 GAgent

1. 创建新的 GAgent 类，实现特定功能
2. 在 TestToolAIGAgent 的提示词中添加工具描述
3. 在 `CreateEventForGAgentAsync` 中添加事件映射
4. 编写相应的单元测试

### 示例：添加 WeatherGAgent

```csharp
[GAgent("weather", "tools")]
public class WeatherGAgent : GAgentBase<WeatherState, WeatherLogEvent>
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Weather information agent");
    }
    
    // 实现天气查询逻辑
}
```

## 注意事项

1. **LLM 配置**：测试使用 OpenAI 作为默认 LLM，确保配置正确
2. **异步操作**：所有 GAgent 调用都是异步的，测试需要正确处理
3. **错误处理**：测试包含了超时和错误场景的验证

## 测试输出示例

```
Math calculation result: 25 * 4 + 10 = 110
Time conversion result: 2024-01-01 15:00:00 JST
Complex instruction result: The meeting starts at 2:30 PM EST...
``` 