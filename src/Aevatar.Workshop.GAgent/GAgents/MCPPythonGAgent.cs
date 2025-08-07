using Aevatar.Core.Abstractions;
using Aevatar.Core;
using Aevatar.GAgents.AIGAgent.State;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.Workshop.GAgent.GAgents;

/// <summary>
/// Python execution configuration for MCP
/// </summary>
[GenerateSerializer]
public class MCPPythonExecutionConfig
{
    [Id(0)] public int TimeoutSeconds { get; set; } = 30;
    [Id(1)] public int MemoryLimitMB { get; set; } = 512;
    [Id(2)] public string EnvironmentName { get; set; } = "default";
    [Id(3)] public List<string> RequiredPackages { get; set; } = new();
    [Id(4)] public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    [Id(5)] public bool EnableNetworkAccess { get; set; } = false;
    [Id(6)] public bool EnableFileSystemAccess { get; set; } = false;
    [Id(7)] public string WorkingDirectory { get; set; } = "/tmp";
    [Id(8)] public bool EnableSandbox { get; set; } = true;
}

/// <summary>
/// Python execution result from MCP server
/// </summary>
[GenerateSerializer]
public class MCPPythonExecutionResult
{
    [Id(0)] public bool Success { get; set; }
    [Id(1)] public string Output { get; set; } = string.Empty;
    [Id(2)] public string ErrorOutput { get; set; } = string.Empty;
    [Id(3)] public int ExitCode { get; set; }
    [Id(4)] public double ExecutionTimeMs { get; set; }
    [Id(5)] public long MemoryUsedMB { get; set; }
    [Id(6)] public string ExecutionId { get; set; } = string.Empty;
    [Id(7)] public DateTime StartTime { get; set; }
    [Id(8)] public DateTime EndTime { get; set; }
    [Id(9)] public Dictionary<string, object> Metadata { get; set; } = new();
    [Id(10)] public string ErrorType { get; set; } = string.Empty;
    [Id(11)] public bool TimedOut { get; set; }
}

/// <summary>
/// Environment management result
/// </summary>
[GenerateSerializer]
public class MCPEnvironmentResult
{
    [Id(0)] public bool Success { get; set; }
    [Id(1)] public string EnvironmentName { get; set; } = string.Empty;
    [Id(2)] public string Message { get; set; } = string.Empty;
    [Id(3)] public List<string> InstalledPackages { get; set; } = new();
    [Id(4)] public string PythonVersion { get; set; } = string.Empty;
    [Id(5)] public Dictionary<string, string> EnvironmentInfo { get; set; } = new();
}

/// <summary>
/// Package installation result
/// </summary>
[GenerateSerializer]
public class MCPPackageInstallResult
{
    [Id(0)] public bool Success { get; set; }
    [Id(1)] public string PackageName { get; set; } = string.Empty;
    [Id(2)] public string InstalledVersion { get; set; } = string.Empty;
    [Id(3)] public string Message { get; set; } = string.Empty;
    [Id(4)] public List<string> Dependencies { get; set; } = new();
    [Id(5)] public double InstallTimeMs { get; set; }
}

/// <summary>
/// MCP Python agent state
/// </summary>
[GenerateSerializer]
public class MCPPythonState : AIGAgentStateBase
{
    [Id(0)] public bool IsInitialized { get; set; }
    [Id(1)] public List<string> AvailableEnvironments { get; set; } = new();
    [Id(2)] public string DefaultEnvironment { get; set; } = "default";
    [Id(3)] public Dictionary<string, MCPPythonExecutionConfig> EnvironmentConfigs { get; set; } = new();
    [Id(4)] public List<string> ActiveExecutions { get; set; } = new();
    [Id(5)] public int TotalExecutions { get; set; }
    [Id(6)] public int SuccessfulExecutions { get; set; }
    [Id(7)] public int FailedExecutions { get; set; }
    [Id(8)] public DateTime LastExecutionTime { get; set; }
    [Id(9)] public Dictionary<string, List<string>> EnvironmentPackages { get; set; } = new();
    [Id(10)] public bool MCPServerConnected { get; set; }
    [Id(11)] public string MCPServerVersion { get; set; } = string.Empty;
    [Id(12)] public DateTime InitializedAt { get; set; }
}

/// <summary>
/// MCP Python state log events
/// </summary>
[GenerateSerializer]
public class MCPPythonStateLogEvent : StateLogEventBase<MCPPythonStateLogEvent> { }

