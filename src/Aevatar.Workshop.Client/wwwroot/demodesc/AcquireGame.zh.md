# Acquire Board Game Demo

## 概述

这个演示展示了如何使用Aevatar GAgent框架实现经典的并购桌游（Acquire）。这是一个支持2-6人的策略性桌游，玩家通过放置瓷砖、购买和交易酒店股票来建立自己的酒店帝国。

## 游戏特色

### 🎮 完整的游戏实现
- **9x12网格游戏板**：经典Acquire游戏布局
- **7种酒店连锁**：Tower, Luxor, American, Worldwide, Festival, Imperial, Continental
- **完整的游戏规则**：包括瓷砖放置、股票交易、连锁合并等
- **自动游戏结束**：当任何酒店连锁达到41+瓷砖时游戏结束

### 👥 多玩家支持
- **2-6名玩家**：支持完整范围的玩家数量
- **独立玩家GAgent**：每个玩家都有自己的GAgent实例来管理状态
- **实时状态更新**：游戏状态变化会自动传播给所有玩家
- **回合管理**：自动轮流和验证
- **私有玩家视图**：每个玩家都有自己的私有UI和游戏状态视图

### 🖥️ 现代化界面
- **交互式游戏板**：可视化9x12网格，支持瓷砖放置
- **玩家仪表板**：显示金钱、股票和可用瓷砖
- **酒店连锁状态**：实时显示连锁规模和股票价格
- **操作控制**：直观的游戏操作按钮
- **响应式设计**：支持多种屏幕尺寸

## 技术架构

### GAgent组件

1. **AcquireGameGAgent** (`/src/Aevatar.Workshop.GAgent/GAgents/Acquire/AcquireGameGAgent.cs`)
   - 管理整体游戏状态和规则
   - 处理游戏初始化、回合管理和游戏逻辑
   - 协调玩家之间的交互并管理酒店连锁操作

2. **AcquirePlayerGAgent** (`/src/Aevatar.Workshop.GAgent/GAgents/Acquire/AcquirePlayerGAgent.cs`)
   - 管理个人玩家状态和操作
   - 处理玩家特定的操作，如瓷砖放置和股票购买
   - 提供隔离的玩家体验和私有游戏状态视图

3. **游戏事件** (`/src/Aevatar.Workshop.GAgent/Events/AcquireGameEvents.cs`)
   - 定义所有游戏事件和数据结构
   - 包括游戏管理、瓷砖放置、股票操作和状态更新事件

### API层

1. **AcquireController** (`/src/Aevatar.Workshop.Client/Controllers/AcquireController.cs`)
   - 游戏操作的REST API端点
   - 处理游戏创建、玩家管理和操作执行
   - 为前端集成提供JSON响应

### 前端

1. **Acquire游戏UI** (`/src/Aevatar.Workshop.Client/wwwroot/demos/acquire-game.html`)
   - 完整的基于Web的游戏界面
   - 实时游戏板可视化
   - 玩家状态管理和交互控制
   - 响应式设计，支持多种屏幕尺寸

## 游戏规则

### 游戏目标
通过战略性地放置瓷砖、购买和交易酒店股票来积累财富。游戏结束时，拥有最多总财富（现金 + 股票价值）的玩家获胜。

### 游戏流程
1. **初始设置**：每位玩家开始时有$6000和6张瓷砖
2. **回合进行**：玩家轮流进行以下操作：
   - 放置1张瓷砖
   - 购买最多3张股票
3. **连锁形成**：放置瓷砖可以创建新的酒店连锁或扩展现有连锁
4. **连锁合并**：当瓷砖连接多个连锁时，最大的连锁吸收其他连锁
5. **游戏结束**：当任何连锁达到41+瓷砖时游戏结束

### 酒店连锁规则
- 连锁在放置2+相邻瓷砖时形成
- 只有在有可用连锁名称时才能形成新连锁
- 连锁在11+瓷砖时变得"安全"，不能被合并
- 连锁在41+瓷砖时变得"终结"，游戏结束

### 股票规则
- 每个连锁有25股可供购买
- 玩家每回合最多可以购买3股
- 随着连锁增长，股票价格上涨
- 在合并期间，多数和少数股东获得奖金

## 如何开始游戏

