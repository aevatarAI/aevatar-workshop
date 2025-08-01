# Acquire 本地双人游玩调试指南

本指南帮助你逐步调试 Acquire 游戏的本地双人游玩流程，识别问题所在。

## 前提条件

1. Aevatar Workshop 应用正在运行
2. 可以访问游戏界面：`/demos/acquire-game.html`
3. 浏览器开发者工具打开（F12），查看 Console 和 Network 标签

## 调试步骤

### 步骤 1：启动应用和访问界面

**操作：**
1. 启动 Aevatar Workshop 应用
2. 在浏览器中访问 `http://localhost:<port>/demos/acquire-game.html`

**预期结果：**
- ✅ 页面正常加载，显示游戏界面
- ✅ 看到 "Create New Game" 和 "Join Game" 按钮
- ✅ 显示 9x12 的空白游戏棋盘
- ✅ Play Info 显示 "Status: Not in game"

**如果失败：**
- ❌ 页面无法加载 → 检查应用是否正常启动
- ❌ 界面显示异常 → 检查浏览器控制台错误

---

### 步骤 2：玩家1创建游戏

**操作：**
1. 点击 "Create New Game" 按钮
2. 输入玩家名称（如 "Player1"）
3. 点击 "Create Game" 按钮

**预期结果：**
- ✅ 弹出成功提示，显示游戏ID
- ✅ Game Status 显示 "Waiting for players..."
- ✅ Play Info 显示玩家名称和状态
- ✅ Players List 显示 Player1
- ✅ 界面按钮状态更新：
  - "Create New Game" 按钮隐藏
  - "Join Game" 按钮隐藏
  - "Leave Game" 按钮显示

**需要检查的API调用：**
```
POST /api/acquire/create
Request: {"gameId": "<generated-id>", "creatorName": "Player1"}
Response: {
  "success": true,
  "gameId": "<game-id>",
  "playerId": "<player-id>",
  "playerName": "Player1",
  "status": "WaitingForPlayers"
}
```

**如果失败：**
- ❌ 创建失败 → 检查 Network 标签中的 API 响应
- ❌ 状态未更新 → 检查 JavaScript 控制台错误

---

### 步骤 3：玩家1检查游戏状态

**操作：**
1. 等待几秒钟，观察界面是否自动更新
2. 检查 Play Info 区域

**预期结果：**
- ✅ Status 显示 "Waiting for players..." 或 "Not in game"
- ✅ Money 显示 $6,000
- ✅ Tiles 显示初始数量（应该是6个）
- ✅ Stocks 显示 0

**需要检查的API调用：**
```
GET /api/acquire/game/{gameId}/state
GET /api/acquire/player/{playerId}/state
```

**如果失败：**
- ❌ 玩家状态未加载 → 检查玩家状态API
- ❌ 显示 "Not in game" → 玩家可能没有正确加入游戏

---

### 步骤 4：玩家2加入游戏

**操作：**
1. **打开新的浏览器窗口或无痕窗口**
2. 访问相同的游戏界面 `http://localhost:<port>/demos/acquire-game.html`
3. 点击 "Join Game" 按钮
4. 输入第一步中显示的游戏ID
5. 输入玩家名称（如 "Player2"）
6. 点击 "Join Game" 按钮

**预期结果：**
- ✅ 玩家2成功加入游戏
- ✅ 两个玩家的界面都显示两个玩家
- ✅ 游戏状态应该自动变为 "InProgress"
- ✅ 随机选择一个玩家作为当前玩家

**需要检查的API调用：**
```
POST /api/acquire/game/{gameId}/join
Request: {"playerName": "Player2"}
Response: {
  "success": true,
  "playerId": "<player2-id>",
  "gameId": "<game-id>",
  "playerName": "Player2"
}
```

**如果失败：**
- ❌ 加入失败 → 检查游戏ID是否正确
- ❌ 游戏状态未更新 → 检查游戏是否自动开始

---

### 步骤 5：验证游戏状态同步

**操作：**
1. 在两个浏览器窗口中观察游戏状态
2. 检查 Players List 是否都显示两个玩家
3. 检查当前玩家指示器

**预期结果：**
- ✅ 两个窗口都显示相同的玩家列表
- ✅ 其中一个窗口显示 "Your turn"
- ✅ 另一个窗口显示 "Waiting..."
- ✅ 游戏状态为 "InProgress"

