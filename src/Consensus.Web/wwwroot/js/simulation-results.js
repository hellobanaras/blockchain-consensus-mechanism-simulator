// Simulation Results Charts and Utilities

// Initialize round performance chart
window.initializeRoundPerformanceChart = function (roundDataJson) {
    try {
        const roundData = JSON.parse(roundDataJson);
        const ctx = document.getElementById('roundPerformanceChart');
        
        if (!ctx) {
            console.warn('Round performance chart canvas not found');
            return;
        }

        // Destroy existing chart if any
        if (window.roundChart) {
            window.roundChart.destroy();
        }

        window.roundChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: roundData.map(r => `Round ${r.round}`),
                datasets: [
                    {
                        label: 'Duration (ms)',
                        data: roundData.map(r => r.duration),
                        borderColor: 'rgb(75, 192, 192)',
                        backgroundColor: 'rgba(75, 192, 192, 0.2)',
                        yAxisID: 'y'
                    },
                    {
                        label: 'Blocks Accepted',
                        data: roundData.map(r => r.blocks),
                        borderColor: 'rgb(255, 99, 132)',
                        backgroundColor: 'rgba(255, 99, 132, 0.2)',
                        yAxisID: 'y1'
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false,
                },
                scales: {
                    x: {
                        display: true,
                        title: {
                            display: true,
                            text: 'Consensus Rounds'
                        }
                    },
                    y: {
                        type: 'linear',
                        display: true,
                        position: 'left',
                        title: {
                            display: true,
                            text: 'Duration (ms)'
                        }
                    },
                    y1: {
                        type: 'linear',
                        display: true,
                        position: 'right',
                        title: {
                            display: true,
                            text: 'Blocks'
                        },
                        grid: {
                            drawOnChartArea: false,
                        },
                    }
                },
                plugins: {
                    title: {
                        display: true,
                        text: 'Round Performance Over Time'
                    },
                    legend: {
                        display: true
                    }
                }
            }
        });
    } catch (error) {
        console.error('Failed to initialize round performance chart:', error);
    }
};

// Initialize block distribution chart
window.initializeBlockDistributionChart = function (blockDataJson) {
    try {
        const blockData = JSON.parse(blockDataJson);
        const ctx = document.getElementById('blockDistributionChart');
        
        if (!ctx) {
            console.warn('Block distribution chart canvas not found');
            return;
        }

        // Destroy existing chart if any
        if (window.blockChart) {
            window.blockChart.destroy();
        }

        // Generate colors for each node
        const colors = [
            'rgba(255, 99, 132, 0.8)',
            'rgba(54, 162, 235, 0.8)',
            'rgba(255, 205, 86, 0.8)',
            'rgba(75, 192, 192, 0.8)',
            'rgba(153, 102, 255, 0.8)',
            'rgba(255, 159, 64, 0.8)',
            'rgba(199, 199, 199, 0.8)',
            'rgba(83, 102, 255, 0.8)',
            'rgba(255, 99, 255, 0.8)',
            'rgba(99, 255, 132, 0.8)'
        ];

        window.blockChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: blockData.map(d => d.label),
                datasets: [{
                    label: 'Blocks Accepted',
                    data: blockData.map(d => d.value),
                    backgroundColor: colors.slice(0, blockData.length),
                    borderColor: colors.slice(0, blockData.length).map(color => color.replace('0.8', '1')),
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: 'Block Production by Node'
                    },
                    legend: {
                        display: true,
                        position: 'bottom'
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = ((context.raw / total) * 100).toFixed(1);
                                return `${context.label}: ${context.raw} blocks (${percentage}%)`;
                            }
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Failed to initialize block distribution chart:', error);
    }
};

// File download utility
window.downloadFile = function (fileName, contentType, base64Data) {
    try {
        // Convert base64 to blob
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: contentType });

        // Create download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        
        // Trigger download
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        
        // Clean up
        window.URL.revokeObjectURL(url);
        
        console.log(`Downloaded file: ${fileName}`);
    } catch (error) {
        console.error('Failed to download file:', error);
        alert('Download failed. Please try again.');
    }
};

// Utility to refresh charts when data changes
window.refreshCharts = function () {
    if (window.roundChart) {
        window.roundChart.update();
    }
    if (window.blockChart) {
        window.blockChart.update();
    }
};

// Auto-refresh functionality for live simulations
window.setupAutoRefresh = function (intervalMs, refreshCallback) {
    if (window.refreshInterval) {
        clearInterval(window.refreshInterval);
    }
    
    window.refreshInterval = setInterval(() => {
        refreshCallback.invokeMethodAsync('RefreshData');
    }, intervalMs);
};

window.stopAutoRefresh = function () {
    if (window.refreshInterval) {
        clearInterval(window.refreshInterval);
        window.refreshInterval = null;
    }
};

// Format large numbers with appropriate suffixes
window.formatNumber = function (num) {
    if (num >= 1e9) {
        return (num / 1e9).toFixed(1) + 'B';
    }
    if (num >= 1e6) {
        return (num / 1e6).toFixed(1) + 'M';
    }
    if (num >= 1e3) {
        return (num / 1e3).toFixed(1) + 'K';
    }
    return num.toString();
};

// Initialize tooltips for better UX
window.initializeTooltips = function () {
    try {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    } catch (error) {
        console.warn('Bootstrap tooltips not available:', error);
    }
};

// Print simulation results
window.printResults = function () {
    window.print();
};

// Copy simulation summary to clipboard
window.copyToClipboard = function (text) {
    try {
        navigator.clipboard.writeText(text).then(() => {
            console.log('Copied to clipboard');
        }).catch(err => {
            console.error('Failed to copy to clipboard:', err);
        });
    } catch (error) {
        console.error('Clipboard API not available:', error);
    }
};

// Initialize page components
document.addEventListener('DOMContentLoaded', function () {
    initializeTooltips();
});

console.log('Simulation results utilities loaded successfully');