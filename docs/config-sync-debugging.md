# 配置同步调试指南

## 问题描述

当尝试使用 `ToolCallingAIGAgent` 时，出现配置未找到的错误：
```
System.InvalidOperationException: LLM configuration not found for: AzureOpenAI
```

## 问题分析

配置同步流程涉及以下组件：

1. **Client 端**：
   - `LlmConfigController` 保存配置到本地文件并通过 `ConfigManagerGAgent` 同步到 Host
   - `ConfigSyncService` 在启动时同步配置

2. **Host 端**：
   - `ConfigManagerGAgent` 接收配置更新事件
   - `WorkshopConfigurationHandler` 处理配置更新
   - `ConfigurationUpdateService` 存储运行时配置
   - `ToolCallingAIGAgent` 获取配置

## 问题根源

`ToolCallingAIGAgent` 原本从 `IOptions<SystemLLMConfigOptions>` 获取配置，这只包含启动时的配置，不包括运行时更新的配置。

## 解决方案

修改 `ToolCallingAIGAgent` 使其：
1. 首先尝试从 `IConfigurationHandler` 获取配置（包含运行时更新）
2. 如果失败，回退到 `IOptions<SystemLLMConfigOptions>`（启动时配置）

## 调试步骤

### 1. 验证配置是否成功同步到 Host

```bash
# 查看 Host 日志
docker logs aevatar-workshop-host 2>&1 | grep -i "config"
```

### 2. 验证 Client 端配置同步

```bash
# 查看 Client 日志
docker logs aevatar-workshop-client 2>&1 | grep -i "config"
```

### 3. 测试配置同步 API

```bash
# 获取当前配置
curl http://localhost:5000/api/llm-configs

# 检查同步状态
curl http://localhost:5000/api/llm-configs/sync-status

# 保存新配置
curl -X POST http://localhost:5000/api/llm-configs \
  -H "Content-Type: application/json" \
  -d '{
    "AzureOpenAI": {
      "ProviderEnum": 2,
      "ModelIdEnum": 1,
      "ModelName": "gpt-4",
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "your-api-key"
    }
  }'
```

### 4. 验证 Host 端配置

在 Host 端添加调试日志：

```csharp
// 在 WorkshopConfigurationHandler.UpdateConfigurationAsync
_logger.LogInformation("Updating configuration type: {Type}, key: {Key}, json: {Json}", 
    configType, configKey, configJson);

// 在 ConfigurationUpdateService.UpdateSystemLLMConfigAsync
_logger.LogInformation("Storing config for key: {Key}, provider: {Provider}", 
    configKey, config.ProviderEnum);
```

### 5. 验证 GAgent 获取配置

在 `ToolCallingAIGAgent.InitializeAsync` 中已添加日志：
- "Retrieved {Count} configurations from handler"
- "Retrieved {Count} configurations from IOptions"
- "Failed to get configurations from handler: {Error}"

## 配置文件位置

- **Client 配置**：
  - `/app/appsettings.json`
  - `/app/appsettings.secrets.json`

- **Host 配置**：
  - 运行时配置存储在 `ConfigurationUpdateService._runtimeLLMConfigs`

## 常见问题

### Q: 配置同步成功但 GAgent 仍然找不到配置？

A: 检查以下几点：
1. 配置键名是否匹配（例如 "AzureOpenAI" vs "azureopenai"）
2. Host 端的 `IConfigurationHandler` 是否正确注册
3. GAgent 的 ServiceProvider 是否能解析 `IConfigurationHandler`

### Q: 配置同步失败？

A: 检查：
1. Orleans 集群连接状态
2. `ConfigManagerGAgent` 是否正确创建
3. 事件处理器是否正确注册

### Q: 如何确认配置已经同步？

A: 可以通过以下方式：
1. 查看日志中的 "Configuration successfully synced to host"
2. 调用 `/api/llm-configs/sync-status` 检查连接状态
3. 在 Host 端日志中查看配置更新消息

## 测试配置同步

使用集成测试验证配置同步：

```bash
cd test/Aevatar.Workshop.Tests
dotnet test --filter "Should_Sync_SystemLLMConfigs" --logger "console;verbosity=detailed"
```

## 建议的改进

1. **配置验证**：在保存配置前验证必需字段
2. **配置持久化**：将运行时配置持久化到数据库或文件
3. **配置版本控制**：跟踪配置更改历史
4. **配置加密**：敏感信息（如 API 密钥）应加密存储 