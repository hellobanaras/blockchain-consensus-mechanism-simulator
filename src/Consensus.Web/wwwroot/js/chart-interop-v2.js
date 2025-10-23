// Chart.js Interop Module for .NET Blazor Analytics Dashboard
// Provides Chart.js integration with Blazor components

// Global chart instances storage
window.chartInstances = window.chartInstances || {};

// Initialize Chart.js defaults
if (typeof Chart !== 'undefined') {
    Chart.defaults.responsive = true;
    Chart.defaults.maintainAspectRatio = false;
    Chart.defaults.plugins.legend.display = true;
    Chart.defaults.plugins.tooltip.enabled = true;
}

// Create a new chart
export function createChart(canvasId, config) {
    try {
        // Get canvas element
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`Canvas element with id '${canvasId}' not found`);
            return null;
        }

        // Destroy existing chart if it exists
        if (window.chartInstances[canvasId]) {
            window.chartInstances[canvasId].destroy();
            delete window.chartInstances[canvasId];
        }

        // Create new chart
        const ctx = canvas.getContext('2d');
        const chart = new Chart(ctx, config);
        
        // Store chart instance
        window.chartInstances[canvasId] = chart;
        
        console.log(`Chart created successfully for canvas: ${canvasId}`);
        return chart;
        
    } catch (error) {
        console.error(`Error creating chart for ${canvasId}:`, error);
        return null;
    }
}

// Update chart data
export function updateChart(canvasId, newData) {
    try {
        const chart = window.chartInstances[canvasId];
        if (!chart) {
            console.error(`Chart instance not found for canvas: ${canvasId}`);
            return false;
        }

        // Update chart data
        if (newData.labels) {
            chart.data.labels = newData.labels;
        }
        if (newData.datasets) {
            chart.data.datasets = newData.datasets;
        }
        
        chart.update('active');
        
        console.log(`Chart updated successfully for canvas: ${canvasId}`);
        return true;
        
    } catch (error) {
        console.error(`Error updating chart for ${canvasId}:`, error);
        return false;
    }
}

// Destroy chart
export function destroyChart(canvasId) {
    try {
        const chart = window.chartInstances[canvasId];
        if (chart) {
            chart.destroy();
            delete window.chartInstances[canvasId];
            console.log(`Chart destroyed successfully for canvas: ${canvasId}`);
            return true;
        }
        return false;
    } catch (error) {
        console.error(`Error destroying chart for ${canvasId}:`, error);
        return false;
    }
}

// Export chart as image
export function exportChart(canvasId, format = 'png') {
    try {
        const chart = window.chartInstances[canvasId];
        if (!chart) {
            console.error(`Chart instance not found for canvas: ${canvasId}`);
            return false;
        }

        // Generate image
        const url = chart.toBase64Image('image/' + format, 1.0);
        
        // Create download link
        const link = document.createElement('a');
        link.download = `${canvasId}-chart.${format}`;
        link.href = url;
        
        // Trigger download
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        
        console.log(`Chart exported successfully for canvas: ${canvasId}`);
        return true;
        
    } catch (error) {
        console.error(`Error exporting chart for ${canvasId}:`, error);
        return false;
    }
}

// Resize chart
export function resizeChart(canvasId) {
    try {
        const chart = window.chartInstances[canvasId];
        if (chart) {
            chart.resize();
            return true;
        }
        return false;
    } catch (error) {
        console.error(`Error resizing chart for ${canvasId}:`, error);
        return false;
    }
}

// Get chart instance
export function getChartInstance(canvasId) {
    return window.chartInstances[canvasId] || null;
}

// Color palettes
export const colorPalettes = {
    primary: ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF', '#FF9F40', '#FF6384', '#C9CBCF', '#4BC0C0', '#FF6384'],
    pastel: ['#FFB3BA', '#BAFFC9', '#BAE1FF', '#FFFFBA', '#FFD1DC', '#E0BBE4', '#957DAD', '#D291BC', '#FFC9DE', '#C9EEFF'],
    vibrant: ['#E74C3C', '#3498DB', '#F39C12', '#2ECC71', '#9B59B6', '#E67E22', '#1ABC9C', '#34495E', '#F1C40F', '#E91E63'],
    monochrome: ['#000000', '#2C2C2C', '#585858', '#848484', '#B0B0B0', '#DCDCDC', '#F8F8F8', '#FFFFFF', '#696969', '#A9A9A9']
};

