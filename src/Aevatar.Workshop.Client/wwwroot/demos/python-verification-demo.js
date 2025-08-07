// Python Verification Demo JavaScript
class PythonVerificationDemo {
    constructor() {
        this.currentHybridMode = 'auto';
        this.apiBase = '/api/PythonVerificationDemo';
        this.executionHistory = {
            legacy: [],
            mcp: [],
            hybrid: []
        };
        this.init();
    }

    init() {
        this.loadAgentStatus();
        this.setupEventListeners();
        this.startPeriodicStatusUpdate();
    }

    setupEventListeners() {
        // Auto-load status when page loads
        document.addEventListener('DOMContentLoaded', () => {
            this.loadAgentStatus();
        });
    }

    startPeriodicStatusUpdate() {
        // Update statistics every 30 seconds
        setInterval(() => {
            this.loadStatistics();
        }, 30000);
    }

    async loadAgentStatus() {
        try {
            const response = await fetch(`${this.apiBase}/status`);
            const data = await response.json();
            
            if (data.success) {
                this.updateStatusIndicator('legacy', 'ready');
                this.updateStatusIndicator('mcp', 'ready');
                this.updateStatusIndicator('hybrid', 'ready');
                
                this.log('System', 'All Python verification agents are available');
            } else {
                this.updateStatusIndicator('legacy', 'error');
                this.updateStatusIndicator('mcp', 'error');
                this.updateStatusIndicator('hybrid', 'error');
                
                this.log('System', `Failed to load agent status: ${data.error}`, 'error');
            }
        } catch (error) {
            this.log('System', `Error loading agent status: ${error.message}`, 'error');
        }
    }

    async loadStatistics() {
        try {
            const response = await fetch(`${this.apiBase}/statistics`);
            const data = await response.json();
            
            if (data.success && data.statistics) {
                this.updateStatistics('legacy', data.statistics.legacy);
                this.updateStatistics('mcp', data.statistics.mcp);
                this.updateStatistics('hybrid', data.statistics.hybrid);
            }
        } catch (error) {
            console.warn('Failed to load statistics:', error);
        }
    }

    updateStatistics(agentType, stats) {
        if (!stats) return;

        const prefix = `${agentType}-stat`;
        
        const statusElement = document.getElementById(`${prefix}-status`);
        const executionsElement = document.getElementById(`${prefix}-executions`);
        const successElement = document.getElementById(`${prefix}-success`);
        const avgTimeElement = document.getElementById(`${prefix}-avg-time`);
        
        if (statusElement) statusElement.textContent = stats.status || 'Unknown';
        if (executionsElement) executionsElement.textContent = stats.totalExecutions || 0;
        if (successElement) successElement.textContent = stats.successRate ? `${(stats.successRate * 100).toFixed(1)}%` : '0%';
        if (avgTimeElement) avgTimeElement.textContent = stats.averageExecutionTime ? `${stats.averageExecutionTime.toFixed(2)}ms` : '-';

        // Update hybrid mode specifically
        if (agentType === 'hybrid') {
            const modeElement = document.getElementById('hybrid-stat-mode');
            if (modeElement) modeElement.textContent = this.currentHybridMode;
        }
    }

    updateStatusIndicator(agentType, status) {
        const indicator = document.getElementById(`${agentType}-status`);
        if (indicator) {
            indicator.className = `status-indicator ${status}`;
        }
    }

    setLoadingState(agentType, isLoading) {
        this.updateStatusIndicator(agentType, isLoading ? 'loading' : 'ready');
        
        // Disable/enable buttons
        const buttons = document.querySelectorAll(`[onclick*="${agentType.charAt(0).toUpperCase() + agentType.slice(1)}"]`);
        buttons.forEach(btn => {
            btn.disabled = isLoading;
        });
    }

    showLoading() {
        document.getElementById('loading-overlay').style.display = 'flex';
    }

    hideLoading() {
        document.getElementById('loading-overlay').style.display = 'none';
    }

    log(source, message, type = 'info') {
        const timestamp = new Date().toLocaleTimeString();
        console.log(`[${timestamp}] ${source}: ${message}`);
        
        // You could also show this in a dedicated log area if needed
        if (type === 'error') {
            console.error(`[${timestamp}] ${source}: ${message}`);
        }
    }

