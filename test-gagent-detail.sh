#!/bin/bash

echo "=== Test GAgent Tools Detailed Info ==="
echo ""

# 1. Initialize agent
echo "1. Initializing agent with DeepSeek..."
INIT_RESPONSE=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/initialize \
  -H "Content-Type: application/json" \
  -d '{"systemLLM": "DeepSeek"}')

AGENT_ID=$(echo $INIT_RESPONSE | grep -o '"agentId":"[^"]*' | cut -d'"' -f4)
echo "Agent ID: $AGENT_ID"
echo ""

# 2. Get available GAgents
echo "2. Getting available GAgents..."
AVAILABLE_GAGENTS=$(curl -s http://localhost:5000/api/DynamicAIMCP/available-gagents)
echo "Available GAgents:"
echo "$AVAILABLE_GAGENTS" | python3 -m json.tool
echo ""

# 3. Configure MCP server (filesystem)
echo "3. Configuring MCP server (filesystem)..."
MCP_RESPONSE=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/configure-servers \
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
echo "MCP configure response: $MCP_RESPONSE"
echo ""

# 4. Configure GAgent tools (MathGAgent)
echo "4. Configuring GAgent tools (tools.math)..."
CONFIGURE_RESPONSE=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/configure-gagent-tools \
  -H "Content-Type: application/json" \
  -d "{
    \"agentId\": \"$AGENT_ID\",
    \"selectedGAgents\": [\"tools.math\", \"tools.timeconverter\"]
  }")
echo "Configure response:"
echo "$CONFIGURE_RESPONSE" | python3 -m json.tool
echo ""

# 5. Wait for tools to be registered
echo "5. Waiting for tools to be registered..."
sleep 5

# 6. Get agent info after configuration
echo "6. Getting agent info after configuration..."
AGENT_INFO=$(curl -s "http://localhost:5000/api/DynamicAIMCP/agent-info?agentId=$AGENT_ID")
echo "Agent info:"
echo "$AGENT_INFO" | python3 -m json.tool
echo ""

# 7. Test with a direct tool-requiring message
echo "7. Testing with direct math calculation..."
MATH_RESPONSE=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/chat-with-details \
  -H "Content-Type: application/json" \
  -d "{
    \"agentId\": \"$AGENT_ID\",
    \"message\": \"Calculate: 42 * 13\"
  }")
echo "Math response:"
echo "$MATH_RESPONSE" | python3 -m json.tool
echo ""

# 8. Test MCP tool
echo "8. Testing MCP tool (list /tmp directory)..."
MCP_TEST=$(curl -s -X POST http://localhost:5000/api/DynamicAIMCP/chat-with-details \
  -H "Content-Type: application/json" \
  -d "{
    \"agentId\": \"$AGENT_ID\",
    \"message\": \"List the files in /tmp directory\"
  }")
echo "MCP test response:"
echo "$MCP_TEST" | python3 -m json.tool | head -100
echo ""

echo "=== Test Complete ===" 