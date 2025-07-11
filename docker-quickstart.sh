#!/bin/bash

set -e

WORKSHOP_ROOT=$(cd "$(dirname "$0")" && pwd)
cd "$WORKSHOP_ROOT"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${BLUE}[Aevatar Docker]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[Aevatar Docker]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[Aevatar Docker]${NC} $1"
}

print_error() {
    echo -e "${RED}[Aevatar Docker]${NC} $1"
}

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    print_error "Docker is not running. Please start Docker and try again."
    exit 1
fi

# Check if docker-compose is available
if ! command -v docker-compose > /dev/null 2>&1; then
    print_error "docker-compose is not installed. Please install docker-compose and try again."
    exit 1
fi

# Parse arguments
MODE=${1:-0}
GREETING=${2:-}
CLEAN_BUILD=${3:-"false"}  # Add option for clean build

print_info "🚀 Starting Aevatar Workshop with Docker..."
print_info "Mode: $MODE"
if [ -n "$GREETING" ]; then
    print_info "Greeting: $GREETING"
fi

# Step 1: Check for port conflicts and stop existing containers
print_info "🔍 Checking for port conflicts..."
PORTS_IN_USE=false
for PORT in 5001 11111 30000; do
    if lsof -i :$PORT > /dev/null 2>&1; then
        print_warning "Port $PORT is in use."
        PORTS_IN_USE=true
    fi
done

if [ "$PORTS_IN_USE" = true ]; then
    print_warning "Stopping existing containers..."
    docker stop aevatar-workshop-host aevatar-workshop-client 2>/dev/null || true
    docker rm aevatar-workshop-host aevatar-workshop-client 2>/dev/null || true
    sleep 2
fi

# Step 2: Clean up if requested
if [ "$CLEAN_BUILD" = "true" ] || [ ! -d ".docker-temp" ]; then
    print_info "🧹 Cleaning up previous builds..."
    rm -rf .docker-temp
    
    # Remove existing images if clean build
    if [ "$CLEAN_BUILD" = "true" ] && docker images | grep -q "aevatar-workshop"; then
        print_warning "Removing existing images..."
        docker rmi $(docker images | grep "aevatar-workshop" | awk '{print $3}') 2>/dev/null || true
    fi
fi

mkdir -p .docker-temp/host .docker-temp/client

# Step 3: Build production projects only (exclude test projects)
print_info "📦 Building production projects..."

# Build dependencies in order
GAGENT_PROJECTS=(
    "gAgents/src/Aevatar.GAgents.Basic/Aevatar.GAgents.Basic.csproj"
    "gAgents/src/Aevatar.GAgents.AI.Abstractions/Aevatar.GAgents.AI.Abstractions.csproj"
    "gAgents/src/Aevatar.GAgents.AIGAgent/Aevatar.GAgents.AIGAgent.csproj"
    "gAgents/src/Aevatar.GAgents.MCP/Aevatar.GAgents.MCP.csproj"
    "gAgents/src/Aevatar.GAgents.SemanticKernel/Aevatar.GAgents.SemanticKernel.csproj"
)

for PROJECT in "${GAGENT_PROJECTS[@]}"; do
    if [ -f "$PROJECT" ]; then
        print_info "Building $(basename $PROJECT .csproj)..."
        if ! dotnet build "$PROJECT" -c Release --verbosity quiet; then
            print_error "Failed to build $PROJECT"
            exit 1
        fi
    fi
done

# Build Workshop projects
print_info "Building Workshop projects..."
if ! dotnet build src/Aevatar.Workshop.GAgent/Aevatar.Workshop.GAgent.csproj -c Release --verbosity quiet; then
    print_error "Failed to build Workshop.GAgent"
    exit 1
fi

if ! dotnet build src/Aevatar.Workshop.Host/Aevatar.Workshop.Host.csproj -c Release --verbosity quiet; then
    print_error "Failed to build Workshop.Host"
    exit 1
fi

