// Theory Reasoning Engine Demo JavaScript

// 中英文文本字典
const I18N_TEXTS = {
    en: {
        // 页面标题和导航
        pageTitle: 'Theory Reasoning Engine Demo',
        navTitle: 'Theory Reasoning Engine',
        systemStatus: 'System Status',
        systemStatusInitializing: 'Initializing...',
        systemReady: 'System Ready',
        systemInitialized: 'System Initialized',
        systemHealthy: 'System Healthy',
        systemIssues: 'System Issues',
        systemError: 'Connection Error',
        initializationFailed: 'Initialization Failed',
        reasoningActive: 'Reasoning Active',
        
        // 控制面板
        controlPanel: 'Control Panel',
        initializeSystem: 'Initialize System',
        reasoningMethods: 'Reasoning Methods',
        deductive: 'Deductive',
        deductiveDesc: 'Logical deduction from premises',
        inductive: 'Inductive',
        inductiveDesc: 'Pattern recognition and generalization',
        abductive: 'Abductive',
        abductiveDesc: 'Best explanation inference',
        analogical: 'Analogical',
        analogicalDesc: 'Cross-domain pattern transfer',
        maxIterations: 'Max Iterations',
        qualityThreshold: 'Quality Threshold',
        targetDomain: 'Target Domain',
        llmSystem: 'LLM System',
        llmSystemDesc: 'Select the AI model for reasoning',
        autoReview: 'Auto Review',
        startReasoning: 'Start Reasoning',
        stopReasoning: 'Stop Reasoning',
        
        // 系统指标
        systemMetrics: 'System Metrics',
        totalTheories: 'Total Theories',
        accepted: 'Accepted',
        activeSessions: 'Active Sessions',
        successRate: 'Success Rate',
        
        // 标签页
        dashboard: 'Dashboard',
        generatedTheories: 'Generated Theories',
        reasoningSessions: 'Reasoning Sessions',
        manualTools: 'Manual Tools',
        
        // 仪表板
        recentActivity: 'Recent Activity',
        noRecentActivity: 'No recent activity',
        theoryOverview: 'Ψ Theory Overview',
        binaryUniverseTheory: 'Binary Universe Theory (Ψ Theory)',
        theoryDescription: 'The Ψ theory is a comprehensive mathematical framework that describes the universe as an infinite recursive structure of binary distinctions. Starting from a single axiom (A1), it derives a complete theory system including corollaries, propositions, theorems, and definitions.',
        coreComponents: 'Core Components:',
        keyFeatures: 'Key Features:',
        selfReferentialFunction: 'Self-Referential Function',
        
        // 理论列表
        refresh: 'Refresh',
        noTheoriesYet: 'No theories generated yet. Start a reasoning session to see results.',
        verified: 'Verified',
        unverified: 'Unverified',
        created: 'Created',
        
        // 会话
        noActiveSessions: 'No reasoning sessions yet. Start a session to begin automatic theory generation.',
        progress: 'Progress',
        iteration: 'Iteration',
        started: 'Started',
        
        // 手动工具
        formalizationTool: 'Formalization Tool',
        theoryContent: 'Theory Content',
        enterTheory: 'Enter natural language theory...',
        targetTool: 'Target Tool',
        formalize: 'Formalize',
        verificationTool: 'Verification Tool',
        enterVerifyTheory: 'Enter theory to verify...',
        formalExpressionOptional: 'Formal Expression (Optional)',
        enterFormalExpression: 'Enter formal expression...',
        verify: 'Verify',
        equivalenceReview: 'Equivalence Review',
        theory: 'Theory',
        naturalLanguageTheory: 'Natural language theory...',
        formal: 'Formal',
        formalExpression: 'Formal expression...',
        python: 'Python',
        pythonCode: 'Python code...',
        review: 'Review',
        toolResults: 'Tool Results',
        useToolsMessage: 'Use the tools above to see results here.',
        
        // 加载和状态
        processing: 'Processing...',
        initializingEngine: 'Initializing Theory Reasoning Engine...',
        startingSession: 'Starting reasoning session...',
        stoppingSession: 'Stopping reasoning session...',
        formalizingTheory: 'Formalizing theory...',
        verifyingTheory: 'Verifying theory...',
        reviewingEquivalence: 'Reviewing equivalence...',
        
        // 消息和警告
        pleaseInitializeFirst: 'Please initialize the system first.',
        selectReasoningMethod: 'Please select at least one reasoning method.',
        noActiveSession: 'No active reasoning session.',
        selectLLMFirst: 'Please select an LLM system first.',
        enterTheoryToFormalize: 'Please enter theory content to formalize.',
        enterTheoryToVerify: 'Please enter theory content to verify.',
        fillAllFieldsForReview: 'Please fill in all three fields for equivalence review.',
        noLLMConfigsFound: 'No LLM configurations found. Please configure LLM models first.',
        loadingLLMConfigs: 'Loading LLM configurations...',
        failedToLoadLLMConfigs: 'Failed to load LLM configurations',
        
        // 结果消息
        systemInitializedSuccess: 'System initialized successfully',
        reasoningSessionStopped: 'Reasoning session stopped.',
        manualFormalizationCompleted: 'Manual formalization completed',
        manualVerificationCompleted: 'Manual verification completed',
        manualEquivalenceReviewCompleted: 'Manual equivalence review completed',
        
        // 错误消息前缀
        failedToInitialize: 'Failed to initialize system: ',
        errorInitializing: 'Error initializing system: ',
        failedToStartReasoning: 'Failed to start reasoning: ',
        errorStartingReasoning: 'Error starting reasoning: ',
        failedToStopReasoning: 'Failed to stop reasoning: ',
        errorStoppingReasoning: 'Error stopping reasoning: ',
        formalizationFailed: 'Formalization failed: ',
        errorFormalizing: 'Error formalizing theory: ',
        verificationFailed: 'Verification failed: ',
        errorVerifying: 'Error verifying theory: ',
        equivalenceReviewFailed: 'Equivalence review failed: ',
        errorReviewingEquivalence: 'Error reviewing equivalence: ',
        failedToLoadLLMConfigsError: 'Failed to load LLM configurations: ',
        
        // 工具结果标题
        formalizationResult: 'Formalization Result',
        verificationResult: 'Verification Result',
        equivalenceReviewResult: 'Equivalence Review'
    },
    zh: {
        // 页面标题和导航
        pageTitle: '理论推理引擎演示',
        navTitle: '理论推理引擎',
        systemStatus: '系统状态',
        systemStatusInitializing: '初始化中...',
        systemReady: '系统就绪',
        systemInitialized: '系统已初始化',
        systemHealthy: '系统健康',
        systemIssues: '系统异常',
        systemError: '连接错误',
        initializationFailed: '初始化失败',
        reasoningActive: '推理运行中',
        
        // 控制面板
        controlPanel: '控制面板',
        initializeSystem: '初始化系统',
        reasoningMethods: '推理方法',
        deductive: '演绎推理',
        deductiveDesc: '从前提进行逻辑演绎',
        inductive: '归纳推理',
        inductiveDesc: '模式识别和泛化',
        abductive: '溯因推理',
        abductiveDesc: '最佳解释推理',
        analogical: '类比推理',
        analogicalDesc: '跨领域模式转移',
        maxIterations: '最大迭代次数',
        qualityThreshold: '质量阈值',
        targetDomain: '目标领域',
        llmSystem: 'LLM系统',
        llmSystemDesc: '选择用于推理的AI模型',
        autoReview: '自动审查',
        startReasoning: '开始推理',
        stopReasoning: '停止推理',
        
        // 系统指标
        systemMetrics: '系统指标',
        totalTheories: '理论总数',
        accepted: '已接受',
        activeSessions: '活跃会话',
        successRate: '成功率',
        
        // 标签页
        dashboard: '仪表板',
        generatedTheories: '生成的理论',
        reasoningSessions: '推理会话',
        manualTools: '手动工具',
        
        // 仪表板
        recentActivity: '最近活动',
        noRecentActivity: '无最近活动',
        theoryOverview: 'Ψ理论概览',
        binaryUniverseTheory: '二元宇宙理论（Ψ理论）',
        theoryDescription: 'Ψ理论是一个综合性的数学框架，将宇宙描述为二元区分的无限递归结构。从单一公理（A1）开始，它推导出一个完整的理论体系，包括推论、命题、定理和定义。',
        coreComponents: '核心组件：',
        keyFeatures: '关键特性：',
        selfReferentialFunction: '自指函数',
        
        // 理论列表
        refresh: '刷新',
        noTheoriesYet: '尚未生成理论。开始推理会话以查看结果。',
        verified: '已验证',
        unverified: '未验证',
        created: '创建时间',
        
        // 会话
        noActiveSessions: '尚无推理会话。启动会话以开始自动理论生成。',
        progress: '进度',
        iteration: '迭代',
        started: '开始时间',
        
        // 手动工具
        formalizationTool: '形式化工具',
        theoryContent: '理论内容',
        enterTheory: '输入自然语言理论...',
        targetTool: '目标工具',
        formalize: '形式化',
        verificationTool: '验证工具',
        enterVerifyTheory: '输入要验证的理论...',
        formalExpressionOptional: '形式表达式（可选）',
        enterFormalExpression: '输入形式表达式...',
        verify: '验证',
        equivalenceReview: '等价审查',
        theory: '理论',
        naturalLanguageTheory: '自然语言理论...',
        formal: '形式化',
        formalExpression: '形式表达式...',
        python: 'Python',
        pythonCode: 'Python代码...',
        review: '审查',
        toolResults: '工具结果',
        useToolsMessage: '使用上方工具查看结果。',
        
        // 加载和状态
        processing: '处理中...',
        initializingEngine: '正在初始化理论推理引擎...',
        startingSession: '正在启动推理会话...',
        stoppingSession: '正在停止推理会话...',
        formalizingTheory: '正在形式化理论...',
        verifyingTheory: '正在验证理论...',
        reviewingEquivalence: '正在审查等价性...',
        
        // 消息和警告
        pleaseInitializeFirst: '请先初始化系统。',
        selectReasoningMethod: '请至少选择一种推理方法。',
        noActiveSession: '没有活跃的推理会话。',
        selectLLMFirst: '请先选择LLM系统。',
        enterTheoryToFormalize: '请输入要形式化的理论内容。',
        enterTheoryToVerify: '请输入要验证的理论内容。',
        fillAllFieldsForReview: '请填写所有三个字段进行等价审查。',
        noLLMConfigsFound: '未找到LLM配置。请先配置LLM模型。',
        loadingLLMConfigs: '正在加载LLM配置...',
        failedToLoadLLMConfigs: '加载LLM配置失败',
        
        // 结果消息
        systemInitializedSuccess: '系统初始化成功',
        reasoningSessionStopped: '推理会话已停止。',
        manualFormalizationCompleted: '手动形式化完成',
        manualVerificationCompleted: '手动验证完成',
        manualEquivalenceReviewCompleted: '手动等价审查完成',
        
        // 错误消息前缀
        failedToInitialize: '系统初始化失败：',
        errorInitializing: '系统初始化错误：',
        failedToStartReasoning: '推理启动失败：',
        errorStartingReasoning: '推理启动错误：',
        failedToStopReasoning: '推理停止失败：',
        errorStoppingReasoning: '推理停止错误：',
        formalizationFailed: '形式化失败：',
        errorFormalizing: '形式化错误：',
        verificationFailed: '验证失败：',
        errorVerifying: '验证错误：',
        equivalenceReviewFailed: '等价审查失败：',
        errorReviewingEquivalence: '等价审查错误：',
        failedToLoadLLMConfigsError: 'LLM配置加载失败：',
        
        // 工具结果标题
        formalizationResult: '形式化结果',
        verificationResult: '验证结果',
        equivalenceReviewResult: '等价审查结果'
    }
};

