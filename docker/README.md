# Aevatar Workshop Docker Deployment

The simplest way to deploy and run Aevatar Workshop using Docker.

## 🚀 Quick Start

Aevatar Workshop provides two simple scripts to manage Docker deployment:

### Start Services

```bash
# Run from aevatar-workshop root directory
./docker-quickstart.sh
```

This script will automatically:
- Build Docker images
- Start all necessary services
- Configure MCP servers
- Display service access URLs

### Stop Services

```bash
# Run from aevatar-workshop root directory
./docker-shutdown.sh
```

This script will:
- Gracefully stop all running containers
- Clean up related resources
- Preserve data volumes (if needed)

---

## 📋 System Requirements

- Docker Engine 20.10+ 
- Docker Compose 2.0+
- At least 2GB available memory
- At least 5GB available disk space

---

## ⚙️ Configuration

### Environment Variables

Docker deployment uses the following default configuration:

| Variable | Default | Description |
|----------|---------|-------------|
| `HOST_PORT` | 5000 | Host service port |
| `CLIENT_PORT` | 5147 | Client service port |
| `ORLEANS_PORT` | 11111 | Orleans Silo port |

### AI Configuration

To use AI features, configure environment variables before starting:

```bash
export OPENAI_API_KEY="your-api-key"
export OPENAI_ENDPOINT="https://your-endpoint.openai.azure.com/"
./docker-quickstart.sh
```

Or modify the environment variables section in `docker-compose.yml`.

---

## 🏗️ Project Structure

```
aevatar-workshop/
├── docker-compose.yml         # Docker Compose configuration
├── docker-quickstart.sh       # Start script
├── docker-shutdown.sh         # Stop script
├── src/
│   ├── Aevatar.Workshop.Host/
│   │   └── Dockerfile         # Host service Dockerfile
│   └── Aevatar.Workshop.Client/
│       └── Dockerfile         # Client service Dockerfile
└── docker/
    ├── README.md              # This document
    └── README.zh.md           # Chinese version
```

---

## 🔍 Common Operations

### View Logs

```bash
# View all service logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f host
docker-compose logs -f client
```

### Access Containers

```bash
# Access Host container
docker exec -it aevatar-workshop-host bash

# Access Client container
docker exec -it aevatar-workshop-client bash
```

### Restart Services

```bash
# Restart all services
docker-compose restart

# Restart specific service
docker-compose restart host
```

---

## 🐛 Troubleshooting

### Port Conflicts

If ports are already in use, modify environment variables:

```bash
export HOST_PORT=5001
export CLIENT_PORT=5148
./docker-quickstart.sh
```

### Insufficient Memory

If you encounter memory issues, increase Docker Desktop's memory allocation:
- Docker Desktop → Settings → Resources → Memory → Allocate at least 4GB

### MCP Server Issues

If MCP servers fail to start:

```bash
# Check MCP service status
docker exec aevatar-workshop-host ps aux | grep mcp

# Reinstall MCP servers
docker exec aevatar-workshop-host npm install -g @modelcontextprotocol/server-filesystem
```

---

## 📊 Resource Usage

Typical resource usage:
- **CPU**: 0.5-2 cores
- **Memory**: 1-2 GB
- **Disk**: 2-5 GB (including images)

---

## 🔄 Updates

To update to the latest version:

```bash
# Pull latest code
git pull

# Rebuild and start
./docker-shutdown.sh
./docker-quickstart.sh
```

---

## 💡 Tips

1. **Data Persistence**: All data is stored in Docker volumes, stopping containers won't lose data
2. **Network Mode**: Services use bridge network by default, can access each other by service name
3. **Health Checks**: Containers include health checks, viewable via `docker ps`

---

## 📚 More Information

- Main Project README: [../README.md](../README.md)
- Docker Compose Configuration: [../docker-compose.yml](../docker-compose.yml)
- Docker Scripts: [docker-quickstart.sh](../docker-quickstart.sh), [docker-shutdown.sh](../docker-shutdown.sh)
- Issue Tracker: [GitHub Issues](https://github.com/aevatarAI/aevatar-workshop/issues)

---

Happy deploying! 🚀