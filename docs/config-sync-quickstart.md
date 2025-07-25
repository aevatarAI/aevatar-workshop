# 配置同步快速入门指南

## 快速开始

本指南将帮助您在5分钟内理解并使用 Aevatar Workshop 的配置同步系统。

## 核心概念

### 1. 配置流向
```
Client (appsettings.json) → ConfigSyncService → ConfigManagerGAgent → AIGAgent
```

### 2. 关键组件
- **ConfigManagerGAgent**: 分布式配置存储
- **ConfigSyncService**: 启动时同步配置
- **AIGAgentBase**: 提供配置获取扩展点

## 步骤指南

### 步骤1: 在Client添加配置

编辑 `aevatar-workshop/src/Aevatar.Workshop.Client/appsettings.json`:

```json
{
  "SystemLLMConfigs": {
    "OpenAI": {
      "ProviderEnum": "OpenAI",
      "ModelIdEnum": "OpenAIGPT4",
      "DeploymentOrModelId": "gpt-4",
      "ApiKey": "your-api-key",
      "Temperature": 0.7
    }
  },
  "MCPServers": [
    {
      "Name": "FileSystemServer",
      "TransportType": "SSE",
      "Endpoint": "http://localhost:3000/sse"
    }
  ]
}
```

### 步骤2: ConfigSyncService自动同步

ConfigSyncService 已经在 Client 的 Program.cs 中注册：

```csharp
// Program.cs
builder.Services.AddHostedService<ConfigSyncService>();
```

启动时会自动：
1. 读取配置文件
2. 转换为 Options 对象
3. 存储到 ConfigManagerGAgent

### 步骤3: 在AIGAgent中使用配置

在您的 AIGAgent 中重写配置获取方法：

```csharp
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.GAgents.AI.Options;
using Newtonsoft.Json;

public class YourAIGAgent : AIGAgentBase<YourState, YourLogEvent>
{
    private readonly IGAgentFactory _gAgentFactory;
    
    public YourAIGAgent(IGAgentFactory gAgentFactory)
    {
        _gAgentFactory = gAgentFactory;
    }
    
    protected override LLMConfig? ResolveSystemConfig(string key)
    {
        try
        {
            // 生成配置类型的确定性GUID
            var configGuid = typeof(SystemLLMConfigOptions).FullName!.ToGuid();
            
            // 获取ConfigManagerGAgent实例
            var configManager = _gAgentFactory.GetGAgent<IConfigManagerGAgent>(configGuid);
            
            // 请求配置
            var requestEvent = new ConfigRequestEvent
            {
                ConfigType = typeof(SystemLLMConfigOptions).FullName!,
                ConfigKey = key // e.g., "OpenAI"
            };
            
            var response = configManager.RequestConfigAsync(requestEvent)
                .GetAwaiter().GetResult();
            
            if (response.Success && !string.IsNullOrEmpty(response.ConfigJson))
            {
                return JsonConvert.DeserializeObject<LLMConfig>(response.ConfigJson);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, $"Failed to get config for key: {key}");
        }
        
        // 回退到基础实现
        return base.ResolveSystemConfig(key);
    }
}
```

## 常见场景

### 场景1: 添加新的配置类型

1. 创建 Options 类：
```csharp
[GenerateSerializer]
public class DatabaseOptions
{
    [Id(0)] public string ConnectionString { get; set; }
    [Id(1)] public int Timeout { get; set; }
}
```

2. 在 ConfigSyncService 中添加同步逻辑：
```csharp
private async Task SyncDatabaseConfigAsync()
{
    var dbOptions = new DatabaseOptions();
    _configuration.GetSection("Database").Bind(dbOptions);
    
    var configGuid = typeof(DatabaseOptions).FullName!.ToGuid();
    var configManager = _gAgentFactory.GetGAgent<IConfigManagerGAgent>(configGuid);
    
    await configManager.UpdateConfigAsync(new ConfigUpdateEvent
    {
        ConfigType = typeof(DatabaseOptions).FullName!,
        ConfigJson = JsonSerializer.Serialize(dbOptions)
    });
}
```

### 场景2: 查询部分配置

获取特定的配置键：

```csharp
var requestEvent = new ConfigRequestEvent
{
    ConfigType = typeof(SystemLLMConfigOptions).FullName!,
    ConfigKey = "OpenAI" // 只获取OpenAI的配置
};
```

### 场景3: 批量获取所有LLM配置

```csharp
protected override List<LLMConfig> GetLLMConfig()
{
    try
    {
        var configGuid = typeof(SystemLLMConfigOptions).FullName!.ToGuid();
        var configManager = _gAgentFactory.GetGAgent<IConfigManagerGAgent>(configGuid);
        
        var response = configManager.RequestConfigAsync(new ConfigRequestEvent
        {
            ConfigType = typeof(SystemLLMConfigOptions).FullName!
        }).GetAwaiter().GetResult();
        
        if (response.Success && !string.IsNullOrEmpty(response.ConfigJson))
        {
            var configs = JsonConvert.DeserializeObject<Dictionary<string, LLMConfig>>(response.ConfigJson);
            return configs?.Values.ToList() ?? new List<LLMConfig>();
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to get LLM configs");
    }
    
    return base.GetLLMConfig();
}
```

## 调试技巧

### 1. 检查配置是否同步成功

查看 ConfigSyncService 的日志：
```
[Information] Successfully synced SystemLLMConfigs to ConfigManagerGAgent
[Information] Successfully synced MCPServers to ConfigManagerGAgent
```

### 2. 验证ConfigManagerGAgent状态

```csharp
var configGuid = typeof(SystemLLMConfigOptions).FullName!.ToGuid();
var configManager = _gAgentFactory.GetGAgent<IConfigManagerGAgent>(configGuid);
var state = await configManager.GetStateAsync();

Console.WriteLine($"Config Type: {state.ConfigType}");
Console.WriteLine($"Last Updated: {state.LastUpdated}");
Console.WriteLine($"Total Updates: {state.TotalUpdates}");
```

### 3. 查看Event Sourcing日志

ConfigManagerGAgent 记录所有配置变更事件：
- `ConfigSetLogEvent`: 配置设置事件
- `ConfigUpdatedLogEvent`: 配置更新结果事件

## 故障排查

### 问题1: 配置未找到

**症状**: `RequestConfigAsync` 返回 "No configuration found"

**解决方案**:
1. 确认 ConfigSyncService 已成功运行
2. 检查配置类型的 FullName 是否一致
3. 验证 appsettings.json 中的配置节点名称

### 问题2: JSON反序列化失败

**症状**: `JsonConvert.DeserializeObject` 抛出异常

**解决方案**:
1. 确保配置结构与 Options 类匹配
2. 检查 JSON 格式是否正确
3. 使用 JsonProperty 特性处理命名差异

### 问题3: ConfigManagerGAgent实例获取失败

**症状**: `GetGAgent` 返回 null 或抛出异常

**解决方案**:
1. 确保使用相同的 GUID 生成方式
2. 检查 Orleans 集群是否正常运行
3. 验证 IGAgentFactory 是否正确注入

## 性能优化建议

1. **缓存配置**: 在 AIGAgent 中缓存常用配置
2. **批量同步**: 在 ConfigSyncService 中并行同步多种配置
3. **懒加载**: 只在需要时获取配置，避免启动时加载过多

## 下一步

- 查看[完整架构文档](./config-sync-architecture.md)了解详细设计
- 参考[组件架构图](./config-sync-component-diagram.md)理解系统结构
- 探索 ConfigManagerGAgent 的高级特性，如事件溯源和版本控制 