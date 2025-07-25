# Aevatar Workshop Docker 部署

使用 Docker 快速部署和运行 Aevatar Workshop 的最简单方法。

## 🚀 快速开始

Aevatar Workshop 提供了两个简单的脚本来管理 Docker 部署：

### 启动服务

```bash
# 在 aevatar-workshop 根目录下运行
./docker-quickstart.sh
```

这个脚本会自动：
- 构建 Docker 镜像
- 启动所有必要的服务
- 配置 MCP 服务器
- 显示服务访问地址

### 停止服务

```bash
# 在 aevatar-workshop 根目录下运行
./docker-shutdown.sh
```

这个脚本会：
- 优雅地停止所有运行中的容器
- 清理相关资源
- 保留数据卷（如需要）

---

## 📋 系统要求

- Docker Engine 20.10+ 
- Docker Compose 2.0+
- 至少 2GB 可用内存
- 至少 5GB 可用磁盘空间

---

## ⚙️ 配置说明

### 环境变量

Docker 部署使用以下默认配置：

| 变量 | 默认值 | 说明 |
|------|--------|------|
| `HOST_PORT` | 5000 | Host 服务端口 |
| `CLIENT_PORT` | 5147 | Client 服务端口 |
| `ORLEANS_PORT` | 11111 | Orleans Silo 端口 |

### AI 配置

如需使用 AI 功能，请在启动前配置环境变量：

```bash
export OPENAI_API_KEY="your-api-key"
export OPENAI_ENDPOINT="https://your-endpoint.openai.azure.com/"
./docker-quickstart.sh
```

或者修改 `docker-compose.yml` 中的环境变量部分。

---

## 🏗️ 项目结构

```
aevatar-workshop/
├── docker-compose.yml         # Docker Compose 配置文件
├── docker-quickstart.sh       # 启动脚本
├── docker-shutdown.sh         # 停止脚本
├── src/
│   ├── Aevatar.Workshop.Host/
│   │   └── Dockerfile         # Host 服务 Dockerfile
│   └── Aevatar.Workshop.Client/
│       └── Dockerfile         # Client 服务 Dockerfile
└── docker/
    ├── README.md              # 英文文档
    └── README.zh.md           # 本文档
```

---

## 🔍 常用操作

### 查看日志

```bash
# 查看所有服务日志
docker-compose logs -f

# 查看特定服务日志
docker-compose logs -f host
docker-compose logs -f client
```

### 进入容器

```bash
# 进入 Host 容器
docker exec -it aevatar-workshop-host bash

# 进入 Client 容器
docker exec -it aevatar-workshop-client bash
```

### 重启服务

```bash
# 重启所有服务
docker-compose restart

# 重启特定服务
docker-compose restart host
```

---

## 🐛 故障排除

### 端口冲突

如果端口已被占用，可以修改环境变量：

```bash
export HOST_PORT=5001
export CLIENT_PORT=5148
./docker-quickstart.sh
```

### 内存不足

如果遇到内存问题，请增加 Docker Desktop 的内存分配：
- Docker Desktop → 设置 → 资源 → 内存 → 至少分配 4GB

### MCP Server 问题

如果 MCP 服务器无法启动：

```bash
# 检查 MCP 服务状态
docker exec aevatar-workshop-host ps aux | grep mcp

# 重新安装 MCP 服务器
docker exec aevatar-workshop-host npm install -g @modelcontextprotocol/server-filesystem
```

---

## 📊 资源使用

典型资源使用情况：
- **CPU**: 0.5-2 核心
- **内存**: 1-2 GB
- **磁盘**: 2-5 GB（包含镜像）

---

## 🔄 更新

更新到最新版本：

```bash
# 拉取最新代码
git pull

# 重新构建并启动
./docker-shutdown.sh
./docker-quickstart.sh
```

---

## 💡 提示

1. **数据持久化**: 所有数据存储在 Docker 卷中，停止容器不会丢失数据
2. **网络模式**: 服务默认使用桥接网络，相互之间可以通过服务名访问
3. **健康检查**: 容器包含健康检查，可通过 `docker ps` 查看状态

---

## 📚 更多信息

- 主项目 README: [../README.zh.md](../README.zh.md)
- Docker Compose 配置: [../docker-compose.yml](../docker-compose.yml)
- Docker 脚本: [docker-quickstart.sh](../docker-quickstart.sh), [docker-shutdown.sh](../docker-shutdown.sh)
- 问题反馈: [GitHub Issues](https://github.com/aevatarAI/aevatar-workshop/issues)

---

祝您使用愉快！ 🚀 