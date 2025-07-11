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
    print_error "Docker is not running."
    exit 1
fi

# Check if docker-compose is available
if ! command -v docker-compose > /dev/null 2>&1; then
    print_error "docker-compose is not installed."
    exit 1
fi

# Parse options
CLEAN_ALL=${1:-"false"}

print_info "🛑 Shutting down Aevatar Workshop Docker services..."

# Check if services are running
if ! docker ps | grep -q "aevatar-workshop"; then
    print_warning "No Aevatar Workshop services are currently running."
else
    # Show current status
    print_info "📊 Current service status:"
    docker-compose ps 2>/dev/null || docker ps | grep aevatar-workshop

    # Stop services gracefully
    print_info "Stopping services..."
    if docker-compose down --remove-orphans 2>/dev/null; then
        print_success "Services stopped successfully!"
    else
        # Fallback: stop containers directly
        print_warning "Stopping containers directly..."
        docker stop aevatar-workshop-host aevatar-workshop-client 2>/dev/null || true
        docker rm aevatar-workshop-host aevatar-workshop-client 2>/dev/null || true
    fi
fi

# Clean up based on options
if [ "$CLEAN_ALL" = "true" ]; then
    print_warning "🧹 Full cleanup requested..."
    
    # Remove images
    print_info "Removing Docker images..."
    if docker images | grep -q "aevatar-workshop"; then
        docker rmi $(docker images | grep "aevatar-workshop" | awk '{print $3}') 2>/dev/null || true
        print_success "Images removed!"
    fi
    
    # Remove .docker-temp directory
    if [ -d ".docker-temp" ]; then
        print_info "Removing temporary build directory..."
        rm -rf .docker-temp
        print_success "Temporary files removed!"
    fi
    
    # Clean up Docker networks
    print_info "Cleaning up Docker networks..."
    docker network rm aevatar-network 2>/dev/null || true
    
    # Clean up volumes
    print_info "Cleaning up Docker volumes..."
    docker volume rm aevatar-logs 2>/dev/null || true
    
    print_success "✨ Full cleanup completed!"
else
    # Interactive cleanup options
    echo ""
    read -p "Remove Docker images? (y/n): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Removing Docker images..."
        if docker images | grep -q "aevatar-workshop"; then
            docker rmi $(docker images | grep "aevatar-workshop" | awk '{print $3}') 2>/dev/null || true
            print_success "Images removed!"
        else
            print_info "No images found to remove."
        fi
    fi
    
    echo ""
    read -p "Remove temporary build files (.docker-temp)? (y/n): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        if [ -d ".docker-temp" ]; then
            rm -rf .docker-temp
            print_success "Temporary files removed!"
        else
            print_info "No temporary files found."
        fi
    fi
fi

# Final status
print_success "🛑 Aevatar Workshop Docker services have been shut down!"
echo ""
print_info "💡 Useful Commands:"
print_info "   • Start again: ./quickstart-docker-unified.sh"
print_info "   • Start with clean build: ./quickstart-docker-unified.sh 0 '' true"
print_info "   • Full cleanup: ./shutdown-docker-unified.sh true"
echo ""

# Show remaining Docker resources
if docker ps -a | grep -q "aevatar-workshop"; then
    print_warning "⚠️  Some containers still exist:"
    docker ps -a | grep aevatar-workshop
fi

if docker images | grep -q "aevatar-workshop"; then
    print_warning "⚠️  Some images still exist:"
    docker images | grep aevatar-workshop
fi 