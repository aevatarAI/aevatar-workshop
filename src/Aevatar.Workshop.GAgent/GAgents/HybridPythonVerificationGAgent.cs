using Aevatar.Core.Abstractions;
using Aevatar.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.Workshop.GAgent.GAgents;

/// <summary>
/// Hybrid Python verification execution mode
/// </summary>
public enum PythonExecutionMode
{
    /// <summary>
    /// Use the original PythonVerificationGAgent
    /// </summary>
    Legacy,
    
    /// <summary>
    /// Use the new MCP-based Python agent
    /// </summary>
    MCP,
    
    /// <summary>
    /// Automatically choose based on availability and configuration
    /// </summary>
    Auto
}

/// <summary>
/// Configuration for hybrid Python verification
/// </summary>
[GenerateSerializer]
public class HybridPythonVerificationConfig
{
    [Id(0)] public PythonExecutionMode PreferredMode { get; set; } = PythonExecutionMode.Auto;
    [Id(1)] public bool EnableFallback { get; set; } = true;
    [Id(2)] public int MCPConnectionTimeoutMs { get; set; } = 5000;
    [Id(3)] public bool LogModeSelections { get; set; } = true;
    [Id(4)] public Dictionary<string, string> MCPServerConfig { get; set; } = new();
}

/// <summary>
/// Execution context for hybrid mode
/// </summary>
[GenerateSerializer]
public class HybridExecutionContext
{
    [Id(0)] public PythonExecutionMode SelectedMode { get; set; }
    [Id(1)] public string ExecutionId { get; set; } = string.Empty;
    [Id(2)] public DateTime StartTime { get; set; }
    [Id(3)] public string Reason { get; set; } = string.Empty;
    [Id(4)] public bool FallbackUsed { get; set; }
    [Id(5)] public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Hybrid Python verification state
/// </summary>
[GenerateSerializer]
public class HybridPythonVerificationState : StateBase
{
    [Id(0)] public bool LegacyAgentAvailable { get; set; }
    [Id(1)] public bool MCPAgentAvailable { get; set; }
    [Id(2)] public PythonExecutionMode CurrentMode { get; set; } = PythonExecutionMode.Auto;
    [Id(3)] public int LegacyExecutionCount { get; set; }
    [Id(4)] public int MCPExecutionCount { get; set; }
    [Id(5)] public int FallbackCount { get; set; }
    [Id(6)] public DateTime LastModeCheck { get; set; }
    [Id(7)] public HybridPythonVerificationConfig Config { get; set; } = new();
    [Id(8)] public List<HybridExecutionContext> RecentExecutions { get; set; } = new();
    [Id(9)] public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Hybrid Python verification state log events
/// </summary>
[GenerateSerializer]
public class HybridPythonVerificationStateLogEvent : StateLogEventBase<HybridPythonVerificationStateLogEvent> { }

[GenerateSerializer]
public class HybridModeSelectedLogEvent : HybridPythonVerificationStateLogEvent
{
    [Id(0)] public PythonExecutionMode SelectedMode { get; set; }
    [Id(1)] public string Reason { get; set; } = string.Empty;
    [Id(2)] public string ExecutionId { get; set; } = string.Empty;
    [Id(3)] public bool FallbackUsed { get; set; }
}

[GenerateSerializer]
public class HybridExecutionCompletedLogEvent : HybridPythonVerificationStateLogEvent
{
    [Id(0)] public PythonExecutionMode ExecutionMode { get; set; }
    [Id(1)] public bool Success { get; set; }
    [Id(2)] public double ExecutionTimeMs { get; set; }
    [Id(3)] public string ExecutionId { get; set; } = string.Empty;
}

/// <summary>
/// Interface for hybrid Python verification that combines legacy and MCP approaches
/// </summary>
public interface IHybridPythonVerificationGAgent : IStateGAgent<HybridPythonVerificationState>
{
    // Configuration
    Task<bool> InitializeAsync(HybridPythonVerificationConfig? config = null);
    Task<bool> SetExecutionModeAsync(PythonExecutionMode mode);
    Task<PythonExecutionMode> GetCurrentModeAsync();
    Task<bool> TestBothAgentsAsync();
    
