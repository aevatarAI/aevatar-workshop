# MCP时间服务器设置指南

## 问题说明
MCP时间服务器需要额外安装Python包`pape-mcp-server-time`才能使用。

## 安装步骤

### 1. 安装时间服务器包
```bash
pip install pape-mcp-server-time
```

或者使用pip3:
```bash
pip3 install pape-mcp-server-time
```

### 2. 验证安装
```bash
python3 -m pape_mcp_server_time --help
```

### 3. 配置已添加
时间服务器配置已经添加到 `src/Aevatar.Workshop.Host/appsettings.json`:

```json
"time": {
  "Command": "python3",
  "Args": ["-m", "pape_mcp_server_time"],
  "ServerName": "time",
  "Description": "Time and timezone conversion tools"
}
```

## 可用工具

时间服务器提供以下工具：

1. **get_current_time** - 获取特定时区的当前时间
   - 参数：`timezone` (string) - IANA时区名称（如 'Asia/Shanghai', 'America/New_York'）

2. **convert_time** - 在时区之间转换时间
   - 参数：
     - `source_timezone` (string) - 源时区
     - `time` (string) - 24小时格式的时间 (HH:MM)
     - `target_timezone` (string) - 目标时区

## 使用示例

1. 获取当前时间：
   - "现在几点了？"（使用系统时区）
   - "东京现在几点？"
   - "纽约现在是什么时间？"

2. 时区转换：
   - "纽约下午4点是北京几点？"
   - "将东京时间上午9:30转换为纽约时间"

## 故障排除

如果时间服务器无法初始化：

1. 确认Python包已安装：
   ```bash
   pip list | grep pape-mcp-server-time
   ```

2. 测试直接运行：
   ```bash
   python3 -m pape_mcp_server_time
   ```

3. 检查Python路径：
   ```bash
   which python3
   ```
   如果路径不是 `/usr/local/bin/python3`，需要更新配置文件中的Command路径。

## 替代方案

如果无法安装Python包，可以考虑：
1. 使用Docker容器运行时间服务器
2. 自己实现一个简单的时间服务GAgent 