[GenerateSerializer]
public class MCPPythonInitializedLogEvent : MCPPythonStateLogEvent
{
    [Id(0)] public string MCPServerVersion { get; set; } = string.Empty;
    [Id(1)] public DateTime InitializedAt { get; set; }
}

[GenerateSerializer]
public class MCPPythonExecutionLogEvent : MCPPythonStateLogEvent
{
    [Id(0)] public string ExecutionId { get; set; } = string.Empty;
    [Id(1)] public bool Success { get; set; }
    [Id(2)] public double ExecutionTimeMs { get; set; }
    [Id(3)] public string EnvironmentName { get; set; } = string.Empty;
    [Id(4)] public int CodeLength { get; set; }
}

[GenerateSerializer]
public class MCPEnvironmentCreatedLogEvent : MCPPythonStateLogEvent
{
    [Id(0)] public string EnvironmentName { get; set; } = string.Empty;
    [Id(1)] public string PythonVersion { get; set; } = string.Empty;
    [Id(2)] public List<string> InitialPackages { get; set; } = new();
}

[GenerateSerializer]
public class MCPPackageInstalledLogEvent : MCPPythonStateLogEvent
{
    [Id(0)] public string EnvironmentName { get; set; } = string.Empty;
    [Id(1)] public string PackageName { get; set; } = string.Empty;
    [Id(2)] public string Version { get; set; } = string.Empty;
    [Id(3)] public double InstallTimeMs { get; set; }
}

/// <summary>
/// Interface for MCP Python agent
/// </summary>
public interface IMCPPythonGAgent : IStateGAgent<MCPPythonState>
{
    // Core execution methods
    Task<bool> InitializeAsync();
    Task<MCPPythonExecutionResult> ExecuteCodeAsync(string code, MCPPythonExecutionConfig? config = null);
    Task<MCPPythonExecutionResult> ExecuteScriptAsync(string scriptPath, MCPPythonExecutionConfig? config = null);
    
    // Environment management
    Task<MCPEnvironmentResult> CreateEnvironmentAsync(string environmentName, string pythonVersion = "3.9");
    Task<bool> DeleteEnvironmentAsync(string environmentName);
    Task<List<string>> ListEnvironmentsAsync();
    Task<MCPEnvironmentResult> GetEnvironmentInfoAsync(string environmentName);
    Task<bool> SetDefaultEnvironmentAsync(string environmentName);
    
    // Package management
    Task<MCPPackageInstallResult> InstallPackageAsync(string environmentName, string packageName, string? version = null);
    Task<bool> UninstallPackageAsync(string environmentName, string packageName);
    Task<List<string>> ListPackagesAsync(string environmentName);
    Task<bool> InstallRequirementsAsync(string environmentName, List<string> requirements);
    
    // Utility methods
    Task<bool> ValidateCodeAsync(string code);
    Task<Dictionary<string, object>> GetExecutionStatsAsync();
    Task<bool> TestMCPConnectionAsync();
    Task<string> GetMCPServerInfoAsync();
}