class TheoryReasoningDemo {
    constructor() {
        this.currentSessionId = null;
        this.apiBase = '/api/TheoryReasoningDemo';
        this.systemInitialized = false;
        this.refreshInterval = null;
        this.thinkingRefreshInterval = null;
        this.currentLanguage = this.detectLanguage();
        
        this.initializeEventListeners();
        this.loadAvailableLLMs();
        this.applyTranslations();
        
        // Initialize thinking tab and check system status on load
        setTimeout(() => {
            this.initializeThinkingTab();
            this.checkInitialSystemStatus();
        }, 500);
    }
    
    detectLanguage() {
        const urlParams = new URLSearchParams(window.location.search);
        const langParam = urlParams.get('lang');
        return (langParam === 'zh') ? 'zh' : 'en';
    }
    
    // 紧急重置功能 - 如果UI卡住可以调用此方法
    emergencyReset() {
        console.log('🚨 Emergency reset initiated');
        
        // 强制隐藏加载modal - 使用最强力的方法
        this.forceHideModal();
        
        // 重置系统状态
        this.systemInitialized = false;
        this.currentSessionId = null;
        
        // 更新UI状态
        updateSystemStatus('idle', this.getText('systemReady'));
        const startBtn = document.getElementById('startReasoningBtn');
        const stopBtn = document.getElementById('stopReasoningBtn');
        
        if (startBtn) startBtn.disabled = true;
        if (stopBtn) stopBtn.disabled = true;
        
        // 清除刷新间隔
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
        
        this.showAlert('info', 'System has been reset. Please try initializing again.');
        console.log('✅ Emergency reset completed');
    }
    
    getText(key) {
        return I18N_TEXTS[this.currentLanguage][key] || I18N_TEXTS['en'][key] || key;
    }
    
    // Generate detailed system report from API data
    generateSystemReport(data) {
        const timestamp = new Date().toLocaleString('en-US', {
            timeZone: 'UTC',
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        });
        
        let report = `=== Theory Reasoning Engine System Report ===\n`;
        report += `Generated at: ${timestamp} UTC\n\n`;
        
        // System Status
        report += `System Status:\n`;
        report += `  Current Status: ${data.status || 'unknown'}\n`;
        report += `  Initialized: ${data.initialized ? 'Yes' : 'No'}\n`;
        report += `  Message: ${data.message || 'No message'}\n\n`;
        
        // Available Agents
        if (data.availableAgents && data.availableAgents.length > 0) {
            report += `Available Agents:\n`;
            data.availableAgents.forEach(agent => {
                report += `  - ${agent}\n`;
            });
            report += `\n`;
        }
        
        // System Health
        report += `System Health: ${data.initialized ? 'HEALTHY' : 'REQUIRES INITIALIZATION'}\n`;
        
        // Additional info if available
        if (data.status === 'reasoning') {
            report += `\nNote: Reasoning session is currently active.\n`;
        }
        
        return report;
    }
    
    // Enhance system report with additional statistics
    async enhanceSystemReportWithStats(reportElement, statusData) {
        try {
            // Fetch additional data
            const [sessionsResponse, theoriesResponse] = await Promise.all([
                fetch(`${this.apiBase}/sessions`),
                fetch(`${this.apiBase}/theories`)
            ]);
            
            let sessionsData = null, theoriesData = null;
            
            if (sessionsResponse.ok) {
                sessionsData = await sessionsResponse.json();
            }
            
            if (theoriesResponse.ok) {
                theoriesData = await theoriesResponse.json();
            }
            
            // Generate enhanced report
            const enhancedReport = this.generateEnhancedSystemReport(statusData, sessionsData, theoriesData);
            reportElement.textContent = enhancedReport;
            
        } catch (error) {
            console.error('❌ Error enhancing system report:', error);
            // Keep the basic report if enhancement fails
        }
    }
    
    // Generate enhanced system report with all available data
    generateEnhancedSystemReport(statusData, sessionsData, theoriesData) {
        const timestamp = new Date().toLocaleString('en-US', {
            timeZone: 'UTC',
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        });
        
        let report = `=== Theory Reasoning Engine System Report ===\n`;
        report += `Generated at: ${timestamp} UTC\n\n`;
        
        // System Status
        report += `System Status:\n`;
        report += `  Current Status: ${statusData.status || 'unknown'}\n`;
        report += `  Initialized: ${statusData.initialized ? 'Yes' : 'No'}\n`;
        report += `  Message: ${statusData.message || 'No message'}\n\n`;
        
        // Session Statistics
        if (sessionsData && sessionsData.success) {
            const activeSessions = sessionsData.sessions ? sessionsData.sessions.filter(s => s.status === 'active').length : 0;
            const completedSessions = sessionsData.sessions ? sessionsData.sessions.filter(s => s.status === 'completed').length : 0;
            const totalSessions = sessionsData.sessions ? sessionsData.sessions.length : 0;
            
            report += `Session Statistics:\n`;
            report += `  Active Sessions: ${activeSessions}\n`;
            report += `  Completed Sessions: ${completedSessions}\n`;
            report += `  Total Sessions: ${totalSessions}\n\n`;
        }
        
        // Theory Generation Statistics
        if (theoriesData && theoriesData.success) {
            const totalTheories = theoriesData.theories ? theoriesData.theories.length : 0;
            const verifiedTheories = theoriesData.theories ? theoriesData.theories.filter(t => t.isVerified).length : 0;
            const successRate = totalTheories > 0 ? Math.round((verifiedTheories / totalTheories) * 100) : 0;
            
            report += `Theory Generation Statistics:\n`;
            report += `  Total Theories Generated: ${totalTheories}\n`;
            report += `  Total Theories Verified: ${verifiedTheories}\n`;
            report += `  Success Rate: ${successRate}%\n\n`;
        }
        
        // Available Agents
        if (statusData.availableAgents && statusData.availableAgents.length > 0) {
            report += `Available Agents:\n`;
            statusData.availableAgents.forEach(agent => {
                report += `  - ${agent}\n`;
            });
            report += `\n`;
        }
        
        // System Health
        report += `System Health: ${statusData.initialized ? 'HEALTHY' : 'REQUIRES INITIALIZATION'}\n`;
        
        // Additional info if available
        if (statusData.status === 'reasoning') {
            report += `\nNote: Reasoning session is currently active.\n`;
        }
        
        return report;
    }
    
    // Check initial system status on page load
    async checkInitialSystemStatus() {
        try {
            console.log('🔍 Checking initial system status...');
            
            // Set initial status
            updateSystemStatus('checking', 'Checking system status...');
            
            const response = await fetch(`${this.apiBase}/system-status`);
            
            if (response.ok) {
                const data = await response.json();
                console.log('📊 System status response:', data);
                
                if (data.success && data.initialized) {
                    this.systemInitialized = true;
                    updateSystemStatus('ready', this.getText('systemReady'));
                    
                    // Generate detailed system report
                    const reportElement = document.getElementById('systemReport');
                    if (reportElement) {
                        reportElement.textContent = this.generateSystemReport(data);
                        
                        // Load additional statistics and update report
                        this.enhanceSystemReportWithStats(reportElement, data);
                    }
                    
                    // Enable start button
                    const startBtn = document.getElementById('startReasoningBtn');
                    if (startBtn) {
                        startBtn.disabled = false;
                    }
                    
                    this.showAlert('success', 'System is ready for reasoning');
                    
                    // Load existing data
                    await this.loadExistingSessions();
                    await this.loadExistingTheories();
                    await this.loadThinkingProcess();
                    
                } else {
                    this.systemInitialized = false;
                    updateSystemStatus('not-initialized', 'System not initialized. Please click Initialize.');
                    
                    // Update system report with current status  
                    const reportElement = document.getElementById('systemReport');
                    if (reportElement) {
                        reportElement.textContent = data.message || 'System needs initialization. Please click Initialize button.';
                    }
                    
                    // Disable start button
                    const startBtn = document.getElementById('startReasoningBtn');
                    if (startBtn) {
                        startBtn.disabled = true;
                    }
                }
            } else {
                console.error('❌ Failed to check system status:', response.status);
                updateSystemStatus('error', 'Error checking system status');
                
                // Update system report with error
                const reportElement = document.getElementById('systemReport');
                if (reportElement) {
                    reportElement.textContent = 'Error checking system status. Please refresh the page.';
                }
            }
        } catch (error) {
            console.error('❌ Error checking initial system status:', error);
            updateSystemStatus('error', 'Error checking system status');
            
            // Update system report with error
            const reportElement = document.getElementById('systemReport');
            if (reportElement) {
                reportElement.textContent = 'Error checking system status. Please refresh the page.';
            }
        }
    }

