// Hybrid Python GAgent Demo JavaScript
class HybridPythonDemo {
    constructor() {
        this.currentMode = 'auto';
        this.apiBase = '/api/PythonVerificationDemo';
        this.agentId = '33333333-3333-3333-3333-333333333333'; // Fixed GUID for demo
        this.availableLLMs = [];
        this.selectedLLM = null;
        this.executionHistory = [];
        this.statistics = {
            totalExecutions: 0,
            successfulExecutions: 0,
            totalExecutionTime: 0
        };
        this.init();
    }

    init() {
        this.loadAvailableLLMs();
        this.setupEventListeners();
        this.checkAgentStatus();
    }

    setupEventListeners() {
        // Auto-refresh statistics every 10 seconds
        setInterval(() => {
            this.updateStatistics();
        }, 10000);

        // Load LLM configurations on page load
        document.addEventListener('DOMContentLoaded', () => {
            this.loadAvailableLLMs();
        });
    }

    async loadAvailableLLMs() {
        try {
            this.log('System', 'Loading available LLM configurations...');
            const response = await fetch('/api/llm-configs/list');
            const llmConfigs = await response.json();
            
            const selectElement = document.getElementById('llm-select');
            selectElement.innerHTML = '';
            
            if (llmConfigs && llmConfigs.length > 0) {
                // The API returns an array of LLM key names
                this.availableLLMs = Array.isArray(llmConfigs) ? llmConfigs : [llmConfigs];
                
                // Add default option
                const defaultOption = document.createElement('option');
                defaultOption.value = '';
                defaultOption.textContent = 'None (Legacy mode only)';
                selectElement.appendChild(defaultOption);
                
                // Add LLM options
                this.availableLLMs.forEach(key => {
                    const option = document.createElement('option');
                    option.value = key;
                    option.textContent = key;
                    selectElement.appendChild(option);
                });
                
                // Set DeepSeek as default if available
                if (this.availableLLMs.includes('DeepSeek')) {
                    selectElement.value = 'DeepSeek';
                    this.selectedLLM = 'DeepSeek';
                } else if (this.availableLLMs.length > 0) {
                    selectElement.value = this.availableLLMs[0];
                    this.selectedLLM = this.availableLLMs[0];
                }
                
                this.log('System', `Loaded ${this.availableLLMs.length} LLM configurations: ${this.availableLLMs.join(', ')}`);
            } else {
                const noOption = document.createElement('option');
                noOption.value = '';
                noOption.textContent = 'No LLM configurations available - Click "Configure LLM API Key" to set up';
                selectElement.appendChild(noOption);
                this.log('System', 'No LLM configurations found. Please configure LLM API Key first.');
                
                // Show helpful message
                setTimeout(() => {
                    alert('⚠️ No LLM configurations found!\n\nTo enable AI features, please:\n1. Click "🔧 Configure LLM API Key" button\n2. Set up your API keys in the system configuration\n3. Refresh this page');
                }, 1000);
            }

            // Add change event listener
            selectElement.addEventListener('change', (e) => {
                this.selectedLLM = e.target.value;
                this.log('System', `Selected LLM: ${this.selectedLLM || 'None'}`);
            });

        } catch (error) {
            this.log('System', `Failed to load LLM configurations: ${error.message}`, 'error');
            const selectElement = document.getElementById('llm-select');
            selectElement.innerHTML = '<option value="">Error loading LLM configs - Click "Configure LLM API Key" to set up</option>';
            
            // Show error message with guidance
            setTimeout(() => {
                alert('❌ Failed to load LLM configurations!\n\nThis might be because:\n1. No LLM configurations exist yet\n2. Network connection issue\n3. Server is not running\n\nSolution:\n- Click "🔧 Configure LLM API Key" to set up LLM configurations\n- Make sure the server is running');
            }, 1000);
        }
    }

    async checkAgentStatus() {
        try {
            const response = await fetch(`${this.apiBase}/status`);
            const data = await response.json();
            
            if (data.success) {
                this.updateStatusIndicator('ready');
                this.updateAgentState('Available');
                this.log('System', 'Hybrid Python GAgent is available');
            } else {
                this.updateStatusIndicator('error');
                this.updateAgentState('Error');
                this.log('System', `Agent status check failed: ${data.error}`, 'error');
            }
        } catch (error) {
            this.updateStatusIndicator('error');
            this.updateAgentState('Unavailable');
            this.log('System', `Failed to check agent status: ${error.message}`, 'error');
        }
    }

    updateStatusIndicator(status) {
        const indicator = document.getElementById('agent-status');
        if (indicator) {
            indicator.className = `status-indicator ${status}`;
        }
    }

