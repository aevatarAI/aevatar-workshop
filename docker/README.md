# Aevatar Workshop Docker 部署指南

## 概述

本指南介绍如何将 Aevatar Workshop Host 容器化部署，包括所有必要的 MCP servers 和依赖项。

## 目录结构

```
docker/
├── Dockerfile                    # 主Dockerfile
├── appsettings.container.json    # 容器化配置文件
├── docker-compose.yml           # Docker Compose配置
├── kubernetes-deployment.yaml   # Kubernetes部署配置
├── build-and-run.sh            # 构建和运行脚本
├── health-check.sh             # 健康检查脚本
└── README.md                   # 本文档
```

## 快速开始

### 1. 使用Docker Compose（推荐）

```bash
# 进入docker目录
cd docker

# 构建并启动服务
./build-and-run.sh compose

# 查看日志
./build-and-run.sh logs compose

# 停止服务
./build-and-run.sh stop compose
```

### 2. 使用Docker直接运行

```bash
# 构建并启动容器
./build-and-run.sh docker

# 查看日志
./build-and-run.sh logs docker

# 停止容器
./build-and-run.sh stop docker
```

### 3. 健康检查

```bash
# 执行健康检查
./health-check.sh
```

## 配置说明

### MCP Servers 配置

容器中预装了以下 MCP servers：

#### Node.js MCP Servers
- **filesystem**: 文件系统操作
- **memory**: 内存存储
- **sequential-thinking**: 顺序思考
- **everything**: 综合工具集
- **context7**: 通用MCP服务器

#### Python MCP Servers
- **time**: 时间和时区转换

### 环境变量

| 变量名 | 默认值 | 说明 |
|--------|--------|------|
| ASPNETCORE_ENVIRONMENT | Production | .NET环境 |
| NODE_ENV | production | Node.js环境 |
| PYTHONPATH | /home/aevatar/.local/lib/python3.9/site-packages | Python包路径 |

### 端口配置

- **5000**: HTTP API端口

### 卷挂载

- **/workspace**: 工作空间目录
- **/app/data**: 应用数据目录
- **/app/logs**: 日志目录

## Kubernetes 部署

### 1. 构建镜像

```bash
# 构建镜像
docker build -t aevatar-workshop:latest -f Dockerfile ..

# 推送到镜像仓库（可选）
docker tag aevatar-workshop:latest your-registry/aevatar-workshop:latest
docker push your-registry/aevatar-workshop:latest
```

### 2. 部署到Kubernetes

```bash
# 应用部署配置
kubectl apply -f kubernetes-deployment.yaml

# 检查部署状态
kubectl get pods -l app=aevatar-workshop

# 查看日志
kubectl logs -f deployment/aevatar-workshop
```

### 3. 访问服务

```bash
# 端口转发（开发环境）
kubectl port-forward service/aevatar-workshop-service 5000:80

# 访问服务
curl http://localhost:5000/health
```

## 故障排除

### 常见问题

#### 1. MCP Server 启动失败

**症状**: 日志显示 "Failed to start MCP server"

**解决方案**:
```bash
# 检查MCP server是否正确安装
docker exec aevatar-workshop-host node --version
docker exec aevatar-workshop-host python3 --version

# 验证MCP server可执行性
docker exec aevatar-workshop-host node /usr/local/lib/node_modules/@modelcontextprotocol/server-filesystem/dist/index.js --help
```

#### 2. 权限问题

**症状**: 容器启动失败，权限被拒绝

**解决方案**:
```bash
# 检查文件权限
ls -la docker/

# 修复权限
chmod +x docker/*.sh
```

#### 3. 端口冲突

**症状**: 端口5000已被占用

**解决方案**:
```bash
# 检查端口占用
netstat -tulpn | grep :5000

# 修改端口映射
docker run -p 5001:5000 aevatar-workshop:latest
```

#### 4. 内存不足

**症状**: 容器启动缓慢或失败

**解决方案**:
```bash
# 增加Docker内存限制
# 在Docker Desktop设置中增加内存分配

# 或使用资源限制运行
docker run --memory=1g --cpus=0.5 aevatar-workshop:latest
```

### 日志分析

#### 查看应用日志
```bash
# Docker Compose
docker-compose logs -f

# Docker
docker logs -f aevatar-workshop-host

# Kubernetes
kubectl logs -f deployment/aevatar-workshop
```

#### 查看MCP Server日志
```bash
# 进入容器
docker exec -it aevatar-workshop-host bash

# 查看MCP相关日志
grep -i "mcp" /app/logs/log-*.log
```

## 性能优化

### 1. 镜像优化

- 使用多阶段构建减少镜像大小
- 清理不必要的包和缓存
- 使用Alpine基础镜像（可选）

### 2. 资源限制

```yaml
resources:
  requests:
    memory: "512Mi"
    cpu: "250m"
  limits:
    memory: "1Gi"
    cpu: "500m"
```

### 3. 缓存优化

- 使用Docker层缓存
- 预安装常用MCP servers
- 配置适当的日志轮转

## 安全考虑

### 1. 非root用户运行

容器使用非root用户 `aevatar` 运行，提高安全性。

### 2. 最小权限原则

- 只挂载必要的卷
- 限制容器权限
- 使用只读文件系统（可选）

### 3. 网络安全

- 使用内部网络
- 配置适当的防火墙规则
- 使用HTTPS（生产环境）

## 监控和维护

### 1. 健康检查

```bash
# 定期执行健康检查
./health-check.sh

# 设置监控告警
# 监控关键指标：CPU、内存、响应时间
```

### 2. 日志管理

```bash
# 日志轮转配置
# 在appsettings.container.json中配置

# 日志分析
docker logs aevatar-workshop-host | grep -i error
```

### 3. 备份策略

```bash
# 备份数据卷
docker run --rm -v aevatar-app-data:/data -v $(pwd):/backup alpine tar czf /backup/app-data-backup.tar.gz -C /data .

# 恢复数据
docker run --rm -v aevatar-app-data:/data -v $(pwd):/backup alpine tar xzf /backup/app-data-backup.tar.gz -C /data
```

## 更新和升级

### 1. 应用更新

```bash
# 重新构建镜像
./build-and-run.sh build

# 滚动更新
docker-compose up -d --force-recreate
```

### 2. MCP Server更新

```bash
# 更新Node.js MCP servers
docker exec aevatar-workshop-host npm update -g @modelcontextprotocol/server-*

# 更新Python MCP servers
docker exec aevatar-workshop-host pip3 install --upgrade pape-mcp-server-time
```

## 支持

如有问题，请：

1. 查看本文档的故障排除部分
2. 检查应用日志
3. 执行健康检查脚本
4. 提交Issue到项目仓库