    // Load existing sessions
    async loadExistingSessions() {
        try {
            const response = await fetch(`${this.apiBase}/sessions`);
            if (response.ok) {
                const data = await response.json();
                console.log('📂 Loaded sessions:', data);
                
                // Update session display if needed
                if (data.success && data.sessions) {
                    this.displaySessions(data.sessions);
                }
            }
        } catch (error) {
            console.error('❌ Error loading sessions:', error);
        }
    }

    // Load existing theories
    async loadExistingTheories() {
        try {
            const response = await fetch(`${this.apiBase}/theories`);
            if (response.ok) {
                const data = await response.json();
                console.log('📚 Loaded theories:', data);
                
                // Update theory display if needed
                if (data.success && data.theories) {
                    this.displayTheories(data.theories);
                }
            }
        } catch (error) {
            console.error('❌ Error loading theories:', error);
        }
    }

    // Load thinking process
    async loadThinkingProcess() {
        try {
            const response = await fetch(`${this.apiBase}/thinking-process/all`);
            if (response.ok) {
                const data = await response.json();
                console.log('🧠 Loaded thinking process:', data);
                
                // Update thinking process display if needed
                if (data.success && data.thinkingSteps) {
                    this.displayThinkingSteps(data.thinkingSteps);
                }
            }
        } catch (error) {
            console.error('❌ Error loading thinking process:', error);
        }
    }

    // Display sessions (placeholder)
    displaySessions(sessions) {
        console.log('📊 Displaying sessions:', sessions.length, 'sessions');
        // Implementation would update the sessions UI
    }

    // Display theories (placeholder)
    displayTheories(theories) {
        console.log('📚 Displaying theories:', theories.length, 'theories');
        // Implementation would update the theories UI
    }

    // Display thinking steps (placeholder)
    displayThinkingSteps(steps) {
        console.log('🧠 Displaying thinking steps:', steps.length, 'steps');
        // Implementation would update the thinking process UI
    }

    // Initialize thinking tab
    initializeThinkingTab() {
        const refreshBtn = document.getElementById('refreshThinkingBtn');
        const autoRefreshBtn = document.getElementById('autoRefreshToggle');
        const sessionFilter = document.getElementById('sessionFilter');
        const stepTypeFilter = document.getElementById('stepTypeFilter');

        if (refreshBtn) {
            refreshBtn.addEventListener('click', () => this.loadThinkingProcess());
        }

        if (autoRefreshBtn) {
            autoRefreshBtn.addEventListener('click', () => this.toggleAutoRefresh());
        }

        if (sessionFilter) {
            sessionFilter.addEventListener('change', () => this.filterThinkingSteps());
        }

        if (stepTypeFilter) {
            stepTypeFilter.addEventListener('change', () => this.filterThinkingSteps());
        }

        console.log('🧠 Thinking tab initialized');
    }

    // Toggle auto refresh for thinking process
    toggleAutoRefresh() {
        const button = document.getElementById('autoRefreshToggle');
        if (!button) return;

        const isAuto = button.getAttribute('data-auto') === 'true';
        
        if (isAuto) {
            // Stop auto refresh
            if (this.thinkingRefreshInterval) {
                clearInterval(this.thinkingRefreshInterval);
                this.thinkingRefreshInterval = null;
            }
            button.setAttribute('data-auto', 'false');
            button.innerHTML = '<i class="fas fa-play me-2"></i>Auto Refresh';
            button.className = 'btn btn-outline-secondary';
        } else {
            // Start auto refresh
            this.thinkingRefreshInterval = setInterval(() => this.loadThinkingProcess(), 5000);
            button.setAttribute('data-auto', 'true');
            button.innerHTML = '<i class="fas fa-pause me-2"></i>Stop Auto';
            button.className = 'btn btn-success';
        }
    }

    // Filter thinking steps
    filterThinkingSteps() {
        // Implementation for filtering thinking steps
        console.log('🔍 Filtering thinking steps...');
    }

    applyTranslations() {
        // 更新页面标题
        document.title = this.getText('pageTitle');
        
        // 更新导航栏标题
        const navTitle = document.querySelector('.navbar-brand');
        if (navTitle) {
            navTitle.innerHTML = `<i class="fas fa-brain me-2"></i>${this.getText('navTitle')}`;
        }
        
        // 更新系统状态文本
        const statusText = document.getElementById('systemStatusText');
        if (statusText && statusText.textContent === 'Initializing...') {
            statusText.textContent = this.getText('systemStatusInitializing');
        }
        
        // 更新所有具有data-i18n属性的元素
        document.querySelectorAll('[data-i18n]').forEach(element => {
            const key = element.getAttribute('data-i18n');
            if (key && this.getText(key) !== key) {
                if (element.tagName === 'INPUT' && (element.type === 'button' || element.type === 'submit')) {
                    element.value = this.getText(key);
                } else if (element.tagName === 'INPUT' && element.hasAttribute('placeholder')) {
                    element.placeholder = this.getText(key);
                } else if (element.tagName === 'TEXTAREA' && element.hasAttribute('placeholder')) {
                    element.placeholder = this.getText(key);
                } else if (element.tagName === 'OPTION') {
                    element.textContent = this.getText(key);
                } else {
                    element.textContent = this.getText(key);
                }
            }
        });
        
        // 更新所有具有data-i18n-placeholder属性的元素
        document.querySelectorAll('[data-i18n-placeholder]').forEach(element => {
            const key = element.getAttribute('data-i18n-placeholder');
            if (key && this.getText(key) !== key) {
                element.placeholder = this.getText(key);
            }
        });
        
        // 手动更新一些特殊的元素
        this.updateSpecialElements();
    }
    
    updateSpecialElements() {
        // 更新控制面板标题
        const controlPanelHeader = document.querySelector('.card-header h5');
        if (controlPanelHeader && controlPanelHeader.innerHTML.includes('Control Panel')) {
            controlPanelHeader.innerHTML = `<i class="fas fa-cogs me-2"></i>${this.getText('controlPanel')}`;
        }
        
        // 更新系统指标标题
        const metricsHeader = document.querySelector('.card-header h6');
        if (metricsHeader && metricsHeader.innerHTML.includes('System Metrics')) {
            metricsHeader.innerHTML = `<i class="fas fa-chart-bar me-2"></i>${this.getText('systemMetrics')}`;
        }
        
        // 更新标签页文本
        const tabs = document.querySelectorAll('#mainTabs .nav-link');
        tabs.forEach(tab => {
            const href = tab.getAttribute('href');
            if (href === '#dashboard') {
                tab.innerHTML = `<i class="fas fa-tachometer-alt me-2"></i>${this.getText('dashboard')}`;
            } else if (href === '#theories') {
                tab.innerHTML = `<i class="fas fa-lightbulb me-2"></i>${this.getText('generatedTheories')}`;
            } else if (href === '#sessions') {
                tab.innerHTML = `<i class="fas fa-tasks me-2"></i>${this.getText('reasoningSessions')}`;
            } else if (href === '#tools') {
                tab.innerHTML = `<i class="fas fa-tools me-2"></i>${this.getText('manualTools')}`;
            }
        });
        
        // 更新按钮文本
        const initBtn = document.getElementById('initializeSystemBtn');
        if (initBtn) {
            initBtn.innerHTML = `<i class="fas fa-play-circle me-2"></i>${this.getText('initializeSystem')}`;
        }
        
        const startBtn = document.getElementById('startReasoningBtn');
        if (startBtn) {
            startBtn.innerHTML = `<i class="fas fa-rocket me-2"></i>${this.getText('startReasoning')}`;
        }
        
        const stopBtn = document.getElementById('stopReasoningBtn');
        if (stopBtn) {
            stopBtn.innerHTML = `<i class="fas fa-stop me-2"></i>${this.getText('stopReasoning')}`;
        }
        
        // 更新理论概览内容
        this.updateTheoryOverview();
        
        // 更新占位符内容
        this.updatePlaceholders();
    }
    
    updateTheoryOverview() {
        const theoryOverviewTitle = document.querySelector('.card-header h6');
        if (theoryOverviewTitle && theoryOverviewTitle.innerHTML.includes('Theory Overview')) {
            theoryOverviewTitle.innerHTML = `<i class="fas fa-brain me-2"></i>${this.getText('theoryOverview')}`;
        }
        
        const theoryTitle = document.querySelector('.card-body h6');
        if (theoryTitle && theoryTitle.textContent.includes('Binary Universe Theory')) {
            theoryTitle.textContent = this.getText('binaryUniverseTheory');
        }
        
        const theoryDesc = document.querySelector('.card-body p.text-muted');
        if (theoryDesc && theoryDesc.textContent.includes('comprehensive mathematical framework')) {
            theoryDesc.textContent = this.getText('theoryDescription');
        }
    }
    
    updatePlaceholders() {
        // 更新空状态消息
        const emptyTheories = document.querySelector('#theoriesList .col-12.text-center p');
        if (emptyTheories && emptyTheories.textContent.includes('No theories generated yet')) {
            emptyTheories.textContent = this.getText('noTheoriesYet');
        }
        
        const emptySessions = document.querySelector('#sessionsList .text-center p');
        if (emptySessions && emptySessions.textContent.includes('No reasoning sessions yet')) {
            emptySessions.textContent = this.getText('noActiveSessions');
        }
        
        const emptyActivity = document.querySelector('#activityLog .text-muted');
        if (emptyActivity && emptyActivity.textContent === 'No recent activity') {
            emptyActivity.textContent = this.getText('noRecentActivity');
        }
        
        const emptyToolResults = document.querySelector('#toolResults .text-center p');
        if (emptyToolResults && emptyToolResults.textContent.includes('Use the tools above')) {
            emptyToolResults.textContent = this.getText('useToolsMessage');
        }
    }