    async makeRequest(endpoint, method = 'GET', body = null) {
        this.showLoading();
        
        try {
            const options = {
                method,
                headers: {
                    'Content-Type': 'application/json',
                }
            };
            
            if (body) {
                options.body = JSON.stringify(body);
            }
            
            const response = await fetch(`${this.apiBase}${endpoint}`, options);
            const data = await response.json();
            
            return data;
        } catch (error) {
            this.log('Request', `Failed to call ${endpoint}: ${error.message}`, 'error');
            return { success: false, error: error.message };
        } finally {
            this.hideLoading();
        }
    }

    formatExecutionResult(result, agentType, success = true) {
        const timestamp = new Date().toLocaleString();
        let output = '';
        
        if (success && result) {
            output = `🚀 Execution completed successfully at ${timestamp}\n`;
            output += `Agent Type: ${agentType}\n`;
            output += `Execution Time: ${result.executionTime || result.totalExecutionTimeMs}ms\n`;
            output += `Exit Code: ${result.exitCode || 0}\n\n`;
            
            if (result.standardOutput) {
                output += `📤 Standard Output:\n${result.standardOutput}\n\n`;
            }
            
            if (result.errorOutput) {
                output += `⚠️ Error Output:\n${result.errorOutput}\n\n`;
            }
            
            if (result.mcpServerInfo) {
                output += `🔗 MCP Server Info:\n${JSON.stringify(result.mcpServerInfo, null, 2)}\n\n`;
            }
        } else {
            output = `❌ Execution failed at ${timestamp}\n`;
            output += `Agent Type: ${agentType}\n`;
            output += `Error: ${result?.error || 'Unknown error'}\n`;
        }
        
        return output;
    }

    updateExecutionResults(agentType, output, isError = false) {
        const outputElement = document.getElementById(`${agentType}-output`);
        if (outputElement) {
            outputElement.textContent = output;
            outputElement.className = `result-output ${isError ? 'error' : 'success'}`;
        }

        // Store in execution history
        this.executionHistory[agentType].push({
            timestamp: new Date(),
            output,
            isError
        });

        // Update comparison table
        this.updateComparisonTable();
    }

    updateComparisonTable() {
        // Update execution times in comparison table
        const history = this.executionHistory;
        
        Object.keys(history).forEach(agentType => {
            const execTimeElement = document.getElementById(`${agentType}-exec-time`);
            const successRateElement = document.getElementById(`${agentType}-success-rate`);
            
            if (history[agentType].length > 0) {
                const lastExecution = history[agentType][history[agentType].length - 1];
                const successCount = history[agentType].filter(ex => !ex.isError).length;
                const successRate = ((successCount / history[agentType].length) * 100).toFixed(1);
                
                if (execTimeElement) {
                    // Extract execution time from output
                    const timeMatch = lastExecution.output.match(/Execution Time: (\d+)ms/);
                    execTimeElement.textContent = timeMatch ? `${timeMatch[1]}ms` : '-';
                }
                
                if (successRateElement) {
                    successRateElement.textContent = `${successRate}%`;
                }
            }
        });
    }
}

// Initialize the demo
const demo = new PythonVerificationDemo();

// Global functions for HTML onclick handlers

async function initializeLegacyAgent() {
    demo.setLoadingState('legacy', true);
    
    const result = await demo.makeRequest('/legacy/initialize', 'POST');
    
    if (result.success) {
        demo.updateStatusIndicator('legacy', 'ready');
        demo.log('Legacy Agent', result.message);
        document.getElementById('legacy-stat-status').textContent = 'Initialized';
    } else {
        demo.updateStatusIndicator('legacy', 'error');
        demo.log('Legacy Agent', result.error, 'error');
        document.getElementById('legacy-stat-status').textContent = 'Error';
    }
    
    demo.setLoadingState('legacy', false);
}

async function initializeMCPAgent() {
    demo.setLoadingState('mcp', true);
    
    const result = await demo.makeRequest('/mcp/initialize', 'POST');
    
    if (result.success) {
        demo.updateStatusIndicator('mcp', 'ready');
        demo.log('MCP Agent', result.message);
        document.getElementById('mcp-stat-status').textContent = 'Initialized';
    } else {
        demo.updateStatusIndicator('mcp', 'error');
        demo.log('MCP Agent', result.error, 'error');
        document.getElementById('mcp-stat-status').textContent = 'Error';
    }
    
    demo.setLoadingState('mcp', false);
}

