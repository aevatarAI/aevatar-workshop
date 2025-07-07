#!/bin/bash

echo "Waiting for service to start..."
sleep 15

echo "Testing Dynamic AI Demo Tool Configuration"

# Test 1: Initialize agent
echo -e "\n1. Initializing agent..."
RESPONSE=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/initialize \
  -H "Content-Type: application/json" \
  -d '{"systemLLM": "DeepSeek"}')

echo "Response: $RESPONSE"

# Extract agentId from response
AGENT_ID=$(echo $RESPONSE | grep -o '"agentId":"[^"]*' | cut -d'"' -f4)

if [ -z "$AGENT_ID" ]; then
    echo "Failed to initialize agent"
    exit 1
fi

echo "Agent initialized with ID: $AGENT_ID"

# Test 2: Get available GAgents
echo -e "\n2. Getting available GAgents..."
curl -s http://localhost:5000/api/DynamicAIMCP/available-gagents | jq .

# Test 3: Configure MCP servers
echo -e "\n3. Configuring MCP servers (filesystem)..."
RESPONSE=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/configure-servers \
  -H "Content-Type: application/json" \
  -d "{
    \"agentId\": \"$AGENT_ID\",
    \"servers\": [{
      \"serverName\": \"filesystem\",
      \"command\": \"npx\",
      \"args\": [\"-y\", \"@modelcontextprotocol/server-filesystem\", \"/tmp\"],
      \"description\": \"Access and manage files\"
    }]
  }")

echo "Response: $RESPONSE" | jq .

# Test 4: Send a chat message
echo -e "\n4. Testing chat with tools..."
RESPONSE=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/chat-with-details \
  -H "Content-Type: application/json" \
  -d "{
    \"agentId\": \"$AGENT_ID\",
    \"message\": \"List the files in /tmp directory\"
  }")

echo "Chat response:"
echo "$RESPONSE" | jq .

echo -e "\nTest completed!" 