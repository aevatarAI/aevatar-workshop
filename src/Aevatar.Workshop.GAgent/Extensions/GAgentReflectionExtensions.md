# GAgent反射扩展方法文档

## 概述

`GAgentReflectionExtensions` 提供了一组扩展方法，用于通过反射自动提取GAgent的元数据信息，包括名称、描述、GrainType和事件处理器列表。这大大简化了维护GAgent信息的工作。

## 主要功能

### 1. ExtractGAgentInfos

从程序集中提取所有GAgent信息。

```csharp
// 从程序集中提取所有GAgent
var assembly = typeof(NotificationGAgent).Assembly;
var allGAgents = assembly.ExtractGAgentInfos();

// 只提取特定命名空间下的GAgent
var demoGAgents = assembly.ExtractGAgentInfos("Aevatar.Workshop.GAgent.GAgents.Demo");
```

### 2. ExtractGAgentInfo

从特定类型提取GAgent信息。

```csharp
var gAgentInfo = typeof(NotificationGAgent).ExtractGAgentInfo();
```

## GAgentInfo 数据结构

```csharp
public class GAgentInfo
{
    public string Name { get; set; }              // 类名
    public string DisplayName { get; set; }       // 显示名称（从Description或DisplayName属性获取）
    public string Description { get; set; }       // 描述（从Description属性获取）
    public GrainType GrainType { get; set; }      // Orleans GrainType
    public List<string> EventHandlers { get; set; } // 事件处理器列表
    public Type Type { get; set; }                // 原始类型引用
}
```

## 提取规则

### DisplayName
优先级：
1. `[DisplayName("...")]` 属性
2. `[Description("...")]` 属性  
3. 类名

### Description
优先级：
1. `[Description("...")]` 属性
2. 默认值：`"{类名} - 演示GAgent"`

### EventHandlers
自动识别以下方法：
1. 标记了 `[EventHandler]` 的方法
2. 标记了 `[AllEventHandler]` 的方法（会添加 `[AllEventHandler]` 后缀）
3. 名为 `HandleEventAsync` 且第一个参数继承自 `EventBase` 的方法

### GrainType
根据 `[GAgent]` 属性参数生成：
- `[GAgent("alias", "namespace")]` → `namespace.alias`
- `[GAgent("alias")]` → `{类的命名空间}.alias`
- `[GAgent]` → `{类的命名空间}.{类名}`

## 使用示例

### 在EventHandlerDemoController中的使用

```csharp
// Demo GAgent types - 使用反射自动提取
private static readonly List<GAgentInfo> DemoGAgents;

static EventHandlerDemoController()
{
    // 使用反射扩展方法自动提取Demo命名空间下的所有GAgent
    var assembly = typeof(NotificationGAgent).Assembly;
    DemoGAgents = assembly.ExtractGAgentInfos("Aevatar.Workshop.GAgent.GAgents.Demo");
    
    // 可以进一步过滤或排序
    DemoGAgents = DemoGAgents
        .Where(g => g.Name != "PublishingGAgent") // 排除特定GAgent
        .OrderBy(g => g.Name)
        .ToList();
}
```

## 注意事项

1. **性能考虑**：反射操作有一定的性能开销，建议在静态构造函数或启动时执行，并缓存结果。

2. **属性要求**：为了让扩展方法能正确提取信息，请确保GAgent类上添加了必要的属性：
   ```csharp
   [System.ComponentModel.Description("通知处理器")]
   [GAgent("notification-demo", "workshop")]
   public class NotificationGAgent : GAgentBase<...>
   ```

3. **事件处理器命名**：确保事件处理方法遵循约定或添加适当的属性标记。

4. **程序集扫描**：`ExtractGAgentInfos` 会扫描整个程序集，使用命名空间过滤可以提高效率。 