async function initializeHybridAgent() {
    demo.setLoadingState('hybrid', true);
    
    const result = await demo.makeRequest('/hybrid/initialize', 'POST', {
        mode: demo.currentHybridMode
    });
    
    if (result.success) {
        demo.updateStatusIndicator('hybrid', 'ready');
        demo.log('Hybrid Agent', result.message);
        document.getElementById('hybrid-stat-status').textContent = 'Initialized';
        document.getElementById('hybrid-stat-mode').textContent = demo.currentHybridMode;
    } else {
        demo.updateStatusIndicator('hybrid', 'error');
        demo.log('Hybrid Agent', result.error, 'error');
        document.getElementById('hybrid-stat-status').textContent = 'Error';
    }
    
    demo.setLoadingState('hybrid', false);
}

async function executeLegacyCode() {
    const code = document.getElementById('legacy-code').value;
    const timeout = parseInt(document.getElementById('legacy-timeout').value);
    
    if (!code.trim()) {
        alert('Please enter Python code to execute');
        return;
    }
    
    demo.setLoadingState('legacy', true);
    
    const result = await demo.makeRequest('/legacy/execute', 'POST', {
        code,
        timeout
    });
    
    const output = demo.formatExecutionResult(result.result, result.agentType, result.success);
    demo.updateExecutionResults('legacy', output, !result.success);
    
    if (result.success) {
        demo.log('Legacy Agent', 'Code executed successfully');
        // Update statistics counter
        const currentCount = parseInt(document.getElementById('legacy-stat-executions').textContent) || 0;
        document.getElementById('legacy-stat-executions').textContent = currentCount + 1;
    } else {
        demo.log('Legacy Agent', `Execution failed: ${result.error}`, 'error');
    }
    
    demo.setLoadingState('legacy', false);
}

async function executeMCPCode() {
    const code = document.getElementById('mcp-code').value;
    const timeout = parseInt(document.getElementById('mcp-timeout').value);
    
    if (!code.trim()) {
        alert('Please enter Python code to execute');
        return;
    }
    
    demo.setLoadingState('mcp', true);
    
    const result = await demo.makeRequest('/mcp/execute', 'POST', {
        code,
        timeout
    });
    
    const output = demo.formatExecutionResult(result.result, result.agentType, result.success);
    demo.updateExecutionResults('mcp', output, !result.success);
    
    if (result.success) {
        demo.log('MCP Agent', 'Code executed successfully');
        // Update statistics counter
        const currentCount = parseInt(document.getElementById('mcp-stat-executions').textContent) || 0;
        document.getElementById('mcp-stat-executions').textContent = currentCount + 1;
    } else {
        demo.log('MCP Agent', `Execution failed: ${result.error}`, 'error');
    }
    
    demo.setLoadingState('mcp', false);
}

async function executeHybridCode() {
    const code = document.getElementById('hybrid-code').value;
    const timeout = parseInt(document.getElementById('hybrid-timeout').value);
    
    if (!code.trim()) {
        alert('Please enter Python code to execute');
        return;
    }
    
    demo.setLoadingState('hybrid', true);
    
    const result = await demo.makeRequest('/hybrid/execute', 'POST', {
        code,
        timeout,
        mode: demo.currentHybridMode
    });
    
    const output = demo.formatExecutionResult(result.result, `${result.agentType} (${result.mode})`, result.success);
    demo.updateExecutionResults('hybrid', output, !result.success);
    
    if (result.success) {
        demo.log('Hybrid Agent', `Code executed successfully in ${result.mode} mode`);
        // Update statistics counter
        const currentCount = parseInt(document.getElementById('hybrid-stat-executions').textContent) || 0;
        document.getElementById('hybrid-stat-executions').textContent = currentCount + 1;
    } else {
        demo.log('Hybrid Agent', `Execution failed: ${result.error}`, 'error');
    }
    
    demo.setLoadingState('hybrid', false);
}

