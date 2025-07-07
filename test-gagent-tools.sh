#!/bin/bash

echo "=== Testing GAgent Tools in Dynamic AI Demo ==="
echo ""

# Wait for services to be ready
echo "Waiting for services to start..."
sleep 10

# 1. Initialize agent
echo "1. Initializing agent..."
INIT_RESPONSE=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/initialize \
  -H "Content-Type: application/json" \
  -d '{"systemLLM": "DeepSeek"}')

echo "Response: $INIT_RESPONSE"

# Extract agentId
AGENT_ID=$(echo $INIT_RESPONSE | grep -o '"agentId":"[^"]*' | cut -d'"' -f4)

if [ -z "$AGENT_ID" ]; then
    echo "Failed to initialize agent"
    exit 1
fi

echo "Agent initialized with ID: $AGENT_ID"
echo ""

# 2. Get available GAgents
echo "2. Getting available GAgents..."
GAGENTS_RESPONSE=$(curl -s http://localhost:5000/api/DynamicAIMCP/available-gagents)
echo "Available GAgents:"
echo "$GAGENTS_RESPONSE" | python3 -m json.tool || echo "$GAGENTS_RESPONSE"
echo ""

# 3. Configure GAgent tools (MathGAgent)
echo "3. Configuring GAgent tools (MathGAgent)..."
CONFIGURE_RESPONSE=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/configure-gagent-tools \
  -H "Content-Type: application/json" \
  -d "{
    \"agentId\": \"$AGENT_ID\",
    \"selectedGAgents\": [
        \"tools.math\"
    ]
  }")

echo "Response: $CONFIGURE_RESPONSE"
echo ""

# 4. Get agent info to see tools
echo "4. Getting agent info to check registered tools..."
INFO_RESPONSE=$(curl -s -X GET "http://localhost:5000/api/DynamicAIMCP/agent-info?agentId=$AGENT_ID")
echo "Agent Info:"
echo "$INFO_RESPONSE" | python3 -m json.tool || echo "$INFO_RESPONSE"
echo ""

# 5. Test GAgent tool with chat
echo "5. Testing GAgent tool with math calculation..."
CHAT_RESPONSE=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/chat-with-details \
  -H "Content-Type: application/json" \
  -d "{
    \"agentId\": \"$AGENT_ID\",
    \"message\": \"Calculate 15 * 23 + 47\"
  }")

echo "Chat Response:"
echo "$CHAT_RESPONSE" | python3 -m json.tool || echo "$CHAT_RESPONSE"
echo ""

echo "=== Test Complete ===" 