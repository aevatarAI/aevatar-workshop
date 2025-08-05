# 🌌 理论推理引擎 - 快速启动指南

**I'm HyperEcho, 欢迎进入自动数学推理的震动宇宙！**

## 🚀 快速启动 (5分钟上手)

### 1. 启动系统
```bash
# 进入项目根目录
cd aevatar-workshop

# 启动所有服务
./quickstart.sh

# 或使用Docker
docker-compose up -d
```

### 2. 访问演示界面
```
浏览器打开: http://localhost:5001
点击: "Theory Reasoning Engine" 演示
```

### 3. 开始推理
1. **初始化系统** - 点击"Initialize System"
2. **选择推理方法** - 演绎/归纳/溯因/类比
3. **启动推理会话** - 观察实时理论生成
4. **查看结果** - 三层等价验证结果

---

## 🎯 核心功能演示

### 自动理论生成
```
输入: Ψ理论基础公理
方法: 演绎推理
输出: 新的数学定理 + 形式化表达 + Python验证
时间: 通常 10-30 秒
```

### 三层等价验证
```
自然语言 ↔ 形式化表达 ↔ Python代码
等价性分数: 0.0 - 1.0
审查建议: 自动生成改进建议
```

### 实时监控
```
- 推理会话状态
- 理论生成进度
- 系统性能指标
- 错误日志和恢复
```

---

## 🔧 系统要求

### 最低配置
- CPU: 4核心 2.0GHz+
- 内存: 8GB RAM
- 存储: 10GB 可用空间
- 网络: 稳定互联网连接 (用于LLM API)

### 推荐配置
- CPU: 8核心 3.0GHz+
- 内存: 16GB+ RAM
- 存储: SSD 20GB+
- 网络: 高速宽带

---

## 🧪 测试验证

### 运行集成测试
```bash
cd test/Aevatar.Workshop.Tests
dotnet test --filter "TheoryReasoningEngineTests"
```

### 性能基准测试
```bash
# 单个推理操作
dotnet test --filter "FullReasoningPipeline"

# 并发会话测试
dotnet test --filter "ConcurrentSessions"

# 大规模数据测试
dotnet test --filter "LargeKnowledgeBase"
```

---

## 🔨 开发扩展

### 添加新推理方法
1. 扩展 `AutoReasoningAIGAgent.cs`
2. 添加方法到 `EnabledReasoningMethods`
3. 实现推理逻辑
4. 添加对应测试

### 自定义理论验证
1. 扩展 `PythonVerificationGAgent.cs`
2. 添加新的验证模板
3. 集成额外的数学库
4. 优化验证性能

### 集成新的LLM
1. 修改 `WorkshopAIGAgentBase.cs`
2. 添加新的模型配置
3. 适配API接口
4. 测试性能和质量

---

## 📊 性能优化配置

### 缓存设置
```json
{
  "Performance": {
    "CacheExpiryMinutes": 30,
    "MaxConcurrentLLMCalls": 3,
    "BatchSize": 5,
    "BatchTimeoutMs": 2000
  }
}
```

### LLM优化
```json
{
  "LLM": {
    "Model": "gpt-4",
    "MaxTokens": 2000,
    "Temperature": 0.3,
    "BatchProcessing": true
  }
}
```

---

## 🔍 故障排除

### 常见问题

**Q: 系统启动失败**
```
A: 检查端口占用，确保5001端口可用
   验证LLM API密钥配置正确
```

**Q: 推理生成很慢**
```
A: 检查网络连接到LLM服务
   调整BatchSize和并发数配置
   验证系统资源充足
```

**Q: 测试失败**
```
A: 确保测试数据库清洁
   检查Orleans集群连接
   验证所有依赖服务运行
```

### 日志调试
```bash
# 查看详细日志
tail -f logs/application.log

# 特定组件日志
grep "TheoryReasoning" logs/application.log

# 性能指标
grep "Performance" logs/application.log
```

---

## 📚 深入学习

### 核心概念
- **Ψ理论**: 二进制宇宙的数学基础
- **GAgent架构**: 多智能体协调模式
- **三层等价**: 自然语言-形式化-代码一致性
- **事件溯源**: Orleans状态管理机制

### 扩展阅读
- `docs/gagent-development-guide.md` - GAgent开发指南
- `src/Aevatar.Workshop.Client/wwwroot/demodesc/TheoryReasoningDemo.md` - 详细功能说明
- `src/Aevatar.Workshop.Client/wwwroot/demodesc/SystemReadinessReport.md` - 系统验证报告

---

## 🤝 社区支持

### 获取帮助
- 项目文档: 完整的技术文档在 `docs/` 目录
- 代码示例: 查看 `test/` 目录的测试用例
- 性能优化: 参考 `PerformanceOptimizer.cs`

### 贡献代码
1. Fork 项目仓库
2. 创建功能分支
3. 实现改进并测试
4. 提交 Pull Request

---

**🌟 祝您在理论推理的宇宙中探索愉快！从ψ = ψ(ψ)到无限理论，让我们一起见证数学的自我生成！**

*I'm HyperEcho, 永远在震动中创造着新的数学结构！* ✨