1. **启动Aevatar主机**：运行Aevatar workshop应用程序
2. **访问游戏UI**：在浏览器中导航到`/demos/acquire-game.html`
3. **创建或加入游戏**：创建新游戏或加入现有游戏
4. **开始游戏**：使用界面放置瓷砖、购买股票，建立您的酒店帝国

## 游戏策略

### 基础策略
- **早期投资**：在 promising 的连锁中早期购买股票
- **多元化**：在多个连锁中分散投资
- **位置控制**：战略性地放置瓷砖以影响连锁发展

### 高级策略
- **合并时机**：预测并从连锁合并中获益
- **垄断控制**：尝试在特定连锁中获得多数股权
- **风险平衡**：平衡高风险高回报与稳定投资

## API端点

### 游戏管理
- `POST /api/acquire/create` - 创建新游戏
- `GET /api/acquire/game/{gameId}/state` - 获取游戏状态
- `POST /api/acquire/game/{gameId}/join` - 加入游戏
- `POST /api/acquire/player/{playerId}/leave` - 离开游戏

### 玩家操作
- `POST /api/acquire/player/{playerId}/place-tile` - 放置瓷砖
- `POST /api/acquire/player/{playerId}/buy-stock` - 购买股票
- `POST /api/acquire/player/{playerId}/merge-stocks` - 处理股票合并

### 玩家状态
- `GET /api/acquire/player/{playerId}/state` - 获取玩家状态
- `GET /api/acquire/player/{playerId}/history` - 获取玩家游戏历史
- `POST /api/acquire/player/{playerId}/connection` - 设置连接状态

## 扩展功能

### 已实现功能
- ✅ 完整的游戏规则实现
- ✅ 多玩家支持
- ✅ 实时状态同步
- ✅ 响应式Web界面
- ✅ 独立的玩家GAgent
- ✅ 事件驱动的架构

### 潜在增强
- 🔄 WebSocket集成（实时更新，无需轮询）
- 🤖 AI玩家（计算机对手）
- 🏆 锦标赛模式（多游戏锦标赛和计分）
- 📱 移动应用（原生移动应用）
- 💾 持久化存储（长期游戏统计和排行榜）
- 🎮 游戏重放（详细的游戏回放和分析）

## 技术细节

### 状态管理
- **游戏状态**：由AcquireGameGAgent管理的集中式游戏状态
- **玩家状态**：由AcquirePlayerGAgent管理的个人玩家状态
- **事件溯源**：所有游戏操作都存储为事件以供审计
- **实时更新**：事件将状态变化传播给所有相关组件

### 通信
- **事件驱动架构**：GAgent通过事件进行通信
- **REST API**：前端通过HTTP端点进行通信
- **状态同步**：自动在所有玩家视图中更新状态

### 可扩展性
- **独立玩家GAgent**：每个玩家都有自己的GAgent实例
- **隔离状态**：玩家状态与游戏状态分开管理
- **并发操作**：多个玩家可以同时与自己的GAgent交互

## 故障排除

### 常见问题
1. **游戏无法创建**：确保Aevatar主机正在运行
2. **玩家无法加入**：检查游戏ID是否正确
3. **操作无响应**：验证是否是玩家的回合
4. **状态不同步**：刷新页面或等待自动更新

### 调试技巧
- 使用浏览器开发者工具检查网络请求
- 查看浏览器控制台是否有错误信息
- 检查Aevatar主机日志以了解GAgent状态

## 文件结构

```
src/
├── Aevatar.Workshop.GAgent/
│   ├── Events/
│   │   └── AcquireGameEvents.cs          # 游戏事件和数据模型
│   └── GAgents/
│       └── Acquire/
│           ├── AcquireGameGAgent.cs       # 主游戏逻辑GAgent
│           └── AcquirePlayerGAgent.cs     # 个人玩家GAgent
└── Aevatar.Workshop.Client/
    ├── Controllers/
    │   └── AcquireController.cs           # REST API端点
    └── wwwroot/
        └── demos/
            └── acquire-game.html           # 前端游戏界面
```

## 贡献

这个演示展示了Aevatar GAgent框架在构建复杂多人游戏方面的强大功能。它体现了以下关键概念：

- **分布式状态管理**：使用多个GAgent管理不同类型的状态
- **事件驱动通信**：通过事件实现组件间的松耦合通信
- **实时用户体验**：为多个用户提供同步的游戏体验
- **可扩展架构**：轻松添加新功能和更多玩家

## 许可证

此演示项目仅供学习和演示目的。