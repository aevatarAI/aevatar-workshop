#!/bin/bash

echo "Testing Dynamic AI MCP Controller..."

# Test the invoke endpoint
echo "Testing invoke endpoint with Math GAgent..."
curl -X POST http://localhost:5000/api/dynamicaimcp/invoke \
  -H "Content-Type: application/json" \
  -d '{
    "agentId": "test-agent-001",
    "message": "What is 15 + 27?",
    "selectedGAgents": ["mathgagent", "timeconvertergagent"]
  }' | jq .

echo ""
echo "Testing with Time Converter GAgent..."
curl -X POST http://localhost:5000/api/dynamicaimcp/invoke \
  -H "Content-Type: application/json" \
  -d '{
    "agentId": "test-agent-002",
    "message": "Convert 2025-01-07T15:30:00Z to different timezone formats",
    "selectedGAgents": ["mathgagent", "timeconvertergagent"]
  }' | jq . 