# LLM Configuration Sync - Client to Host

## 概述

重构后的 `LlmConfigController` 现在支持两个功能：
1. 保存配置到本地文件 (`appsettings.secrets.json`)
2. 同步配置到 Host 服务

## 架构

```
Client (LlmConfigController)
    ↓
    ├── 保存到本地文件
    │   └── appsettings.secrets.json
    │
    └── 通过 Orleans 同步到 Host
        ├── IGAgentFactory.GetGAgentAsync<IConfigManagerGAgent>()
        ├── 创建 ConfigUpdateEvent
        └── IGAgentExecutor.ExecuteGAgentEventHandler()
            └── Host (ConfigManagerGAgent)
                └── IConfigurationHandler.UpdateConfigurationAsync()
```

## API 变更

### POST `/api/llm-configs`

保存 LLM 配置并同步到 Host。

**请求体**：
```json
{
  "OpenAI": {
    "ProviderEnum": 1,
    "ModelIdEnum": 0,
    "ModelName": "gpt-4",
    "Endpoint": "https://api.openai.com",
    "ApiKey": "your-api-key"
  }
}
```

**响应**：
```json
{
  "message": "Configuration saved locally and synced to host",
  "localSave": true,
  "hostSync": true
}
```

**失败响应**：
```json
{
  "message": "Configuration saved locally but failed to sync to host: error message",
  "localSave": true,
  "hostSync": false,
  "error": "Detailed error message"
}
```

### GET `/api/llm-configs/sync-status`

检查与 Host 的连接状态。

**响应**：
```json
{
  "connected": true,
  "hostAvailable": true,
  "message": "Connected to Orleans cluster"
}
```

## 实现细节

### 依赖注入

`LlmConfigController` 现在需要以下依赖：
- `IGAgentFactory`: 获取 Orleans grains
- `IGAgentExecutor`: 执行 grain 事件处理器
- `ILogger<LlmConfigController>`: 日志记录

### 错误处理

1. **本地保存失败**：返回 500 错误
2. **Host 同步失败**：本地保存成功，返回 200 但标记 `hostSync: false`
3. **连接失败**：同上处理

### 配置流程

1. 客户端调用 POST `/api/llm-configs`
2. Controller 保存配置到本地文件
3. Controller 通过 `ConfigManagerGAgent` 发送 `ConfigUpdateEvent`
4. Host 的 `ConfigManagerGAgent` 接收事件
5. Host 调用 `IConfigurationHandler.UpdateConfigurationAsync`
6. 配置被更新到 Host 的运行时选项

## 测试

提供了完整的单元测试覆盖：
- 成功同步场景
- Host 同步失败场景
- 连接失败场景
- 同步状态检查

运行测试：
```bash
dotnet test --filter "FullyQualifiedName~LlmConfigControllerTests"
```

## 使用示例

### JavaScript/TypeScript
```javascript
async function saveLlmConfig(config) {
  const response = await fetch('/api/llm-configs', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(config)
  });
  
  const result = await response.json();
  
  if (result.hostSync) {
    console.log('Configuration synced successfully');
  } else {
    console.warn('Configuration saved locally but sync failed:', result.error);
  }
}
```

### 检查连接状态
```javascript
async function checkSyncStatus() {
  const response = await fetch('/api/llm-configs/sync-status');
  const status = await response.json();
  
  if (!status.connected) {
    console.error('Not connected to Orleans cluster');
  }
}
```

## 注意事项

1. 配置始终会先保存到本地，即使 Host 同步失败
2. Host 同步是异步的，有 30 秒超时
3. 日志会记录所有同步尝试和结果
4. 建议在应用启动时检查同步状态 