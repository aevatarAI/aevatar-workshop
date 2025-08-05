# Theory Reasoning Engine - Comprehensive Guide
# 理论推理引擎 - 详尽指南

## Table of Contents / 目录

1. [System Overview / 系统概述](#system-overview--系统概述)
2. [Architecture Design / 架构设计](#architecture-design--架构设计)
3. [GAgent Components / GAgent组件](#gagent-components--gagent组件)
4. [API Documentation / API文档](#api-documentation--api文档)
5. [Frontend Usage Guide / 前端使用指南](#frontend-usage-guide--前端使用指南)
6. [Session Management / 会话管理](#session-management--会话管理)
7. [Theory Generation & Storage / 理论生成与存储](#theory-generation--storage--理论生成与存储)
8. [AI Thinking Process / AI思考过程](#ai-thinking-process--ai思考过程)
9. [Configuration Management / 配置管理](#configuration-management--配置管理)
10. [Troubleshooting / 故障排除](#troubleshooting--故障排除)

---

## System Overview / 系统概述

### English

The Theory Reasoning Engine is an advanced AI-powered system designed to automatically generate, validate, and manage mathematical and logical theories. Built on the Aevatar framework using Orleans distributed computing, it leverages multiple specialized GAgents (General Agents) to perform different aspects of reasoning and theory management.

**Key Features:**
- **Multi-method Reasoning**: Supports deductive, inductive, abductive, and analogical reasoning
- **Automated Theory Generation**: AI-powered generation of mathematical theories with formal expressions
- **Python Verification**: Automatic validation of theories using Python code execution
- **Session Management**: Complete tracking of reasoning sessions with persistent storage
- **Real-time Thinking Process**: Capture and display AI's step-by-step reasoning process
- **Theory Export**: Export theories as structured Markdown files
- **Multi-language Support**: English and Chinese interface support

### 中文

理论推理引擎是一个先进的AI驱动系统，旨在自动生成、验证和管理数学和逻辑理论。基于Aevatar框架和Orleans分布式计算构建，利用多个专业化的GAgent（通用代理）来执行推理和理论管理的不同方面。

**核心功能：**
- **多方法推理**：支持演绎、归纳、溯因和类比推理
- **自动理论生成**：AI驱动的数学理论生成，包含形式表达式
- **Python验证**：使用Python代码执行自动验证理论
- **会话管理**：完整跟踪推理会话并持久化存储
- **实时思考过程**：捕获并显示AI的逐步推理过程
- **理论导出**：将理论导出为结构化Markdown文件
- **多语言支持**：英文和中文界面支持

---

## Architecture Design / 架构设计

### System Architecture / 系统架构

```
┌─────────────────────────────────────────────────────────────┐
│                    Frontend (React/HTML)                    │
│                        前端界面                              │
├─────────────────────────────────────────────────────────────┤
│                  API Controllers                            │
│               TheoryReasoningDemoController                  │
├─────────────────────────────────────────────────────────────┤
│                    GAgent Layer                             │
│                     GAgent层                                │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐│
│  │   Coordinator   │  │  Knowledge Base │  │  AI Reasoning││
│  │     协调器      │  │     知识库      │  │   AI推理     ││
│  └─────────────────┘  └─────────────────┘  └──────────────┘│
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐│
│  │  Verification   │  │   Formalization │  │    Review    ││
│  │     验证        │  │     形式化      │  │     评审     ││
│  └─────────────────┘  └─────────────────┘  └──────────────┘│
├─────────────────────────────────────────────────────────────┤
│                Orleans Distributed Runtime                  │
│                  Orleans分布式运行时                        │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow / 数据流

1. **Session Initiation / 会话启动**: User starts a reasoning session through frontend
2. **Theory Generation / 理论生成**: AI generates new theories using various reasoning methods
3. **Verification / 验证**: Python code validates generated theories
4. **Review / 评审**: Automated quality assessment and equivalence checking
5. **Storage / 存储**: Theories stored in knowledge base and exported as Markdown files
6. **Thinking Process / 思考过程**: AI's reasoning steps captured and displayed in real-time

---

## GAgent Components / GAgent组件

### 1. TheoryReasoningCoordinatorGAgent / 理论推理协调器

**Purpose / 用途:**
- Central orchestrator for the entire reasoning process
- Manages reasoning sessions and coordinates between other GAgents
- 整个推理过程的中央协调器，管理推理会话并协调其他GAgent

**Key Responsibilities / 主要职责:**
- Session lifecycle management (start, pause, resume, complete)
- Task distribution to specialized GAgents
- System initialization and health monitoring
- Event handling and state management

**GUID:** `11111111-1111-1111-1111-111111111111`

**Core Methods / 核心方法:**
```csharp
Task<bool> InitializeSystemAsync(string llmSystem)
Task<string> StartReasoningSessionAsync(ReasoningSessionConfig config)
Task<bool> PauseReasoningSessionAsync(string sessionId)
Task<bool> ResumeReasoningSessionAsync(string sessionId)
Task<bool> CompleteReasoningSessionAsync(string sessionId)
Task<List<ReasoningSession>> GetActiveSessionsAsync()
Task<List<ReasoningSession>> GetCompletedSessionsAsync()
```

### 2. TheoryKnowledgeGAgent / 理论知识库代理

**Purpose / 用途:**
- Centralized storage and retrieval of mathematical theories
- Theory ID generation and management
- 数学理论的集中存储和检索，理论ID生成和管理

**Key Responsibilities / 主要职责:**
- Store and index theories by type (Axiom, Theorem, Lemma, etc.)
- Generate unique theory IDs with proper sequencing
- Provide search and retrieval capabilities
- Maintain theory statistics and relationships

**GUID:** `22222222-2222-2222-2222-222222222222`

**Theory Types / 理论类型:**
- **A**: Axioms / 公理
- **T**: Theorems / 定理
- **L**: Lemmas / 引理
- **P**: Propositions / 命题
- **C**: Corollaries / 推论
- **D**: Definitions / 定义

**Core Methods / 核心方法:**
```csharp
Task<bool> InitializeWithPsiTheoryAsync()
Task<string> AddTheoryAsync(TheoryElement theory)
Task<TheoryElement?> GetTheoryAsync(string theoryId)
Task<List<TheoryElement>> GetAllTheoriesAsync()
Task<List<TheoryElement>> GetTheoriesByTypeAsync(string type)
Task<Dictionary<string, int>> GetStatisticsAsync()
```

### 3. AutoReasoningAIGAgent / 自动推理AI代理

**Purpose / 用途:**
- AI-powered theory generation using various reasoning methods
- Thinking process capture and documentation
- 使用各种推理方法进行AI驱动的理论生成，思考过程捕获和记录

**Key Responsibilities / 主要职责:**
- Generate new theories using deductive, inductive, abductive, and analogical reasoning
- Capture detailed thinking steps during reasoning process
- Parse and structure AI-generated content
- Publish thinking events for real-time monitoring

**GUID:** `66666666-6666-6666-6666-666666666666`

**Reasoning Methods / 推理方法:**
- **Deductive / 演绎推理**: From general principles to specific conclusions
- **Inductive / 归纳推理**: From specific observations to general patterns
- **Abductive / 溯因推理**: From observations to best explanations
- **Analogical / 类比推理**: From similar structures to new domains

**Core Methods / 核心方法:**
```csharp
Task<List<string>> PerformDeductiveReasoningAsync(List<string> premiseTheoryIds, string targetDomain, string? sessionId = null)
Task<List<string>> PerformInductiveReasoningAsync(List<string> exampleTheoryIds, string pattern, string? sessionId = null)
Task<List<string>> PerformAbductiveReasoningAsync(string conclusionTheory, string domain, string? sessionId = null)
Task<List<string>> PerformAnalogicalReasoningAsync(string sourceTheoryId, string targetDomain, string? sessionId = null)
```

### 4. FormalizationAIGAgent / 形式化AI代理

**Purpose / 用途:**
- Convert natural language theories into formal mathematical expressions
- Generate executable Python code for theory validation
- 将自然语言理论转换为形式数学表达式，生成可执行的Python代码用于理论验证

**Key Responsibilities / 主要职责:**
- LaTeX/MathML formula generation
- Python code synthesis for computational verification
- Symbolic mathematics integration
- Format standardization

**GUID:** `33333333-3333-3333-3333-333333333333`

**Core Methods / 核心方法:**
```csharp
Task<FormalizationResult> FormalizeTheoryAsync(string theoryId, string theoryContent, string targetTool)
Task<string> GeneratePythonCodeAsync(string theoryContent, string formalExpression)
Task<bool> ValidateFormalizationAsync(string formalExpression, string pythonCode)
```

### 5. PythonVerificationGAgent / Python验证代理

**Purpose / 用途:**
- Execute Python code to validate mathematical theories
- Environment setup and package management
- 执行Python代码验证数学理论，环境设置和包管理

**Key Responsibilities / 主要职责:**
- Python environment isolation and management
- Code execution with security constraints
- Result interpretation and error handling
- Package installation and dependency management

**GUID:** `44444444-4444-4444-4444-444444444444`

**Core Methods / 核心方法:**
```csharp
Task<VerificationResult> VerifyPythonCodeAsync(string pythonCode, string theoryId)
Task<bool> ValidatePythonSyntaxAsync(string pythonCode)
Task<bool> InstallRequiredPackagesAsync(List<string> packages)
Task<PythonEnvironmentInfo> GetEnvironmentInfoAsync()
```

### 6. EquivalenceReviewGAgent / 等价审查代理

**Purpose / 用途:**
- Review generated theories for quality and equivalence
- Automated acceptance/rejection decisions
- 审查生成的理论质量和等价性，自动接受/拒绝决策

**Key Responsibilities / 主要职责:**
- Theory quality assessment
- Duplicate detection and equivalence checking
- Automated review workflows
- Quality metrics calculation

**GUID:** `55555555-5555-5555-5555-555555555555`

**Core Methods / 核心方法:**
```csharp
Task<ReviewResult> ReviewTheoryAsync(string theoryId)
Task<bool> CheckEquivalenceAsync(string theoryId1, string theoryId2)
Task<float> CalculateQualityScoreAsync(string theoryId)
Task<List<ReviewResult>> GetReviewHistoryAsync(string sessionId)
```

---

## API Documentation / API文档

### Base URL / 基础URL
```
http://localhost:5001/api/TheoryReasoningDemo
```

### Authentication / 认证
Currently no authentication required / 当前无需认证

### Endpoints / 端点

#### 1. System Management / 系统管理

##### GET /initialize
**Purpose**: Initialize the reasoning system / 初始化推理系统

**Parameters:**
- `systemLLM` (string, optional): LLM system to use (default: "OpenAI")

**Response:**
```json
{
  "success": true,
  "message": "System initialized successfully",
  "availableAgents": [
    "TheoryReasoningCoordinator",
    "TheoryKnowledge", 
    "AutoReasoning",
    "Formalization",
    "PythonVerification",
    "EquivalenceReview"
  ]
}
```

##### GET /system-status
**Purpose**: Get current system status / 获取当前系统状态

**Response:**
```json
{
  "success": true,
  "status": "initialized",
  "message": "System is ready for reasoning",
  "totalTheories": 25,
  "activeSessions": 1,
  "completedSessions": 3,
  "systemHealth": "healthy"
}
```

#### 2. Session Management / 会话管理

##### POST /start-reasoning
**Purpose**: Start a new reasoning session / 启动新的推理会话

**Request Body:**
```json
{
  "systemLLM": "AzureOpenAI",
  "maxIterations": 5,
  "qualityThreshold": 0.8,
  "enableAutoReview": true,
  "enableAutoRevision": false,
  "targetDomain": "information_theory",
  "enabledReasoningMethods": ["deductive", "inductive"]
}
```

**Response:**
```json
{
  "success": true,
  "sessionId": "21776724-5a83-4f24-8893-493fb7cf178e",
  "message": "Reasoning session started successfully",
  "config": {
    "maxIterations": 5,
    "qualityThreshold": 0.8,
    "targetDomain": "information_theory"
  }
}
```

##### GET /sessions
**Purpose**: Get all reasoning sessions / 获取所有推理会话

**Response:**
```json
{
  "success": true,
  "sessions": [
    {
      "sessionId": "21776724-5a83-4f24-8893-493fb7cf178e",
      "status": "completed",
      "startedAt": "2024-01-15T10:30:00Z",
      "completedAt": "2024-01-15T10:45:00Z",
      "currentIteration": 3,
      "generatedTheoryIds": ["P2-1", "T2-1"],
      "acceptedTheoryIds": ["P2-1"],
      "rejectedTheoryIds": ["T2-1"],
      "successRate": 0.5,
      "thinkingStepsCount": 15
    }
  ]
}
```

##### POST /pause-reasoning
**Purpose**: Pause an active reasoning session / 暂停活跃的推理会话

**Request Body:**
```json
{
  "sessionId": "21776724-5a83-4f24-8893-493fb7cf178e"
}
```

##### POST /resume-reasoning
**Purpose**: Resume a paused reasoning session / 恢复暂停的推理会话

##### POST /stop-reasoning
**Purpose**: Stop and complete a reasoning session / 停止并完成推理会话

#### 3. Theory Management / 理论管理

##### GET /theories
**Purpose**: Get all theories in the knowledge base / 获取知识库中的所有理论

**Response:**
```json
{
  "success": true,
  "theories": [
    {
      "fullId": "A1",
      "type": "A",
      "content": "Information is a measure of the reduction in uncertainty",
      "formalExpression": "I(X) = -\\sum_{x} P(x) \\log P(x)",
      "pythonCode": "import math\ndef information_content(prob):\n    return -math.log2(prob)",
      "isVerified": true,
      "qualityScore": 0.95,
      "createdAt": "2024-01-15T10:30:00Z",
      "dependencies": [],
      "metadata": {
        "domain": "information_theory",
        "complexity": "fundamental"
      }
    }
  ],
  "totalCount": 25,
  "byType": {
    "A": 3,
    "T": 8,
    "L": 5,
    "P": 6,
    "C": 2,
    "D": 1
  }
}
```

##### GET /theory/{theoryId}
**Purpose**: Get detailed information for a specific theory / 获取特定理论的详细信息

**Response:**
```json
{
  "success": true,
  "theory": {
    "id": "P2-1",
    "type": "P",
    "content": "The entropy of a discrete random variable X is maximized when all outcomes are equally likely",
    "formalExpression": "H(X) \\leq \\log |\\mathcal{X}|",
    "pythonCode": "def max_entropy(n):\n    return math.log2(n)",
    "dependencies": ["A1", "D1-1"],
    "isVerified": true,
    "qualityScore": 0.87,
    "createdAt": "2024-01-15T10:35:00Z",
    "verificationResults": {
      "pythonExecutionSuccess": true,
      "formalValidation": true,
      "testCases": [
        {"input": [2], "expected": 1.0, "actual": 1.0, "passed": true},
        {"input": [4], "expected": 2.0, "actual": 2.0, "passed": true}
      ]
    }
  },
  "markdownContent": "# Theory P2-1: Proposition\n\n**Content**: The entropy of..."
}
```

#### 4. Thinking Process / 思考过程

##### GET /thinking-process/all
**Purpose**: Get all thinking steps from all sessions / 获取所有会话的思考步骤

**Response:**
```json
{
  "success": true,
  "totalSessions": 2,
  "totalSteps": 25,
  "thinkingSteps": [
    {
      "sessionId": "21776724-5a83-4f24-8893-493fb7cf178e",
      "sessionStatus": "completed",
      "stepId": "step-001",
      "reasoningType": "deductive",
      "stepType": "analysis",
      "content": "Analyzing the given axioms to identify potential deduction paths",
      "reasoning": "Starting with fundamental information theory axioms, I need to establish logical connections...",
      "timestamp": "2024-01-15T10:31:00Z",
      "iteration": 1,
      "metadata": {
        "complexity": "medium",
        "confidence": 0.85
      }
    }
  ],
  "stepsByType": {
    "analysis": 8,
    "synthesis": 7,
    "verification": 6,
    "conclusion": 4
  },
  "stepsByReasoningType": {
    "deductive": 15,
    "inductive": 10
  }
}
```

##### GET /session/{sessionId}/thinking-process
**Purpose**: Get thinking steps for a specific session / 获取特定会话的思考步骤

#### 5. Theory Operations / 理论操作

##### POST /formalize-theory
**Purpose**: Manually trigger theory formalization / 手动触发理论形式化

**Request Body:**
```json
{
  "theoryId": "P2-1",
  "theoryContent": "The entropy is maximized when outcomes are equally likely",
  "targetTool": "sympy",
  "systemLLM": "AzureOpenAI"
}
```

##### POST /verify-theory
**Purpose**: Manually trigger theory verification / 手动触发理论验证

##### POST /review-theory
**Purpose**: Manually trigger theory review / 手动触发理论审查

---

## Frontend Usage Guide / 前端使用指南

### Accessing the Interface / 访问界面

1. **Navigate to**: `http://localhost:5001/demos/theory-reasoning-demo.html`
2. **Browser Requirements**: Modern browser with JavaScript enabled
3. **Language Support**: Toggle between English and Chinese using the language selector

### Step-by-Step Usage / 分步使用指南

#### Step 1: System Initialization / 第一步：系统初始化

**English:**
1. On first load, you'll see "System not initialized"
2. Click the "Initialize System" button
3. Select your preferred LLM system from the dropdown (OpenAI, AzureOpenAI, etc.)
4. Wait for initialization to complete (usually 5-10 seconds)
5. Status should change to "System is ready for reasoning"

**中文:**
1. 首次加载时，您会看到"系统未初始化"
2. 点击"初始化系统"按钮
3. 从下拉菜单选择您偏好的LLM系统（OpenAI、AzureOpenAI等）
4. 等待初始化完成（通常5-10秒）
5. 状态应变更为"系统准备就绪"

#### Step 2: Configure Reasoning Session / 第二步：配置推理会话

**English:**
1. Navigate to the "Start Reasoning" section
2. Configure session parameters:
   - **Max Iterations**: Number of reasoning cycles (default: 5)
   - **Quality Threshold**: Minimum quality score for theory acceptance (0.0-1.0)
   - **Target Domain**: Subject area for theory generation
   - **Reasoning Methods**: Select from deductive, inductive, abductive, analogical
   - **Auto Review**: Enable automatic theory review
   - **Auto Revision**: Enable automatic theory revision

**中文:**
1. 导航到"开始推理"部分
2. 配置会话参数：
   - **最大迭代次数**：推理循环次数（默认：5）
   - **质量阈值**：理论接受的最低质量分数（0.0-1.0）
   - **目标领域**：理论生成的主题领域
   - **推理方法**：从演绎、归纳、溯因、类比中选择
   - **自动审查**：启用自动理论审查
   - **自动修正**：启用自动理论修正

#### Step 3: Start Reasoning / 第三步：开始推理

**English:**
1. Click "Start Reasoning" button
2. A new session ID will be generated
3. Monitor progress in real-time:
   - **Session Status**: Shows current state (active/paused/completed)
   - **Current Iteration**: Progress through reasoning cycles
   - **Generated Theories**: Number of theories created
   - **Success Rate**: Percentage of accepted theories

**中文:**
1. 点击"开始推理"按钮
2. 将生成新的会话ID
3. 实时监控进度：
   - **会话状态**：显示当前状态（活跃/暂停/完成）
   - **当前迭代**：推理循环进度
   - **生成理论**：创建的理论数量
   - **成功率**：接受理论的百分比

#### Step 4: Monitor Thinking Process / 第四步：监控思考过程

**English:**
1. Switch to "AI Thinking Process" tab
2. Enable "Auto Refresh" to see real-time updates
3. Filter thinking steps by:
   - **Session**: View steps from specific sessions
   - **Reasoning Type**: Filter by deductive, inductive, etc.
   - **Step Type**: Filter by analysis, synthesis, verification, conclusion
4. Each thinking step shows:
   - **Content**: What the AI is thinking about
   - **Reasoning**: Detailed explanation of the thought process
   - **Timestamp**: When the step occurred
   - **Confidence**: AI's confidence in this step

**中文:**
1. 切换到"AI思考过程"标签
2. 启用"自动刷新"以查看实时更新
3. 按以下条件过滤思考步骤：
   - **会话**：查看特定会话的步骤
   - **推理类型**：按演绎、归纳等过滤
   - **步骤类型**：按分析、综合、验证、结论过滤
4. 每个思考步骤显示：
   - **内容**：AI正在思考的内容
   - **推理**：思考过程的详细解释
   - **时间戳**：步骤发生的时间
   - **置信度**：AI对此步骤的置信度

#### Step 5: View Generated Theories / 第五步：查看生成的理论

**English:**
1. Navigate to "Generated Theories" tab
2. Browse theories by type:
   - **A**: Axioms (fundamental assumptions)
   - **T**: Theorems (proven statements)
   - **L**: Lemmas (supporting propositions)
   - **P**: Propositions (general statements)
   - **C**: Corollaries (direct consequences)
   - **D**: Definitions (formal definitions)
3. Click on any theory card to view detailed information:
   - **Content**: Natural language description
   - **Formal Expression**: Mathematical notation (LaTeX)
   - **Python Code**: Executable verification code
   - **Dependencies**: Related theories
   - **Verification Results**: Test outcomes
   - **Quality Score**: Automated assessment

**中文:**
1. 导航到"生成理论"标签
2. 按类型浏览理论：
   - **A**：公理（基本假设）
   - **T**：定理（已证明的陈述）
   - **L**：引理（支持命题）
   - **P**：命题（一般陈述）
   - **C**：推论（直接后果）
   - **D**：定义（形式定义）
3. 点击任何理论卡片查看详细信息：
   - **内容**：自然语言描述
   - **形式表达式**：数学记号（LaTeX）
   - **Python代码**：可执行验证代码
   - **依赖关系**：相关理论
   - **验证结果**：测试结果
   - **质量分数**：自动评估

#### Step 6: Export and Save / 第六步：导出和保存

**English:**
1. **Export Individual Theories**:
   - Click "Export as Markdown" button in theory detail modal
   - Download structured .md file with all theory information
2. **Session Files** (Automatic):
   - Each session creates a directory: `reasoning-session-{sessionId}/`
   - Contains individual theory files: `{TheoryId}_{Type}.md`
   - Session summary: `session_summary.md`
   - README file with session overview
3. **Persistent Storage**:
   - All completed sessions remain visible after page refresh
   - Theory database persists across system restarts
   - Session history maintained indefinitely

**中文:**
1. **导出单个理论**：
   - 在理论详情模态框中点击"导出为Markdown"按钮
   - 下载包含所有理论信息的结构化.md文件
2. **会话文件**（自动）：
   - 每个会话创建目录：`reasoning-session-{sessionId}/`
   - 包含单个理论文件：`{TheoryId}_{Type}.md`
   - 会话摘要：`session_summary.md`
   - README文件包含会话概览
3. **持久存储**：
   - 所有完成的会话在页面刷新后保持可见
   - 理论数据库在系统重启后持续存在
   - 会话历史无限期维护

### Interface Features / 界面功能

#### Real-time Updates / 实时更新
- Session status updates automatically
- Theory generation progress shown in real-time
- Thinking process streams live during reasoning
- Error notifications appear immediately

#### Responsive Design / 响应式设计
- Mobile-friendly interface
- Adaptive layout for different screen sizes
- Touch-friendly controls on tablets
- Keyboard shortcuts for power users

#### Accessibility / 可访问性
- Screen reader compatible
- High contrast mode available
- Keyboard navigation support
- ARIA labels for all interactive elements

---

## Session Management / 会话管理

### Session Lifecycle / 会话生命周期

1. **Creation / 创建**: New session with unique GUID identifier
2. **Configuration / 配置**: Parameters set for reasoning behavior
3. **Execution / 执行**: AI generates theories through multiple iterations
4. **Monitoring / 监控**: Real-time progress tracking and intervention
5. **Completion / 完成**: Automatic or manual session termination
6. **Archival / 存档**: Persistent storage with Markdown export

### Session States / 会话状态

- **Initializing / 初始化中**: Session being set up
- **Active / 活跃**: Currently generating theories
- **Paused / 暂停**: Temporarily suspended by user
- **Completed / 完成**: Finished successfully
- **Failed / 失败**: Terminated due to error
- **Cancelled / 取消**: Manually stopped by user

### Configuration Options / 配置选项

```json
{
  "maxIterations": 10,
  "qualityThreshold": 0.75,
  "enableAutoReview": true,
  "enableAutoRevision": false,
  "targetDomain": "information_theory",
  "enabledReasoningMethods": [
    "deductive",
    "inductive", 
    "abductive",
    "analogical"
  ],
  "reviewCriteria": {
    "minQualityScore": 0.7,
    "requireVerification": true,
    "allowDuplicates": false
  }
}
```

---

## Theory Generation & Storage / 理论生成与存储

### Generation Process / 生成过程

1. **Context Analysis / 上下文分析**: AI analyzes existing theories in target domain
2. **Method Selection / 方法选择**: Choose appropriate reasoning method
3. **Content Generation / 内容生成**: Create natural language theory description
4. **Formalization / 形式化**: Convert to mathematical expressions
5. **Code Generation / 代码生成**: Create Python verification code
6. **Validation / 验证**: Execute and test generated code
7. **Review / 审查**: Quality assessment and acceptance decision

### Theory Structure / 理论结构

```csharp
public class TheoryElement
{
    public string Id { get; set; }                    // e.g., "P2-1"
    public string Type { get; set; }                  // A, T, L, P, C, D
    public string Content { get; set; }               // Natural language
    public string? FormalExpression { get; set; }     // LaTeX/MathML
    public string? PythonCode { get; set; }           // Executable code
    public List<string> Dependencies { get; set; }    // Related theories
    public bool IsVerified { get; set; }              // Validation status
    public float QualityScore { get; set; }           // 0.0 - 1.0
    public DateTime CreatedAt { get; set; }           // Timestamp
    public string ReasoningMethod { get; set; }       // Generation method
    public Dictionary<string, string> Metadata { get; set; }
}
```

### Storage Mechanisms / 存储机制

#### 1. In-Memory Knowledge Base / 内存知识库
- Fast access for active reasoning sessions
- Organized by theory type and dependencies
- Supports complex queries and relationships

#### 2. Markdown Files / Markdown文件
- Human-readable format for each theory
- Structured with metadata headers
- Version control friendly
- Easy integration with documentation systems

#### 3. Session Directories / 会话目录
```
reasoning-session-{sessionId}/
├── README.md                 # Session overview
├── session_summary.md        # Detailed summary
├── A1_A.md                  # Axiom theories
├── T1-1_T.md                # Theorem theories
├── P2-1_P.md                # Proposition theories
└── L1-1_L.md                # Lemma theories
```

---

## AI Thinking Process / AI思考过程

### Thinking Step Structure / 思考步骤结构

```csharp
public class ThinkingStep
{
    public string StepId { get; set; }              // Unique identifier
    public string StepType { get; set; }            // analysis, synthesis, etc.
    public string Content { get; set; }             // What AI is thinking
    public string Reasoning { get; set; }           // Why AI thinks this
    public DateTime Timestamp { get; set; }         // When step occurred
    public Dictionary<string, string> Metadata { get; set; }
}
```

### Step Types / 步骤类型

- **Analysis / 分析**: Breaking down complex problems
- **Synthesis / 综合**: Combining ideas to form new concepts
- **Verification / 验证**: Checking validity of reasoning
- **Conclusion / 结论**: Drawing final insights
- **Reflection / 反思**: Meta-cognitive evaluation

### Capture Mechanism / 捕获机制

1. **Real-time Generation / 实时生成**: Steps captured during AI reasoning
2. **Event Publishing / 事件发布**: Thinking events broadcast to system
3. **Session Association / 会话关联**: Steps linked to specific sessions
4. **UI Streaming / UI流式传输**: Live updates to frontend interface

### Reasoning Types / 推理类型

#### Deductive Reasoning / 演绎推理
- **Process**: General principles → Specific conclusions
- **Example**: "All prime numbers > 2 are odd" + "7 is prime" → "7 is odd"
- **AI Thinking**: "I'll start with known axioms and apply logical rules..."

#### Inductive Reasoning / 归纳推理
- **Process**: Specific observations → General patterns  
- **Example**: Multiple entropy calculations → General entropy maximization principle
- **AI Thinking**: "Looking at these examples, I notice a pattern..."

#### Abductive Reasoning / 溯因推理
- **Process**: Observations → Best explanations
- **Example**: Information theory principles → Communication channel behavior
- **AI Thinking**: "Given these effects, the most likely cause is..."

#### Analogical Reasoning / 类比推理
- **Process**: Similar structures → New domains
- **Example**: Thermodynamic entropy → Information entropy
- **AI Thinking**: "This concept in physics might apply to information theory..."

---

## Configuration Management / 配置管理

### LLM Configuration / LLM配置

The system supports multiple LLM providers through centralized configuration:

```json
{
  "llmConfigurations": [
    {
      "id": "OpenAI",
      "name": "OpenAI GPT-4",
      "provider": "OpenAI",
      "modelName": "gpt-4",
      "apiKey": "sk-...",
      "isActive": true
    },
    {
      "id": "AzureOpenAI", 
      "name": "Azure OpenAI GPT-4",
      "provider": "AzureOpenAI",
      "modelName": "gpt-4",
      "endpointUrl": "https://your-resource.openai.azure.com/",
      "apiKey": "your-api-key",
      "deploymentName": "gpt-4",
      "apiVersion": "2024-02-15-preview",
      "isActive": true
    }
  ]
}
```

### System Configuration / 系统配置

#### Reasoning Parameters / 推理参数
- **Default Iterations**: 5
- **Quality Thresholds**: 0.7 (minimum), 0.9 (excellent)
- **Timeout Settings**: 30 seconds per reasoning step
- **Retry Logic**: 3 attempts for failed operations

#### Performance Settings / 性能设置
- **Concurrent Sessions**: Maximum 10 active sessions
- **Memory Limits**: 1GB per session
- **Storage Quotas**: 100MB per session directory
- **Cleanup Policies**: Archive sessions older than 30 days

---

## Troubleshooting / 故障排除

### Common Issues / 常见问题

#### 1. System Won't Initialize / 系统无法初始化

**Symptoms / 症状:**
- "System not initialized" message persists
- Initialize button doesn't respond
- Error messages in browser console

**Solutions / 解决方案:**

**English:**
1. Check browser console for JavaScript errors
2. Verify API connectivity to `http://localhost:5001`
3. Ensure Orleans cluster is running properly
4. Check LLM API keys and endpoints
5. Restart the application host

**中文:**
1. 检查浏览器控制台的JavaScript错误
2. 验证API连接到`http://localhost:5001`
3. 确保Orleans集群正常运行
4. 检查LLM API密钥和端点
5. 重启应用程序主机

#### 2. Empty Thinking Process / 思考过程为空

**Symptoms / 症状:**
- Thinking process tab shows no steps
- API returns empty `thinkingSteps` array
- Session completes but no thinking recorded

**Solutions / 解决方案:**

**English:**
1. Verify session ID propagation in reasoning methods
2. Check event publishing from `AutoReasoningAIGAgent`
3. Ensure `HandleThinkingStepsGeneratedAsync` is working
4. Confirm GAgent ID consistency between components

**中文:**
1. 验证推理方法中的会话ID传播
2. 检查`AutoReasoningAIGAgent`的事件发布
3. 确保`HandleThinkingStepsGeneratedAsync`正常工作
4. 确认组件间GAgent ID的一致性

#### 3. Theory Generation Fails / 理论生成失败

**Symptoms / 症状:**
- Session starts but no theories generated
- Error messages about LLM communication
- Timeout errors during reasoning

**Solutions / 解决方案:**

**English:**
1. Verify LLM API connectivity and credentials
2. Check rate limits and quotas
3. Ensure adequate timeout settings
4. Validate input parameters and prompts
5. Monitor system resources (CPU, memory)

**中文:**
1. 验证LLM API连接和凭据
2. 检查速率限制和配额
3. 确保适当的超时设置
4. 验证输入参数和提示
5. 监控系统资源（CPU、内存）

#### 4. Python Verification Errors / Python验证错误

**Symptoms / 症状:**
- Theories generated but verification fails
- Python execution errors
- Package installation failures

**Solutions / 解决方案:**

**English:**
1. Check Python environment setup
2. Verify required packages are installed
3. Review generated Python code for syntax errors
4. Ensure virtual environment isolation
5. Check system permissions for package installation

**中文:**
1. 检查Python环境设置
2. 验证所需包已安装
3. 检查生成的Python代码语法错误
4. 确保虚拟环境隔离
5. 检查包安装的系统权限

### Debugging Tools / 调试工具

#### 1. Browser Developer Tools / 浏览器开发者工具
- Network tab: Monitor API requests/responses
- Console tab: JavaScript errors and logs
- Application tab: Local storage and session data

#### 2. System Logs / 系统日志
- Orleans silo logs for distributed system issues
- GAgent execution logs for component-specific problems
- LLM communication logs for AI-related errors

#### 3. API Testing / API测试
```bash
# Test system status
curl http://localhost:5001/api/TheoryReasoningDemo/system-status

# Test theory retrieval
curl http://localhost:5001/api/TheoryReasoningDemo/theories

# Test thinking process
curl http://localhost:5001/api/TheoryReasoningDemo/thinking-process/all
```

### Performance Optimization / 性能优化

#### 1. Session Management / 会话管理
- Limit concurrent sessions based on system capacity
- Implement session cleanup for old/completed sessions
- Use pagination for large theory collections

#### 2. Memory Management / 内存管理
- Regular garbage collection for Orleans grains
- Limit thinking step history per session
- Implement data compression for long-term storage

#### 3. Network Optimization / 网络优化
- Cache frequently accessed theories
- Implement API response compression
- Use WebSocket connections for real-time updates

---

## Advanced Features / 高级功能

### 1. Theory Dependency Analysis / 理论依赖分析

The system automatically tracks and analyzes dependencies between theories:

```csharp
// Example: Theory P2-1 depends on A1 and D1-1
var dependencies = await knowledgeAgent.GetDependenciesAsync("P2-1");
// Returns: ["A1", "D1-1"]

var dependents = await knowledgeAgent.GetDependentsAsync("A1");  
// Returns: All theories that depend on A1
```

### 2. Quality Metrics / 质量指标

Automated quality assessment using multiple criteria:

- **Logical Consistency**: Contradiction detection
- **Mathematical Validity**: Formal expression correctness
- **Verification Success**: Python code execution results
- **Novelty Score**: Uniqueness compared to existing theories
- **Clarity Score**: Natural language comprehensibility

### 3. Batch Processing / 批处理

Support for processing multiple reasoning sessions:

```csharp
var batchConfig = new BatchReasoningConfig
{
    SessionConfigs = multipleConfigs,
    MaxConcurrency = 3,
    TimeoutPerSession = TimeSpan.FromMinutes(30)
};

var results = await coordinator.ExecuteBatchReasoningAsync(batchConfig);
```

### 4. Export Formats / 导出格式

Multiple export options for theories and sessions:

- **Markdown**: Human-readable documentation
- **LaTeX**: Academic paper integration  
- **JSON**: Programmatic access
- **PDF**: Formatted reports
- **DOCX**: Microsoft Word documents

---

## Integration Guide / 集成指南

### 1. Embedding in Other Applications / 嵌入其他应用

```javascript
// Embed the reasoning engine in your web application
<iframe src="http://localhost:5001/demos/theory-reasoning-demo.html" 
        width="100%" height="800px" frameborder="0">
</iframe>

// Or use the API directly
const reasoningClient = new TheoryReasoningClient('http://localhost:5001');
const session = await reasoningClient.startReasoning(config);
```

### 2. Custom GAgent Development / 自定义GAgent开发

Create specialized reasoning agents for specific domains:

```csharp
[GAgent("custom-reasoning", "my-domain")]
public class CustomReasoningGAgent : GAgentBase<CustomState, CustomStateLogEvent>, ICustomReasoningGAgent
{
    // Implement domain-specific reasoning logic
    public async Task<List<string>> PerformDomainSpecificReasoningAsync(CustomInput input)
    {
        // Your custom reasoning implementation
    }
}
```

### 3. Third-party LLM Integration / 第三方LLM集成

Extend the system to support additional LLM providers:

```csharp
public class CustomLLMProvider : ILLMProvider
{
    public async Task<string> GenerateCompletionAsync(string prompt, LLMConfig config)
    {
        // Implement your LLM provider integration
    }
}
```

---

## Conclusion / 结论

### English

The Theory Reasoning Engine represents a comprehensive solution for automated mathematical theory generation and validation. Through its distributed architecture, AI-powered reasoning capabilities, and intuitive user interface, it enables researchers and educators to explore mathematical concepts systematically and efficiently.

Key benefits include:
- **Automated Discovery**: AI-driven theory generation reduces manual effort
- **Quality Assurance**: Integrated verification ensures theory validity
- **Transparency**: Complete thinking process visibility builds trust
- **Flexibility**: Multiple reasoning methods support diverse problem types
- **Persistence**: Session management and export enable long-term research

The system's modular design allows for easy extension and customization, making it suitable for various academic and research applications.

### 中文

理论推理引擎代表了自动化数学理论生成和验证的综合解决方案。通过其分布式架构、AI驱动的推理能力和直观的用户界面，它使研究人员和教育工作者能够系统化和高效地探索数学概念。

主要优势包括：
- **自动发现**：AI驱动的理论生成减少人工工作
- **质量保证**：集成验证确保理论有效性
- **透明度**：完整的思考过程可见性建立信任
- **灵活性**：多种推理方法支持不同问题类型
- **持久性**：会话管理和导出支持长期研究

系统的模块化设计允许轻松扩展和自定义，使其适用于各种学术和研究应用。

---

## Appendix / 附录

### A. Theory Type Reference / 理论类型参考

| Type | Full Name | Description (EN) | Description (CN) |
|------|-----------|------------------|------------------|
| A | Axiom | Fundamental assumptions | 基本假设 |
| T | Theorem | Proven mathematical statements | 已证明的数学陈述 |
| L | Lemma | Supporting propositions for theorems | 定理的支持命题 |
| P | Proposition | General mathematical statements | 一般数学陈述 |
| C | Corollary | Direct consequences of theorems | 定理的直接后果 |
| D | Definition | Formal definitions of concepts | 概念的形式定义 |

### B. Reasoning Method Comparison / 推理方法比较

| Method | Strength | Use Cases | Example Domain |
|--------|----------|-----------|----------------|
| Deductive | Logical certainty | Theorem proving | Geometry |
| Inductive | Pattern discovery | Empirical observations | Statistics |
| Abductive | Best explanation | Hypothesis formation | Physics |
| Analogical | Cross-domain insight | Knowledge transfer | Mathematics |

### C. API Status Codes / API状态码

| Code | Meaning | Description |
|------|---------|-------------|
| 200 | Success | Request completed successfully |
| 400 | Bad Request | Invalid parameters or request format |
| 404 | Not Found | Requested resource does not exist |
| 500 | Internal Error | Server-side processing error |
| 503 | Service Unavailable | System temporarily unavailable |

### D. Configuration Examples / 配置示例

#### Beginner Configuration / 初学者配置
```json
{
  "maxIterations": 3,
  "qualityThreshold": 0.6,
  "enableAutoReview": true,
  "enableAutoRevision": false,
  "targetDomain": "basic_algebra",
  "enabledReasoningMethods": ["deductive"]
}
```

#### Advanced Configuration / 高级配置
```json
{
  "maxIterations": 10,
  "qualityThreshold": 0.85,
  "enableAutoReview": true,
  "enableAutoRevision": true,
  "targetDomain": "advanced_topology",
  "enabledReasoningMethods": ["deductive", "inductive", "abductive", "analogical"],
  "customParameters": {
    "verificationTimeout": 60,
    "maxDependencyDepth": 5,
    "allowExperimental": true
  }
}
```

---

*Document Version: 1.0*  
*Last Updated: January 15, 2024*  
*Language: English & Chinese*