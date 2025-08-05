# Enhanced Python Verification Agent - Complete Guide

## 🌌 Overview

The `PythonVerificationGAgent` has been completely upgraded to provide enterprise-grade Python script execution with comprehensive security, environment isolation, dependency management, and resource control.

## ✨ New Features

### 🔒 Security & Sandboxing
- **Script Security Validation**: Automatically detects and blocks dangerous operations
- **Sandbox Execution**: Isolated environment with restricted access to system resources
- **Network Control**: Configurable network access restrictions
- **Module Restrictions**: Whitelist-based module import control

### 🏗️ Environment Management
- **Virtual Environment Support**: Create and manage isolated Python environments
- **Environment Configuration**: Per-environment settings for Python version, dependencies, and security
- **Multi-Environment Support**: Support multiple environments for different use cases
- **Environment Lifecycle**: Create, configure, use, and delete environments as needed

### 📦 Dependency Management
- **Automatic Detection**: Extract dependencies from Python scripts automatically
- **Environment-Specific Installation**: Install packages in specific virtual environments
- **Package Management**: Install, list, and uninstall packages per environment
- **Dependency Tracking**: Track installed packages and their versions

### ⏱️ Execution Control
- **Timeout Management**: Configurable execution timeouts with forced termination
- **Resource Limits**: Memory and CPU usage controls
- **Concurrent Execution Control**: Limit number of simultaneous script executions
- **Progress Monitoring**: Real-time execution status and performance metrics

### 🔍 Enhanced Error Handling
- **Comprehensive Exception Capture**: Detailed error information with stack traces
- **Meaningful Error Messages**: User-friendly error descriptions
- **Execution Result Details**: Complete execution metadata including timing and resource usage
- **Timeout Handling**: Clear timeout detection and reporting

## 🚀 Usage Examples

### Basic Script Execution

```csharp
// Create Python verification agent
var pythonAgent = await gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());

// Initialize with default settings
await pythonAgent.InitializeAsync();

// Execute a simple script
var script = @"
import math
def calculate_fibonacci(n):
    if n <= 1:
        return n
    return calculate_fibonacci(n-1) + calculate_fibonacci(n-2)

result = calculate_fibonacci(10)
print(f'Fibonacci(10) = {result}')
";

var result = await pythonAgent.ExecutePythonScriptAsync(script);

Console.WriteLine($"Execution Success: {result.Success}");
Console.WriteLine($"Output: {result.StandardOutput}");
Console.WriteLine($"Execution Time: {result.ExecutionTimeSeconds}s");
```

### Environment Management

```csharp
// Create a secure environment for data science
var dataEnvConfig = new PythonEnvironmentConfig
{
    EnvironmentName = "data_science",
    PythonVersion = "3.9",
    Dependencies = new List<string> { "numpy", "pandas", "matplotlib" },
    MaxExecutionTimeSeconds = 60,
    MaxMemoryMB = 1024,
    IsSandboxed = true,
    EnableNetworkAccess = false,
    AllowedModules = new List<string> { "numpy", "pandas", "matplotlib", "math", "json" }
};

await pythonAgent.CreateEnvironmentAsync("data_science", dataEnvConfig);

// Execute script in the data science environment
var dataScript = @"
import numpy as np
import pandas as pd

# Create sample data
data = np.random.randn(100, 4)
df = pd.DataFrame(data, columns=['A', 'B', 'C', 'D'])

# Basic statistics
stats = df.describe()
print('Data Statistics:')
print(stats)
";

var dataResult = await pythonAgent.ExecutePythonScriptAsync(dataScript, dataEnvConfig);
```

### Security Validation

```csharp
// Test script security validation
var dangerousScript = @"
import os
import subprocess

# This script will be blocked by security validation
os.system('rm -rf /')  # Dangerous operation
subprocess.run(['curl', 'http://malicious-site.com'])  # Network access
";

var isSecure = await pythonAgent.ValidateScriptSecurityAsync(dangerousScript);
Console.WriteLine($"Script is secure: {isSecure}"); // Will be false

// The script execution will be blocked
var blockedResult = await pythonAgent.ExecutePythonScriptAsync(dangerousScript);
Console.WriteLine($"Execution blocked: {blockedResult.ErrorOutput}");
```

### Dependency Management

