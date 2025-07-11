# 端口修改完成 ✅

## 修改内容

1. **docker-compose.yml**
   - Client端口从 `5000:80` 改为 `5001:80`

2. **quickstart-docker.sh**
   - Web Client URL: `http://localhost:5000` → `http://localhost:5001`
   - 自动打开浏览器URL已更新

## 下一步操作

运行以下命令启动服务：
```bash
./quickstart-docker.sh
```

## 新的访问地址

- Web Client: **http://localhost:5001**
- Orleans Dashboard: http://localhost:11111

## 为什么不kill端口5000的进程？

端口5000被macOS的ControlCenter（控制中心）占用，这是AirPlay接收器功能。
不建议强制kill系统进程，因为：
- 可能影响系统稳定性
- AirPlay功能会失效
- 系统可能自动重启该进程

更好的解决方案是使用其他端口（如5001）或在系统设置中关闭AirPlay接收器。 