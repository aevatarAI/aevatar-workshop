# 如何使用 GAgent 实现规则

## 📍 规则位置

我已经为你创建了两个文档来帮助规范 GAgent 的实现：

1. **详细文档**：`aevatar-workshop/docs/GAgentImplementationRules.md`
   - 包含完整的实现指南和示例
   - 适合人工阅读和学习

2. **AI 规则文件**：`aevatar-workshop/.cursor/rules/gagent-implementation.md`
   - 简洁的规则清单，专为 AI 助手设计
   - 包含必须遵守的关键规则和检查清单

## 🤖 如何让 AI 助手遵守这些规则

### 方式 1：使用 Cursor Rules（推荐）

`.cursor/rules/` 目录下的规则会被 Cursor 自动加载。当你请求实现 GAgent 相关功能时，AI 会自动参考这些规则。

### 方式 2：在提示中引用规则

在你的请求中明确提及：
```
请按照 gagent-implementation 规则实现一个新的 GAgent
```

### 方式 3：使用 fetch_rules 工具

如果 AI 没有自动应用规则，你可以要求：
```
请先获取 gagent-implementation 规则，然后实现 GAgent
```

## 📋 规则要点总结

### ❌ 常见错误
1. **Logger 注入错误**
   ```csharp
   // 错误：注入 logger
   public MyGAgent(ILogger<MyGAgent> logger) { }
   ```

2. **使用 IGrainFactory**
   ```csharp
   // 错误：使用 IGrainFactory
   var grainFactory = ServiceProvider.GetRequiredService<IGrainFactory>();
   ```

### ✅ 正确做法
1. **使用内置 Logger**
   ```csharp
   // 正确：使用 GAgentBase 的 Logger 属性
   Logger.LogInformation("Message");
   ```

2. **使用 IGAgentFactory**
   ```csharp
   // 正确：使用 IGAgentFactory
   var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
   ```

## 🔧 修复已有问题

如果发现 AI 生成的代码有问题，可以：

1. **直接指出错误**
   ```
   这个 GAgent 实现有问题，不应该注入 logger，请修复
   ```

2. **引用规则修复**
   ```
   请按照 gagent-implementation 规则修复这个 GAgent
   ```

## 💡 最佳实践

1. **项目初期**：告诉 AI 这个项目使用 Aevatar 框架，需要遵守 GAgent 实现规则

2. **代码审查**：定期检查生成的代码是否符合规则

3. **持续改进**：如果发现新的常见错误，更新规则文档

## 📝 规则维护

- 如果发现新的实现模式或最佳实践，请更新：
  - `docs/GAgentImplementationRules.md`（详细说明）
  - `.cursor/rules/gagent-implementation.md`（AI 规则）

- 保持两个文档同步，确保规则的一致性

通过这种方式，你可以确保 AI 助手在实现 GAgent 时始终遵守正确的模式！ 