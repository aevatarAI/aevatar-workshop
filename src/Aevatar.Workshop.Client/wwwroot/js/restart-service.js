// Restart Service Management
const restartService = {
  // DOM Elements
  restartBtn: null,

  // Initialize the restart service module
  init() {
    this.restartBtn = document.getElementById('restartBtn');

    // Bind event handlers
    if (this.restartBtn) {
      this.restartBtn.onclick = () => this.restartServices();
    }
  },

  // Restart all services
  async restartServices() {
    if (!this.restartBtn) {
      console.error('Restart button not found');
      return;
    }

    this.restartBtn.disabled = true;
    this.restartBtn.style.background = '#f87171';

    try {
      const resp = await fetch('/api/restart', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({})
      });
      
      if (resp.ok) {
        this.restartBtn.style.background = '#34d399';
        alert('Restarting services in background, you can close current page now.');
      } else {
        this.restartBtn.style.background = '#f87171';
        alert('Failed to restart services automatically, please run `sh quickstart.sh` manually.');
      }
    } catch (error) {
      this.restartBtn.style.background = '#f87171';
      alert('Failed to restart services automatically, please run `sh quickstart.sh` manually.');
    } finally {
      this.restartBtn.disabled = false;
    }
  },
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => restartService.init()); 