    // Core verification methods (unified interface)
    Task<VerificationResult> VerifyTheoryAsync(string theoryId, string theoryContent, string? formalExpression = null);
    Task<string> GeneratePythonCodeAsync(string theoryContent, string? formalExpression = null);
    Task<VerificationResult> ExecutePythonCodeAsync(string theoryId, string pythonCode, List<TestCase> testCases);
    Task<List<TestCase>> GenerateTestCasesAsync(string theoryContent, string pythonCode);
    Task<bool> ValidatePythonCodeAsync(string pythonCode);
    
    // Execution methods with mode selection
    Task<ScriptExecutionResult> ExecutePythonScriptAsync(string script, PythonEnvironmentConfig? config = null);
    Task<MCPPythonExecutionResult> ExecuteViaMCPAsync(string code, MCPPythonExecutionConfig? config = null);
    
    // Statistics and monitoring
    Task<Dictionary<string, object>> GetHybridStatsAsync();
    Task<List<HybridExecutionContext>> GetRecentExecutionsAsync(int count = 10);
    Task<Dictionary<string, double>> GetPerformanceComparisonAsync();
}

/// <summary>
/// Hybrid Python verification agent that combines legacy PythonVerificationGAgent with new MCP-based approach
/// Provides backward compatibility while enabling gradual migration to MCP
/// </summary>
[GAgent("python.verification.hybrid", "reasoning")]
public class HybridPythonVerificationGAgent : GAgentBase<HybridPythonVerificationState, HybridPythonVerificationStateLogEvent>,
    IHybridPythonVerificationGAgent
{
    private IPythonVerificationGAgent? _legacyAgent;
    private IMCPPythonGAgent? _mcpAgent;
    private readonly IGAgentFactory _gAgentFactory;

    public HybridPythonVerificationGAgent()
    {
        _gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
    }

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Hybrid Python verification agent that combines legacy and MCP-based Python execution with automatic fallback");

    #region Initialization

    public async Task<bool> InitializeAsync(HybridPythonVerificationConfig? config = null)
    {
        try
        {
            Logger.LogInformation("Initializing HybridPythonVerificationGAgent...");

            if (config != null)
            {
                State.Config = config;
            }

            // Initialize both agents
            await InitializeLegacyAgentAsync();
            await InitializeMCPAgentAsync();

            // Test both agents to determine availability
            await TestBothAgentsAsync();

            // Select initial mode
            var selectedMode = await DetermineBestModeAsync("initialization");
            State.CurrentMode = selectedMode;

            Logger.LogInformation("HybridPythonVerificationGAgent initialized. Legacy: {LegacyAvailable}, MCP: {MCPAvailable}, Selected: {SelectedMode}",
                State.LegacyAgentAvailable, State.MCPAgentAvailable, selectedMode);

            return State.LegacyAgentAvailable || State.MCPAgentAvailable;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize HybridPythonVerificationGAgent");
            return false;
        }
    }

    private async Task InitializeLegacyAgentAsync()
    {
        try
        {
            _legacyAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>();
            var initResult = await _legacyAgent.InitializeAsync();
            State.LegacyAgentAvailable = initResult;
            
            if (initResult)
            {
                Logger.LogInformation("Legacy PythonVerificationGAgent initialized successfully");
            }
            else
            {
                Logger.LogWarning("Legacy PythonVerificationGAgent initialization failed");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to initialize legacy Python agent");
            State.LegacyAgentAvailable = false;
        }
    }

    private async Task InitializeMCPAgentAsync()
    {
        try
        {
            _mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPPythonGAgent>();
            var initResult = await _mcpAgent.InitializeAsync();
            State.MCPAgentAvailable = initResult;
            
            if (initResult)
            {
                Logger.LogInformation("MCP Python agent initialized successfully");
            }
            else
            {
                Logger.LogWarning("MCP Python agent initialization failed");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to initialize MCP Python agent");
            State.MCPAgentAvailable = false;
        }
    }

    #endregion

    #region Mode Management

    public async Task<bool> SetExecutionModeAsync(PythonExecutionMode mode)
    {
        if (mode == PythonExecutionMode.Legacy && !State.LegacyAgentAvailable)
        {
            Logger.LogWarning("Cannot set mode to Legacy - agent not available");
            return false;
        }

        if (mode == PythonExecutionMode.MCP && !State.MCPAgentAvailable)
        {
            Logger.LogWarning("Cannot set mode to MCP - agent not available");
            return false;
        }

        State.CurrentMode = mode;
        Logger.LogInformation("Execution mode set to: {Mode}", mode);
        return true;
    }

    public Task<PythonExecutionMode> GetCurrentModeAsync()
    {
        return Task.FromResult(State.CurrentMode);
    }

    public async Task<bool> TestBothAgentsAsync()
    {
        Logger.LogInformation("Testing both Python agents...");

        // Test legacy agent
        if (_legacyAgent != null)
        {
            try
            {
                var testCode = "print('Hello from legacy')";
                var result = await _legacyAgent.ValidatePythonCodeAsync(testCode);
                State.LegacyAgentAvailable = true;
                Logger.LogInformation("Legacy agent test: SUCCESS");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Legacy agent test failed");
                State.LegacyAgentAvailable = false;
            }
        }

        // Test MCP agent
        if (_mcpAgent != null)
        {
            try
            {
                var connectionTest = await _mcpAgent.TestMCPConnectionAsync();
                State.MCPAgentAvailable = connectionTest;
                Logger.LogInformation("MCP agent test: {Result}", connectionTest ? "SUCCESS" : "FAILED");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "MCP agent test failed");
                State.MCPAgentAvailable = false;
            }
        }

        State.LastModeCheck = DateTime.UtcNow;
        return State.LegacyAgentAvailable || State.MCPAgentAvailable;
    }

    private async Task<PythonExecutionMode> DetermineBestModeAsync(string context)
    {
        switch (State.Config.PreferredMode)
        {
            case PythonExecutionMode.Legacy:
                return State.LegacyAgentAvailable ? PythonExecutionMode.Legacy : 
                       (State.MCPAgentAvailable ? PythonExecutionMode.MCP : PythonExecutionMode.Legacy);

            case PythonExecutionMode.MCP:
                return State.MCPAgentAvailable ? PythonExecutionMode.MCP : 
                       (State.LegacyAgentAvailable ? PythonExecutionMode.Legacy : PythonExecutionMode.MCP);

            case PythonExecutionMode.Auto:
            default:
                // Auto selection logic - prefer MCP if available and performing well
                if (State.MCPAgentAvailable && State.LegacyAgentAvailable)
                {
                    // If both are available, use performance metrics to decide
                    if (State.PerformanceMetrics.ContainsKey("mcp_avg_time") && 
                        State.PerformanceMetrics.ContainsKey("legacy_avg_time"))
                    {
                        var mcpTime = State.PerformanceMetrics["mcp_avg_time"];
                        var legacyTime = State.PerformanceMetrics["legacy_avg_time"];
                        
                        // Prefer MCP if it's not significantly slower (within 20%)
                        return mcpTime <= legacyTime * 1.2 ? PythonExecutionMode.MCP : PythonExecutionMode.Legacy;
                    }
                    
                    // Default to MCP for new features
                    return PythonExecutionMode.MCP;
                }
                
                return State.MCPAgentAvailable ? PythonExecutionMode.MCP : PythonExecutionMode.Legacy;
        }
    }

    #endregion

    #region Core Verification Methods

    public async Task<VerificationResult> VerifyTheoryAsync(string theoryId, string theoryContent, string? formalExpression = null)
    {
        var context = await BeginExecutionAsync("VerifyTheory");
        
        try
        {
            VerificationResult result;
            
            if (context.SelectedMode == PythonExecutionMode.MCP && _mcpAgent != null)
            {
                // Convert MCP result to VerificationResult
                var mcpResult = await _mcpAgent.ExecuteCodeAsync($"# Theory verification for {theoryId}\n# {theoryContent}");
                result = ConvertMCPToVerificationResult(mcpResult, theoryId);
            }
            else if (_legacyAgent != null)
            {
                result = await _legacyAgent.VerifyTheoryAsync(theoryId, theoryContent, formalExpression);
                if (context.SelectedMode == PythonExecutionMode.MCP)
                {
                    context.FallbackUsed = true;
                    State.FallbackCount++;
                }
            }
            else
            {
                throw new InvalidOperationException("No Python verification agent available");
            }

            await CompleteExecutionAsync(context, true, 0);
            return result;
        }
        catch (Exception ex)
        {
            await CompleteExecutionAsync(context, false, 0);
            Logger.LogError(ex, "Error in VerifyTheoryAsync");
            throw;
        }
    }

    public async Task<string> GeneratePythonCodeAsync(string theoryContent, string? formalExpression = null)
    {
        var context = await BeginExecutionAsync("GeneratePythonCode");
        
        try
        {
            string result;
            
            if (context.SelectedMode == PythonExecutionMode.MCP && _mcpAgent != null)
            {
                // Use MCP agent to generate Python code
                var prompt = $"Generate Python code to implement this mathematical theory: {theoryContent}";
                if (!string.IsNullOrEmpty(formalExpression))
                {
                    prompt += $"\nFormal expression: {formalExpression}";
                }
                
                var mcpResult = await _mcpAgent.ExecuteCodeAsync($"# Code generation request\n# {prompt}");
                result = ExtractGeneratedCode(mcpResult.Output);
            }
            else if (_legacyAgent != null)
            {
                result = await _legacyAgent.GeneratePythonCodeAsync(theoryContent, formalExpression);
                if (context.SelectedMode == PythonExecutionMode.MCP)
                {
                    context.FallbackUsed = true;
                    State.FallbackCount++;
                }
            }
            else
            {
                throw new InvalidOperationException("No Python verification agent available");
            }

            await CompleteExecutionAsync(context, true, 0);
            return result;
        }
        catch (Exception ex)
        {
            await CompleteExecutionAsync(context, false, 0);
            Logger.LogError(ex, "Error in GeneratePythonCodeAsync");
            throw;
        }
    }

    public async Task<VerificationResult> ExecutePythonCodeAsync(string theoryId, string pythonCode, List<TestCase> testCases)
    {
        var context = await BeginExecutionAsync("ExecutePythonCode");
        
        try
        {
            VerificationResult result;
            
            if (context.SelectedMode == PythonExecutionMode.MCP && _mcpAgent != null)
            {
                var mcpResult = await _mcpAgent.ExecuteCodeAsync(pythonCode);
                result = ConvertMCPToVerificationResult(mcpResult, theoryId);
            }
            else if (_legacyAgent != null)
            {
                result = await _legacyAgent.ExecutePythonCodeAsync(theoryId, pythonCode, testCases);
                if (context.SelectedMode == PythonExecutionMode.MCP)
                {
                    context.FallbackUsed = true;
                    State.FallbackCount++;
                }
            }
            else
            {
                throw new InvalidOperationException("No Python verification agent available");
            }

            await CompleteExecutionAsync(context, result.TestsPassed, 0);
            return result;
        }
        catch (Exception ex)
        {
            await CompleteExecutionAsync(context, false, 0);
            Logger.LogError(ex, "Error in ExecutePythonCodeAsync");
            throw;
        }
    }

    public async Task<List<TestCase>> GenerateTestCasesAsync(string theoryContent, string pythonCode)
    {
        var context = await BeginExecutionAsync("GenerateTestCases");
        
        try
        {
            List<TestCase> result;
            
            if (context.SelectedMode == PythonExecutionMode.MCP && _mcpAgent != null)
            {
                // Generate test cases using MCP
                var prompt = $"Generate test cases for this theory and code:\nTheory: {theoryContent}\nCode:\n{pythonCode}";
                var mcpResult = await _mcpAgent.ExecuteCodeAsync($"# Test case generation\n# {prompt}");
                result = ParseTestCasesFromOutput(mcpResult.Output);
            }
            else if (_legacyAgent != null)
            {
                result = await _legacyAgent.GenerateTestCasesAsync(theoryContent, pythonCode);
                if (context.SelectedMode == PythonExecutionMode.MCP)
                {
                    context.FallbackUsed = true;
                    State.FallbackCount++;
                }
            }
            else
            {
                throw new InvalidOperationException("No Python verification agent available");
            }

            await CompleteExecutionAsync(context, true, 0);
            return result;
        }
        catch (Exception ex)
        {
            await CompleteExecutionAsync(context, false, 0);
            Logger.LogError(ex, "Error in GenerateTestCasesAsync");
            throw;
        }
    }

    public async Task<bool> ValidatePythonCodeAsync(string pythonCode)
    {
        var context = await BeginExecutionAsync("ValidatePythonCode");
        
        try
        {
            bool result;
            
            if (context.SelectedMode == PythonExecutionMode.MCP && _mcpAgent != null)
            {
                result = await _mcpAgent.ValidateCodeAsync(pythonCode);
            }
            else if (_legacyAgent != null)
            {
                result = await _legacyAgent.ValidatePythonCodeAsync(pythonCode);
                if (context.SelectedMode == PythonExecutionMode.MCP)
                {
                    context.FallbackUsed = true;
                    State.FallbackCount++;
                }
            }
            else
            {
                throw new InvalidOperationException("No Python verification agent available");
            }

            await CompleteExecutionAsync(context, result, 0);
            return result;
        }
        catch (Exception ex)
        {
            await CompleteExecutionAsync(context, false, 0);
            Logger.LogError(ex, "Error in ValidatePythonCodeAsync");
            throw;
        }
    }

    #endregion

    #region Direct Execution Methods

    public async Task<ScriptExecutionResult> ExecutePythonScriptAsync(string script, PythonEnvironmentConfig? config = null)
    {
        var context = await BeginExecutionAsync("ExecutePythonScript");
        
        try
        {
            ScriptExecutionResult result;
            
            if (context.SelectedMode == PythonExecutionMode.Legacy && _legacyAgent != null)
            {
                result = await _legacyAgent.ExecutePythonScriptAsync(script, config);
            }
            else if (context.SelectedMode == PythonExecutionMode.MCP && _mcpAgent != null)
            {
                var mcpConfig = ConvertToMCPConfig(config);
                var mcpResult = await _mcpAgent.ExecuteCodeAsync(script, mcpConfig);
                result = ConvertMCPToScriptResult(mcpResult);
            }
            else
            {
                throw new InvalidOperationException("No Python execution agent available for current mode");
            }

            await CompleteExecutionAsync(context, result.Success, result.ExecutionTimeSeconds * 1000);
            return result;
        }
        catch (Exception ex)
        {
            await CompleteExecutionAsync(context, false, 0);
            Logger.LogError(ex, "Error in ExecutePythonScriptAsync");
            throw;
        }
    }

    public async Task<MCPPythonExecutionResult> ExecuteViaMCPAsync(string code, MCPPythonExecutionConfig? config = null)
    {
        if (_mcpAgent == null)
        {
            throw new InvalidOperationException("MCP Python agent not available");
        }

        var context = await BeginExecutionAsync("ExecuteViaMCP");
        context.SelectedMode = PythonExecutionMode.MCP;
        
        try
        {
            var result = await _mcpAgent.ExecuteCodeAsync(code, config);
            await CompleteExecutionAsync(context, result.Success, result.ExecutionTimeMs);
            return result;
        }
        catch (Exception ex)
        {
            await CompleteExecutionAsync(context, false, 0);
            Logger.LogError(ex, "Error in ExecuteViaMCPAsync");
            throw;
        }
    }

    #endregion

    #region Statistics and Monitoring

    public async Task<Dictionary<string, object>> GetHybridStatsAsync()
    {
        var stats = new Dictionary<string, object>
        {
            ["LegacyAgentAvailable"] = State.LegacyAgentAvailable,
            ["MCPAgentAvailable"] = State.MCPAgentAvailable,
            ["CurrentMode"] = State.CurrentMode.ToString(),
            ["LegacyExecutionCount"] = State.LegacyExecutionCount,
            ["MCPExecutionCount"] = State.MCPExecutionCount,
            ["FallbackCount"] = State.FallbackCount,
            ["LastModeCheck"] = State.LastModeCheck,
            ["RecentExecutionsCount"] = State.RecentExecutions.Count,
            ["PerformanceMetrics"] = State.PerformanceMetrics
        };

        // Add individual agent stats if available
        if (_legacyAgent != null && State.LegacyAgentAvailable)
        {
            try
            {
                var legacyStats = await _legacyAgent.GetVerificationStatsAsync();
                stats["LegacyAgentStats"] = legacyStats;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to get legacy agent stats");
            }
        }

        if (_mcpAgent != null && State.MCPAgentAvailable)
        {
            try
            {
                var mcpStats = await _mcpAgent.GetExecutionStatsAsync();
                stats["MCPAgentStats"] = mcpStats;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to get MCP agent stats");
            }
        }

        return stats;
    }

    public Task<List<HybridExecutionContext>> GetRecentExecutionsAsync(int count = 10)
    {
        var recent = State.RecentExecutions
            .OrderByDescending(e => e.StartTime)
            .Take(count)
            .ToList();
        
        return Task.FromResult(recent);
    }

    public Task<Dictionary<string, double>> GetPerformanceComparisonAsync()
    {
        return Task.FromResult(new Dictionary<string, double>(State.PerformanceMetrics));
    }

    #endregion

    #region Helper Methods

    private async Task<HybridExecutionContext> BeginExecutionAsync(string operation)
    {
        var executionId = Guid.NewGuid().ToString();
        var selectedMode = await DetermineBestModeAsync(operation);
        
        var context = new HybridExecutionContext
        {
            SelectedMode = selectedMode,
            ExecutionId = executionId,
            StartTime = DateTime.UtcNow,
            Reason = $"Auto-selected for {operation}"
        };

        if (State.Config.LogModeSelections)
        {
            Logger.LogInformation("Selected mode {Mode} for operation {Operation} (ID: {ExecutionId})", 
                selectedMode, operation, executionId);
        }

        RaiseEvent(new HybridModeSelectedLogEvent
        {
            SelectedMode = selectedMode,
            Reason = context.Reason,
            ExecutionId = executionId,
            FallbackUsed = false
        });

        return context;
    }

    private async Task CompleteExecutionAsync(HybridExecutionContext context, bool success, double executionTimeMs)
    {
        context.Metadata["Success"] = success;
        context.Metadata["ExecutionTimeMs"] = executionTimeMs;

        // Update execution counts
        if (context.SelectedMode == PythonExecutionMode.Legacy)
        {
            State.LegacyExecutionCount++;
        }
        else if (context.SelectedMode == PythonExecutionMode.MCP)
        {
            State.MCPExecutionCount++;
        }

        // Update performance metrics
        var modeKey = context.SelectedMode.ToString().ToLowerInvariant();
        var avgTimeKey = $"{modeKey}_avg_time";
        var countKey = $"{modeKey}_count";

        if (State.PerformanceMetrics.ContainsKey(avgTimeKey) && State.PerformanceMetrics.ContainsKey(countKey))
        {
            var currentAvg = State.PerformanceMetrics[avgTimeKey];
            var currentCount = State.PerformanceMetrics[countKey];
            var newAvg = (currentAvg * currentCount + executionTimeMs) / (currentCount + 1);
            
            State.PerformanceMetrics[avgTimeKey] = newAvg;
            State.PerformanceMetrics[countKey] = currentCount + 1;
        }
        else
        {
            State.PerformanceMetrics[avgTimeKey] = executionTimeMs;
            State.PerformanceMetrics[countKey] = 1;
        }

        // Add to recent executions (keep last 50)
        State.RecentExecutions.Add(context);
        if (State.RecentExecutions.Count > 50)
        {
            State.RecentExecutions.RemoveAt(0);
        }

        RaiseEvent(new HybridExecutionCompletedLogEvent
        {
            ExecutionMode = context.SelectedMode,
            Success = success,
            ExecutionTimeMs = executionTimeMs,
            ExecutionId = context.ExecutionId
        });

        await ConfirmEvents();
    }

    // Conversion helpers
    private VerificationResult ConvertMCPToVerificationResult(MCPPythonExecutionResult mcpResult, string theoryId)
    {
        return new VerificationResult
        {
            TheoryId = theoryId,
            TestsPassed = mcpResult.Success,
            ErrorOutput = mcpResult.ErrorOutput,
            StandardOutput = mcpResult.Output,
            ExecutionTime = mcpResult.ExecutionTimeMs,
            CreatedAt = DateTime.UtcNow,
            TestResults = "MCP execution result", // Simple string result
            TotalTests = 1,
            PassedTests = mcpResult.Success ? 1 : 0
        };
    }

    private ScriptExecutionResult ConvertMCPToScriptResult(MCPPythonExecutionResult mcpResult)
    {
        return new ScriptExecutionResult
        {
            Success = mcpResult.Success,
            StandardOutput = mcpResult.Output,
            ErrorOutput = mcpResult.ErrorOutput,
            ExitCode = mcpResult.ExitCode,
            ExecutionTimeSeconds = mcpResult.ExecutionTimeMs / 1000.0,
            MemoryUsedMB = mcpResult.MemoryUsedMB,
            TimedOut = mcpResult.TimedOut,
            StartTime = mcpResult.StartTime,
            EndTime = mcpResult.EndTime
        };
    }

    private MCPPythonExecutionConfig ConvertToMCPConfig(PythonEnvironmentConfig? config)
    {
        if (config == null)
            return new MCPPythonExecutionConfig();

        return new MCPPythonExecutionConfig
        {
            TimeoutSeconds = config.MaxExecutionTimeSeconds,
            MemoryLimitMB = (int)config.MaxMemoryMB,
            EnvironmentName = config.EnvironmentName,
            EnvironmentVariables = config.EnvironmentVariables,
            EnableNetworkAccess = config.EnableNetworkAccess,
            WorkingDirectory = config.WorkingDirectory,
            EnableSandbox = config.IsSandboxed
        };
    }

    private string ExtractGeneratedCode(string output)
    {
        // Simple extraction - in production would be more sophisticated
        if (output.Contains("```python"))
        {
            var start = output.IndexOf("```python") + 9;
            var end = output.IndexOf("```", start);
            if (end > start)
            {
                return output.Substring(start, end - start).Trim();
            }
        }
        return output;
    }

    private List<TestCase> ParseTestCasesFromOutput(string output)
    {
        // Simple parsing - would be more sophisticated in production
        return new List<TestCase>();
    }

    #endregion

    #region State Management

    protected override void GAgentTransitionState(HybridPythonVerificationState state, StateLogEventBase<HybridPythonVerificationStateLogEvent> @event)
    {
        switch (@event)
        {
            case HybridModeSelectedLogEvent modeEvent:
                // Mode selection is already handled in BeginExecutionAsync
                break;

            case HybridExecutionCompletedLogEvent execEvent:
                // Execution completion is already handled in CompleteExecutionAsync
                break;
        }
    }

    #endregion
}