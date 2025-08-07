# MCP Python GAgent 使用示例

## 概述

本文档提供 MCP Python GAgent 实现的详细使用示例，展示如何在实际项目中使用新的 MCP-based Python 执行功能。

## 前置准备

### 1. 安装 MCP Python Server

```bash
# 安装 MCP Python 服务器
npm install -g @modelcontextprotocol/server-python

# 验证安装
npx @modelcontextprotocol/server-python --help
```

### 2. 配置环境

在 `appsettings.json` 中添加 MCP 服务器配置：

```json
{
  "MCPServer": {
    "MCPServers": {
      "python-executor": {
        "Command": "npx",
        "Args": ["@modelcontextprotocol/server-python"],
        "Env": {
          "PYTHON_SANDBOX": "true",
          "MAX_EXECUTION_TIME": "30",
          "MAX_MEMORY_MB": "512",
          "ENABLE_PACKAGES": "numpy,sympy,matplotlib,pandas,scipy"
        },
        "Description": "Python code execution with sandboxing",
        "Enabled": true
      }
    }
  }
}
```

## 使用示例

### 1. 基础的 MCP Python Agent 使用

```csharp
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.GAgents;

// 获取 MCP Python Agent
var gAgentFactory = serviceProvider.GetRequiredService<IGAgentFactory>();
var mcpPythonAgent = await gAgentFactory.GetGAgentAsync<IMCPPythonGAgent>(Guid.NewGuid());

// 初始化
var initResult = await mcpPythonAgent.InitializeAsync();
if (!initResult)
{
    throw new InvalidOperationException("MCP Python agent initialization failed");
}

// 执行简单的 Python 代码
var pythonCode = @"
import math

def calculate_circle_area(radius):
    return math.pi * radius ** 2

# 计算半径为5的圆的面积
area = calculate_circle_area(5)
print(f'Circle area: {area}')
";

var executionResult = await mcpPythonAgent.ExecuteCodeAsync(pythonCode);

Console.WriteLine($"Execution successful: {executionResult.Success}");
Console.WriteLine($"Output: {executionResult.Output}");
Console.WriteLine($"Execution time: {executionResult.ExecutionTimeMs} ms");
```

### 2. 环境管理示例

```csharp
// 创建新的 Python 环境
var envResult = await mcpPythonAgent.CreateEnvironmentAsync("data-science", "3.10");
if (envResult.Success)
{
    Console.WriteLine($"Environment created: {envResult.EnvironmentName}");
    Console.WriteLine($"Python version: {envResult.PythonVersion}");
}

// 安装包到特定环境
var packageResult = await mcpPythonAgent.InstallPackageAsync("data-science", "pandas", "2.0.0");
if (packageResult.Success)
{
    Console.WriteLine($"Package installed: {packageResult.PackageName} v{packageResult.InstalledVersion}");
}

// 在特定环境中执行代码
var config = new MCPPythonExecutionConfig
{
    EnvironmentName = "data-science",
    TimeoutSeconds = 60,
    MemoryLimitMB = 1024,
    EnableSandbox = true
};

var dataCode = @"
import pandas as pd
import numpy as np

# 创建测试数据
data = {
    'Name': ['Alice', 'Bob', 'Charlie'],
    'Age': [25, 30, 35],
    'Score': [85.5, 92.0, 78.5]
}

df = pd.DataFrame(data)
print('DataFrame created:')
print(df)

# 统计分析
print(f'Average age: {df[\"Age\"].mean()}')
print(f'Average score: {df[\"Score\"].mean()}')
";

var dataResult = await mcpPythonAgent.ExecuteCodeAsync(dataCode, config);
Console.WriteLine(dataResult.Output);
```

### 3. 混合模式使用 (HybridPythonVerificationGAgent)

```csharp
// 获取混合 Python 验证 Agent
var hybridAgent = await gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(Guid.NewGuid());

// 配置混合模式
var hybridConfig = new HybridPythonVerificationConfig
{
    PreferredMode = PythonExecutionMode.Auto, // 自动选择最佳模式
    EnableFallback = true,                     // 启用回退机制
    LogModeSelections = true                   // 记录模式选择日志
};

await hybridAgent.InitializeAsync(hybridConfig);

// 验证数学理论
var theoryContent = "The quadratic formula: x = (-b ± √(b²-4ac)) / 2a";
var formalExpression = "x = (-b ± sqrt(b²-4ac)) / (2a)";

var verificationResult = await hybridAgent.VerifyTheoryAsync(
    "quadratic-formula", 
    theoryContent, 
    formalExpression
);

Console.WriteLine($"Theory verification: {verificationResult.TestsPassed}");
Console.WriteLine($"Test results: {verificationResult.TestResults}");
Console.WriteLine($"Execution time: {verificationResult.ExecutionTime} ms");

// 生成 Python 代码
var generatedCode = await hybridAgent.GeneratePythonCodeAsync(
    "Calculate the roots of a quadratic equation ax² + bx + c = 0",
    "x = (-b ± √(b²-4ac)) / 2a"
);

Console.WriteLine("Generated Python code:");
Console.WriteLine(generatedCode);
```