```csharp
// Extract dependencies from a script
var scriptWithDependencies = @"
import requests
import beautifulsoup4 as bs4
from sklearn import linear_model
import matplotlib.pyplot as plt
";

var dependencies = await pythonAgent.ExtractScriptDependenciesAsync(scriptWithDependencies);
Console.WriteLine($"Detected dependencies: {string.Join(", ", dependencies)}");
// Output: requests, beautifulsoup4, sklearn, matplotlib

// Install dependencies in an environment
await pythonAgent.InstallDependenciesAsync("ml_environment", dependencies);

// Check installed packages
var installedPackages = await pythonAgent.GetInstalledPackagesAsync("ml_environment");
```

### Timeout and Resource Control

```csharp
// Create environment with strict limits
var restrictedConfig = new PythonEnvironmentConfig
{
    EnvironmentName = "restricted",
    MaxExecutionTimeSeconds = 5,  // 5 second timeout
    MaxMemoryMB = 256,            // 256MB memory limit
    IsSandboxed = true
};

await pythonAgent.CreateEnvironmentAsync("restricted", restrictedConfig);

// Script that would timeout
var longRunningScript = @"
import time
print('Starting long operation...')
time.sleep(10)  # This will be killed after 5 seconds
print('This will never be printed')
";

var timeoutResult = await pythonAgent.ExecutePythonScriptWithTimeoutAsync(
    longRunningScript, 5, restrictedConfig);

Console.WriteLine($"Timed out: {timeoutResult.TimedOut}");
Console.WriteLine($"Error: {timeoutResult.ErrorOutput}");
```

### Theory Verification with Enhanced Features

```csharp
// Enhanced theory verification with automatic dependency management
var theoryId = "binary_optimization_theory";
var theoryCode = @"
import numpy as np
from scipy.optimize import minimize

def phi_encode(binary_sequence):
    '''Encode binary sequence using golden ratio optimization'''
    phi = (1 + np.sqrt(5)) / 2
    encoded = []
    
    for i, bit in enumerate(binary_sequence):
        if bit == '1':
            encoded.append(phi ** i)
        else:
            encoded.append(1 / (phi ** i))
    
    return np.array(encoded)

def verify_phi_optimization(binary_sequence):
    '''Verify that phi encoding reduces sequence length'''
    original_length = len(binary_sequence)
    encoded = phi_encode(binary_sequence)
    
    # Use compression techniques
    compressed_length = len(np.unique(encoded))
    
    return compressed_length <= original_length

def test_optimization_efficiency():
    '''Test optimization on various binary sequences'''
    test_sequences = [
        '1010101010',
        '1111000011',
        '1100110011',
        '1001001001'
    ]
    
    results = []
    for seq in test_sequences:
        efficient = verify_phi_optimization(seq)
        results.append(efficient)
        print(f'Sequence {seq}: Efficient = {efficient}')
    
    return all(results)
";

var testCases = new List<TestCase>
{
    new TestCase
    {
        TestName = "test_phi_encoding",
        TestDescription = "Test phi encoding function",
        TestCode = "assert phi_encode('101') is not None"
    },
    new TestCase
    {
        TestName = "test_optimization_verification",
        TestDescription = "Test optimization verification",
        TestCode = "assert verify_phi_optimization('1010') == True"
    },
    new TestCase
    {
        TestName = "test_efficiency_suite",
        TestDescription = "Test complete efficiency suite",
        TestCode = "assert test_optimization_efficiency() == True"
    }
};

// Execute with enhanced verification
var verificationResult = await pythonAgent.ExecutePythonCodeAsync(
    theoryId, theoryCode, testCases);

Console.WriteLine($"Theory Verification Result:");
Console.WriteLine($"  Tests Passed: {verificationResult.TestsPassed}");
Console.WriteLine($"  Execution Time: {verificationResult.ExecutionTime}s");
Console.WriteLine($"  Environment: {verificationResult.EnvironmentName}");
Console.WriteLine($"  Dependencies Installed: {string.Join(", ", verificationResult.InstalledDependencies)}");
Console.WriteLine($"  Memory Used: {verificationResult.ExecutionResult?.MemoryUsedMB}MB");
```

## 🔧 Configuration Options

### PythonEnvironmentConfig

```csharp
public class PythonEnvironmentConfig
{
    public string EnvironmentName { get; set; } = string.Empty;
    public string PythonVersion { get; set; } = "3.9";
    public List<string> Dependencies { get; set; } = new();
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    public int MaxExecutionTimeSeconds { get; set; } = 30;
    public long MaxMemoryMB { get; set; } = 512;
    public bool EnableNetworkAccess { get; set; } = false;
    public List<string> AllowedModules { get; set; } = new();
    public string WorkingDirectory { get; set; } = string.Empty;
    public bool IsSandboxed { get; set; } = true;
}
```