async function installLegacyPackages() {
    const packages = ['numpy', 'matplotlib', 'pandas', 'scipy'];
    
    demo.setLoadingState('legacy', true);
    
    const result = await demo.makeRequest('/legacy/install-packages', 'POST', {
        packages
    });
    
    if (result.success) {
        demo.log('Legacy Agent', `Packages installed: ${result.installedPackages?.join(', ') || 'none'}`);
        if (result.failedPackages?.length > 0) {
            demo.log('Legacy Agent', `Failed packages: ${result.failedPackages.join(', ')}`, 'error');
        }
    } else {
        demo.log('Legacy Agent', `Package installation failed: ${result.error}`, 'error');
    }
    
    demo.setLoadingState('legacy', false);
}

async function installMCPPackages() {
    const packages = ['numpy', 'sympy', 'matplotlib', 'pandas', 'scipy'];
    
    demo.setLoadingState('mcp', true);
    
    const result = await demo.makeRequest('/mcp/install-packages', 'POST', {
        packages
    });
    
    if (result.success) {
        demo.log('MCP Agent', `Packages installed: ${result.installedPackages?.join(', ') || 'none'}`);
        if (result.failedPackages?.length > 0) {
            demo.log('MCP Agent', `Failed packages: ${result.failedPackages.join(', ')}`, 'error');
        }
    } else {
        demo.log('MCP Agent', `Package installation failed: ${result.error}`, 'error');
    }
    
    demo.setLoadingState('mcp', false);
}

async function installHybridPackages() {
    const packages = ['numpy', 'matplotlib', 'pandas', 'scipy', 'sympy'];
    
    demo.setLoadingState('hybrid', true);
    
    const result = await demo.makeRequest('/hybrid/install-packages', 'POST', {
        packages,
        mode: demo.currentHybridMode
    });
    
    if (result.success) {
        demo.log('Hybrid Agent', `Packages installed in ${result.mode} mode: ${result.installedPackages?.join(', ') || 'none'}`);
        if (result.failedPackages?.length > 0) {
            demo.log('Hybrid Agent', `Failed packages: ${result.failedPackages.join(', ')}`, 'error');
        }
    } else {
        demo.log('Hybrid Agent', `Package installation failed: ${result.error}`, 'error');
    }
    
    demo.setLoadingState('hybrid', false);
}

async function testMCPConnection() {
    demo.setLoadingState('mcp', true);
    
    const result = await demo.makeRequest('/mcp/connection-test');
    
    if (result.success) {
        demo.log('MCP Agent', `Connection test successful - Response time: ${result.responseTimeMs}ms`);
        document.getElementById('mcp-connection').textContent = result.connectionStatus;
        
        if (result.serverInfo) {
            demo.log('MCP Agent', `Server Info: ${JSON.stringify(result.serverInfo)}`);
        }
    } else {
        demo.log('MCP Agent', `Connection test failed: ${result.error}`, 'error');
        document.getElementById('mcp-connection').textContent = 'Disconnected';
    }
    
    demo.setLoadingState('mcp', false);
}

async function testHybridMCPConnection() {
    demo.setLoadingState('hybrid', true);
    
    const result = await demo.makeRequest('/hybrid/connection-test');
    
    if (result.success) {
        demo.log('Hybrid Agent', `MCP connection test successful - Response time: ${result.responseTimeMs}ms`);
        document.getElementById('hybrid-connection').textContent = result.connectionStatus;
        
        if (result.serverInfo) {
            demo.log('Hybrid Agent', `Server Info: ${JSON.stringify(result.serverInfo)}`);
        }
    } else {
        demo.log('Hybrid Agent', `MCP connection test failed: ${result.error}`, 'error');
        document.getElementById('hybrid-connection').textContent = 'Disconnected';
    }
    
    demo.setLoadingState('hybrid', false);
}

function setHybridMode(mode) {
    demo.currentHybridMode = mode;
    
    // Update UI
    document.querySelectorAll('.mode-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    
    document.querySelector(`[data-mode="${mode}"]`).classList.add('active');
    document.getElementById('hybrid-stat-mode').textContent = mode;
    
    demo.log('Hybrid Agent', `Mode changed to: ${mode}`);
}

function switchResultTab(tabName) {
    // Update tab buttons
    document.querySelectorAll('.result-tab').forEach(tab => {
        tab.classList.remove('active');
    });
    document.querySelector(`[data-tab="${tabName}"]`).classList.add('active');
    
    // Update content
    document.querySelectorAll('.result-content').forEach(content => {
        content.classList.remove('active');
    });
    document.getElementById(`${tabName}-results`).classList.add('active');
    
    demo.log('UI', `Switched to ${tabName} results tab`);
}