**需要检查的状态：**
```javascript
// 游戏状态
gameState.status === "InProgress"
gameState.players.length === 2
gameState.currentPlayerId !== null

// 玩家状态
playerState.isCurrentTurn === true/false (取决于当前玩家)
playerState.money === 6000
playerState.tileIds.length === 6
```

**如果失败：**
- ❌ 状态不同步 → 检查事件通信
- ❌ 游戏未开始 → 检查游戏开始逻辑

---

### 步骤 6：当前玩家执行操作

**操作：**
1. 在当前玩家的窗口中：
2. 点击手牌中的一个瓦片
3. 点击棋盘上的一个空位
4. 点击 "Place Selected Tile" 按钮

**预期结果：**
- ✅ 瓦片成功放置到棋盘上
- ✅ 手牌中减少一个瓦片
- ✅ 棋盘上显示新放置的瓦片
- ✅ 可能触发酒店连锁形成

**需要检查的API调用：**
```
POST /api/acquire/player/{playerId}/place-tile
Request: {"row": <row>, "column": <col>, "tileId": "<tile-id>"}
Response: {
  "success": true,
  "playerId": "<player-id>",
  "tile": {"tileId": "<tile-id>", "row": <row>, "column": <col>}
}
```

**如果失败：**
- ❌ 无法放置瓦片 → 检查是否当前玩家
- ❌ API 调用失败 → 检查瓦片放置逻辑

---

### 步骤 7：回合切换

**操作：**
1. 当前玩家点击 "End Turn" 按钮
2. 等待状态更新

**预期结果：**
- ✅ 另一个玩家获得当前回合
- ✅ 界面更新显示新的当前玩家
- ✅ 两个玩家的界面都正确显示状态

**需要检查的API调用：**
```
// 回合结束应该触发状态更新
GET /api/acquire/game/{gameId}/state
GET /api/acquire/player/{playerId}/state
```

**如果失败：**
- ❌ 回合未切换 → 检查回合管理逻辑
- ❌ 状态未同步 → 检查事件传播

---

## 常见问题诊断

### 问题1：创建游戏后 Status 仍显示 "Not in game"

**可能原因：**
1. 玩家GAgent创建失败
2. 玩家加入游戏失败
3. 状态更新未正确传播

**检查方法：**
1. 检查 Network 标签中的 API 响应
2. 验证 `/api/acquire/player/{playerId}/state` 调用
3. 检查 AcquirePlayerGAgent 的 JoinGameAsync 方法

### 问题2：游戏状态未同步

**可能原因：**
1. 事件通信未正确建立
2. GAgent 之间未正确注册
3. 状态更新事件未正确处理

**检查方法：**
1. 检查 GAgent 事件订阅
2. 验证事件处理器是否正确触发
3. 检查事件传播逻辑

### 问题3：玩家无法加入游戏

**可能原因：**
1. 游戏ID错误
2. 游戏未正确创建
3. 玩家数量限制

**检查方法：**
1. 验证游戏ID格式
2. 检查游戏GAgent状态
3. 检查游戏配置

## 调试工具使用

### 浏览器开发者工具

1. **Console 标签**：
   - 查看 JavaScript 错误
   - 添加 console.log 调试信息

2. **Network 标签**：
   - 检查 API 调用状态
   - 验证请求和响应格式
   - 检查响应时间

3. **Application 标签**：
   - 检查 Session Storage
   - 验证会话数据

### 服务器端日志

检查以下日志级别：
- Information: 正常操作流程
- Warning: 警告信息
- Error: 错误信息
- Debug: 详细调试信息

## 测试检查清单

- [ ] 应用正常启动
- [ ] 游戏界面正确加载
- [ ] 创建游戏功能正常
- [ ] 玩家加入功能正常
- [ ] 状态同步正常
- [ ] 回合管理正常
- [ ] 瓦片放置功能正常
- [ ] 游戏状态正确显示
- [ ] 双人游戏可以正常进行

## 报告问题

如果发现问题，请提供以下信息：

1. **失败的步骤**：具体哪个步骤出现问题
2. **预期结果**：期望看到什么
3. **实际结果**：实际看到了什么
4. **错误信息**：浏览器控制台或网络标签中的错误
5. **API响应**：相关API调用的响应内容
6. **重现步骤**：如何重现问题

通过这个调试指南，你应该能够识别出游戏流程中的具体问题所在。