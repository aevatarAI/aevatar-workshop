# Python Verification Demo Guide

## 概述 / Overview

这个demo页面展示了三种不同的Python执行实现方式，让用户可以比较它们的功能和性能。

This demo page showcases three different Python execution implementations, allowing users to compare their functionality and performance.

## 功能特性 / Features

### 1. Legacy Python GAgent（传统Python代理）
- **描述**: 使用传统的子进程执行Python代码
- **特点**: 直接系统调用，成熟稳定
- **适用场景**: 基础Python脚本执行

### 2. MCP Python GAgent（MCP Python代理）  
- **描述**: 基于Model Context Protocol的安全Python执行
- **特点**: 沙箱化执行，增强安全性
- **适用场景**: 需要安全隔离的Python代码执行

### 3. Hybrid Python GAgent（混合Python代理）
- **描述**: 自适应代理，可在Legacy和MCP模式间切换
- **特点**: 向后兼容，渐进式迁移支持
- **适用场景**: 需要灵活性和兼容性的环境

## 页面功能 / Page Functions

### 代理初始化 / Agent Initialization
每个代理都有独立的初始化按钮，可以单独初始化和测试。

Each agent has its own initialization button for independent setup and testing.

### 代码执行 / Code Execution
- **输入**: Python代码，执行超时时间
- **输出**: 标准输出，错误输出，执行时间，退出代码
- **Input**: Python code, execution timeout
- **Output**: Standard output, error output, execution time, exit code

### 包管理 / Package Management
支持安装常用Python包：
- numpy
- matplotlib  
- pandas
- scipy
- sympy

Supports installation of common Python packages.

### 连接测试 / Connection Testing
- **MCP Agent**: 测试与MCP服务器的连接
- **Hybrid Agent**: 测试两种底层代理的可用性
- **MCP Agent**: Test connection to MCP server
- **Hybrid Agent**: Test availability of both underlying agents

### 统计信息 / Statistics
实时显示每个代理的执行统计：
- 执行次数
- 成功率
- 平均执行时间

Real-time execution statistics for each agent:
- Execution count
- Success rate  
- Average execution time

### 结果比较 / Result Comparison
并排比较三种代理的执行结果和性能指标。

Side-by-side comparison of execution results and performance metrics.

## 使用步骤 / Usage Steps

### 1. 启动应用 / Start Application
```bash
dotnet run --project src/Aevatar.Workshop.Host
```

### 2. 访问Demo页面 / Access Demo Page
打开浏览器访问: `http://localhost:5001/demos/python-verification-demo.html`

Open browser and navigate to: `http://localhost:5001/demos/python-verification-demo.html`

### 3. 初始化代理 / Initialize Agents
点击每个代理的"Initialize Agent"按钮进行初始化。

Click "Initialize Agent" button for each agent to initialize.

### 4. 执行Python代码 / Execute Python Code
1. 在代码输入框中输入Python代码
2. 设置执行超时时间（可选）
3. 点击"Execute Code"按钮
4. 查看执行结果

1. Enter Python code in the code input box
2. Set execution timeout (optional)
3. Click "Execute Code" button  
4. View execution results

### 5. 比较结果 / Compare Results
切换到"Comparison"标签页查看详细的性能对比。

Switch to "Comparison" tab to view detailed performance comparison.

## API接口 / API Endpoints

### 状态查询 / Status Query
- `GET /api/PythonVerificationDemo/status` - 获取代理状态

### 代理初始化 / Agent Initialization  
- `POST /api/PythonVerificationDemo/legacy/initialize` - 初始化Legacy代理
- `POST /api/PythonVerificationDemo/mcp/initialize` - 初始化MCP代理
- `POST /api/PythonVerificationDemo/hybrid/initialize` - 初始化Hybrid代理

### 代码执行 / Code Execution
- `POST /api/PythonVerificationDemo/legacy/execute` - Legacy代理执行
- `POST /api/PythonVerificationDemo/mcp/execute` - MCP代理执行  
- `POST /api/PythonVerificationDemo/hybrid/execute` - Hybrid代理执行

### 包管理 / Package Management
- `POST /api/PythonVerificationDemo/legacy/install-packages` - Legacy包安装
- `POST /api/PythonVerificationDemo/mcp/install-packages` - MCP包安装
- `POST /api/PythonVerificationDemo/hybrid/install-packages` - Hybrid包安装

### 连接测试 / Connection Testing
- `GET /api/PythonVerificationDemo/mcp/connection-test` - MCP连接测试
- `GET /api/PythonVerificationDemo/hybrid/connection-test` - Hybrid连接测试

