// Chart.js interop for Blazor
window.chartFunctions = {
    charts: {},

    createChart: function (canvasId, config) {
        try {
            // Destroy existing chart if it exists
            if (this.charts[canvasId]) {
                this.charts[canvasId].destroy();
            }

            const ctx = document.getElementById(canvasId);
            if (!ctx) {
                console.error(`Canvas element with id '${canvasId}' not found`);
                return false;
            }

            this.charts[canvasId] = new Chart(ctx, config);
            return true;
        } catch (error) {
            console.error('Error creating chart:', error);
            return false;
        }
    },

    updateChart: function (canvasId, newData, newLabels = null) {
        try {
            const chart = this.charts[canvasId];
            if (!chart) {
                console.error(`Chart with id '${canvasId}' not found`);
                return false;
            }

            if (newLabels) {
                chart.data.labels = newLabels;
            }

            if (Array.isArray(newData)) {
                // Single dataset
                chart.data.datasets[0].data = newData;
            } else {
                // Multiple datasets
                newData.forEach((dataset, index) => {
                    if (chart.data.datasets[index]) {
                        chart.data.datasets[index].data = dataset.data;
                        if (dataset.label) chart.data.datasets[index].label = dataset.label;
                        if (dataset.backgroundColor) chart.data.datasets[index].backgroundColor = dataset.backgroundColor;
                        if (dataset.borderColor) chart.data.datasets[index].borderColor = dataset.borderColor;
                    }
                });
            }

            chart.update();
            return true;
        } catch (error) {
            console.error('Error updating chart:', error);
            return false;
        }
    },

    destroyChart: function (canvasId) {
        try {
            if (this.charts[canvasId]) {
                this.charts[canvasId].destroy();
                delete this.charts[canvasId];
                return true;
            }
            return false;
        } catch (error) {
            console.error('Error destroying chart:', error);
            return false;
        }
    },

    // Predefined chart configurations
    getBarChartConfig: function (title, labels, data, backgroundColor = null) {
        return {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: title,
                    data: data,
                    backgroundColor: backgroundColor || [
                        '#3498db', '#e74c3c', '#2ecc71', '#f39c12', '#9b59b6',
                        '#1abc9c', '#e67e22', '#34495e', '#e91e63', '#00bcd4'
                    ],
                    borderColor: backgroundColor || [
                        '#2980b9', '#c0392b', '#27ae60', '#e67e22', '#8e44ad',
                        '#16a085', '#d35400', '#2c3e50', '#c2185b', '#0097a7'
                    ],
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: title,
                        font: { size: 16, weight: 'bold' }
                    },
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: { display: true, color: '#e0e0e0' },
                        ticks: { font: { size: 12 } }
                    },
                    x: {
                        grid: { display: false },
                        ticks: { font: { size: 12 } }
                    }
                }
            }
        };
    },

    getDoughnutChartConfig: function (title, labels, data, backgroundColor = null) {
        return {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    label: title,
                    data: data,
                    backgroundColor: backgroundColor || [
                        '#3498db', '#e74c3c', '#2ecc71', '#f39c12', '#9b59b6',
                        '#1abc9c', '#e67e22', '#34495e', '#e91e63', '#00bcd4'
                    ],
                    borderWidth: 2,
                    borderColor: '#ffffff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: title,
                        font: { size: 16, weight: 'bold' }
                    },
                    legend: {
                        position: 'bottom',
                        labels: { font: { size: 12 } }
                    }
                }
            }
        };
    },

    getLineChartConfig: function (title, labels, datasets) {
        return {
            type: 'line',
            data: {
                labels: labels,
                datasets: datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: title,
                        font: { size: 16, weight: 'bold' }
                    },
                    legend: {
                        position: 'bottom',
                        labels: { font: { size: 12 } }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: { display: true, color: '#e0e0e0' },
                        ticks: { font: { size: 12 } }
                    },
                    x: {
                        grid: { display: true, color: '#f0f0f0' },
                        ticks: { font: { size: 12 } }
                    }
                },
                elements: {
                    line: {
                        tension: 0.2
                    },
                    point: {
                        radius: 4,
                        hoverRadius: 6
                    }
                }
            }
        };
    }
};