if ! dotnet build src/Aevatar.Workshop.Client/Aevatar.Workshop.Client.csproj -c Release --verbosity quiet; then
    print_error "Failed to build Workshop.Client"
    exit 1
fi

print_success "All projects built successfully!"

# Step 4: Publish applications
print_info "📁 Publishing applications..."

# Publish Host
print_info "Publishing Host application..."
dotnet publish src/Aevatar.Workshop.Host/Aevatar.Workshop.Host.csproj \
    -c Release \
    -o .docker-temp/host \
    --verbosity quiet \
    --no-build \
    --self-contained false \
    -p:PublishSingleFile=false \
    -p:PublishReadyToRun=false

# Clean up conflicting configuration files
find .docker-temp/host -name "appsettings*.json" -not -path "*Host*" -delete 2>/dev/null || true

# Ensure correct appsettings
cp src/Aevatar.Workshop.Host/appsettings.json .docker-temp/host/
if [ -f "src/Aevatar.Workshop.Host/appsettings.secrets.json" ]; then
    cp src/Aevatar.Workshop.Host/appsettings.secrets.json .docker-temp/host/
fi

# Publish Client
print_info "Publishing Client application..."
dotnet publish src/Aevatar.Workshop.Client/Aevatar.Workshop.Client.csproj \
    -c Release \
    -o .docker-temp/client \
    --verbosity quiet \
    --no-build \
    --self-contained false \
    -p:PublishSingleFile=false \
    -p:PublishReadyToRun=false

# Copy Client static files
if [ -d "src/Aevatar.Workshop.Client/wwwroot" ]; then
    cp -r src/Aevatar.Workshop.Client/wwwroot .docker-temp/client/
fi

print_success "Applications published successfully!"

# Step 5: Create optimized Dockerfiles
print_info "🐳 Creating Docker configuration..."