    initializeEventListeners() {
        // Control panel buttons
        document.getElementById('initializeSystemBtn').addEventListener('click', () => this.initializeSystem());
        document.getElementById('startReasoningBtn').addEventListener('click', () => this.startReasoning());
        document.getElementById('stopReasoningBtn').addEventListener('click', () => this.stopReasoning());
        
        // Refresh buttons
        document.getElementById('refreshTheoriesBtn').addEventListener('click', () => this.refreshTheories());
        document.getElementById('refreshSessionsBtn').addEventListener('click', () => this.refreshSessions());
        
        // Manual tools
        document.getElementById('formalizeBtn').addEventListener('click', () => this.formalizeTheory());
        document.getElementById('verifyBtn').addEventListener('click', () => this.verifyTheory());
        document.getElementById('reviewBtn').addEventListener('click', () => this.reviewEquivalence());
        
        // Reasoning method selection
        document.querySelectorAll('.reasoning-method input[type="checkbox"]').forEach(checkbox => {
            checkbox.addEventListener('change', (e) => {
                const method = e.target.closest('.reasoning-method');
                if (e.target.checked) {
                    method.classList.add('selected');
                } else {
                    method.classList.remove('selected');
                }
            });
        });

        // Initialize selected methods
        document.querySelectorAll('.reasoning-method input[type="checkbox"]:checked').forEach(checkbox => {
            checkbox.closest('.reasoning-method').classList.add('selected');
        });
    }