### Environment-Specific Settings

```csharp
// Development environment - less restrictive
var devConfig = new PythonEnvironmentConfig
{
    EnvironmentName = "development",
    MaxExecutionTimeSeconds = 300,    // 5 minutes
    MaxMemoryMB = 2048,               // 2GB
    EnableNetworkAccess = true,       // Allow network
    IsSandboxed = false               // Less restrictions
};

// Production environment - highly restrictive
var prodConfig = new PythonEnvironmentConfig
{
    EnvironmentName = "production",
    MaxExecutionTimeSeconds = 30,     // 30 seconds
    MaxMemoryMB = 256,                // 256MB
    EnableNetworkAccess = false,      // No network
    IsSandboxed = true,               // Full sandbox
    AllowedModules = new List<string> { "math", "json", "datetime" }
};

// Testing environment - balanced
var testConfig = new PythonEnvironmentConfig
{
    EnvironmentName = "testing",
    MaxExecutionTimeSeconds = 60,     // 1 minute
    MaxMemoryMB = 512,                // 512MB
    EnableNetworkAccess = false,      // No network
    IsSandboxed = true,               // Sandboxed
    Dependencies = new List<string> { "pytest", "numpy" }
};
```

## 🛡️ Security Features

### Blocked Operations

The security validator automatically blocks these dangerous operations:

- **System Commands**: `subprocess`, `os.system`, `eval`, `exec`
- **File System**: `open()`, `file()`, `pathlib` operations
- **Network Access**: `urllib`, `requests`, `socket`, `http` (when disabled)
- **Dynamic Code**: `compile`, `__import__`, `globals`, `locals`

### Sandbox Restrictions

When sandboxing is enabled:

- Dangerous built-ins are disabled
- Network access is controlled
- Resource limits are enforced
- Module imports are restricted to whitelist

## 📊 Monitoring and Metrics

### ScriptExecutionResult

```csharp
public class ScriptExecutionResult
{
    public bool Success { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string ErrorOutput { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public double ExecutionTimeSeconds { get; set; }
    public long MemoryUsedMB { get; set; }
    public bool TimedOut { get; set; }
    public Exception? Exception { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string ScriptHash { get; set; } = string.Empty;
}
```

### Usage Analytics

```csharp
// Get verification statistics
var stats = await pythonAgent.GetVerificationStatsAsync();
Console.WriteLine($"Total Verifications: {stats["total"]}");
Console.WriteLine($"Successful: {stats["passed"]}");
Console.WriteLine($"Failed: {stats["failed"]}");

// Get environment usage
var environments = await pythonAgent.ListEnvironmentsAsync();
foreach (var env in environments)
{
    var config = await pythonAgent.GetEnvironmentConfigAsync(env);
    var packages = await pythonAgent.GetInstalledPackagesAsync(env);
    Console.WriteLine($"Environment {env}: {packages.Count} packages installed");
}
```

## 🎯 Best Practices

### 1. Environment Strategy

- **Use separate environments** for different types of scripts
- **Pre-create environments** with required dependencies
- **Use restrictive settings** for untrusted code
- **Clean up unused environments** periodically

### 2. Security Guidelines

- **Always enable sandboxing** for untrusted scripts
- **Restrict network access** unless specifically needed
- **Use whitelisted modules** for maximum security
- **Set appropriate timeouts** to prevent resource exhaustion

### 3. Performance Optimization

- **Reuse environments** when possible
- **Pre-install common dependencies** in base environments
- **Set reasonable resource limits** based on expected usage
- **Monitor execution metrics** to optimize settings

### 4. Error Handling

- **Check security validation** before execution
- **Handle timeout scenarios** gracefully
- **Provide meaningful error messages** to users
- **Log execution details** for debugging

## 🔄 Migration from Previous Version

If you're upgrading from the previous version:

1. **Update interface usage** - New methods are available
2. **Add environment configuration** - Create environments for your use cases
3. **Review security settings** - Enable appropriate sandbox settings
4. **Update error handling** - Handle new timeout and security scenarios
5. **Test thoroughly** - Verify all existing functionality works with new features

## 🚀 Next Steps

The enhanced PythonVerificationGAgent now provides enterprise-grade Python execution capabilities with comprehensive security, environment management, and monitoring. Use these features to build robust, secure, and scalable Python execution systems in your applications.

For questions or additional features, please refer to the source code or create an issue in the repository.