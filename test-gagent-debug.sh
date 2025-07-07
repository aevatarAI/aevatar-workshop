#!/bin/bash

echo "=== Deep Debug GAgent Tools Registration ==="
echo ""

# 1. Initialize agent
echo "Step 1: Initializing agent..."
INIT_RESPONSE=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/initialize \
  -H "Content-Type: application/json" \
  -d '{"systemLLM": "DeepSeek"}')

echo "Init Response: $INIT_RESPONSE"
AGENT_ID=$(echo $INIT_RESPONSE | grep -o '"agentId":"[^"]*' | cut -d'"' -f4)

if [ -z "$AGENT_ID" ]; then
    echo "Failed to initialize agent"
    exit 1
fi

echo "Agent ID: $AGENT_ID"
echo ""

# 2. Get initial agent info
echo "Step 2: Getting initial agent info..."
INITIAL_INFO=$(curl -s "http://localhost:5000/api/DynamicAIMCP/agent-info?agentId=$AGENT_ID")
echo "Initial agent info:"
echo "$INITIAL_INFO" | python3 -m json.tool 2>/dev/null || echo "RAW: $INITIAL_INFO"
echo ""

# 3. Configure GAgent tools
echo "Step 3: Configuring GAgent tools (tools.math)..."
CONFIGURE_REQUEST="{
    \"agentId\": \"$AGENT_ID\",
    \"selectedGAgents\": [\"tools.math\"]
}"
echo "Configure request: $CONFIGURE_REQUEST"

CONFIGURE_RESPONSE=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/configure-gagent-tools \
  -H "Content-Type: application/json" \
  -d "$CONFIGURE_REQUEST")

echo "Configure response: $CONFIGURE_RESPONSE"
echo ""

# 4. Get agent info after configuration
echo "Step 4: Getting agent info after configuration..."
sleep 2 # Give some time for the configuration to take effect
AFTER_INFO=$(curl -s "http://localhost:5000/api/DynamicAIMCP/agent-info?agentId=$AGENT_ID")
echo "Agent info after configuration:"
echo "$AFTER_INFO" | python3 -m json.tool 2>/dev/null || echo "RAW: $AFTER_INFO"
echo ""

# 5. Test a simple message without math
echo "Step 5: Testing simple message..."
SIMPLE_RESPONSE=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/chat-with-details \
  -H "Content-Type: application/json" \
  -d "{
    \"agentId\": \"$AGENT_ID\",
    \"message\": \"What tools do you have available?\"
  }")

echo "Simple message response:"
echo "$SIMPLE_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "RAW: $SIMPLE_RESPONSE"
echo ""

# 6. Test math calculation
echo "Step 6: Testing math calculation..."
MATH_RESPONSE=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/chat-with-details \
  -H "Content-Type: application/json" \
  -d "{
    \"agentId\": \"$AGENT_ID\",
    \"message\": \"Please calculate 25 + 17 using the math tool\"
  }")

echo "Math calculation response:"
echo "$MATH_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "RAW: $MATH_RESPONSE"
echo ""

echo "=== Debug Complete ===" 