// File download utilities for analytics dashboard
window.downloadFile = function (fileName, base64Data) {
    try {
        // Convert base64 to blob
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        
        // Determine MIME type based on file extension
        const extension = fileName.split('.').pop().toLowerCase();
        let mimeType = 'application/octet-stream';
        
        switch (extension) {
            case 'csv':
                mimeType = 'text/csv';
                break;
            case 'json':
                mimeType = 'application/json';
                break;
            case 'pdf':
                mimeType = 'application/pdf';
                break;
            case 'xlsx':
                mimeType = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
                break;
            case 'txt':
                mimeType = 'text/plain';
                break;
            case 'png':
                mimeType = 'image/png';
                break;
            case 'jpg':
            case 'jpeg':
                mimeType = 'image/jpeg';
                break;
        }
        
        // Create blob and download
        const blob = new Blob([byteArray], { type: mimeType });
        const url = window.URL.createObjectURL(blob);
        
        // Create temporary download link
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        a.style.display = 'none';
        
        // Trigger download
        document.body.appendChild(a);
        a.click();
        
        // Cleanup
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        
        console.log(`Downloaded file: ${fileName}`);
        return true;
    } catch (error) {
        console.error('Error downloading file:', error);
        return false;
    }
};

// Export current page as PDF (using browser print functionality)
window.exportPageAsPdf = function (title = 'Analytics Dashboard') {
    try {
        // Set page title for the print dialog
        const originalTitle = document.title;
        document.title = title;
        
        // Trigger browser print dialog
        window.print();
        
        // Restore original title
        document.title = originalTitle;
        
        return true;
    } catch (error) {
        console.error('Error exporting page as PDF:', error);
        return false;
    }
};

// Copy data to clipboard
window.copyToClipboard = function (text) {
    try {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            return navigator.clipboard.writeText(text);
        } else {
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = text;
            textArea.style.position = 'fixed';
            textArea.style.left = '-999999px';
            textArea.style.top = '-999999px';
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            
            const successful = document.execCommand('copy');
            document.body.removeChild(textArea);
            
            return Promise.resolve(successful);
        }
    } catch (error) {
        console.error('Error copying to clipboard:', error);
        return Promise.reject(error);
    }
};

// Show notification to user
window.showNotification = function (message, type = 'info') {
    try {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
        notification.style.top = '20px';
        notification.style.right = '20px';
        notification.style.zIndex = '9999';
        notification.style.maxWidth = '300px';
        
        notification.innerHTML = `
            ${message}
            <button type="button" class="btn-close" onclick="this.parentElement.remove()"></button>
        `;
        
        // Add to page
        document.body.appendChild(notification);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (notification.parentElement) {
                notification.remove();
            }
        }, 5000);
        
        return true;
    } catch (error) {
        console.error('Error showing notification:', error);
        return false;
    }
};

// Format number for display
window.formatNumber = function (value, decimals = 2) {
    try {
        if (typeof value !== 'number') {
            value = parseFloat(value);
        }
        
        if (isNaN(value)) {
            return '0';
        }
        
        return value.toLocaleString(undefined, {
            minimumFractionDigits: decimals,
            maximumFractionDigits: decimals
        });
    } catch (error) {
        console.error('Error formatting number:', error);
        return value.toString();
    }
};

// Export chart data as image using html2canvas (if available)
window.exportChartAsImage = async function (chartId, format = 'png') {
    try {
        const canvas = document.getElementById(chartId);
        if (!canvas) {
            throw new Error(`Chart with ID '${chartId}' not found`);
        }
        
        // Get chart image data
        const imageData = canvas.toDataURL(`image/${format}`);
        
        // Convert to blob and download
        const base64Data = imageData.split(',')[1];
        const fileName = `chart-${chartId}-${new Date().getTime()}.${format}`;
        
        return window.downloadFile(fileName, base64Data);
    } catch (error) {
        console.error('Error exporting chart as image:', error);
        return false;
    }
};

console.log('Analytics dashboard utilities loaded successfully');