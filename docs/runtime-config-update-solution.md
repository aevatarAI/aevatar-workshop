# 运行时配置更新解决方案

## 概述

通过实现自定义的 `RuntimeConfigurationProvider`，我们可以让 ConfigManagerGAgent 直接修改 `IOptions<SystemLLMConfigOptions>` 等配置选项。

## 架构

```
ConfigManagerGAgent
    ↓
IConfigurationHandler (WorkshopConfigurationHandler)
    ↓
IConfigurationUpdateService (ConfigurationUpdateService)
    ↓
RuntimeConfigurationProvider
    ↓
IOptionsMonitor<SystemLLMConfigOptions> (自动重载)
```

## 实现细节

### 1. RuntimeConfigurationSource/Provider

创建了自定义的配置源和提供程序：

```csharp
public class RuntimeConfigurationProvider : ConfigurationProvider
{
    private readonly ConcurrentDictionary<string, string> _data = new();
    private ConfigurationReloadToken _reloadToken = new();

    public void UpdateSystemLLMConfig(string configKey, LLMConfig config)
    {
        // 将配置存储为键值对
        var prefix = $"SystemLLMConfigs:{configKey}";
        _data[$"{prefix}:ProviderEnum"] = config.ProviderEnum.ToString();
        // ... 其他字段
        
        // 更新配置数据
        Data = _data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        // 触发配置重载
        OnReload();
    }
}
```

### 2. 配置注册

在 `Program.cs` 中注册运行时配置源：

```csharp
// 创建共享的运行时配置提供程序实例
var runtimeConfigProvider = new RuntimeConfigurationProvider();

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Add(new RuntimeConfigurationSource { Provider = runtimeConfigProvider })
    .AddEnvironmentVariables()
    .Build();
```

### 3. ConfigurationUpdateService 改进

修改为使用 `RuntimeConfigurationProvider`：

```csharp
public class ConfigurationUpdateService : IConfigurationUpdateService
{
    private readonly RuntimeConfigurationProvider _runtimeConfigProvider;
    
    public Task<bool> UpdateSystemLLMConfigAsync(string configKey, LLMConfig config)
    {
        // 更新运行时配置提供程序
        _runtimeConfigProvider.UpdateSystemLLMConfig(configKey, config);
        
        // 配置更改会自动触发 IOptionsMonitor 重新加载
        return Task.FromResult(true);
    }
}
```

## 工作流程

1. **Client 发送配置更新**
   - LlmConfigController 通过 ConfigManagerGAgent 发送 ConfigUpdateEvent

2. **Host 处理配置更新**
   - ConfigManagerGAgent 接收事件
   - 调用 IConfigurationHandler.UpdateConfigurationAsync
   - WorkshopConfigurationHandler 调用 IConfigurationUpdateService
   - ConfigurationUpdateService 更新 RuntimeConfigurationProvider

3. **配置自动重载**
   - RuntimeConfigurationProvider 调用 OnReload()
   - 触发 IOptionsMonitor 的更改通知
   - 所有使用 IOptions/IOptionsMonitor 的服务自动获得新配置

## 优势

1. **无缝集成**：与 ASP.NET Core 配置系统完全集成
2. **自动重载**：配置更改后自动通知所有依赖的服务
3. **类型安全**：继续使用强类型的配置选项
4. **向后兼容**：现有使用 IOptions 的代码无需修改

## 使用示例

GAgent 可以继续使用标准的依赖注入方式获取配置：

```csharp
public class ToolCallingAIGAgent : GAgentBase<...>
{
    private readonly SystemLLMConfigOptions _llmConfigOptions;
    
    public ToolCallingAIGAgent(IOptions<SystemLLMConfigOptions> llmConfigOptions)
    {
        _llmConfigOptions = llmConfigOptions.Value;
    }
}
```

或者使用 IOptionsMonitor 获取最新配置：

```csharp
public class SomeService
{
    private readonly IOptionsMonitor<SystemLLMConfigOptions> _optionsMonitor;
    
    public void DoSomething()
    {
        var currentConfig = _optionsMonitor.CurrentValue;
        // 始终获得最新的配置
    }
}
```

## 注意事项

1. **配置持久化**：当前实现仅在内存中存储配置，重启后会丢失。生产环境应考虑持久化到数据库或文件。

2. **并发安全**：RuntimeConfigurationProvider 使用 ConcurrentDictionary 确保线程安全。

3. **配置验证**：建议在更新配置前进行验证，确保配置有效。

4. **性能考虑**：频繁的配置更新可能触发大量重载，应适当控制更新频率。 