### 4. 性能监控和统计

```csharp
// 获取 MCP Agent 执行统计
var mcpStats = await mcpPythonAgent.GetExecutionStatsAsync();
foreach (var stat in mcpStats)
{
    Console.WriteLine($"{stat.Key}: {stat.Value}");
}

// 获取混合 Agent 统计
var hybridStats = await hybridAgent.GetHybridStatsAsync();
Console.WriteLine($"Legacy executions: {hybridStats["LegacyExecutionCount"]}");
Console.WriteLine($"MCP executions: {hybridStats["MCPExecutionCount"]}");
Console.WriteLine($"Fallback count: {hybridStats["FallbackCount"]}");

// 获取性能比较
var performanceComparison = await hybridAgent.GetPerformanceComparisonAsync();
if (performanceComparison.ContainsKey("mcp_avg_time") && performanceComparison.ContainsKey("legacy_avg_time"))
{
    var mcpAvg = performanceComparison["mcp_avg_time"];
    var legacyAvg = performanceComparison["legacy_avg_time"];
    Console.WriteLine($"MCP average time: {mcpAvg:F2} ms");
    Console.WriteLine($"Legacy average time: {legacyAvg:F2} ms");
    Console.WriteLine($"Performance ratio: {(mcpAvg/legacyAvg):F2}");
}
```

### 5. 错误处理和回退机制

```csharp
try
{
    // 尝试使用 MCP 模式
    await hybridAgent.SetExecutionModeAsync(PythonExecutionMode.MCP);
    
    var result = await hybridAgent.ExecutePythonScriptAsync(@"
        import numpy as np
        # 一些可能失败的操作
        result = np.random.random((1000, 1000)).sum()
        print(f'Matrix sum: {result}')
    ");
    
    Console.WriteLine($"MCP execution result: {result.Success}");
}
catch (Exception ex)
{
    Console.WriteLine($"MCP execution failed: {ex.Message}");
    
    // 自动回退到 Legacy 模式
    await hybridAgent.SetExecutionModeAsync(PythonExecutionMode.Legacy);
    Console.WriteLine("Switched to Legacy mode");
}

// 检查最近的执行历史
var recentExecutions = await hybridAgent.GetRecentExecutionsAsync(5);
foreach (var execution in recentExecutions)
{
    Console.WriteLine($"Execution {execution.ExecutionId}:");
    Console.WriteLine($"  Mode: {execution.SelectedMode}");
    Console.WriteLine($"  Fallback used: {execution.FallbackUsed}");
    Console.WriteLine($"  Reason: {execution.Reason}");
}
```

### 6. 高级配置示例

```csharp
// 自定义 MCP 执行配置
var advancedConfig = new MCPPythonExecutionConfig
{
    TimeoutSeconds = 120,
    MemoryLimitMB = 2048,
    EnvironmentName = "high-performance",
    RequiredPackages = new List<string> { "tensorflow", "pytorch", "scikit-learn" },
    EnvironmentVariables = new Dictionary<string, string>
    {
        ["CUDA_VISIBLE_DEVICES"] = "0",
        ["OMP_NUM_THREADS"] = "8"
    },
    EnableNetworkAccess = false,
    EnableFileSystemAccess = false,
    WorkingDirectory = "/tmp/secure",
    EnableSandbox = true
};

// 机器学习代码执行
var mlCode = @"
import tensorflow as tf
import numpy as np

# 创建简单的神经网络
model = tf.keras.Sequential([
    tf.keras.layers.Dense(10, activation='relu', input_shape=(4,)),
    tf.keras.layers.Dense(3, activation='softmax')
])

# 编译模型
model.compile(optimizer='adam', loss='categorical_crossentropy', metrics=['accuracy'])

# 创建测试数据
X_test = np.random.random((10, 4))
y_test = tf.keras.utils.to_categorical(np.random.randint(0, 3, 10), 3)

# 训练模型
model.fit(X_test, y_test, epochs=5, verbose=0)

print('Model training completed')
print(f'Model summary: {model.count_params()} parameters')
";

var mlResult = await mcpPythonAgent.ExecuteCodeAsync(mlCode, advancedConfig);
Console.WriteLine($"ML execution: {mlResult.Success}");
```

