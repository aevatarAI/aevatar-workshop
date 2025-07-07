Write-Host "Testing Dynamic AI with GAgent Tools..." -ForegroundColor Green

# Step 1: Initialize agent
Write-Host "`nStep 1: Initializing agent..." -ForegroundColor Yellow
$initResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/dynamicaimcp/initialize" -Method POST -ContentType "application/json" -Body '{"systemLLM":"DeepSeek"}'
$initResult = $initResponse.Content | ConvertFrom-Json
$agentId = $initResult.agentId
Write-Host "Agent created: $agentId" -ForegroundColor Cyan

# Step 2: Configure GAgent tools
Write-Host "`nStep 2: Configuring GAgent tools..." -ForegroundColor Yellow
$configBody = @{
    agentId = $agentId
    selectedGAgents = @("mathgagent/math", "timeconvertergagent/timeconverter")
} | ConvertTo-Json
$configResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/dynamicaimcp/configure-gagent-tools" -Method POST -ContentType "application/json" -Body $configBody
$configResult = $configResponse.Content | ConvertFrom-Json
Write-Host "Configuration result: $($configResult.message)" -ForegroundColor Cyan

# Step 3: Get agent info
Write-Host "`nStep 3: Getting agent info..." -ForegroundColor Yellow
$infoResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/dynamicaimcp/agent-info?agentId=$agentId" -Method GET
$infoResult = $infoResponse.Content | ConvertFrom-Json
Write-Host "Total tools available: $($infoResult.totalTools)" -ForegroundColor Cyan
Write-Host "GAgent functions: $($infoResult.registeredGAgentFunctions -join ', ')" -ForegroundColor Cyan

# Step 4: Test with math calculation
Write-Host "`nStep 4: Testing math calculation..." -ForegroundColor Yellow
$mathBody = @{
    agentId = $agentId
    message = "Please use your math tool to calculate the square root of 144"
} | ConvertTo-Json
$mathResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/dynamicaimcp/chat-with-details" -Method POST -ContentType "application/json" -Body $mathBody
$mathResult = $mathResponse.Content | ConvertFrom-Json
Write-Host "Response: $($mathResult.response)" -ForegroundColor Green
if ($mathResult.toolCalls.Count -gt 0) {
    Write-Host "Tool calls made:" -ForegroundColor Yellow
    $mathResult.toolCalls | ForEach-Object {
        Write-Host "  - Tool: $($_.toolName) (Duration: $($_.durationMs)ms)" -ForegroundColor Cyan
    }
}

# Step 5: Test with time conversion
Write-Host "`nStep 5: Testing time conversion..." -ForegroundColor Yellow
$timeBody = @{
    agentId = $agentId
    message = "Please use your time converter tool to convert 2025-01-07T15:30:00Z to Tokyo time"
} | ConvertTo-Json
$timeResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/dynamicaimcp/chat-with-details" -Method POST -ContentType "application/json" -Body $timeBody
$timeResult = $timeResponse.Content | ConvertFrom-Json
Write-Host "Response: $($timeResult.response)" -ForegroundColor Green
if ($timeResult.toolCalls.Count -gt 0) {
    Write-Host "Tool calls made:" -ForegroundColor Yellow
    $timeResult.toolCalls | ForEach-Object {
        Write-Host "  - Tool: $($_.toolName) (Duration: $($_.durationMs)ms)" -ForegroundColor Cyan
    }
}

Write-Host "`nTest completed!" -ForegroundColor Green 