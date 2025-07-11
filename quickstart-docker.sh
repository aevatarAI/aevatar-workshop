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
    echo -e "${BLUE}[Aevatar Docker Workshop]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[Aevatar Docker Workshop]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[Aevatar Docker Workshop]${NC} $1"
}

print_error() {
    echo -e "${RED}[Aevatar Docker Workshop]${NC} $1"
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

# Parse arguments for Client
MODE=${1:-0}
GREETING=${2:-}

print_info "Starting Aevatar Workshop with Docker..."
print_info "Mode: $MODE"
if [ -n "$GREETING" ]; then
    print_info "Greeting: $GREETING"
fi

# Step 1: Clean up existing containers and images
print_info "Cleaning up existing containers and images..."

# Stop and remove existing containers
if docker-compose ps | grep -q "aevatar-workshop"; then
    print_warning "Stopping existing containers..."
    docker-compose down --remove-orphans
fi

# Remove existing images if they exist
if docker images | grep -q "aevatar-workshop"; then
    print_warning "Removing existing images..."
    docker rmi $(docker images | grep "aevatar-workshop" | awk '{print $3}') 2>/dev/null || true
fi

# Step 2: Build Docker images
print_info "Building Docker images..."
print_info "This may take several minutes on first run..."

# Use the retry build script for better reliability
if ! ./docker-build-retry.sh; then
    print_error "Failed to build Docker images. Please check the error messages above."
    print_info "You can try running './docker-build-retry.sh' separately for more detailed output."
    exit 1
fi

print_success "Docker images built successfully!"

# Step 3: Start services
print_info "Starting services with docker-compose..."

# Set environment variables for client parameters
export CLIENT_MODE=$MODE
export CLIENT_GREETING=$GREETING

# Start services in background
if ! docker-compose up -d; then
    print_error "Failed to start services. Please check the error messages above."
    exit 1
fi

# Step 4: Wait for services to be healthy
print_info "Waiting for services to start..."

# Wait for Host service
print_info "Waiting for Host service to be ready..."
timeout 120 bash -c '
    while ! docker-compose ps | grep aevatar-workshop-host | grep -q healthy; do
        echo "Host service starting..."
        sleep 5
    done
' || {
    print_error "Host service failed to start within 120 seconds"
    print_info "Showing Host service logs:"
    docker-compose logs aevatar-host
    exit 1
}

print_success "Host service is ready!"

# Wait for Client service
print_info "Waiting for Client service to be ready..."
timeout 60 bash -c '
    while ! docker-compose ps | grep aevatar-workshop-client | grep -q healthy; do
        echo "Client service starting..."
        sleep 5
    done
' || {
    print_error "Client service failed to start within 60 seconds"
    print_info "Showing Client service logs:"
    docker-compose logs aevatar-client
    exit 1
}

print_success "Client service is ready!"

# Step 5: Show service status
print_success "🚀 Aevatar Workshop is running with Docker!"
echo ""
print_info "📊 Service Status:"
docker-compose ps

echo ""
print_info "🌐 Access URLs:"
print_info "   • Web Client: http://localhost:5001"
print_info "   • Orleans Dashboard: http://localhost:11111"

echo ""
print_info "📋 Useful Commands:"
print_info "   • View logs: docker-compose logs -f"
print_info "   • View Host logs: docker-compose logs -f aevatar-host"
print_info "   • View Client logs: docker-compose logs -f aevatar-client"
print_info "   • Stop services: docker-compose down"
print_info "   • Restart services: docker-compose restart"

echo ""
print_info "🔧 MCP Servers:"
print_info "   • MCP servers are automatically started within the Host container"
print_info "   • Supported: GitHub, MiniMax, WeRead, Zhipu Web Search"

echo ""
print_warning "💡 Note: Initial startup may take longer as MCP packages are installed"
print_success "Ready to use! Open http://localhost:5001 in your browser."

# Optional: Open browser automatically (macOS)
if [[ "$OSTYPE" == "darwin"* ]]; then
    read -p "Open browser automatically? (y/n): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        open http://localhost:5001
    fi
fi