    async initializeSystem() {
        console.log('🔧 Starting system initialization...');
        
        if (!this.validateLLMConfiguration()) {
            console.log('❌ LLM configuration validation failed');
            return;
        }

        console.log('✅ LLM configuration validation passed');
        this.showLoading(this.getText('initializingEngine'));
        
        try {
            const selectedLLM = document.getElementById('systemLLM').value;
            console.log('📡 Selected LLM:', selectedLLM);
            console.log('🚀 Making API call to:', `${this.apiBase}/initialize`);
            
            const requestBody = { systemLLM: selectedLLM };
            console.log('📤 Request body:', requestBody);
            
            // 添加30秒超时控制
            const controller = new AbortController();
            const timeoutId = setTimeout(() => {
                console.log('⏰ Request timeout, aborting...');
                controller.abort();
            }, 30000); // 30秒超时
            
            const response = await fetch(`${this.apiBase}/initialize`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(requestBody),
                signal: controller.signal
            });
            
            clearTimeout(timeoutId);
            
            console.log('📡 Response status:', response.status);
            console.log('📡 Response ok:', response.ok);
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const result = await response.json();
            console.log('📥 Response result:', result);
            
            if (result.success) {
                console.log('✅ Initialization successful');
                this.systemInitialized = true;
                updateSystemStatus('healthy', this.getText('systemInitialized'));
                
                // 确保按钮状态正确更新
                const startBtn = document.getElementById('startReasoningBtn');
                const stopBtn = document.getElementById('stopReasoningBtn');
                if (startBtn) {
                    startBtn.disabled = false;
                    console.log('✅ Start button enabled');
                }
                if (stopBtn) {
                    stopBtn.disabled = true;
                    console.log('✅ Stop button disabled');
                }
                
                // 更新系统报告
                const reportElement = document.getElementById('systemReport');
                if (reportElement) {
                    reportElement.textContent = result.systemReport;
                    console.log('✅ System report updated');
                } else {
                    console.warn('⚠️ System report element not found');
                }
                
                this.addActivityLog(this.getText('systemInitializedSuccess'), 'success');
                this.startRefreshInterval();
                this.showAlert('success', this.getText('systemInitializedSuccess'));
                console.log('✅ All initialization steps completed');
            } else {
                console.log('❌ Initialization failed:', result.message);
                updateSystemStatus('error', this.getText('initializationFailed'));
                this.showAlert('error', this.getText('failedToInitialize') + result.message);
            }
        } catch (error) {
            console.error('💥 Error during initialization:', error);
            updateSystemStatus('error', this.getText('systemError'));
            this.showAlert('error', this.getText('errorInitializing') + error.message);
        } finally {
            console.log('🔄 Finally block executing - hiding loading indicator');
            try {
                this.hideLoading();
                console.log('✅ Loading indicator hidden successfully');
            } catch (hideError) {
                console.error('❌ Error hiding loading indicator:', hideError);
                // 强制移除modal
                const modal = document.getElementById('loadingModal');
                if (modal) {
                    modal.style.display = 'none';
                    modal.classList.remove('show');
                    document.body.classList.remove('modal-open');
                    // 移除背景遮罩
                    const backdrops = document.querySelectorAll('.modal-backdrop');
                    backdrops.forEach(backdrop => backdrop.remove());
                    console.log('🔧 Force removed modal elements');
                }
            }
        }
    }

    async startReasoning() {
        if (!this.systemInitialized) {
            this.showAlert('warning', this.getText('pleaseInitializeFirst'));
            return;
        }

        if (!this.validateLLMConfiguration()) {
            return;
        }

        const enabledMethods = Array.from(document.querySelectorAll('.reasoning-method input:checked'))
            .map(input => input.id);
        
        if (enabledMethods.length === 0) {
            this.showAlert('warning', this.getText('selectReasoningMethod'));
            return;
        }

        const config = {
            enabledMethods: enabledMethods,
            maxIterations: parseInt(document.getElementById('maxIterations').value),
            qualityThreshold: parseFloat(document.getElementById('qualityThreshold').value),
            enableAutoReview: document.getElementById('enableAutoReview').checked,
            enableAutoRevision: false,
            targetDomain: document.getElementById('targetDomain').value,
            systemLLM: document.getElementById('systemLLM').value
        };

        this.showLoading(this.getText('startingSession'));

        try {
            const response = await fetch(`${this.apiBase}/start-reasoning`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(config)
            });

            const result = await response.json();

            if (result.success) {
                this.currentSessionId = result.sessionId;
                updateSystemStatus('running', this.getText('reasoningActive'));
                document.getElementById('startReasoningBtn').disabled = true;
                document.getElementById('stopReasoningBtn').disabled = false;
                this.addActivityLog(`${this.getText('started')} ${this.getText('reasoningSessions')}: ${result.sessionId}`, 'info');
                this.showAlert('success', `${this.getText('reasoningSessions')} ${this.getText('started')}: ${result.sessionId}`);
            } else {
                this.showAlert('error', this.getText('failedToStartReasoning') + result.message);
            }
        } catch (error) {
            this.showAlert('error', this.getText('errorStartingReasoning') + error.message);
        } finally {
            this.hideLoading();
        }
    }

    async stopReasoning() {
        if (!this.currentSessionId) {
            this.showAlert('warning', this.getText('noActiveSession'));
            return;
        }

        this.showLoading(this.getText('stoppingSession'));

        try {
            const response = await fetch(`${this.apiBase}/stop-session/${this.currentSessionId}`, {
                method: 'POST'
            });

            const result = await response.json();

            if (result.success) {
                updateSystemStatus('idle', this.getText('systemReady'));
                document.getElementById('startReasoningBtn').disabled = false;
                document.getElementById('stopReasoningBtn').disabled = true;
                this.addActivityLog(`${this.getText('stopReasoning')} ${this.getText('reasoningSessions')}: ${this.currentSessionId}`, 'warning');
                this.currentSessionId = null;
                this.showAlert('info', this.getText('reasoningSessionStopped'));
            } else {
                this.showAlert('error', this.getText('failedToStopReasoning') + result.message);
            }
        } catch (error) {
            this.showAlert('error', this.getText('errorStoppingReasoning') + error.message);
        } finally {
            this.hideLoading();
        }
    }

    async refreshStats() {
        try {
            const response = await fetch(`${this.apiBase}/system-stats`);
            const result = await response.json();

            if (result.success) {
                document.getElementById('totalTheories').textContent = result.stats.TotalTheoriesGenerated || 0;
                document.getElementById('acceptedTheories').textContent = result.stats.TotalTheoriesAccepted || 0;
                document.getElementById('activeSessions').textContent = result.stats.ActiveSessions || 0;
                
                const successRate = result.stats.SuccessRatePercent || 0;
                document.getElementById('successRate').textContent = successRate + '%';
                
                // Update system health
                if (result.systemHealth === 'HEALTHY') {
                    updateSystemStatus('healthy', this.getText('systemHealthy'));
                } else {
                    updateSystemStatus('warning', this.getText('systemIssues'));
                }
            }
        } catch (error) {
            console.error('Error refreshing stats:', error);
        }
    }

    async refreshTheories() {
        if (!this.currentSessionId) {
            return;
        }

        try {
            const response = await fetch(`${this.apiBase}/generated-theories/${this.currentSessionId}`);
            const result = await response.json();

            if (result.success) {
                this.displayTheories(result.theories);
            }
        } catch (error) {
            console.error('Error refreshing theories:', error);
        }
    }

    async refreshSessions() {
        try {
            const response = await fetch(`${this.apiBase}/system-stats`);
            const result = await response.json();

            if (result.success) {
                this.displaySessions(result.activeSessions);
            }
        } catch (error) {
            console.error('Error refreshing sessions:', error);
        }
    }

    async formalizeTheory() {
        const theoryContent = document.getElementById('formalizationInput').value.trim();
        const targetTool = document.getElementById('formalizationTool').value;

        if (!theoryContent) {
            this.showAlert('warning', this.getText('enterTheoryToFormalize'));
            return;
        }

        if (!this.validateLLMConfiguration()) {
            return;
        }

        this.showLoading(this.getText('formalizingTheory'));

        try {
            const selectedLLM = document.getElementById('systemLLM').value;
            const response = await fetch(`${this.apiBase}/formalize-theory`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    theoryId: 'manual-' + Date.now(),
                    theoryContent: theoryContent,
                    targetTool: targetTool,
                    systemLLM: selectedLLM
                })
            });

            const result = await response.json();

            if (result.success) {
                this.displayToolResult(this.getText('formalizationResult'), result.formalizationResult);
                this.addActivityLog(this.getText('manualFormalizationCompleted'), 'success');
            } else {
                this.showAlert('error', this.getText('formalizationFailed') + result.message);
            }
        } catch (error) {
            this.showAlert('error', this.getText('errorFormalizing') + error.message);
        } finally {
            this.hideLoading();
        }
    }

    async verifyTheory() {
        const theoryContent = document.getElementById('verificationInput').value.trim();
        const formalExpression = document.getElementById('formalExpressionInput').value.trim();

        if (!theoryContent) {
            this.showAlert('warning', this.getText('enterTheoryToVerify'));
            return;
        }

        if (!this.validateLLMConfiguration()) {
            return;
        }

        this.showLoading(this.getText('verifyingTheory'));

        try {
            const selectedLLM = document.getElementById('systemLLM').value;
            const response = await fetch(`${this.apiBase}/verify-theory`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    theoryId: 'manual-' + Date.now(),
                    theoryContent: theoryContent,
                    formalExpression: formalExpression || null,
                    systemLLM: selectedLLM
                })
            });

            const result = await response.json();

            if (result.success) {
                this.displayToolResult(this.getText('verificationResult'), result.verificationResult);
                this.addActivityLog(this.getText('manualVerificationCompleted'), 'success');
            } else {
                this.showAlert('error', this.getText('verificationFailed') + result.message);
            }
        } catch (error) {
            this.showAlert('error', this.getText('errorVerifying') + error.message);
        } finally {
            this.hideLoading();
        }
    }

    async reviewEquivalence() {
        const theoryContent = document.getElementById('reviewTheoryInput').value.trim();
        const formalExpression = document.getElementById('reviewFormalInput').value.trim();
        const pythonCode = document.getElementById('reviewPythonInput').value.trim();

        if (!theoryContent || !formalExpression || !pythonCode) {
            this.showAlert('warning', this.getText('fillAllFieldsForReview'));
            return;
        }

        if (!this.validateLLMConfiguration()) {
            return;
        }

        this.showLoading(this.getText('reviewingEquivalence'));

        try {
            const selectedLLM = document.getElementById('systemLLM').value;
            const response = await fetch(`${this.apiBase}/review-equivalence`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    theoryId: 'manual-' + Date.now(),
                    theoryContent: theoryContent,
                    formalExpression: formalExpression,
                    pythonCode: pythonCode,
                    systemLLM: selectedLLM
                })
            });

            const result = await response.json();

            if (result.success) {
                this.displayToolResult(this.getText('equivalenceReviewResult'), result.equivalenceReview);
                this.addActivityLog(this.getText('manualEquivalenceReviewCompleted'), 'success');
            } else {
                this.showAlert('error', this.getText('equivalenceReviewFailed') + result.message);
            }
        } catch (error) {
            this.showAlert('error', this.getText('errorReviewingEquivalence') + error.message);
        } finally {
            this.hideLoading();
        }
    }

    displayTheories(theories) {
        const container = document.getElementById('theoriesList');
        
        if (!theories || theories.length === 0) {
            container.innerHTML = `
                <div class="col-12 text-center text-muted">
                    <i class="fas fa-lightbulb fa-3x mb-3"></i>
                    <p>${this.getText('noTheoriesYet')}</p>
                </div>`;
            return;
        }

        container.innerHTML = theories.map(theory => `
            <div class="col-lg-6 mb-3">
                <div class="card theory-card h-100" style="cursor: pointer;" data-theory-id="${theory.id}" onclick="showTheoryDetail('${theory.id}')">
                    <div class="card-header d-flex justify-content-between align-items-center">
                        <span class="fw-bold">${theory.fullId}</span>
                        <span class="badge bg-${this.getTheoryTypeBadge(theory.type)}">${theory.type}</span>
                    </div>
                    <div class="card-body">
                        <p class="card-text">${this.truncateText(theory.content, 150)}</p>
                        ${theory.formalExpression ? `
                            <div class="small mb-2">
                                <strong>Formal:</strong> <code>${this.truncateText(theory.formalExpression, 100)}</code>
                            </div>
                        ` : ''}
                        <div class="d-flex justify-content-between align-items-center">
                            <small class="text-muted">${theory.reasoningMethod}</small>
                            <div>
                                <span class="badge bg-${theory.isVerified ? 'success' : 'secondary'}">
                                    ${theory.isVerified ? this.getText('verified') : this.getText('unverified')}
                                </span>
                                <span class="badge bg-info">${(theory.qualityScore * 100).toFixed(0)}%</span>
                            </div>
                        </div>
                    </div>
                    <div class="card-footer small text-muted">
                        <i class="fas fa-eye me-1"></i>Click to view details • ${this.getText('created')}: ${new Date(theory.createdAt).toLocaleString()}
                    </div>
                </div>
            </div>
        `).join('');
    }

    displaySessions(sessions) {
        const container = document.getElementById('sessionsList');
        
        if (!sessions || sessions.length === 0) {
            container.innerHTML = `
                <div class="text-center text-muted">
                    <i class="fas fa-tasks fa-3x mb-3"></i>
                    <p>${this.getText('noActiveSessions')}</p>
                </div>`;
            return;
        }

        container.innerHTML = sessions.map(session => `
            <div class="card mb-3">
                <div class="card-header d-flex justify-content-between align-items-center">
                    <span class="fw-bold">${session.sessionId}</span>
                    <span class="badge bg-${this.getStatusBadge(session.status)}">${session.status}</span>
                </div>
                <div class="card-body">
                    <div class="row">
                        <div class="col-md-6">
                            <strong>${this.getText('progress')}:</strong> ${this.getText('iteration')} ${session.currentIteration}
                            <div class="progress mt-2">
                                <div class="progress-bar" role="progressbar" 
                                     style="width: ${(session.currentIteration / 10 * 100)}%">
                                </div>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <strong>${this.getText('started')}:</strong> ${new Date(session.startedAt).toLocaleString()}
                        </div>
                    </div>
                </div>
            </div>
        `).join('');
    }

    displayToolResult(title, result) {
        const container = document.getElementById('toolResults');
        
        const resultHtml = `
            <div class="card mb-3">
                <div class="card-header">
                    <h6 class="mb-0">${title}</h6>
                </div>
                <div class="card-body">
                    <pre class="code-block">${JSON.stringify(result, null, 2)}</pre>
                </div>
            </div>
        `;
        
        container.innerHTML = resultHtml + container.innerHTML;
    }

    updateSystemStatus(status, text) {
        console.log(`🔄 Updating system status: ${status} -> ${text}`);
        
        const indicator = document.getElementById('systemStatus');
        const statusText = document.getElementById('systemStatusText');
        
        if (!indicator) {
            console.error('❌ systemStatus element not found!');
            return;
        }
        
        if (!statusText) {
            console.error('❌ systemStatusText element not found!');
            return;
        }
        
        indicator.className = `status-indicator status-${status}`;
        statusText.textContent = text;
        
        console.log(`✅ System status updated successfully: ${status} -> ${text}`);
    }

    addActivityLog(message, type = 'info') {
        const container = document.getElementById('activityLog');
        const timestamp = new Date().toLocaleTimeString();
        
        const iconMap = {
            'success': 'fas fa-check-circle text-success',
            'error': 'fas fa-times-circle text-danger',
            'warning': 'fas fa-exclamation-triangle text-warning',
            'info': 'fas fa-info-circle text-info'
        };
        
        const logEntry = `
            <div class="timeline-item">
                <div class="d-flex align-items-center">
                    <i class="${iconMap[type]} me-2"></i>
                    <strong>${timestamp}</strong>
                </div>
                <div class="ms-4">${message}</div>
            </div>
        `;
        
        if (container.querySelector('.text-muted')) {
            container.innerHTML = '';
        }
        
        container.insertAdjacentHTML('afterbegin', logEntry);
        
        // Keep only last 10 entries
        const entries = container.querySelectorAll('.timeline-item');
        if (entries.length > 10) {
            entries[entries.length - 1].remove();
        }
    }

    showLoading(text = 'Processing...') {
        console.log('🔄 Showing loading modal:', text);
        const loadingTextElement = document.getElementById('loadingText');
        const loadingModalElement = document.getElementById('loadingModal');
        
        if (!loadingTextElement || !loadingModalElement) {
            console.error('❌ Loading modal elements not found!');
            console.error('loadingText:', loadingTextElement);
            console.error('loadingModal:', loadingModalElement);
            return;
        }
        
        loadingTextElement.textContent = text;
        new bootstrap.Modal(loadingModalElement).show();
    }

    hideLoading() {
        console.log('🔄 Hiding loading modal');
        const loadingModalElement = document.getElementById('loadingModal');
        
        if (!loadingModalElement) {
            console.error('❌ Loading modal element not found for hiding!');
            return;
        }
        
        try {
            // 方法1: 尝试使用Bootstrap API
            const modal = bootstrap.Modal.getInstance(loadingModalElement);
            if (modal) {
                console.log('✅ Modal instance found, hiding...');
                modal.hide();
                
                // 等待一下让动画完成，然后检查是否真的隐藏了
                setTimeout(() => {
                    if (loadingModalElement.classList.contains('show')) {
                        console.warn('⚠️ Modal still showing after hide(), forcing...');
                        this.forceHideModal();
                    }
                }, 300);
            } else {
                console.warn('⚠️ No modal instance found, using force hide...');
                this.forceHideModal();
            }
        } catch (error) {
            console.error('❌ Error hiding modal:', error);
            this.forceHideModal();
        }
    }
    
    forceHideModal() {
        console.log('🔧 Force hiding modal...');
        const loadingModalElement = document.getElementById('loadingModal');
        
        if (loadingModalElement) {
            // 移除所有显示相关的类和样式
            loadingModalElement.classList.remove('show', 'd-block');
            loadingModalElement.style.display = 'none';
            loadingModalElement.setAttribute('aria-hidden', 'true');
            loadingModalElement.removeAttribute('aria-modal');
            loadingModalElement.removeAttribute('role');
        }
        
        // 移除body上的modal相关类
        document.body.classList.remove('modal-open');
        
        // 移除所有背景遮罩
        const backdrops = document.querySelectorAll('.modal-backdrop');
        backdrops.forEach(backdrop => {
            backdrop.remove();
            console.log('🗑️ Removed backdrop');
        });
        
        // 恢复页面滚动
        document.body.style.paddingRight = '';
        document.body.style.overflow = '';
        
        console.log('✅ Modal force hidden');
    }

    showAlert(type, message) {
        // Create and show a toast notification
        const toastContainer = document.getElementById('toastContainer') || this.createToastContainer();
        
        const toastId = 'toast-' + Date.now();
        const toast = document.createElement('div');
        toast.id = toastId;
        toast.className = 'toast';
        toast.setAttribute('role', 'alert');
        
        const bgColor = {
            'success': 'bg-success',
            'error': 'bg-danger',
            'warning': 'bg-warning',
            'info': 'bg-info'
        }[type] || 'bg-info';
        
        toast.innerHTML = `
            <div class="toast-header ${bgColor} text-white">
                <strong class="me-auto">Theory Reasoning Engine</strong>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">${message}</div>
        `;
        
        toastContainer.appendChild(toast);
        
        const bsToast = new bootstrap.Toast(toast);
        bsToast.show();
        
        // Remove after hiding
        toast.addEventListener('hidden.bs.toast', () => {
            toast.remove();
        });
    }

    createToastContainer() {
        const container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '9999';
        document.body.appendChild(container);
        return container;
    }

    startRefreshInterval() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
        }
        
        this.refreshInterval = setInterval(() => {
            this.refreshStats();
            if (this.currentSessionId) {
                this.refreshTheories();
                this.refreshSessions();
            }
        }, 5000); // Refresh every 5 seconds
    }

    getTheoryTypeBadge(type) {
        const badgeMap = {
            'A': 'danger',   // Axioms
            'C': 'primary',  // Corollaries
            'P': 'info',     // Propositions
            'T': 'success',  // Theorems
            'D': 'warning',  // Definitions
            'L': 'secondary',// Lemmas
            'M': 'dark'      // Meta-theorems
        };
        return badgeMap[type] || 'secondary';
    }

    getStatusBadge(status) {
        const badgeMap = {
            'running': 'primary',
            'completed': 'success',
            'failed': 'danger',
            'paused': 'warning',
            'pending': 'secondary'
        };
        return badgeMap[status] || 'secondary';
    }

    truncateText(text, maxLength) {
        if (text.length <= maxLength) {
            return text;
        }
        return text.substring(0, maxLength) + '...';
    }

    // Load available LLM configurations
    async loadAvailableLLMs() {
        try {
            const response = await fetch('/api/llm-configs/list');
            const llmKeys = await response.json();
            
            const selectElement = document.getElementById('systemLLM');
            selectElement.innerHTML = ''; // Clear existing options
            
            if (llmKeys && llmKeys.length > 0) {
                llmKeys.forEach(key => {
                    const option = document.createElement('option');
                    option.value = key;
                    option.textContent = key;
                    selectElement.appendChild(option);
                });
                
                // Set default selection
                if (llmKeys.includes('gpt-4')) {
                    selectElement.value = 'gpt-4';
                } else if (llmKeys.includes('DeepSeek')) {
                    selectElement.value = 'DeepSeek';
                } else {
                    selectElement.value = llmKeys[0];
                }
                
                console.log(`Loaded ${llmKeys.length} LLM configurations`);
            } else {
                // No LLM configurations found
                const option = document.createElement('option');
                option.value = '';
                option.textContent = this.getText('noLLMConfigsFound');
                option.disabled = true;
                selectElement.appendChild(option);
                selectElement.value = '';
                
                this.showAlert('warning', this.getText('noLLMConfigsFound'));
            }
        } catch (error) {
            console.error('Failed to load LLM configurations:', error);
            
            // Show error state
            const selectElement = document.getElementById('systemLLM');
            selectElement.innerHTML = `<option value="" disabled>${this.getText('failedToLoadLLMConfigs')}</option>`;
            selectElement.value = '';
            
            this.showAlert('error', this.getText('failedToLoadLLMConfigsError') + error.message);
        }
    }

    // Validate LLM configuration before operations
    validateLLMConfiguration() {
        const selectedLLM = document.getElementById('systemLLM').value;
        if (!selectedLLM) {
            this.showAlert('warning', this.getText('selectLLMFirst'));
            return false;
        }
        return true;
    }
}

