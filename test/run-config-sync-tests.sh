#!/bin/bash

echo "🌌 Running Configuration Sync Tests..."
echo "======================================"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Function to run tests with formatting
run_test() {
    local test_name=$1
    local filter=$2
    
    echo -e "\n${YELLOW}Running: ${test_name}${NC}"
    echo "----------------------------------------"
    
    if dotnet test Aevatar.Workshop.Tests --filter "$filter" --logger "console;verbosity=normal"; then
        echo -e "${GREEN}✅ ${test_name} passed!${NC}"
    else
        echo -e "${RED}❌ ${test_name} failed!${NC}"
        exit 1
    fi
}

# Navigate to test directory
cd "$(dirname "$0")"

# Run different test suites
echo -e "${YELLOW}1. Testing ConfigManagerGAgent basics...${NC}"
run_test "ConfigManagerGAgent Tests" "FullyQualifiedName~ConfigManagerGAgentTests"

echo -e "\n${YELLOW}2. Testing Configuration Sync Integration...${NC}"
run_test "Config Sync Integration Tests" "FullyQualifiedName~ConfigSyncIntegrationTests"

echo -e "\n${YELLOW}3. Running all Config Sync tests with detailed output...${NC}"
dotnet test Aevatar.Workshop.Tests \
    --filter "FullyQualifiedName~ConfigManagerGAgentTests|ConfigSyncIntegrationTests" \
    --logger "console;verbosity=detailed" \
    --logger "html;LogFileName=config-sync-test-results.html"

echo -e "\n${GREEN}🎉 All Configuration Sync tests completed!${NC}"
echo -e "Test results saved to: TestResults/config-sync-test-results.html" 