cat > .docker-temp/Dockerfile.host << 'EOF'
FROM mcr.microsoft.com/dotnet/aspnet:9.0

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && apt-get clean && rm -rf /var/lib/apt/lists/*

# Install Node.js for MCP servers
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && npm install -g npm@latest \
    && npm install -g @modelcontextprotocol/server-github minimax-mcp-js

WORKDIR /app
COPY . .

# Create logs directory
RUN mkdir -p /app/Logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_ENVIRONMENT=Production

EXPOSE 80 11111 30000

ENTRYPOINT ["dotnet", "Aevatar.Workshop.Host.dll"]
EOF

cat > .docker-temp/Dockerfile.client << 'EOF'
FROM mcr.microsoft.com/dotnet/aspnet:9.0

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && apt-get clean && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY . .

# Create logs directory
RUN mkdir -p /app/Logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_ENVIRONMENT=Production

EXPOSE 80

ENTRYPOINT ["dotnet", "Aevatar.Workshop.Client.dll"]
EOF

# Step 6: Build Docker images
print_info "🔨 Building Docker images..."

print_info "Building Host image..."
if ! docker build -t aevatar-workshop-host:latest -f .docker-temp/Dockerfile.host .docker-temp/host/; then
    print_error "Failed to build Host image"
    exit 1
fi

print_info "Building Client image..."
if ! docker build -t aevatar-workshop-client:latest -f .docker-temp/Dockerfile.client .docker-temp/client/; then
    print_error "Failed to build Client image"
    exit 1
fi

print_success "Docker images built successfully!"

# Step 7: Create docker-compose configuration
print_info "📝 Creating docker-compose configuration..."

cat > docker-compose.yml << EOF
version: '3.8'

services:
  aevatar-host:
    image: aevatar-workshop-host:latest
    container_name: aevatar-workshop-host
    ports:
      - "11111:80"        # Orleans Dashboard
      - "30000:30000"     # Orleans Silo
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
      - DOTNET_ENVIRONMENT=Production
    networks:
      - aevatar-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    logging:
      driver: json-file
      options:
        max-size: "10m"
        max-file: "3"

  aevatar-client:
    image: aevatar-workshop-client:latest
    container_name: aevatar-workshop-client
    ports:
      - "5001:80"         # Web Client
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
      - DOTNET_ENVIRONMENT=Production
      - ORLEANS_GATEWAY_HOST=aevatar-host
      - ORLEANS_GATEWAY_PORT=30000
    depends_on:
      aevatar-host:
        condition: service_healthy
    networks:
      - aevatar-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 30s
    logging:
      driver: json-file
      options:
        max-size: "10m"
        max-file: "3"

networks:
  aevatar-network:
    driver: bridge
    name: aevatar-network

volumes:
  aevatar-logs:
    driver: local
    name: aevatar-logs
EOF

# Step 8: Start services
print_info "🚀 Starting services..."

# Stop existing containers
docker-compose down --remove-orphans 2>/dev/null || true

# Start services
if ! docker-compose up -d; then
    print_error "Failed to start services"
    exit 1
fi

# Step 9: Wait for services to be healthy
print_info "⏳ Waiting for services to start..."

# Wait for Host service
print_info "Waiting for Host service to be ready..."
TIMEOUT=120
ELAPSED=0
while ! docker-compose ps | grep aevatar-workshop-host | grep -q "healthy\|Up"; do
    if [ $ELAPSED -ge $TIMEOUT ]; then
        print_error "Host service failed to start within $TIMEOUT seconds"
        print_info "Showing Host service logs:"
        docker-compose logs --tail=50 aevatar-host
        exit 1
    fi
    echo -n "."
    sleep 5
    ELAPSED=$((ELAPSED + 5))
done
echo ""
print_success "Host service is ready!"

# Wait for Client service
print_info "Waiting for Client service to be ready..."
TIMEOUT=60
ELAPSED=0
while ! docker-compose ps | grep aevatar-workshop-client | grep -q "healthy\|Up"; do
    if [ $ELAPSED -ge $TIMEOUT ]; then
        print_error "Client service failed to start within $TIMEOUT seconds"
        print_info "Showing Client service logs:"
        docker-compose logs --tail=50 aevatar-client
        exit 1
    fi
    echo -n "."
    sleep 5
    ELAPSED=$((ELAPSED + 5))
done
echo ""
print_success "Client service is ready!"

# Step 10: Show final status
print_success "🎉 Aevatar Workshop is running with Docker!"
echo ""
print_info "📊 Service Status:"
docker-compose ps

echo ""
print_info "🌐 Access URLs:"
print_info "   • Web Client: http://localhost:5001"
print_info "   • Orleans Dashboard: http://localhost:11111"
print_info "   • Client Health: http://localhost:5001/health"
print_info "   • Host Health: http://localhost:11111/health"

echo ""
print_info "📋 Useful Commands:"
print_info "   • View all logs: docker-compose logs -f"
print_info "   • View Host logs: docker-compose logs -f aevatar-host"
print_info "   • View Client logs: docker-compose logs -f aevatar-client"
print_info "   • Stop services: docker-compose down"
print_info "   • Restart services: docker-compose restart"
print_info "   • Clean rebuild: ./quickstart-docker-unified.sh 0 '' true"

echo ""
print_info "🔧 MCP Servers:"
print_info "   • MCP servers are automatically started within the Host container"
print_info "   • Supported: GitHub (@modelcontextprotocol/server-github)"
print_info "   • Supported: MiniMax (minimax-mcp-js)"

echo ""
if [ "$CLEAN_BUILD" = "true" ]; then
    print_warning "💡 Note: Clean build performed. Initial startup may take longer."
fi
print_success "✅ Ready to use! Open http://localhost:5001 in your browser."

# Optional: Open browser automatically (macOS/Linux)
if [[ "$OSTYPE" == "darwin"* ]] || [[ "$OSTYPE" == "linux-gnu"* ]]; then
    read -p "Open browser automatically? (y/n): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        if [[ "$OSTYPE" == "darwin"* ]]; then
            open http://localhost:5001
        else
            xdg-open http://localhost:5001 2>/dev/null || echo "Please open http://localhost:5001 manually"
        fi
    fi
fi 