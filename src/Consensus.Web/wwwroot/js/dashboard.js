/**
 * Toast notifications for the consensus simulator
 */

window.showToast = function (message, type = 'info', duration = 3000) {
    // Create toast container if it doesn't exist
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '1055';
        document.body.appendChild(container);
    }

    // Create toast element
    const toastId = 'toast-' + Date.now();
    const toastElement = document.createElement('div');
    toastElement.id = toastId;
    toastElement.className = 'toast align-items-center border-0';
    toastElement.setAttribute('role', 'alert');
    toastElement.setAttribute('aria-live', 'assertive');
    toastElement.setAttribute('aria-atomic', 'true');

    // Set background color based on type
    const bgClass = {
        'success': 'bg-success text-white',
        'warning': 'bg-warning text-dark',
        'error': 'bg-danger text-white',
        'danger': 'bg-danger text-white',
        'info': 'bg-primary text-white',
        'primary': 'bg-primary text-white'
    }[type] || 'bg-light text-dark';

    toastElement.classList.add(...bgClass.split(' '));

    // Set toast content
    toastElement.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${getToastIcon(type)} ${message}
            </div>
            <button type="button" class="btn-close btn-close-${type === 'warning' ? 'dark' : 'white'} me-2 m-auto" 
                    data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;

    // Add to container
    container.appendChild(toastElement);

    // Initialize Bootstrap toast
    const bsToast = new bootstrap.Toast(toastElement, {
        delay: duration,
        autohide: true
    });

    // Remove element after hiding
    toastElement.addEventListener('hidden.bs.toast', function () {
        toastElement.remove();
    });

    // Show toast
    bsToast.show();
};

function getToastIcon(type) {
    const icons = {
        'success': '<i class="bi bi-check-circle me-2"></i>',
        'warning': '<i class="bi bi-exclamation-triangle me-2"></i>',
        'error': '<i class="bi bi-x-circle me-2"></i>',
        'danger': '<i class="bi bi-x-circle me-2"></i>',
        'info': '<i class="bi bi-info-circle me-2"></i>',
        'primary': '<i class="bi bi-info-circle me-2"></i>'
    };
    return icons[type] || '<i class="bi bi-info-circle me-2"></i>';
}

/**
 * Auto-scroll console logs to bottom
 */
window.scrollToBottom = function (elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

/**
 * Copy text to clipboard with feedback
 */
window.copyToClipboard = function (text, successMessage = 'Copied to clipboard!') {
    navigator.clipboard.writeText(text).then(function () {
        showToast(successMessage, 'success', 2000);
    }).catch(function (err) {
        console.error('Could not copy text: ', err);
        showToast('Failed to copy to clipboard', 'error', 2000);
    });
};

/**
 * Download data as JSON file
 */
window.downloadJson = function (data, filename = 'simulation-data.json') {
    const jsonStr = JSON.stringify(data, null, 2);
    const blob = new Blob([jsonStr], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    
    showToast(`Downloaded ${filename}`, 'success', 2000);
};

/**
 * Download data as CSV file
 */
window.downloadCsv = function (data, filename = 'simulation-data.csv') {
    let csvContent = '';
    
    if (Array.isArray(data) && data.length > 0) {
        // Get headers from first object
        const headers = Object.keys(data[0]);
        csvContent += headers.join(',') + '\n';
        
        // Add data rows
        data.forEach(row => {
            const values = headers.map(header => {
                let value = row[header];
                if (typeof value === 'string') {
                    value = '"' + value.replace(/"/g, '""') + '"';
                }
                return value || '';
            });
            csvContent += values.join(',') + '\n';
        });
    }
    
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    
    showToast(`Downloaded ${filename}`, 'success', 2000);
};

/**
 * Format bytes to human readable format
 */
window.formatBytes = function (bytes, decimals = 2) {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
    
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
};

/**
 * Format duration to human readable format
 */
window.formatDuration = function (milliseconds) {
    if (milliseconds < 1000) {
        return milliseconds + 'ms';
    }
    
    const seconds = Math.floor(milliseconds / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);
    
    if (days > 0) {
        return `${days}d ${hours % 24}h ${minutes % 60}m`;
    } else if (hours > 0) {
        return `${hours}h ${minutes % 60}m ${seconds % 60}s`;
    } else if (minutes > 0) {
        return `${minutes}m ${seconds % 60}s`;
    } else {
        return `${seconds}s`;
    }
};

/**
 * Debounce function for search inputs and other frequent events
 */
window.debounce = function (func, wait, immediate) {
    let timeout;
    return function executedFunction() {
        const context = this;
        const args = arguments;
        const later = function () {
            timeout = null;
            if (!immediate) func.apply(context, args);
        };
        const callNow = immediate && !timeout;
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
        if (callNow) func.apply(context, args);
    };
};

/**
 * Initialize charts (placeholder for Chart.js integration)
 */
window.initializeChart = function (canvasId, chartType, data, options) {
    console.log(`Initializing ${chartType} chart on ${canvasId}`, data, options);
    // TODO: Implement Chart.js integration when charts are needed
    return {
        update: function (newData) {
            console.log(`Updating chart ${canvasId}`, newData);
        },
        destroy: function () {
            console.log(`Destroying chart ${canvasId}`);
        }
    };
};

/**
 * Connection status indicator
 */
window.updateConnectionStatus = function (status) {
    const indicators = document.querySelectorAll('.connection-indicator');
    indicators.forEach(indicator => {
        indicator.className = 'connection-indicator';
        switch (status.toLowerCase()) {
            case 'connected':
                indicator.classList.add('bg-success');
                indicator.title = 'Connected to real-time updates';
                break;
            case 'connecting':
            case 'reconnecting':
                indicator.classList.add('bg-warning');
                indicator.title = 'Connecting to real-time updates...';
                break;
            case 'disconnected':
            default:
                indicator.classList.add('bg-danger');
                indicator.title = 'Disconnected from real-time updates';
                break;
        }
    });
};

/**
 * Console.log equivalent for debugging in production
 */
if (typeof console === 'undefined' || !console.log) {
    window.console = {
        log: function () { },
        error: function () { },
        warn: function () { },
        info: function () { }
    };
}

// Initialize on DOM ready
document.addEventListener('DOMContentLoaded', function () {
    console.log('Consensus Simulator Dashboard JavaScript loaded');
    
    // Add connection status indicator if it doesn't exist
    if (!document.querySelector('.connection-indicator')) {
        const indicator = document.createElement('div');
        indicator.className = 'connection-indicator position-fixed bottom-0 end-0 m-3 rounded-circle bg-secondary';
        indicator.style.width = '12px';
        indicator.style.height = '12px';
        indicator.style.zIndex = '1060';
        indicator.title = 'Connection status';
        document.body.appendChild(indicator);
    }
});