// Initialize the demo when the page loads
let theoryReasoningDemo; // 全局变量供调试使用

document.addEventListener('DOMContentLoaded', () => {
    // 检查Bootstrap是否正确加载
    if (typeof bootstrap === 'undefined') {
        console.error('❌ Bootstrap is not loaded! Modal functions may not work properly.');
    } else {
        console.log('✅ Bootstrap loaded successfully');
    }
    
    theoryReasoningDemo = new TheoryReasoningDemo();
    
    // 在控制台中暴露调试功能
    window.emergencyReset = () => theoryReasoningDemo.emergencyReset();
    window.forceHideModal = () => theoryReasoningDemo.forceHideModal();
    window.debugModal = () => {
        const modal = document.getElementById('loadingModal');
        console.log('Modal element:', modal);
        console.log('Modal classes:', modal ? modal.classList.toString() : 'N/A');
        console.log('Modal display:', modal ? modal.style.display : 'N/A');
        console.log('Body classes:', document.body.classList.toString());
        console.log('Backdrops:', document.querySelectorAll('.modal-backdrop').length);
    };
    
    console.log('🎯 Theory Reasoning Demo initialized!');
    console.log('💡 Available debug commands:');
    console.log('   - emergencyReset() : Reset the entire system');
    console.log('   - forceHideModal() : Force hide the loading modal');
    console.log('   - debugModal() : Debug modal state');
});

// ==================== AI THINKING PROCESS MANAGEMENT ====================

let autoRefreshInterval = null;
let currentThinkingData = [];

// Initialize thinking process tab
async function initializeThinkingTab() {
    const refreshBtn = document.getElementById('refreshThinkingBtn');
    const autoRefreshBtn = document.getElementById('autoRefreshToggle');
    const sessionFilter = document.getElementById('sessionFilter');
    const stepTypeFilter = document.getElementById('stepTypeFilter');

    if (refreshBtn) {
        refreshBtn.addEventListener('click', loadThinkingProcess);
    }

    if (autoRefreshBtn) {
        autoRefreshBtn.addEventListener('click', toggleAutoRefresh);
    }

    if (sessionFilter) {
        sessionFilter.addEventListener('change', filterThinkingSteps);
    }

    if (stepTypeFilter) {
        stepTypeFilter.addEventListener('change', filterThinkingSteps);
    }

    // Load initial data
    await loadThinkingProcess();
}

// Load thinking process data
async function loadThinkingProcess() {
    try {
        showElement('loadingModal');
        updateLoadingText(getI18nText('loadingThinkingProcess', 'Loading AI thinking process...'));

        const response = await fetch('/api/TheoryReasoningDemo/thinking-process/all');
        const data = await response.json();

        if (data.success) {
            currentThinkingData = data.thinkingSteps;
            updateThinkingStatistics(data);
            updateSessionFilter(data);
            displayThinkingSteps(data.thinkingSteps);
        } else {
            console.error('Failed to load thinking process:', data.message);
            showErrorMessage('Failed to load thinking process: ' + (data.message || 'Unknown error'));
        }
    } catch (error) {
        console.error('Error loading thinking process:', error);
        showErrorMessage('Error loading thinking process: ' + error.message);
    } finally {
        hideElement('loadingModal');
    }
}

// Update thinking statistics
function updateThinkingStatistics(data) {
    const totalSteps = document.getElementById('totalSteps');
    const analysisSteps = document.getElementById('analysisSteps');
    const synthesisSteps = document.getElementById('synthesisSteps');
    const conclusionSteps = document.getElementById('conclusionSteps');

    if (totalSteps) totalSteps.textContent = data.totalSteps || 0;
    if (analysisSteps) analysisSteps.textContent = data.stepsByType?.analysis || 0;
    if (synthesisSteps) synthesisSteps.textContent = data.stepsByType?.synthesis || 0;
    if (conclusionSteps) conclusionSteps.textContent = data.stepsByType?.conclusion || 0;
}

// Update session filter dropdown
function updateSessionFilter(data) {
    const sessionFilter = document.getElementById('sessionFilter');
    if (!sessionFilter) return;

    // Get unique sessions
    const sessions = [...new Set(data.thinkingSteps.map(step => step.sessionId))];
    
    // Clear existing options except "All Sessions"
    sessionFilter.innerHTML = '<option value="">All Sessions</option>';
    
    sessions.forEach(sessionId => {
        const option = document.createElement('option');
        option.value = sessionId;
        option.textContent = `Session ${sessionId.substring(0, 8)}...`;
        sessionFilter.appendChild(option);
    });
}

// Display thinking steps
function displayThinkingSteps(steps) {
    const timeline = document.getElementById('thinkingTimeline');
    if (!timeline) return;

    if (!steps || steps.length === 0) {
        timeline.innerHTML = `
            <div class="text-center text-muted py-5">
                <i class="fas fa-brain fa-3x mb-3"></i>
                <p>No thinking steps found. Start a reasoning session to see AI thinking process.</p>
            </div>
        `;
        return;
    }

    const stepsHtml = steps.map(step => createThinkingStepHtml(step)).join('');
    timeline.innerHTML = stepsHtml;
}

// Create HTML for a thinking step
function createThinkingStepHtml(step) {
    const timestamp = new Date(step.timestamp).toLocaleString();
    const stepType = step.stepType || 'analysis';
    const icon = getStepIcon(stepType);
    const reasoningType = step.reasoningType ? `[${step.reasoningType.toUpperCase()}]` : '';
    
    let aiResponseHtml = '';
    if (step.aiResponse && step.aiResponse.trim()) {
        const truncated = step.aiResponse.length > 200 
            ? step.aiResponse.substring(0, 200) + '...' 
            : step.aiResponse;
        aiResponseHtml = `
            <div class="ai-response-preview">
                <strong>AI Response:</strong><br>
                ${escapeHtml(truncated)}
                ${step.aiResponse.length > 200 ? `<br><small class="text-muted">(${step.aiResponse.length - 200} more characters)</small>` : ''}
            </div>
        `;
    }

    let metadataHtml = '';
    if (step.metadata && Object.keys(step.metadata).length > 0) {
        const metadataItems = Object.entries(step.metadata)
            .map(([key, value]) => `<span class="badge bg-light text-dark me-1">${key}: ${value}</span>`)
            .join('');
        metadataHtml = `
            <div class="thinking-metadata">
                <strong>Metadata:</strong> ${metadataItems}
            </div>
        `;
    }

    return `
        <div class="thinking-step ${stepType}" data-session="${step.sessionId}" data-step-type="${stepType}">
            <div class="d-flex align-items-start">
                <div class="step-icon ${stepType}">
                    <i class="fas ${icon}"></i>
                </div>
                <div class="flex-grow-1">
                    <div class="d-flex justify-content-between align-items-center mb-2">
                        <h6 class="mb-0">
                            ${reasoningType} ${stepType.charAt(0).toUpperCase() + stepType.slice(1)} 
                            <small class="text-muted">Iteration ${step.iteration || 0}</small>
                        </h6>
                        <span class="step-timestamp">${timestamp}</span>
                    </div>
                    <div class="mb-2">
                        <strong>Content:</strong> ${escapeHtml(step.content)}
                    </div>
                    ${step.reasoning ? `
                        <div class="mb-2">
                            <strong>Reasoning:</strong> ${escapeHtml(step.reasoning)}
                        </div>
                    ` : ''}
                    ${step.initialPrompt ? `
                        <div class="mb-2">
                            <strong>Initial Prompt:</strong> 
                            <div class="ai-response-preview">${escapeHtml(step.initialPrompt)}</div>
                        </div>
                    ` : ''}
                    ${aiResponseHtml}
                    ${metadataHtml}
                </div>
            </div>
        </div>
    `;
}