### 7. 与理论推理引擎集成

```csharp
// 在理论推理过程中使用 MCP Python Agent
public class TheoryVerificationService
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IHybridPythonVerificationGAgent _pythonAgent;

    public async Task<bool> VerifyMathematicalTheory(string theoryId, string theoryContent)
    {
        // 生成验证代码
        var verificationCode = await _pythonAgent.GeneratePythonCodeAsync(
            theoryContent, 
            null // 无正式表达式
        );

        // 生成测试用例
        var testCases = await _pythonAgent.GenerateTestCasesAsync(theoryContent, verificationCode);

        // 执行验证
        var result = await _pythonAgent.ExecutePythonCodeAsync(theoryId, verificationCode, testCases);

        // 记录验证结果
        Console.WriteLine($"Theory {theoryId} verification: {result.TestsPassed}");
        Console.WriteLine($"Test cases passed: {result.PassedTests}/{result.TotalTests}");

        return result.TestsPassed;
    }
}
```

## 最佳实践

### 1. 性能优化

```csharp
// 预热 MCP 连接
await mcpPythonAgent.TestMCPConnectionAsync();

// 使用连接池模式（如果支持）
var connectionInfo = await mcpPythonAgent.GetMCPServerInfoAsync();
Console.WriteLine($"Connected to MCP server: {connectionInfo}");

// 批量操作
var codes = new List<string> { /* multiple Python scripts */ };
var results = new List<MCPPythonExecutionResult>();

foreach (var code in codes)
{
    var result = await mcpPythonAgent.ExecuteCodeAsync(code);
    results.Add(result);
}
```

### 2. 安全考虑

```csharp
// 总是验证代码
var codeToValidate = @"
import os
# 潜在的不安全操作
os.system('rm -rf /')
";

var isValid = await mcpPythonAgent.ValidateCodeAsync(codeToValidate);
if (!isValid)
{
    throw new SecurityException("Potentially unsafe code detected");
}

// 使用沙箱模式
var secureConfig = new MCPPythonExecutionConfig
{
    EnableSandbox = true,
    EnableNetworkAccess = false,
    EnableFileSystemAccess = false,
    TimeoutSeconds = 30,
    MemoryLimitMB = 256
};
```

### 3. 监控和诊断

```csharp
// 定期检查系统健康状况
public class MCPHealthMonitor
{
    private readonly IMCPPythonGAgent _mcpAgent;
    private readonly ILogger<MCPHealthMonitor> _logger;

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var connectionTest = await _mcpAgent.TestMCPConnectionAsync();
            var serverInfo = await _mcpAgent.GetMCPServerInfoAsync();
            var stats = await _mcpAgent.GetExecutionStatsAsync();

            _logger.LogInformation("MCP Health Check: Connection={Connection}, Server={Server}, TotalExecutions={Total}",
                connectionTest, serverInfo, stats["TotalExecutions"]);

            return connectionTest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP health check failed");
            return false;
        }
    }
}
```

## 故障排除

### 常见问题

1. **MCP 服务器连接失败**
   ```csharp
   // 检查服务器状态
   var connected = await mcpPythonAgent.TestMCPConnectionAsync();
   if (!connected)
   {
       // 检查配置或重启 MCP 服务器
       Console.WriteLine("MCP server not available, check configuration");
   }
   ```

2. **Python 包缺失**
   ```csharp
   // 安装缺失的包
   var packages = new List<string> { "numpy", "pandas", "matplotlib" };
   var installResult = await mcpPythonAgent.InstallRequirementsAsync("default", packages);
   ```

3. **性能问题**
   ```csharp
   // 监控执行时间
   var stats = await mcpPythonAgent.GetExecutionStatsAsync();
   var avgTime = (double)stats["TotalExecutions"] > 0 ? 
       (double)stats["TotalExecutionTime"] / (double)stats["TotalExecutions"] : 0;
   
   if (avgTime > 5000) // 超过5秒
   {
       Console.WriteLine("Performance issue detected, consider optimization");
   }
   ```

## 总结

MCP Python GAgent 提供了一个强大而灵活的 Python 代码执行解决方案，具有以下优势：

- **标准化接口**：符合 MCP 协议标准
- **安全执行**：内置沙箱和安全验证
- **高性能**：优化的执行引擎
- **易于扩展**：支持多种编程语言
- **向后兼容**：通过混合模式保持兼容性

通过这些示例，您可以在自己的项目中有效地集成和使用 MCP Python 功能。