// Restart Service Management
const restartService = {
  // DOM Elements
  restartBtn: null,
  restartMsg: null,

  // Initialize the restart service module
  init() {
    this.restartBtn = document.getElementById('restartBtn');
    this.restartMsg = document.getElementById('restartMsg');

    // Bind event handlers
    if (this.restartBtn) {
      this.restartBtn.onclick = () => this.restartServices();
    }
  },

  // Restart all services
  async restartServices() {
    if (!this.restartBtn || !this.restartMsg) {
      console.error('Required DOM elements not found');
      return;
    }

    console.log('DEBUG: Restart button clicked');
    this.restartBtn.disabled = true;
    this.restartBtn.style.background = '#f87171';
    this.restartMsg.textContent = 'Restarting, please wait...';

    try {
      console.log('DEBUG: Sending restart request to /api/restart');
      const resp = await fetch('/api/restart', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
      });

      console.log('DEBUG: Restart response status:', resp.status);
      
      if (resp.ok) {
        const text = await resp.text();
        console.log('DEBUG: Restart success:', text);
        this.restartMsg.textContent = text;
        this.restartBtn.style.background = '#34d399';
        
        // Wait for services to restart
        await this.waitForServices();
      } else {
        const error = await resp.text();
        this.showError('Failed to restart services. Please run \`sh quickstart.sh\` manually.');
      }
    } catch (error) {
      this.showError('Failed to restart services. Please run \`sh quickstart.sh\` manually.');
    } finally {
      this.restartBtn.disabled = false;
    }
  },

  // Wait for services to come back online
  async waitForServices() {
    let attempts = 0;
    const maxAttempts = 30; // 30 seconds timeout
    
    while (attempts < maxAttempts) {
      try {
        const resp = await fetch('/api/health');
        if (resp.ok) {
          console.log('DEBUG: Services are back online');
          this.restartMsg.textContent = 'Services restarted successfully!';
          return;
        }
      } catch (error) {
        console.log('DEBUG: Waiting for services to come back online...');
      }
      
      await new Promise(resolve => setTimeout(resolve, 1000));
      attempts++;
    }
    
    this.showError('Services may not have restarted properly. Please check the logs.');
  },

  // Show error message
  showError(message) {
    this.restartMsg.textContent = message;
    this.restartBtn.style.background = '#f87171';
  }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => restartService.init()); 