// Get icon for step type
function getStepIcon(stepType) {
    switch (stepType) {
        case 'analysis': return 'fa-search';
        case 'synthesis': return 'fa-puzzle-piece';
        case 'evaluation': return 'fa-balance-scale';
        case 'conclusion': return 'fa-flag-checkered';
        default: return 'fa-circle';
    }
}

// Filter thinking steps
function filterThinkingSteps() {
    const sessionFilter = document.getElementById('sessionFilter');
    const stepTypeFilter = document.getElementById('stepTypeFilter');
    
    const selectedSession = sessionFilter?.value || '';
    const selectedStepType = stepTypeFilter?.value || '';
    
    let filteredSteps = currentThinkingData;
    
    if (selectedSession) {
        filteredSteps = filteredSteps.filter(step => step.sessionId === selectedSession);
    }
    
    if (selectedStepType) {
        filteredSteps = filteredSteps.filter(step => step.stepType === selectedStepType);
    }
    
    displayThinkingSteps(filteredSteps);
}

// Toggle auto refresh
function toggleAutoRefresh() {
    const button = document.getElementById('autoRefreshToggle');
    if (!button) return;

    const isAuto = button.getAttribute('data-auto') === 'true';
    
    if (isAuto) {
        // Stop auto refresh
        if (autoRefreshInterval) {
            clearInterval(autoRefreshInterval);
            autoRefreshInterval = null;
        }
        button.setAttribute('data-auto', 'false');
        button.innerHTML = '<i class="fas fa-play me-2"></i>Auto Refresh';
        button.className = 'btn btn-outline-secondary';
    } else {
        // Start auto refresh
        autoRefreshInterval = setInterval(loadThinkingProcess, 5000); // Refresh every 5 seconds
        button.setAttribute('data-auto', 'true');
        button.innerHTML = '<i class="fas fa-pause me-2"></i>Stop Auto';
        button.className = 'btn btn-success';
    }
}

// Escape HTML characters
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Note: Initialization is now handled by TheoryReasoningDemo class constructor

// ==================== PAGE INITIALIZATION ====================

// Initialize page on load - check system status and load existing data
async function initializePageOnLoad() {
    try {
        console.log('Initializing page...');
        
        // 1. Check system status first
        await checkAndInitializeSystem();
        
        // 2. Load existing sessions
        await loadExistingSessions();
        
        // 3. Load existing theories
        await loadExistingTheories();
        
        // 4. Update UI with current state
        await updateSystemMetrics();
        
        console.log('Page initialization completed');
    } catch (error) {
        console.error('Error during page initialization:', error);
        showNotification(getLocalizedText('initializationFailed'), 'error');
    }
}

// Check system status and initialize if needed
async function checkAndInitializeSystem() {
    try {
        // Check if system is already initialized
        const response = await fetch('/api/TheoryReasoningDemo/system-status');
        
        if (response.ok) {
            const data = await response.json();
            console.log('System status:', data);
            
            if (data.success && data.initialized) {
                // System already initialized
                updateSystemStatus('ready');
                showNotification('System already initialized and ready', 'success');
                
                // Enable controls
                enableControlButtons();
                return true;
            }
        }
        
        // System not initialized, try to initialize automatically
        console.log('System not initialized, attempting auto-initialization...');
        updateSystemStatus('initializing');
        
        const initResponse = await fetch('/api/TheoryReasoningDemo/initialize', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                SystemLLM: getSelectedLLM()
            })
        });
        
        if (initResponse.ok) {
            const initData = await initResponse.json();
            if (initData.success) {
                updateSystemStatus('ready');
                showNotification('System auto-initialized successfully', 'success');
                enableControlButtons();
                return true;
            }
        }
        
        // Auto-initialization failed, but don't show error - user can manually initialize
        updateSystemStatus('not-initialized');
        console.log('Auto-initialization failed, user can manually initialize');
        return false;
        
    } catch (error) {
        console.error('Error checking system status:', error);
        updateSystemStatus('error');
        return false;
    }
}

// Load existing sessions
async function loadExistingSessions() {
    try {
        const response = await fetch('/api/TheoryReasoningDemo/sessions');
        if (response.ok) {
            const data = await response.json();
            if (data.success && data.sessions && data.sessions.length > 0) {
                console.log(`Found ${data.sessions.length} existing sessions`);
                displaySessionsInUI(data.sessions);
            }
        }
    } catch (error) {
        console.error('Error loading existing sessions:', error);
    }
}

// Load existing theories
async function loadExistingTheories() {
    try {
        const response = await fetch('/api/TheoryReasoningDemo/theories');
        if (response.ok) {
            const data = await response.json();
            if (data.success && data.theories && data.theories.length > 0) {
                console.log(`Found ${data.theories.length} existing theories`);
                displayTheoriesInUI(data.theories);
            }
        }
    } catch (error) {
        console.error('Error loading existing theories:', error);
    }
}

// Display sessions in UI
function displaySessionsInUI(sessions) {
    // Update the sessions tab content
    const sessionsTabContent = document.querySelector('#sessions .row');
    if (sessionsTabContent && sessions.length > 0) {
        let sessionsHtml = '';
        
        sessions.forEach(session => {
            const statusBadge = getSessionStatusBadge(session.status);
            const completionRate = session.maxIterations > 0 
                ? Math.round((session.currentIteration / session.maxIterations) * 100) 
                : 0;
            
            sessionsHtml += `
                <div class="col-md-6 mb-3">
                    <div class="card">
                        <div class="card-header d-flex justify-content-between align-items-center">
                            <h6 class="mb-0">Session ${session.sessionId.substring(0, 8)}</h6>
                            ${statusBadge}
                        </div>
                        <div class="card-body">
                            <div class="d-flex justify-content-between mb-2">
                                <span><strong>Started:</strong> ${new Date(session.startedAt).toLocaleString()}</span>
                            </div>
                            ${session.completedAt ? `
                                <div class="d-flex justify-content-between mb-2">
                                    <span><strong>Completed:</strong> ${new Date(session.completedAt).toLocaleString()}</span>
                                </div>
                            ` : ''}
                            <div class="mb-2">
                                <span><strong>Progress:</strong> ${session.currentIteration}/${session.maxIterations} iterations (${completionRate}%)</span>
                                <div class="progress mt-1">
                                    <div class="progress-bar" style="width: ${completionRate}%"></div>
                                </div>
                            </div>
                            <div class="row text-center">
                                <div class="col-4">
                                    <div class="metric-value">${session.generatedTheories}</div>
                                    <div class="metric-label">Generated</div>
                                </div>
                                <div class="col-4">
                                    <div class="metric-value">${session.acceptedTheories}</div>
                                    <div class="metric-label">Accepted</div>
                                </div>
                                <div class="col-4">
                                    <div class="metric-value">${Math.round(session.successRate * 100)}%</div>
                                    <div class="metric-label">Success Rate</div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        });
        
        sessionsTabContent.innerHTML = sessionsHtml;
    }
}

// Display theories in UI
function displayTheoriesInUI(theories) {
    // Update the theories tab content
    const theoriesTabContent = document.querySelector('#theories .row');
    if (theoriesTabContent && theories.length > 0) {
        let theoriesHtml = '';
        
        theories.forEach(theory => {
            const verificationBadge = theory.isVerified 
                ? '<span class="badge bg-success">Verified</span>'
                : '<span class="badge bg-secondary">Unverified</span>';
            
            const theoryContent = theory.content.length > 150 
                ? theory.content.substring(0, 150) + '...'
                : theory.content;
            
            theoriesHtml += `
                <div class="col-md-6 mb-3">
                    <div class="card">
                        <div class="card-header d-flex justify-content-between align-items-center">
                            <h6 class="mb-0">${theory.fullId || theory.id}</h6>
                            ${verificationBadge}
                        </div>
                        <div class="card-body">
                            <p class="card-text">${theoryContent}</p>
                            <div class="d-flex justify-content-between">
                                <small class="text-muted">Method: ${theory.reasoningMethod}</small>
                                <small class="text-muted">Score: ${theory.qualityScore.toFixed(2)}</small>
                            </div>
                            <div class="mt-2">
                                <small class="text-muted">Created: ${new Date(theory.createdAt).toLocaleString()}</small>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        });
        
        theoriesTabContent.innerHTML = theoriesHtml;
    }
}

// Get session status badge
function getSessionStatusBadge(status) {
    switch (status.toLowerCase()) {
        case 'completed':
            return '<span class="badge bg-success">Completed</span>';
        case 'running':
            return '<span class="badge bg-primary">Running</span>';
        case 'failed':
            return '<span class="badge bg-danger">Failed</span>';
        case 'paused':
            return '<span class="badge bg-warning">Paused</span>';
        default:
            return '<span class="badge bg-secondary">Unknown</span>';
    }
}

