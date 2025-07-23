# PowerShell script to run Configuration Sync Tests

Write-Host "🌌 Running Configuration Sync Tests..." -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

# Function to run tests with formatting
function Run-Test {
    param(
        [string]$TestName,
        [string]$Filter
    )
    
    Write-Host "`n Running: $TestName" -ForegroundColor Yellow
    Write-Host "----------------------------------------"
    
    $result = dotnet test Aevatar.Workshop.Tests --filter $Filter --logger "console;verbosity=normal"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ $TestName passed!" -ForegroundColor Green
    } else {
        Write-Host "❌ $TestName failed!" -ForegroundColor Red
        exit 1
    }
}

# Navigate to test directory
Set-Location $PSScriptRoot

# Run different test suites
Write-Host "`n1. Testing ConfigManagerGAgent basics..." -ForegroundColor Yellow
Run-Test "ConfigManagerGAgent Tests" "FullyQualifiedName~ConfigManagerGAgentTests"

Write-Host "`n2. Testing Configuration Sync Integration..." -ForegroundColor Yellow
Run-Test "Config Sync Integration Tests" "FullyQualifiedName~ConfigSyncIntegrationTests"

Write-Host "`n3. Running all Config Sync tests with detailed output..." -ForegroundColor Yellow
dotnet test Aevatar.Workshop.Tests `
    --filter "FullyQualifiedName~ConfigManagerGAgentTests|ConfigSyncIntegrationTests" `
    --logger "console;verbosity=detailed" `
    --logger "html;LogFileName=config-sync-test-results.html"

Write-Host "`n🎉 All Configuration Sync tests completed!" -ForegroundColor Green
Write-Host "Test results saved to: TestResults\config-sync-test-results.html" -ForegroundColor Cyan 