// Animation presets
export const animationPresets = {
    bounce: {
        duration: 2000,
        easing: 'easeInBounce'
    },
    fade: {
        duration: 1500,
        easing: 'easeInQuart'
    },
    slide: {
        duration: 1000,
        easing: 'easeOutQuart'
    }
};

// Default chart options
export const defaultOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        legend: {
            display: true,
            position: 'top'
        },
        tooltip: {
            enabled: true,
            mode: 'index',
            intersect: false
        }
    }
};

// Utility functions
export function generateColors(count, palette = 'primary') {
    const colors = colorPalettes[palette] || colorPalettes.primary;
    const result = [];
    
    for (let i = 0; i < count; i++) {
        result.push(colors[i % colors.length]);
    }
    
    return result;
}

export function hexToRgba(hex, alpha = 1) {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    if (result) {
        const r = parseInt(result[1], 16);
        const g = parseInt(result[2], 16);
        const b = parseInt(result[3], 16);
        return `rgba(${r}, ${g}, ${b}, ${alpha})`;
    }
    return hex;
}

// Chart type specific configurations
export const chartConfigs = {
    bar: {
        type: 'bar',
        options: {
            ...defaultOptions,
            scales: {
                y: {
                    beginAtZero: true
                }
            }
        }
    },
    line: {
        type: 'line',
        options: {
            ...defaultOptions,
            elements: {
                line: {
                    tension: 0.1
                }
            }
        }
    },
    pie: {
        type: 'pie',
        options: {
            ...defaultOptions,
            plugins: {
                ...defaultOptions.plugins,
                legend: {
                    display: true,
                    position: 'right'
                }
            }
        }
    },
    doughnut: {
        type: 'doughnut',
        options: {
            ...defaultOptions,
            plugins: {
                ...defaultOptions.plugins,
                legend: {
                    display: true,
                    position: 'right'
                }
            }
        }
    }
};

// Analytics-specific helper functions
export function createWinnerDistributionChart(canvasId, data) {
    const config = {
        type: 'bar',
        data: {
            labels: Object.keys(data),
            datasets: [{
                label: 'Blocks Won',
                data: Object.values(data),
                backgroundColor: generateColors(Object.keys(data).length, 'primary'),
                borderColor: generateColors(Object.keys(data).length, 'primary'),
                borderWidth: 2
            }]
        },
        options: {
            ...chartConfigs.bar.options,
            plugins: {
                ...chartConfigs.bar.options.plugins,
                title: {
                    display: true,
                    text: 'Block Winner Distribution'
                }
            }
        }
    };
    
    return createChart(canvasId, config);
}

export function createAlgorithmDistributionChart(canvasId, data) {
    const config = {
        type: 'pie',
        data: {
            labels: Object.keys(data),
            datasets: [{
                data: Object.values(data),
                backgroundColor: generateColors(Object.keys(data).length, 'vibrant'),
                borderColor: generateColors(Object.keys(data).length, 'vibrant'),
                borderWidth: 2
            }]
        },
        options: {
            ...chartConfigs.pie.options,
            plugins: {
                ...chartConfigs.pie.options.plugins,
                title: {
                    display: true,
                    text: 'Algorithm Distribution'
                }
            }
        }
    };
    
    return createChart(canvasId, config);
}

export function createPerformanceTrendChart(canvasId, labels, datasets) {
    const config = {
        type: 'line',
        data: {
            labels: labels,
            datasets: datasets.map((dataset, index) => ({
                ...dataset,
                borderColor: generateColors(datasets.length, 'primary')[index],
                backgroundColor: hexToRgba(generateColors(datasets.length, 'primary')[index], 0.1),
                borderWidth: 2,
                fill: false,
                tension: 0.1
            }))
        },
        options: {
            ...chartConfigs.line.options,
            plugins: {
                ...chartConfigs.line.options.plugins,
                title: {
                    display: true,
                    text: 'Performance Trends Over Time'
                }
            }
        }
    };
    
    return createChart(canvasId, config);
}

// Initialize module
console.log('Chart.js interop module loaded successfully');

// Ensure Chart.js is available
if (typeof Chart === 'undefined') {
    console.warn('Chart.js is not loaded. Please include Chart.js before this module.');
}