// Update system status (standalone function)
function updateSystemStatus(status, text) {
    const indicator = document.getElementById('systemStatus');
    const statusText = document.getElementById('systemStatusText');
    
    if (indicator && statusText) {
        indicator.className = `status-indicator status-${status}`;
        statusText.textContent = text || getStatusText(status);
    }
}

// Get status text based on status
function getStatusText(status) {
    const statusMap = {
        'ready': 'System Ready',
        'initializing': 'Initializing...',
        'not-initialized': 'System not initialized. Please click Initialize.',
        'error': 'System Error',
        'running': 'Reasoning in Progress...'
    };
    return statusMap[status] || status;
}

// Show notification (standalone function)
function showNotification(message, type = 'info') {
    console.log(`[${type.toUpperCase()}] ${message}`);
    
    // Try to use the toast system if available
    const toastContainer = document.querySelector('.toast-container');
    if (toastContainer) {
        const toastId = 'toast-' + Date.now();
        const toastHtml = `
            <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="toast-header">
                    <strong class="me-auto">System</strong>
                    <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body ${type === 'error' ? 'text-danger' : type === 'success' ? 'text-success' : ''}">${message}</div>
            </div>
        `;
        
        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        const toastElement = document.getElementById(toastId);
        if (toastElement) {
            const toast = new bootstrap.Toast(toastElement);
            toast.show();
            
            // Auto remove after showing
            toastElement.addEventListener('hidden.bs.toast', () => {
                toastElement.remove();
            });
        }
    } else {
        // Fallback to alert
        alert(message);
    }
}

// Enable control buttons after initialization
function enableControlButtons() {
    const initButton = document.getElementById('initializeBtn');
    const startButton = document.getElementById('startReasoningBtn');
    
    if (initButton) {
        initButton.disabled = false;
        initButton.textContent = getLocalizedText('systemReady');
        initButton.className = 'btn btn-success disabled';
    }
    
    if (startButton) {
        startButton.disabled = false;
    }
}

// Get selected LLM system
function getSelectedLLM() {
    const llmSelect = document.getElementById('llmSystem');
    return llmSelect ? llmSelect.value : 'OpenAI';
}

// Show theory detail modal
async function showTheoryDetail(theoryId) {
    const modal = new bootstrap.Modal(document.getElementById('theoryDetailModal'));
    const loadingDiv = document.getElementById('theoryDetailLoading');
    const contentDiv = document.getElementById('theoryDetailContent');
    const modalTitle = document.getElementById('theoryDetailModalLabel');
    const exportBtn = document.getElementById('exportTheoryBtn');
    
    // Show loading state
    loadingDiv.style.display = 'block';
    contentDiv.style.display = 'none';
    modalTitle.innerHTML = '<i class="fas fa-lightbulb me-2"></i>Loading Theory Details...';
    
    // Show modal
    modal.show();
    
    try {
        const response = await fetch(`/api/TheoryReasoningDemo/theory/${theoryId}`);
        const data = await response.json();
        
        if (data.success && data.theory) {
            const theory = data.theory;
            
            // Update modal title
            modalTitle.innerHTML = `<i class="fas fa-lightbulb me-2"></i>Theory ${theory.fullId}: ${getTheoryTypeName(theory.type)}`;
            
            // Generate detailed content
            const htmlContent = generateTheoryDetailHTML(theory);
            contentDiv.innerHTML = htmlContent;
            
            // Store markdown content for export
            exportBtn.setAttribute('data-theory-id', theory.id);
            exportBtn.setAttribute('data-markdown', theory.markdownContent);
            
            // Hide loading and show content
            loadingDiv.style.display = 'none';
            contentDiv.style.display = 'block';
        } else {
            throw new Error(data.message || 'Failed to load theory details');
        }
    } catch (error) {
        console.error('Error loading theory detail:', error);
        
        contentDiv.innerHTML = `
            <div class="alert alert-danger">
                <i class="fas fa-exclamation-triangle me-2"></i>
                <strong>Error loading theory details:</strong> ${error.message}
            </div>
        `;
        
        loadingDiv.style.display = 'none';
        contentDiv.style.display = 'block';
        modalTitle.innerHTML = '<i class="fas fa-exclamation-triangle me-2"></i>Error Loading Theory';
    }
}

// Generate HTML content for theory detail modal
function generateTheoryDetailHTML(theory) {
    const typeNames = {
        'A': 'Axiom',
        'C': 'Corollary', 
        'D': 'Definition',
        'L': 'Lemma',
        'P': 'Proposition',
        'T': 'Theorem'
    };
    
    let html = `
        <div class="theory-detail">
            <!-- Theory Header -->
            <div class="row mb-4">
                <div class="col-md-8">
                    <h4>${theory.fullId}: ${theory.type}</h4>
                    <p class="text-muted mb-0">
                        <i class="fas fa-calendar me-2"></i>Created: ${new Date(theory.createdAt).toLocaleString()}
                    </p>
                </div>
                <div class="col-md-4 text-end">
                    <span class="badge bg-${getTheoryTypeBadge(theory.type)} fs-6 me-2">${typeNames[theory.type] || 'Unknown'}</span>
                    <span class="badge bg-${theory.isVerified ? 'success' : 'secondary'} fs-6 me-2">
                        ${theory.isVerified ? '✅ Verified' : '❌ Unverified'}
                    </span>
                    <span class="badge bg-info fs-6">${(theory.qualityScore * 100).toFixed(0)}%</span>
                </div>
            </div>

            <!-- Theory Content -->
            <div class="card mb-3">
                <div class="card-header">
                    <h6 class="mb-0"><i class="fas fa-file-text me-2"></i>Content</h6>
                </div>
                <div class="card-body">
                    <div class="theory-content">${formatTextContent(theory.content)}</div>
                </div>
            </div>
    `;

    // Formal Expression
    if (theory.formalExpression) {
        html += `
            <div class="card mb-3">
                <div class="card-header">
                    <h6 class="mb-0"><i class="fas fa-math me-2"></i>Formal Expression</h6>
                </div>
                <div class="card-body">
                    <pre class="bg-light p-3 rounded"><code>${theory.formalExpression}</code></pre>
                </div>
            </div>
        `;
    }

    // Python Code
    if (theory.pythonCode) {
        html += `
            <div class="card mb-3">
                <div class="card-header">
                    <h6 class="mb-0"><i class="fab fa-python me-2"></i>Python Code</h6>
                </div>
                <div class="card-body">
                    <pre class="bg-dark text-light p-3 rounded"><code class="language-python">${theory.pythonCode}</code></pre>
                </div>
            </div>
        `;
    }

    // Dependencies
    if (theory.dependencies && theory.dependencies.length > 0) {
        html += `
            <div class="card mb-3">
                <div class="card-header">
                    <h6 class="mb-0"><i class="fas fa-link me-2"></i>Dependencies</h6>
                </div>
                <div class="card-body">
                    <div class="d-flex flex-wrap gap-2">
                        ${theory.dependencies.map(dep => `<span class="badge bg-outline-primary">${dep}</span>`).join('')}
                    </div>
                </div>
            </div>
        `;
    }

    // Metadata
    if (theory.metadata && Object.keys(theory.metadata).length > 0) {
        html += `
            <div class="card mb-3">
                <div class="card-header">
                    <h6 class="mb-0"><i class="fas fa-tags me-2"></i>Metadata</h6>
                </div>
                <div class="card-body">
                    <div class="row">
                        ${Object.entries(theory.metadata).map(([key, value]) => `
                            <div class="col-md-6 mb-2">
                                <strong>${key}:</strong> ${value}
                            </div>
                        `).join('')}
                    </div>
                </div>
            </div>
        `;
    }

    // Reasoning Method
    html += `
        <div class="card">
            <div class="card-header">
                <h6 class="mb-0"><i class="fas fa-brain me-2"></i>Reasoning Information</h6>
            </div>
            <div class="card-body">
                <div class="row">
                    <div class="col-md-6">
                        <strong>Reasoning Method:</strong> ${theory.reasoningMethod || 'Unknown'}
                    </div>
                    <div class="col-md-6">
                        <strong>Quality Score:</strong> ${(theory.qualityScore * 100).toFixed(1)}%
                    </div>
                </div>
            </div>
        </div>
    `;

    html += '</div>';
    return html;
}

// Format text content with basic markdown-like formatting
function formatTextContent(text) {
    if (!text) return '';
    
    return text
        .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')  // Bold
        .replace(/\*(.*?)\*/g, '<em>$1</em>')             // Italic
        .replace(/`(.*?)`/g, '<code>$1</code>')           // Inline code
        .replace(/\n/g, '<br>')                           // Line breaks
        .replace(/###\s+(.*?)(?=\n|$)/g, '<h5>$1</h5>')  // H5 headers
        .replace(/##\s+(.*?)(?=\n|$)/g, '<h4>$1</h4>')   // H4 headers
        .replace(/#\s+(.*?)(?=\n|$)/g, '<h3>$1</h3>');   // H3 headers
}

// Get theory type badge color
function getTheoryTypeBadge(type) {
    const badgeColors = {
        'A': 'danger',    // Axiom - red
        'C': 'warning',   // Corollary - yellow  
        'D': 'info',      // Definition - blue
        'L': 'secondary', // Lemma - gray
        'P': 'primary',   // Proposition - blue
        'T': 'success'    // Theorem - green
    };
    return badgeColors[type] || 'secondary';
}

// Get theory type name
function getTheoryTypeName(type) {
    const typeNames = {
        'A': 'Axiom',
        'C': 'Corollary', 
        'D': 'Definition',
        'L': 'Lemma',
        'P': 'Proposition',
        'T': 'Theorem'
    };
    return typeNames[type] || 'Unknown';
}

// Export theory as markdown
document.addEventListener('DOMContentLoaded', function() {
    const exportBtn = document.getElementById('exportTheoryBtn');
    if (exportBtn) {
        exportBtn.addEventListener('click', function() {
            const theoryId = this.getAttribute('data-theory-id');
            const markdownContent = this.getAttribute('data-markdown');
            
            if (markdownContent && theoryId) {
                // Create and download file
                const blob = new Blob([markdownContent], { type: 'text/markdown' });
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `Theory_${theoryId}.md`;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
                
                showNotification('Theory exported successfully!', 'success');
            }
        });
    }
});