// LLM Configuration Management
const llmConfigs = {
  // DOM Elements
  modal: null,
  configBtn: null,
  closeBtn: null,
  saveBtn: null,
  llmSelect: null,
  apiKeyInput: null,
  statusMsg: null,

  // Initialize the LLM config module
  init() {
    this.modal = document.getElementById('configModal');
    this.configBtn = document.getElementById('configBtn');
    this.closeBtn = document.getElementById('modalCloseBtn');
    this.saveBtn = document.getElementById('modalSaveBtn');
    this.llmSelect = document.getElementById('llmSelect');
    this.apiKeyInput = document.getElementById('apiKeyInput');
    this.statusMsg = document.getElementById('configStatusMsg');

    // Bind event handlers
    this.configBtn.onclick = () => this.showModal();
    this.closeBtn.onclick = () => this.hideModal();
    this.saveBtn.onclick = () => this.saveConfig();

    // Initial setup
    this.populateLLMList();
    this.checkConfig();
  },

  // Show the configuration modal
  showModal() {
    this.modal.style.display = 'flex';
  },

  // Hide the configuration modal
  hideModal() {
    this.modal.style.display = 'none';
  },

  // Populate the LLM dropdown list
  async populateLLMList() {
    try {
      const resp = await fetch('/api/llm-configs/list');
      const llms = await resp.json();
      this.llmSelect.innerHTML = llms.map(llm => 
        `<option value="${llm}">${llm}</option>`
      ).join('');
    } catch (error) {
      console.error('Error fetching LLM list:', error);
      this.showError('Failed to load LLM list');
    }
  },

  // Check current LLM configuration
  async checkConfig() {
    try {
      const resp = await fetch('/api/llm-configs/check');
      const data = await resp.json();
      if (!data.ok) {
        this.showModal();
      }
    } catch (error) {
      console.error('Error checking config:', error);
      this.showError('Failed to check configuration');
    }
  },

  // Save LLM configuration
  async saveConfig() {
    const llm = this.llmSelect.value;
    const apiKey = this.apiKeyInput.value.trim();
    
    if (!apiKey) {
      this.showError('Please enter an API key');
      return;
    }

    try {
      this.statusMsg.textContent = 'Saving configuration...';
      this.saveBtn.disabled = true;

      const resp = await fetch('/api/llm-configs', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ llm, apiKey })
      });

      if (resp.ok) {
        this.statusMsg.textContent = 'Configuration saved successfully!';
        this.statusMsg.style.color = '#059669';
        setTimeout(() => this.hideModal(), 1500);
      } else {
        const error = await resp.text();
        this.showError(`Failed to save configuration: ${error}`);
      }
    } catch (error) {
      console.error('Error saving config:', error);
      this.showError('Failed to save configuration');
    } finally {
      this.saveBtn.disabled = false;
    }
  },

  // Show error message
  showError(message) {
    this.statusMsg.textContent = message;
    this.statusMsg.style.color = '#dc2626';
  }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => llmConfigs.init()); 