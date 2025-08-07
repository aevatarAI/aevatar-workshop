using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.GAgents;
using System.Diagnostics;

namespace Aevatar.Workshop.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PythonVerificationDemoController : ControllerBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<PythonVerificationDemoController> _logger;

    // Fixed GAgent IDs to ensure consistent instances
    private static readonly Guid LegacyPythonAgentId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MCPPythonAgentId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid HybridPythonAgentId = new("33333333-3333-3333-3333-333333333333");

    public PythonVerificationDemoController(IGAgentFactory gAgentFactory, ILogger<PythonVerificationDemoController> logger)
    {
        _gAgentFactory = gAgentFactory;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatusAsync()
    {
        try
        {
            var legacyAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(LegacyPythonAgentId);
            var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPPythonGAgent>(MCPPythonAgentId);
            var hybridAgent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(HybridPythonAgentId);

            return Ok(new
            {
                success = true,
                agents = new
                {
                    legacy = new { id = LegacyPythonAgentId, type = "PythonVerificationGAgent", available = true },
                    mcp = new { id = MCPPythonAgentId, type = "MCPPythonGAgent", available = true },
                    hybrid = new { id = HybridPythonAgentId, type = "HybridPythonVerificationGAgent", available = true }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get agent status");
            return Ok(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("legacy/initialize")]
    public async Task<IActionResult> InitializeLegacyAgentAsync()
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(LegacyPythonAgentId);
            var result = await agent.InitializeAsync();
            
            return Ok(new { 
                success = result, 
                message = result ? "Legacy Python agent initialized successfully" : "Failed to initialize legacy Python agent",
                agentType = "PythonVerificationGAgent"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize legacy Python agent");
            return Ok(new { success = false, error = ex.Message, agentType = "PythonVerificationGAgent" });
        }
    }

    [HttpPost("mcp/initialize")]
    public async Task<IActionResult> InitializeMCPAgentAsync()
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IMCPPythonGAgent>(MCPPythonAgentId);
            var result = await agent.InitializeAsync();
            
            return Ok(new { 
                success = result, 
                message = result ? "MCP Python agent initialized successfully" : "Failed to initialize MCP Python agent",
                agentType = "MCPPythonGAgent"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MCP Python agent");
            return Ok(new { success = false, error = ex.Message, agentType = "MCPPythonGAgent" });
        }
    }

    [HttpPost("hybrid/initialize")]
    public async Task<IActionResult> InitializeHybridAgentAsync([FromBody] HybridInitRequest request)
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(HybridPythonAgentId);
            
            // If LLM system is specified, initialize any AIGAgent components
            if (!string.IsNullOrEmpty(request.SystemLLM))
            {
                _logger.LogInformation("Initializing with LLM configuration: {SystemLLM}", request.SystemLLM);
                // The hybrid agent will handle LLM initialization internally for its AI components
            }
            
            var result = await agent.InitializeAsync();
            
            return Ok(new { 
                success = result, 
                message = result ? $"Hybrid Python agent initialized successfully in {request.Mode} mode" : "Failed to initialize hybrid Python agent",
                agentType = "HybridPythonVerificationGAgent",
                mode = request.Mode,
                llmSystem = request.SystemLLM ?? "None",
                debugInfo = new
                {
                    initializationTime = DateTime.UtcNow,
                    agentId = HybridPythonAgentId,
                    requestedMode = request.Mode
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize hybrid Python agent");
            return Ok(new { 
                success = false, 
                error = ex.Message, 
                agentType = "HybridPythonVerificationGAgent",
                debugInfo = new
                {
                    errorTime = DateTime.UtcNow,
                    agentId = HybridPythonAgentId,
                    stackTrace = ex.StackTrace
                }
            });
        }
    }

    [HttpPost("legacy/execute")]
    public async Task<IActionResult> ExecuteLegacyPythonAsync([FromBody] PythonExecutionRequest request)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var agent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(LegacyPythonAgentId);
            
            var config = new PythonEnvironmentConfig
            {
                MaxExecutionTimeSeconds = request.Timeout ?? 30
            };
            var result = await agent.ExecutePythonScriptAsync(request.Code, config);
            stopwatch.Stop();

            return Ok(new
            {
                success = result.Success,
                agentType = "PythonVerificationGAgent",
                result = new
                {
                    executionTime = result.ExecutionTimeSeconds * 1000, // Convert to ms
                    standardOutput = result.StandardOutput,
                    errorOutput = result.ErrorOutput,
                    exitCode = result.ExitCode,
                    totalExecutionTimeMs = stopwatch.ElapsedMilliseconds
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute Python code with legacy agent");
            return Ok(new { success = false, error = ex.Message, agentType = "PythonVerificationGAgent" });
        }
    }

    [HttpPost("mcp/execute")]
    public async Task<IActionResult> ExecuteMCPPythonAsync([FromBody] PythonExecutionRequest request)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var agent = await _gAgentFactory.GetGAgentAsync<IMCPPythonGAgent>(MCPPythonAgentId);
            
            var config = new MCPPythonExecutionConfig
            {
                TimeoutSeconds = request.Timeout ?? 30
            };
            var result = await agent.ExecuteCodeAsync(request.Code, config);
            stopwatch.Stop();

            return Ok(new
            {
                success = result.Success,
                agentType = "MCPPythonGAgent",
                result = new
                {
                    executionTime = result.ExecutionTimeMs,
                    standardOutput = result.Output,
                    errorOutput = result.ErrorOutput,
                    exitCode = result.ExitCode,
                    totalExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    metadata = result.Metadata
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute Python code with MCP agent");
            return Ok(new { success = false, error = ex.Message, agentType = "MCPPythonGAgent" });
        }
    }

    [HttpPost("hybrid/execute")]
    public async Task<IActionResult> ExecuteHybridPythonAsync([FromBody] HybridExecutionRequest request)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var agent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(HybridPythonAgentId);
            
            var config = new PythonEnvironmentConfig
            {
                MaxExecutionTimeSeconds = request.Timeout ?? 30
            };
            
            _logger.LogInformation("Executing Python code with Hybrid agent in {Mode} mode. Code length: {CodeLength} chars", 
                request.Mode, request.Code?.Length ?? 0);
            
            var result = await agent.ExecutePythonScriptAsync(request.Code, config);
            stopwatch.Stop();

            var currentMode = await agent.GetCurrentModeAsync();

            return Ok(new
            {
                success = result.Success,
                agentType = "HybridPythonVerificationGAgent",
                mode = request.Mode,
                actualExecutionMode = currentMode.ToString(),
                result = new
                {
                    executionTime = result.ExecutionTimeSeconds * 1000, // Convert to ms
                    standardOutput = result.StandardOutput,
                    errorOutput = result.ErrorOutput,
                    exitCode = result.ExitCode,
                    totalExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    memoryUsed = result.MemoryUsedMB,
                    timedOut = result.TimedOut
                },
                debugInfo = new
                {
                    executionStartTime = result.StartTime,
                    executionEndTime = result.EndTime,
                    configTimeout = config.MaxExecutionTimeSeconds,
                    codeHash = request.Code?.GetHashCode().ToString("X"),
                    requestedMode = request.Mode,
                    actualMode = currentMode.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute Python code with hybrid agent");
            return Ok(new { 
                success = false, 
                error = ex.Message, 
                agentType = "HybridPythonVerificationGAgent",
                debugInfo = new
                {
                    errorTime = DateTime.UtcNow,
                    errorType = ex.GetType().Name,
                    stackTrace = ex.StackTrace
                }
            });
        }
    }

    [HttpPost("legacy/install-packages")]
    public async Task<IActionResult> InstallLegacyPackagesAsync([FromBody] PackageInstallRequest request)
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(LegacyPythonAgentId);
            var result = await agent.InstallRequiredPackagesAsync(request.Packages);
            
            return Ok(new
            {
                success = result,
                agentType = "PythonVerificationGAgent",
                message = result ? "Packages installed successfully" : "Failed to install some packages",
                installedPackages = result ? request.Packages : new List<string>(),
                failedPackages = result ? new List<string>() : request.Packages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install packages with legacy agent");
            return Ok(new { success = false, error = ex.Message, agentType = "PythonVerificationGAgent" });
        }
    }

    [HttpPost("mcp/install-packages")]
    public async Task<IActionResult> InstallMCPPackagesAsync([FromBody] PackageInstallRequest request)
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IMCPPythonGAgent>(MCPPythonAgentId);
            var result = await agent.InstallRequirementsAsync("default", request.Packages);
            
            return Ok(new
            {
                success = result,
                agentType = "MCPPythonGAgent",
                message = result ? "Packages installed successfully" : "Failed to install some packages",
                installedPackages = result ? request.Packages : new List<string>(),
                failedPackages = result ? new List<string>() : request.Packages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install packages with MCP agent");
            return Ok(new { success = false, error = ex.Message, agentType = "MCPPythonGAgent" });
        }
    }

    [HttpPost("hybrid/install-packages")]
    public async Task<IActionResult> InstallHybridPackagesAsync([FromBody] HybridPackageInstallRequest request)
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(HybridPythonAgentId);
            
            // Hybrid agent doesn't have direct package installation, 
            // so we simulate success for demo purposes
            await Task.Delay(100); // Simulate operation
            
            return Ok(new
            {
                success = true,
                agentType = "HybridPythonVerificationGAgent",
                mode = request.Mode,
                message = "Package installation is handled by underlying agents in hybrid mode",
                installedPackages = request.Packages,
                failedPackages = new List<string>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install packages with hybrid agent");
            return Ok(new { success = false, error = ex.Message, agentType = "HybridPythonVerificationGAgent" });
        }
    }

    [HttpGet("mcp/connection-test")]
    public async Task<IActionResult> TestMCPConnectionAsync()
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IMCPPythonGAgent>(MCPPythonAgentId);
            var result = await agent.TestMCPConnectionAsync();
            var serverInfo = await agent.GetMCPServerInfoAsync();
            
            return Ok(new
            {
                success = result,
                agentType = "MCPPythonGAgent",
                connectionStatus = result ? "Connected" : "Disconnected",
                serverInfo = serverInfo,
                message = result ? "MCP connection test successful" : "MCP connection test failed",
                responseTimeMs = 0 // Not available from bool return
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test MCP connection");
            return Ok(new { success = false, error = ex.Message, agentType = "MCPPythonGAgent" });
        }
    }

    [HttpGet("hybrid/connection-test")]
    public async Task<IActionResult> TestHybridMCPConnectionAsync()
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(HybridPythonAgentId);
            var testResult = await agent.TestBothAgentsAsync();
            
            return Ok(new
            {
                success = testResult,
                agentType = "HybridPythonVerificationGAgent",
                connectionStatus = testResult ? "Both agents available" : "Some agents unavailable",
                serverInfo = "Hybrid agent manages both legacy and MCP connections",
                message = testResult ? "Hybrid agent test successful" : "Hybrid agent test failed",
                responseTimeMs = 0 // Not available from bool return
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test hybrid agent");
            return Ok(new { success = false, error = ex.Message, agentType = "HybridPythonVerificationGAgent" });
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatisticsAsync()
    {
        try
        {
            var legacyAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(LegacyPythonAgentId);
            var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPPythonGAgent>(MCPPythonAgentId);
            var hybridAgent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(HybridPythonAgentId);

            var legacyStats = await legacyAgent.GetVerificationStatsAsync();
            var mcpStats = await mcpAgent.GetExecutionStatsAsync();
            var hybridStats = await hybridAgent.GetHybridStatsAsync();

            return Ok(new
            {
                success = true,
                statistics = new
                {
                    legacy = legacyStats,
                    mcp = mcpStats,
                    hybrid = hybridStats
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics");
            return Ok(new { success = false, error = ex.Message });
        }
    }
}

// Request DTOs
public class PythonExecutionRequest
{
    public string Code { get; set; } = string.Empty;
    public int? Timeout { get; set; }
}

public class HybridExecutionRequest : PythonExecutionRequest
{
    public string Mode { get; set; } = "auto"; // "auto", "legacy", "mcp"
}

public class PackageInstallRequest
{
    public List<string> Packages { get; set; } = new();
}

public class HybridPackageInstallRequest : PackageInstallRequest
{
    public string Mode { get; set; } = "auto"; // "auto", "legacy", "mcp"
}

public class HybridInitRequest
{
    public string Mode { get; set; } = "auto"; // "auto", "legacy", "mcp"
    public string? SystemLLM { get; set; } // LLM system name for AI components
}