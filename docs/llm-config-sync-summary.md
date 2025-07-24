# LLM 配置同步实现总结

## 概述

本次重构实现了从 Client 到 Host 的配置同步功能，解决了 `ToolCallingAIGAgent` 无法获取运行时更新配置的问题。

## 重构内容

### 1. LlmConfigController 重构

**改动**：
- 添加了依赖注入：`IGAgentFactory`, `IGAgentExecutor`, `ILogger`
- 在 `SaveLlmConfigs` 方法中增加了配置同步到 Host 的功能
- 新增了 `GetSyncStatus` 方法检查连接状态

**关键代码**：
```csharp
// Step 1: 保存到本地文件
jsonObj["SystemLLMConfigs"] = JObject.Parse(newConfigs.ToString());
await System.IO.File.WriteAllTextAsync(secretsPath, jsonObj.ToString(...));

// Step 2: 同步到 host via ConfigManagerGAgent
var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>();
await _gAgentExecutor.ExecuteGAgentEventHandler(configManager, updateEvent);
```

### 2. ToolCallingAIGAgent 修复

**问题**：
- 原本从 `IOptions<SystemLLMConfigOptions>` 获取配置
- 这只包含启动时的配置，不包括运行时更新

**解决方案**：
```csharp
// 首先尝试从 IConfigurationHandler 获取配置（包含运行时更新）
var configHandler = ServiceProvider.GetService<IConfigurationHandler>();
if (configHandler != null)
{
    var (success, configJson, errorMessage) = await configHandler.GetConfigurationAsync("SystemLLMConfigs");
    if (success && !string.IsNullOrEmpty(configJson))
    {
        systemConfigs = JsonSerializer.Deserialize<Dictionary<string, LLMConfig>>(configJson);
    }
}

// 如果失败，回退到 IOptions
if (systemConfigs == null)
{
    var options = ServiceProvider.GetRequiredService<IOptions<SystemLLMConfigOptions>>();
    systemConfigs = options.Value.SystemLLMConfigs;
}
```

## 配置同步架构

```
Client                              Host
------                              ----
LlmConfigController                 ConfigManagerGAgent
       |                                   |
       |-- ConfigUpdateEvent -->          |
       |                                   |
                                    WorkshopConfigurationHandler
                                           |
                                    ConfigurationUpdateService
                                           |
                                    (Runtime Config Storage)
                                           |
                                    ToolCallingAIGAgent
                                    (via IConfigurationHandler)
```

## API 变更

### POST /api/llm-configs
- 现在会同时保存到本地和同步到 Host
- 响应包含 `localSave` 和 `hostSync` 状态

### GET /api/llm-configs/sync-status
- 新增 API，检查与 Orleans 集群的连接状态

## 测试

创建了单元测试验证：
- 成功同步场景
- 连接失败场景
- 同步状态检查

## 注意事项

1. **配置键名必须完全匹配**
   - 例如："AzureOpenAI" 而不是 "azureopenai"

2. **依赖注入链**
   - Client: `IGAgentFactory` -> `ConfigManagerGAgent`
   - Host: `IConfigurationHandler` -> `ConfigurationUpdateService`

3. **错误处理**
   - 本地保存失败：返回 500 错误
   - Host 同步失败：返回 200，但 `hostSync: false`

## 调试建议

1. 检查日志中的配置同步消息
2. 验证配置键名是否匹配
3. 确认 Orleans 集群连接正常
4. 使用 `/api/llm-configs/sync-status` 检查连接状态

## 未来改进

1. 配置持久化到数据库
2. 配置版本控制
3. 配置加密存储
4. 配置变更通知机制 