/// <summary>
/// MCP-based Python execution agent using Model Context Protocol
/// </summary>
[GAgent("mcp.python", "reasoning")]
public class MCPPythonGAgent : WorkshopAIGAgentBase<MCPPythonState, MCPPythonStateLogEvent>, IMCPPythonGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("MCP-based Python execution agent that uses Model Context Protocol for secure Python code execution");

    #region Initialization

    public async Task<bool> InitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing MCPPythonGAgent...");

            await InitializeAsync(new InitializeDto
            {
                LLMConfig = new LLMConfigDto
                {
                    SystemLLM = "AzureOpenAI"
                }
            });

            // Test MCP connection first
            if (!await TestMCPConnectionAsync())
            {
                Logger.LogError("Failed to connect to MCP Python server");
                return false;
            }

            // Get MCP server info
            var serverInfo = await GetMCPServerInfoAsync();
            
            // Initialize default environment
            var defaultEnvResult = await CreateEnvironmentAsync(State.DefaultEnvironment);
            if (!defaultEnvResult.Success)
            {
                Logger.LogWarning("Failed to create default environment, continuing anyway");
            }

            // Install common packages
            var commonPackages = new List<string> { "numpy", "sympy", "matplotlib", "pandas" };
            await InstallRequirementsAsync(State.DefaultEnvironment, commonPackages);

            // Update state
            RaiseEvent(new MCPPythonInitializedLogEvent
            {
                MCPServerVersion = serverInfo,
                InitializedAt = DateTime.UtcNow
            });

            await ConfirmEvents();

            Logger.LogInformation("MCPPythonGAgent initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize MCPPythonGAgent");
            return false;
        }
    }

    #endregion

    #region Core Execution

    public async Task<MCPPythonExecutionResult> ExecuteCodeAsync(string code, MCPPythonExecutionConfig? config = null)
    {
        config ??= GetDefaultConfig();
        var executionId = Guid.NewGuid().ToString();

        Logger.LogInformation("Executing Python code via MCP (ID: {ExecutionId})", executionId);

        var result = new MCPPythonExecutionResult
        {
            ExecutionId = executionId,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Validate code first
            if (config.EnableSandbox && !await ValidateCodeAsync(code))
            {
                result.Success = false;
                result.ErrorOutput = "Code failed security validation";
                result.ErrorType = "SecurityValidationError";
                return result;
            }

            // Prepare MCP tool call
            var prompt = $@"Execute this Python code in environment '{config.EnvironmentName}':

```python
{code}
```

Configuration:
- Timeout: {config.TimeoutSeconds} seconds
- Memory limit: {config.MemoryLimitMB} MB
- Sandbox: {config.EnableSandbox}
- Working directory: {config.WorkingDirectory}
- Environment variables: {JsonSerializer.Serialize(config.EnvironmentVariables)}

Return the execution result including output, errors, and execution metadata.";

            // Call MCP tool through AI agent
            var response = await ChatWithHistoryAndToolsAsync(prompt);
            result = ParseExecutionResponse(response.Response, executionId);
            result.EndTime = DateTime.UtcNow;
            result.ExecutionTimeMs = (result.EndTime - result.StartTime).TotalMilliseconds;

            // Log execution
            RaiseEvent(new MCPPythonExecutionLogEvent
            {
                ExecutionId = executionId,
                Success = result.Success,
                ExecutionTimeMs = result.ExecutionTimeMs,
                EnvironmentName = config.EnvironmentName,
                CodeLength = code.Length
            });

            await ConfirmEvents();

            Logger.LogInformation("Python code execution completed (ID: {ExecutionId}, Success: {Success})", 
                executionId, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing Python code via MCP (ID: {ExecutionId})", executionId);
            result.Success = false;
            result.ErrorOutput = ex.Message;
            result.ErrorType = "SystemError";
            result.EndTime = DateTime.UtcNow;
            return result;
        }
    }

    public async Task<MCPPythonExecutionResult> ExecuteScriptAsync(string scriptPath, MCPPythonExecutionConfig? config = null)
    {
        config ??= GetDefaultConfig();

        var prompt = $@"Execute Python script at path '{scriptPath}' in environment '{config.EnvironmentName}'.

Configuration:
- Timeout: {config.TimeoutSeconds} seconds
- Memory limit: {config.MemoryLimitMB} MB
- Working directory: {config.WorkingDirectory}

Return the execution result including output, errors, and execution metadata.";

        try
        {
            var response = await ChatWithHistoryAndToolsAsync(prompt);
            var result = ParseExecutionResponse(response.Response, Guid.NewGuid().ToString());
            
            Logger.LogInformation("Python script execution completed: {ScriptPath}", scriptPath);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing Python script: {ScriptPath}", scriptPath);
            return new MCPPythonExecutionResult
            {
                Success = false,
                ErrorOutput = ex.Message,
                ErrorType = "SystemError"
            };
        }
    }

    #endregion

    #region Environment Management

    public async Task<MCPEnvironmentResult> CreateEnvironmentAsync(string environmentName, string pythonVersion = "3.9")
    {
        Logger.LogInformation("Creating Python environment: {EnvironmentName} (Python {PythonVersion})", 
            environmentName, pythonVersion);

        var prompt = $@"Create a new Python environment named '{environmentName}' with Python version {pythonVersion}.

Requirements:
- Environment name: {environmentName}
- Python version: {pythonVersion}
- Enable virtual environment isolation
- Install basic packages: pip, setuptools, wheel

Return the creation result with environment details.";

        try
        {
            var response = await ChatWithHistoryAndToolsAsync(prompt);
            var result = ParseEnvironmentResponse(response.Response, environmentName);

            if (result.Success)
            {
                RaiseEvent(new MCPEnvironmentCreatedLogEvent
                {
                    EnvironmentName = environmentName,
                    PythonVersion = pythonVersion,
                    InitialPackages = result.InstalledPackages
                });

                await ConfirmEvents();
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating environment: {EnvironmentName}", environmentName);
            return new MCPEnvironmentResult
            {
                Success = false,
                EnvironmentName = environmentName,
                Message = ex.Message
            };
        }
    }

    public async Task<bool> DeleteEnvironmentAsync(string environmentName)
    {
        Logger.LogInformation("Deleting Python environment: {EnvironmentName}", environmentName);

        var prompt = $"Delete Python environment '{environmentName}' and clean up all associated files.";

        try
        {
            var response = await ChatWithHistoryAndToolsAsync(prompt);
            var success = response.Response.Contains("success", StringComparison.OrdinalIgnoreCase) &&
                         !response.Response.Contains("error", StringComparison.OrdinalIgnoreCase);

            if (success)
            {
                // Update state to remove environment
                if (State.AvailableEnvironments.Contains(environmentName))
                {
                    State.AvailableEnvironments.Remove(environmentName);
                }
                State.EnvironmentConfigs.Remove(environmentName);
                State.EnvironmentPackages.Remove(environmentName);
            }

            return success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting environment: {EnvironmentName}", environmentName);
            return false;
        }
    }

    public async Task<List<string>> ListEnvironmentsAsync()
    {
        var prompt = "List all available Python environments with their details.";

        try
        {
            var response = await ChatWithHistoryAndToolsAsync(prompt);
            return ParseEnvironmentList(response.Response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error listing environments");
            return new List<string>();
        }
    }

    public async Task<MCPEnvironmentResult> GetEnvironmentInfoAsync(string environmentName)
    {
        var prompt = $"Get detailed information about Python environment '{environmentName}' including Python version, installed packages, and configuration.";

        try
        {
            var response = await ChatWithHistoryAndToolsAsync(prompt);
            return ParseEnvironmentResponse(response.Response, environmentName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting environment info: {EnvironmentName}", environmentName);
            return new MCPEnvironmentResult
            {
                Success = false,
                EnvironmentName = environmentName,
                Message = ex.Message
            };
        }
    }

    public async Task<bool> SetDefaultEnvironmentAsync(string environmentName)
    {
        try
        {
            State.DefaultEnvironment = environmentName;
            Logger.LogInformation("Set default environment to: {EnvironmentName}", environmentName);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error setting default environment: {EnvironmentName}", environmentName);
            return false;
        }
    }

    #endregion

    #region Package Management

    public async Task<MCPPackageInstallResult> InstallPackageAsync(string environmentName, string packageName, string? version = null)
    {
        var packageSpec = version != null ? $"{packageName}=={version}" : packageName;
        Logger.LogInformation("Installing package {PackageSpec} in environment {EnvironmentName}", 
            packageSpec, environmentName);

        var prompt = $@"Install Python package '{packageSpec}' in environment '{environmentName}'.

Package: {packageSpec}
Environment: {environmentName}
Use pip for installation with appropriate flags for safe installation.

Return installation result with package details and dependencies.";

        try
        {
            var response = await ChatWithHistoryAndToolsAsync(prompt);
            var result = ParsePackageInstallResponse(response.Response, packageName);

            if (result.Success)
            {
                RaiseEvent(new MCPPackageInstalledLogEvent
                {
                    EnvironmentName = environmentName,
                    PackageName = packageName,
                    Version = result.InstalledVersion,
                    InstallTimeMs = result.InstallTimeMs
                });

                await ConfirmEvents();
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error installing package {PackageName} in environment {EnvironmentName}", 
                packageName, environmentName);
            return new MCPPackageInstallResult
            {
                Success = false,
                PackageName = packageName,
                Message = ex.Message
            };
        }
    }

    public async Task<bool> UninstallPackageAsync(string environmentName, string packageName)
    {
        var prompt = $"Uninstall Python package '{packageName}' from environment '{environmentName}'.";

        try
        {
            var response = await ChatWithHistoryAndToolsAsync(prompt);
            return response.Response.Contains("success", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error uninstalling package {PackageName} from environment {EnvironmentName}", 
                packageName, environmentName);
            return false;
        }
    }

    public async Task<List<string>> ListPackagesAsync(string environmentName)
    {
        var prompt = $"List all installed packages in Python environment '{environmentName}' with their versions.";

        try
        {
            var response = await ChatWithHistoryAndToolsAsync(prompt);
            return ParsePackageList(response.Response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error listing packages in environment {EnvironmentName}", environmentName);
            return new List<string>();
        }
    }

    public async Task<bool> InstallRequirementsAsync(string environmentName, List<string> requirements)
    {
        Logger.LogInformation("Installing {Count} packages in environment {EnvironmentName}", 
            requirements.Count, environmentName);

        var requirementsText = string.Join("\n", requirements);
        var prompt = $@"Install multiple Python packages in environment '{environmentName}':

{requirementsText}

Install all packages efficiently using pip. Return success status.";

        try
        {
            var response = await ChatWithHistoryAndToolsAsync(prompt);
            return response.Response.Contains("success", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error installing requirements in environment {EnvironmentName}", environmentName);
            return false;
        }
    }

    #endregion

    #region Utility Methods

    public async Task<bool> ValidateCodeAsync(string code)
    {
        var prompt = $@"Validate this Python code for security and syntax:

```python
{code}
```

Check for:
- Syntax errors
- Security vulnerabilities (file system access, network calls, subprocess usage)
- Potentially dangerous operations
- Resource-intensive operations

Return validation result.";

        try
        {
            var response = await ChatWithHistoryAndToolsAsync(prompt);
            return response.Response.Contains("valid", StringComparison.OrdinalIgnoreCase) &&
                   !response.Response.Contains("error", StringComparison.OrdinalIgnoreCase) &&
                   !response.Response.Contains("security", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating Python code");
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GetExecutionStatsAsync()
    {
        return new Dictionary<string, object>
        {
            ["TotalExecutions"] = State.TotalExecutions,
            ["SuccessfulExecutions"] = State.SuccessfulExecutions,
            ["FailedExecutions"] = State.FailedExecutions,
            ["SuccessRate"] = State.TotalExecutions > 0 ? 
                (double)State.SuccessfulExecutions / State.TotalExecutions * 100 : 0,
            ["AvailableEnvironments"] = State.AvailableEnvironments,
            ["DefaultEnvironment"] = State.DefaultEnvironment,
            ["LastExecutionTime"] = State.LastExecutionTime,
            ["MCPServerConnected"] = State.MCPServerConnected,
            ["MCPServerVersion"] = State.MCPServerVersion
        };
    }

    public async Task<bool> TestMCPConnectionAsync()
    {
        var prompt = "Test connection to MCP Python server and verify it's responding correctly.";

        try
        {
            var response = await ChatWithHistoryAndToolsAsync(prompt);
            var connected = response.Response.Contains("connected", StringComparison.OrdinalIgnoreCase) ||
                           response.Response.Contains("success", StringComparison.OrdinalIgnoreCase);
            
            State.MCPServerConnected = connected;
            return connected;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error testing MCP connection");
            State.MCPServerConnected = false;
            return false;
        }
    }

    public async Task<string> GetMCPServerInfoAsync()
    {
        var prompt = "Get MCP Python server version and capability information.";

        try
        {
            var response = await ChatWithHistoryAndToolsAsync(prompt);
            var serverInfo = ExtractServerInfo(response.Response);
            State.MCPServerVersion = serverInfo;
            return serverInfo;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting MCP server info");
            return "Unknown";
        }
    }

    #endregion

    #region State Management

    protected override void AIGAgentTransitionState(MCPPythonState state, StateLogEventBase<MCPPythonStateLogEvent> @event)
    {
        switch (@event)
        {
            case MCPPythonInitializedLogEvent initEvent:
                state.IsInitialized = true;
                state.MCPServerConnected = true;
                state.MCPServerVersion = initEvent.MCPServerVersion;
                state.InitializedAt = initEvent.InitializedAt;
                break;

            case MCPPythonExecutionLogEvent execEvent:
                state.TotalExecutions++;
                state.LastExecutionTime = DateTime.UtcNow;
                if (execEvent.Success)
                    state.SuccessfulExecutions++;
                else
                    state.FailedExecutions++;
                break;

            case MCPEnvironmentCreatedLogEvent envEvent:
                if (!state.AvailableEnvironments.Contains(envEvent.EnvironmentName))
                    state.AvailableEnvironments.Add(envEvent.EnvironmentName);
                state.EnvironmentPackages[envEvent.EnvironmentName] = envEvent.InitialPackages;
                break;

            case MCPPackageInstalledLogEvent pkgEvent:
                if (!state.EnvironmentPackages.ContainsKey(pkgEvent.EnvironmentName))
                    state.EnvironmentPackages[pkgEvent.EnvironmentName] = new List<string>();
                
                var packageEntry = $"{pkgEvent.PackageName}=={pkgEvent.Version}";
                if (!state.EnvironmentPackages[pkgEvent.EnvironmentName].Contains(packageEntry))
                    state.EnvironmentPackages[pkgEvent.EnvironmentName].Add(packageEntry);
                break;
        }
    }

    #endregion

    #region Helper Methods

    private MCPPythonExecutionConfig GetDefaultConfig()
    {
        return new MCPPythonExecutionConfig
        {
            TimeoutSeconds = 30,
            MemoryLimitMB = 512,
            EnvironmentName = State.DefaultEnvironment,
            EnableSandbox = true,
            WorkingDirectory = "/tmp"
        };
    }

    private MCPPythonExecutionResult ParseExecutionResponse(string response, string executionId)
    {
        // Simple parsing - in real implementation, this would be more sophisticated
        var result = new MCPPythonExecutionResult
        {
            ExecutionId = executionId,
            Success = !response.Contains("error", StringComparison.OrdinalIgnoreCase),
            Output = ExtractOutput(response),
            ErrorOutput = ExtractErrorOutput(response),
            ExitCode = ExtractExitCode(response)
        };

        return result;
    }

    private MCPEnvironmentResult ParseEnvironmentResponse(string response, string environmentName)
    {
        return new MCPEnvironmentResult
        {
            Success = response.Contains("success", StringComparison.OrdinalIgnoreCase),
            EnvironmentName = environmentName,
            Message = ExtractMessage(response),
            InstalledPackages = ExtractPackageList(response),
            PythonVersion = ExtractPythonVersion(response)
        };
    }

    private MCPPackageInstallResult ParsePackageInstallResponse(string response, string packageName)
    {
        return new MCPPackageInstallResult
        {
            Success = response.Contains("success", StringComparison.OrdinalIgnoreCase),
            PackageName = packageName,
            InstalledVersion = ExtractInstalledVersion(response),
            Message = ExtractMessage(response),
            Dependencies = ExtractDependencies(response)
        };
    }

    private List<string> ParseEnvironmentList(string response)
    {
        // Simple parsing - extract environment names from response
        var environments = new List<string>();
        var lines = response.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("environment", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                    environments.Add(parts[0]);
            }
        }
        return environments;
    }

    private List<string> ParsePackageList(string response)
    {
        var packages = new List<string>();
        var lines = response.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("==") || line.Contains(">="))
            {
                packages.Add(line.Trim());
            }
        }
        return packages;
    }

    // Extract helper methods (simplified implementations)
    private string ExtractOutput(string response) => response.Contains("Output:") ? 
        response.Split("Output:")[1].Split("Error:")[0].Trim() : "";
    
    private string ExtractErrorOutput(string response) => response.Contains("Error:") ? 
        response.Split("Error:")[1].Trim() : "";
    
    private int ExtractExitCode(string response) => response.Contains("exit code", StringComparison.OrdinalIgnoreCase) ? 
        int.TryParse(response.Split("exit code")[1].Split(' ')[0], out int code) ? code : 0 : 0;
    
    private string ExtractMessage(string response) => response.Length > 200 ? response.Substring(0, 200) + "..." : response;
    
    private List<string> ExtractPackageList(string response) => ParsePackageList(response);
    
    private string ExtractPythonVersion(string response) => response.Contains("Python") ? 
        response.Split("Python")[1].Split(' ')[0].Trim() : "3.9";
    
    private string ExtractInstalledVersion(string response) => response.Contains("version") ? 
        response.Split("version")[1].Split(' ')[0].Trim() : "latest";
    
    private List<string> ExtractDependencies(string response) => new List<string>();
    
    private string ExtractServerInfo(string response) => response.Contains("version") ? 
        response.Split("version")[1].Split(' ')[0].Trim() : "1.0.0";

    #endregion
}