    updateAgentState(state) {
        const element = document.getElementById('agent-state');
        if (element) {
            element.textContent = state;
        }
    }

    showLoading() {
        document.getElementById('loading-overlay').style.display = 'flex';
    }

    hideLoading() {
        document.getElementById('loading-overlay').style.display = 'none';
    }

    log(source, message, type = 'info') {
        const timestamp = new Date().toLocaleTimeString();
        const logMessage = `[${timestamp}] ${source}: ${message}`;
        
        console.log(logMessage);
        
        if (type === 'error') {
            console.error(logMessage);
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

    displayResults(result, debugInfo = null) {
        const outputElement = document.getElementById('execution-output');
        const debugElement = document.getElementById('debug-info');
        const debugContentElement = document.getElementById('debug-content');
        
        if (outputElement) {
            let output = '';
            const timestamp = new Date().toLocaleString();
            
            if (result.success) {
                output = `🚀 Execution completed successfully at ${timestamp}\n`;
                output += `Agent Type: ${result.agentType}\n`;
                output += `Execution Mode: ${result.mode || 'unknown'}\n`;
                output += `Actual Mode: ${result.actualExecutionMode || 'unknown'}\n`;
                output += `Execution Time: ${result.result?.executionTime || 0}ms\n`;
                output += `Total Time: ${result.result?.totalExecutionTimeMs || 0}ms\n`;
                output += `Memory Used: ${result.result?.memoryUsed || 0}MB\n`;
                output += `Exit Code: ${result.result?.exitCode || 0}\n`;
                
                if (result.result?.timedOut) {
                    output += `⚠️ Execution timed out\n`;
                }
                
                output += '\n📤 Standard Output:\n';
                output += result.result?.standardOutput || '(no output)';
                
                if (result.result?.errorOutput) {
                    output += '\n\n⚠️ Error Output:\n';
                    output += result.result.errorOutput;
                }
                
                outputElement.className = 'result-output success';
                
                // Update statistics
                this.statistics.totalExecutions++;
                this.statistics.successfulExecutions++;
                this.statistics.totalExecutionTime += (result.result?.totalExecutionTimeMs || 0);
                
            } else {
                output = `❌ Execution failed at ${timestamp}\n`;
                output += `Agent Type: ${result.agentType}\n`;
                output += `Error: ${result.error || 'Unknown error'}\n`;
                
                outputElement.className = 'result-output error';
                
                // Update statistics
                this.statistics.totalExecutions++;
            }
            
            outputElement.textContent = output;
            
            // Store in execution history
            this.executionHistory.push({
                timestamp: new Date(),
                result,
                debugInfo
            });
            
            // Keep only last 10 executions
            if (this.executionHistory.length > 10) {
                this.executionHistory = this.executionHistory.slice(-10);
            }
        }
        
        // Display debug information
        if (debugInfo && debugElement && debugContentElement) {
            debugElement.style.display = 'block';
            
            let debugHtml = '';
            Object.entries(debugInfo).forEach(([key, value]) => {
                const displayKey = key.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase());
                const displayValue = typeof value === 'object' ? JSON.stringify(value) : String(value);
                
                debugHtml += `
                    <div class="debug-item">
                        <span class="debug-label">${displayKey}:</span>
                        <span class="debug-value">${displayValue}</span>
                    </div>
                `;
            });
            
            debugContentElement.innerHTML = debugHtml;
        }
        
        // Update statistics display
        this.updateStatisticsDisplay();
    }

    updateStatisticsDisplay() {
        const executionCountElement = document.getElementById('execution-count');
        const successRateElement = document.getElementById('success-rate');
        const avgTimeElement = document.getElementById('avg-time');
        
        if (executionCountElement) {
            executionCountElement.textContent = this.statistics.totalExecutions;
        }
        
        if (successRateElement) {
            const rate = this.statistics.totalExecutions > 0 
                ? ((this.statistics.successfulExecutions / this.statistics.totalExecutions) * 100).toFixed(1)
                : 0;
            successRateElement.textContent = `${rate}%`;
        }
        
        if (avgTimeElement) {
            const avgTime = this.statistics.successfulExecutions > 0 
                ? (this.statistics.totalExecutionTime / this.statistics.successfulExecutions).toFixed(1)
                : 0;
            avgTimeElement.textContent = avgTime > 0 ? `${avgTime}ms` : '-';
        }
    }

    async updateStatistics() {
        try {
            const response = await fetch(`${this.apiBase}/statistics`);
            const data = await response.json();
            
            if (data.success && data.statistics?.hybrid) {
                const hybridStats = data.statistics.hybrid;
                
                // Update statistics from server
                Object.entries(hybridStats).forEach(([key, value]) => {
                    const element = document.getElementById(`stat-${key}`);
                    if (element) {
                        element.textContent = typeof value === 'number' ? value.toFixed(1) : value;
                    }
                });
            }
        } catch (error) {
            this.log('Statistics', `Failed to update statistics: ${error.message}`, 'error');
        }
    }
}

// Initialize the demo
const demo = new HybridPythonDemo();

// Global functions for HTML onclick handlers

// Open LLM configuration page
function openLLMConfig() {
    demo.log('System', 'Opening LLM configuration interface...');
    window.open('/', '_blank');
}

async function initializeAgent() {
    demo.updateStatusIndicator('loading');
    demo.updateAgentState('Initializing...');
    
    const selectedLLM = document.getElementById('llm-select').value;
    
    const result = await demo.makeRequest('/hybrid/initialize', 'POST', {
        mode: demo.currentMode,
        systemLLM: selectedLLM || null
    });
    
    if (result.success) {
        demo.updateStatusIndicator('ready');
        demo.updateAgentState('Initialized');
        demo.log('Agent', `Initialized successfully in ${result.mode} mode with LLM: ${result.llmSystem}`);
        
        document.getElementById('current-mode').textContent = result.mode;
        
        // Display debug info
        if (result.debugInfo) {
            demo.displayResults(result, result.debugInfo);
        }
    } else {
        demo.updateStatusIndicator('error');
        demo.updateAgentState('Failed');
        demo.log('Agent', `Initialization failed: ${result.error}`, 'error');
        
        if (result.debugInfo) {
            demo.displayResults(result, result.debugInfo);
        }
    }
}

async function testConnections() {
    demo.updateStatusIndicator('loading');
    
    const result = await demo.makeRequest('/hybrid/connection-test');
    
    if (result.success) {
        demo.updateStatusIndicator('ready');
        document.getElementById('connection-status').textContent = result.connectionStatus;
        demo.log('Connection', result.message);
    } else {
        demo.updateStatusIndicator('error');
        document.getElementById('connection-status').textContent = 'Failed';
        demo.log('Connection', result.error, 'error');
    }
}

async function executeCode() {
    const code = document.getElementById('python-code').value;
    const timeout = parseInt(document.getElementById('timeout').value);
    
    if (!code.trim()) {
        alert('Please enter Python code to execute');
        return;
    }
    
    demo.updateStatusIndicator('loading');
    
    const result = await demo.makeRequest('/hybrid/execute', 'POST', {
        code,
        timeout,
        mode: demo.currentMode
    });
    
    demo.displayResults(result, result.debugInfo);
    
    if (result.success) {
        demo.updateStatusIndicator('ready');
        demo.log('Execution', `Code executed successfully in ${result.actualExecutionMode} mode`);
        
        // Update current mode display
        if (result.actualExecutionMode) {
            document.getElementById('current-mode').textContent = result.actualExecutionMode;
        }
    } else {
        demo.updateStatusIndicator('error');
        demo.log('Execution', `Code execution failed: ${result.error}`, 'error');
    }
}

async function installPackages() {
    const packages = ['numpy', 'matplotlib', 'pandas', 'scipy', 'sympy'];
    
    demo.updateStatusIndicator('loading');
    
    const result = await demo.makeRequest('/hybrid/install-packages', 'POST', {
        packages,
        mode: demo.currentMode
    });
    
    if (result.success) {
        demo.updateStatusIndicator('ready');
        demo.log('Packages', result.message);
        
        const output = `📦 Package Installation Results\n` +
                      `Mode: ${result.mode}\n` +
                      `Status: ${result.success ? 'Success' : 'Failed'}\n` +
                      `Message: ${result.message}\n` +
                      `Installed: ${result.installedPackages?.join(', ') || 'none'}\n` +
                      `Failed: ${result.failedPackages?.join(', ') || 'none'}`;
        
        document.getElementById('execution-output').textContent = output;
        document.getElementById('execution-output').className = 'result-output success';
    } else {
        demo.updateStatusIndicator('error');
        demo.log('Packages', `Package installation failed: ${result.error}`, 'error');
        
        document.getElementById('execution-output').textContent = `❌ Package installation failed: ${result.error}`;
        document.getElementById('execution-output').className = 'result-output error';
    }
}

function setExecutionMode(mode) {
    demo.currentMode = mode;
    
    // Update UI
    document.querySelectorAll('.mode-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    
    document.querySelector(`[data-mode="${mode}"]`).classList.add('active');
    document.getElementById('current-mode').textContent = mode;
    
    demo.log('Mode', `Execution mode changed to: ${mode}`);
}