// Chart.js interop module for Blazor analytics dashboard
import Chart from 'https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.js';

// Chart instances registry
const chartInstances = new Map();

// Default chart configuration
const defaultConfig = {
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
    },
    interaction: {
        mode: 'nearest',
        axis: 'x',
        intersect: false
    }
};

// Color palettes
const colorPalettes = {
    primary: ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF', '#FF9F40', '#FF6384', '#C9CBCF'],
    pastel: ['#FFB3BA', '#BAFFC9', '#BAE1FF', '#FFFFBA', '#FFD1DC', '#E0BBE4', '#957DAD', '#D291BC'],
    vibrant: ['#E74C3C', '#3498DB', '#F39C12', '#2ECC71', '#9B59B6', '#E67E22', '#1ABC9C', '#34495E']
};

// Chart creation function
export function createChart(canvasId, chartData) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        throw new Error(`Canvas element with id '${canvasId}' not found`);
    }

    const ctx = canvas.getContext('2d');
    
    // Process chart data and configuration
    const config = processChartConfig(chartData);
    
    // Create Chart.js instance
    const chart = new Chart(ctx, config);
    
    // Store chart instance for future reference
    chartInstances.set(canvasId, chart);
    
    return {
        updateData: (newData) => updateChartData(chart, newData),
        destroy: () => destroyChart(canvasId),
        downloadChart: (format) => downloadChart(chart, format),
        getChart: () => chart
    };
}

// Process chart configuration from Blazor data
function processChartConfig(chartData) {
    const config = {
        type: chartData.type || 'bar',
        data: {
            labels: chartData.labels || [],
            datasets: processDatasets(chartData.datasets || [], chartData.type)
        },
        options: mergeDeep(defaultConfig, chartData.options || {})
    };

    // Apply chart type specific configurations
    applyChartTypeConfig(config);

    return config;
}

// Process datasets with color assignments
function processDatasets(datasets, chartType) {
    return datasets.map((dataset, index) => {
        const processedDataset = { ...dataset };
        
        // Assign colors if not provided
        if (!dataset.backgroundColor || dataset.backgroundColor.length === 0) {
            processedDataset.backgroundColor = getColors(dataset.data.length, index, 0.8);
        }
        
        if (!dataset.borderColor || dataset.borderColor.length === 0) {
            processedDataset.borderColor = getColors(dataset.data.length, index, 1.0);
        }

        // Set default border width
        if (processedDataset.borderWidth === undefined) {
            processedDataset.borderWidth = chartType === 'line' ? 2 : 1;
        }

        // Set line chart specific properties
        if (chartType === 'line') {
            processedDataset.fill = processedDataset.fill !== undefined ? processedDataset.fill : false;
            processedDataset.tension = processedDataset.tension !== undefined ? processedDataset.tension : 0.1;
        }

        return processedDataset;
    });
}

// Get colors for chart elements
function getColors(count, datasetIndex = 0, alpha = 1.0) {
    const palette = colorPalettes.primary;
    const colors = [];
    
    for (let i = 0; i < count; i++) {
        const colorIndex = (i + datasetIndex) % palette.length;
        const baseColor = palette[colorIndex];
        
        if (alpha < 1.0) {
            // Convert hex to rgba
            const rgba = hexToRgba(baseColor, alpha);
            colors.push(rgba);
        } else {
            colors.push(baseColor);
        }
    }
    
    return colors;
}

// Convert hex color to rgba
function hexToRgba(hex, alpha) {
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

// Apply chart type specific configurations
function applyChartTypeConfig(config) {
    switch (config.type) {
        case 'bar':
            config.options.scales = {
                ...config.options.scales,
                y: {
                    beginAtZero: true,
                    ...config.options.scales?.y
                }
            };
            break;
            
        case 'line':
            config.options.scales = {
                ...config.options.scales,
                x: {
                    display: true,
                    ...config.options.scales?.x
                },
                y: {
                    display: true,
                    ...config.options.scales?.y
                }
            };
            break;
            
        case 'pie':
        case 'doughnut':
            config.options.plugins = {
                ...config.options.plugins,
                legend: {
                    display: true,
                    position: 'right',
                    ...config.options.plugins?.legend
                }
            };
            break;
            
        case 'histogram':
            // Histogram is implemented as a bar chart with specific styling
            config.type = 'bar';
            config.options.scales = {
                ...config.options.scales,
                x: {
                    display: true,
                    title: {
                        display: true,
                        text: 'Value Range'
                    },
                    ...config.options.scales?.x
                },
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Frequency'
                    },
                    ...config.options.scales?.y
                }
            };
            break;
    }
}

// Update chart data
function updateChartData(chart, newData) {
    chart.data.labels = newData.labels || [];
    chart.data.datasets = processDatasets(newData.datasets || [], chart.config.type);
    chart.update('active');
}

// Destroy chart instance
function destroyChart(canvasId) {
    const chart = chartInstances.get(canvasId);
    if (chart) {
        chart.destroy();
        chartInstances.delete(canvasId);
    }
}

// Download chart as image
function downloadChart(chart, format = 'png') {
    const canvas = chart.canvas;
    const url = canvas.toDataURL(`image/${format}`);
    
    const link = document.createElement('a');
    link.download = `chart-${Date.now()}.${format}`;
    link.href = url;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// Deep merge utility function
function mergeDeep(target, source) {
    const output = Object.assign({}, target);
    if (isObject(target) && isObject(source)) {
        Object.keys(source).forEach(key => {
            if (isObject(source[key])) {
                if (!(key in target))
                    Object.assign(output, { [key]: source[key] });
                else
                    output[key] = mergeDeep(target[key], source[key]);
            } else {
                Object.assign(output, { [key]: source[key] });
            }
        });
    }
    return output;
}

// Check if value is an object
function isObject(item) {
    return item && typeof item === 'object' && !Array.isArray(item);
}

// Animation configurations
export const animationPresets = {
    none: {
        animation: false
    },
    fade: {
        animation: {
            duration: 1000,
            easing: 'easeInOutQuart'
        }
    },
    slide: {
        animation: {
            duration: 1200,
            easing: 'easeOutQuart'
        }
    },
    bounce: {
        animation: {
            duration: 1500,
            easing: 'easeOutBounce'
        }
    }
};

// Export additional utilities for advanced chart manipulation
export function getChartInstance(canvasId) {
    return chartInstances.get(canvasId);
}

export function updateChartColors(canvasId, colorPalette = 'primary') {
    const chart = chartInstances.get(canvasId);
    if (chart && colorPalettes[colorPalette]) {
        chart.data.datasets.forEach((dataset, index) => {
            dataset.backgroundColor = getColors(dataset.data.length, index, 0.8);
            dataset.borderColor = getColors(dataset.data.length, index, 1.0);
        });
        chart.update();
    }
}

export function resizeChart(canvasId) {
    const chart = chartInstances.get(canvasId);
    if (chart) {
        chart.resize();
    }
}

// Cleanup all charts (useful for page navigation)
export function destroyAllCharts() {
    chartInstances.forEach((chart, canvasId) => {
        chart.destroy();
    });
    chartInstances.clear();
}