### 统计信息 / Statistics
- `GET /api/PythonVerificationDemo/statistics` - 获取所有代理统计

## 示例代码 / Example Code

### 基础数学运算 / Basic Math Operations
```python
import math

# 计算阶乘
def factorial(n):
    return math.factorial(n)

# 计算平方根
def sqrt(x):
    return math.sqrt(x)

print(f"5的阶乘: {factorial(5)}")
print(f"16的平方根: {sqrt(16)}")
```

### NumPy数组操作 / NumPy Array Operations
```python
import numpy as np

# 创建数组
arr = np.array([1, 2, 3, 4, 5])

# 基本统计
print(f"数组: {arr}")
print(f"总和: {np.sum(arr)}")
print(f"平均值: {np.mean(arr)}")
print(f"标准差: {np.std(arr)}")
```

### 科学计算 / Scientific Computing
```python
import numpy as np
import math

# 数值计算示例
def calculate_pi_approximation(n):
    """使用级数近似计算π"""
    pi_approx = 0
    for i in range(n):
        pi_approx += ((-1)**i) / (2*i + 1)
    return 4 * pi_approx

# 计算π的近似值
pi_calc = calculate_pi_approximation(1000)
print(f"π的近似值: {pi_calc}")
print(f"实际π值: {math.pi}")
print(f"误差: {abs(pi_calc - math.pi)}")
```

## 故障排除 / Troubleshooting

### 常见问题 / Common Issues

1. **代理初始化失败** / Agent Initialization Failed
   - 检查系统是否安装了Python
   - 确保所需的依赖包已安装
   - Check if Python is installed on the system
   - Ensure required dependencies are installed

2. **MCP连接失败** / MCP Connection Failed  
   - 检查MCP服务器配置
   - 确保MCP服务器正在运行
   - Check MCP server configuration
   - Ensure MCP server is running

3. **包安装失败** / Package Installation Failed
   - 检查网络连接
   - 验证Python包管理器权限
   - Check network connection
   - Verify Python package manager permissions

### 日志查看 / Log Viewing
查看控制台和浏览器开发者工具获取详细错误信息。

Check console and browser developer tools for detailed error information.

## 技术架构 / Technical Architecture

### 后端架构 / Backend Architecture
- **Controller**: `PythonVerificationDemoController` - 提供RESTful API
- **GAgents**: 三种Python执行代理的实现
- **Services**: 状态管理和执行协调

### 前端架构 / Frontend Architecture  
- **HTML**: 响应式布局，Bootstrap样式
- **JavaScript**: 异步API调用，实时状态更新
- **CSS**: 现代化UI设计，动画效果

### 数据流 / Data Flow
1. 前端发送执行请求
2. Controller路由到相应的GAgent
3. GAgent执行Python代码
4. 返回执行结果和统计信息
5. 前端更新UI显示

1. Frontend sends execution request
2. Controller routes to appropriate GAgent  
3. GAgent executes Python code
4. Returns execution results and statistics
5. Frontend updates UI display

## 扩展开发 / Extension Development

### 添加新的执行模式 / Adding New Execution Modes
1. 创建新的GAgent实现
2. 在Controller中添加相应的API端点
3. 更新前端UI以支持新模式

### 自定义代码模板 / Custom Code Templates
可以在JavaScript中添加预定义的代码模板：

```javascript
const codeTemplates = {
    'basic-math': `import math\nprint(f"π = {math.pi}")`,
    'numpy-demo': `import numpy as np\narr = np.array([1,2,3])\nprint(arr)`
};
```

## 性能优化 / Performance Optimization

### 建议 / Recommendations
- 使用适当的超时设置避免长时间等待
- 监控内存使用情况，特别是大数据处理
- 定期清理执行历史记录
- Use appropriate timeout settings to avoid long waits
- Monitor memory usage, especially for large data processing  
- Regularly clean execution history

## 安全考虑 / Security Considerations

### 代码执行安全 / Code Execution Security
- MCP模式提供沙箱化执行环境
- 限制文件系统访问权限
- 设置内存和CPU使用限制
- MCP mode provides sandboxed execution environment
- Restrict file system access permissions
- Set memory and CPU usage limits

### 输入验证 / Input Validation
- 验证Python代码语法
- 过滤危险的系统调用
- 限制导入的模块
- Validate Python code syntax
- Filter dangerous system calls
- Restrict imported modules

---

这个demo页面为Python代码执行提供了一个全面的测试和比较平台，帮助开发者理解不同实现方式的优缺点。

This demo page provides a comprehensive testing and comparison platform for Python code execution, helping developers understand the pros and cons of different implementation approaches.