// SignalR Analytics Client Module
// Handles real-time analytics updates via SignalR connection

export class AnalyticsSignalRClient {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.subscriptions = new Set();
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 5000; // 5 seconds
        this.eventHandlers = new Map();
        this.debug = false;
    }

    /**
     * Initialize SignalR connection to analytics hub
     * @param {boolean} debug - Enable debug logging
     */
    async initialize(debug = false) {
        this.debug = debug;
        
        try {
            // Create connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/analyticsHub", {
                    skipNegotiation: false,
                    transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
                })
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: (retryContext) => {
                        if (retryContext.previousRetryCount < this.maxReconnectAttempts) {
                            return this.reconnectDelay * Math.pow(2, retryContext.previousRetryCount);
                        }
                        return null; // Stop reconnecting after max attempts
                    }
                })
                .configureLogging(this.debug ? signalR.LogLevel.Debug : signalR.LogLevel.Information)
                .build();

            // Set up event handlers
            this.setupEventHandlers();
            
            // Connect
            await this.connect();
            
            this.log('Analytics SignalR client initialized successfully');
            return true;
        } catch (error) {
            this.logError('Failed to initialize SignalR client:', error);
            return false;
        }
    }

    /**
     * Connect to the SignalR hub
     */
    async connect() {
        if (this.isConnected) {
            this.log('Already connected to analytics hub');
            return;
        }

        try {
            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;
            this.log('Connected to analytics hub');
            
            // Trigger connected event
            this.trigger('connected');
            
            // Request initial data
            await this.requestInitialData();
            
        } catch (error) {
            this.logError('Failed to connect to analytics hub:', error);
            this.isConnected = false;
            
            // Trigger connection error event
            this.trigger('connectionError', error);
            
            // Attempt reconnection
            this.scheduleReconnect();
        }
    }

    /**
     * Disconnect from the SignalR hub
     */
    async disconnect() {
        if (this.connection && this.isConnected) {
            try {
                await this.connection.stop();
                this.isConnected = false;
                this.log('Disconnected from analytics hub');
                this.trigger('disconnected');
            } catch (error) {
                this.logError('Error during disconnection:', error);
            }
        }
    }

    /**
     * Set up SignalR event handlers
     */
    setupEventHandlers() {
        // Connection lifecycle events
        this.connection.onreconnecting((error) => {
            this.log('Attempting to reconnect...', error);
            this.isConnected = false;
            this.trigger('reconnecting', error);
        });

        this.connection.onreconnected((connectionId) => {
            this.log('Reconnected with connection ID:', connectionId);
            this.isConnected = true;
            this.trigger('reconnected', connectionId);
            
            // Resubscribe to previous subscriptions
            this.resubscribeAll();
        });

        this.connection.onclose((error) => {
            this.log('Connection closed', error);
            this.isConnected = false;
            this.trigger('disconnected', error);
            
            if (error) {
                this.scheduleReconnect();
            }
        });

        // Analytics data events
        this.connection.on('InitialAnalyticsData', (data) => {
            this.log('Received initial analytics data:', data);
            this.trigger('initialData', data);
        });

        this.connection.on('RealTimeStatsUpdate', (data) => {
            this.log('Received real-time stats update:', data);
            this.trigger('realTimeStats', data);
        });

        this.connection.on('AnalyticsUpdate', (data) => {
            this.log('Received analytics update:', data);
            this.trigger('analyticsUpdate', data);
        });

        this.connection.on('PerformanceUpdate', (data) => {
            this.log('Received performance update:', data);
            this.trigger('performanceUpdate', data);
        });

        this.connection.on('SimulationUpdate', (data) => {
            this.log('Received simulation update:', data);
            this.trigger('simulationUpdate', data);
        });

        this.connection.on('ChartUpdate', (data) => {
            this.log('Received chart update:', data);
            this.trigger('chartUpdate', data);
        });

        this.connection.on('ChartDataUpdate', (data) => {
            this.log('Received chart data update:', data);
            this.trigger('chartDataUpdate', data);
        });

        this.connection.on('AnalyticsSummaryUpdate', (data) => {
            this.log('Received analytics summary update:', data);
            this.trigger('analyticsSummaryUpdate', data);
        });

        this.connection.on('ClientStatsUpdate', (data) => {
            this.log('Received client stats update:', data);
            this.trigger('clientStats', data);
        });

        // Error handling
        this.connection.on('Error', (message) => {
            this.logError('Received error from hub:', message);
            this.trigger('error', message);
        });
    }

    /**
     * Subscribe to specific analytics updates
     * @param {string} subscriptionType - Type of analytics to subscribe to
     */
    async subscribe(subscriptionType) {
        if (!this.isConnected) {
            this.logError('Cannot subscribe - not connected to hub');
            return false;
        }

        try {
            await this.connection.invoke('SubscribeToAnalytics', subscriptionType);
            this.subscriptions.add(subscriptionType);
            this.log(`Subscribed to ${subscriptionType} updates`);
            return true;
        } catch (error) {
            this.logError(`Failed to subscribe to ${subscriptionType}:`, error);
            return false;
        }
    }

    /**
     * Unsubscribe from specific analytics updates
     * @param {string} subscriptionType - Type of analytics to unsubscribe from
     */
    async unsubscribe(subscriptionType) {
        if (!this.isConnected) {
            this.logError('Cannot unsubscribe - not connected to hub');
            return false;
        }

        try {
            await this.connection.invoke('UnsubscribeFromAnalytics', subscriptionType);
            this.subscriptions.delete(subscriptionType);
            this.log(`Unsubscribed from ${subscriptionType} updates`);
            return true;
        } catch (error) {
            this.logError(`Failed to unsubscribe from ${subscriptionType}:`, error);
            return false;
        }
    }

    /**
     * Request real-time statistics update
     */
    async requestRealTimeStats() {
        if (!this.isConnected) {
            this.logError('Cannot request stats - not connected to hub');
            return;
        }

        try {
            await this.connection.invoke('RequestRealTimeStats');
        } catch (error) {
            this.logError('Failed to request real-time stats:', error);
        }
    }

    /**
     * Request chart data update
     * @param {string} chartType - Type of chart to update
     * @param {string} timeRange - Time range for chart data
     */
    async requestChartUpdate(chartType, timeRange = 'Last 24 Hours') {
        if (!this.isConnected) {
            this.logError('Cannot request chart update - not connected to hub');
            return;
        }

        try {
            await this.connection.invoke('RequestChartUpdate', chartType, timeRange);
        } catch (error) {
            this.logError(`Failed to request chart update for ${chartType}:`, error);
        }
    }

    /**
     * Get client statistics
     */
    async getClientStats() {
        if (!this.isConnected) {
            this.logError('Cannot get client stats - not connected to hub');
            return;
        }

        try {
            await this.connection.invoke('GetClientStats');
        } catch (error) {
            this.logError('Failed to get client stats:', error);
        }
    }

    /**
     * Request initial data after connection
     */
    async requestInitialData() {
        await this.requestRealTimeStats();
        await this.getClientStats();
    }

    /**
     * Resubscribe to all previous subscriptions
     */
    async resubscribeAll() {
        for (const subscription of this.subscriptions) {
            await this.subscribe(subscription);
        }
    }

    /**
     * Schedule reconnection attempt
     */
    scheduleReconnect() {
        if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            this.logError('Max reconnection attempts reached. Stopping reconnection.');
            this.trigger('maxReconnectAttemptsReached');
            return;
        }

        this.reconnectAttempts++;
        const delay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1);
        
        this.log(`Scheduling reconnection attempt ${this.reconnectAttempts} in ${delay}ms`);
        
        setTimeout(async () => {
            if (!this.isConnected) {
                await this.connect();
            }
        }, delay);
    }

    /**
     * Add event handler
     * @param {string} event - Event name
     * @param {Function} handler - Event handler function
     */
    on(event, handler) {
        if (!this.eventHandlers.has(event)) {
            this.eventHandlers.set(event, []);
        }
        this.eventHandlers.get(event).push(handler);
    }

    /**
     * Remove event handler
     * @param {string} event - Event name
     * @param {Function} handler - Event handler function to remove
     */
    off(event, handler) {
        if (this.eventHandlers.has(event)) {
            const handlers = this.eventHandlers.get(event);
            const index = handlers.indexOf(handler);
            if (index > -1) {
                handlers.splice(index, 1);
            }
        }
    }

    /**
     * Trigger event
     * @param {string} event - Event name
     * @param {*} data - Event data
     */
    trigger(event, data) {
        if (this.eventHandlers.has(event)) {
            this.eventHandlers.get(event).forEach(handler => {
                try {
                    handler(data);
                } catch (error) {
                    this.logError(`Error in event handler for ${event}:`, error);
                }
            });
        }
    }

    /**
     * Get connection status
     */
    getStatus() {
        return {
            isConnected: this.isConnected,
            connectionState: this.connection?.state || 'Disconnected',
            reconnectAttempts: this.reconnectAttempts,
            subscriptions: Array.from(this.subscriptions)
        };
    }

    /**
     * Log message (if debug enabled)
     */
    log(...args) {
        if (this.debug) {
            console.log('[AnalyticsSignalR]', ...args);
        }
    }

    /**
     * Log error message
     */
    logError(...args) {
        console.error('[AnalyticsSignalR]', ...args);
    }

    /**
     * Cleanup and dispose resources
     */
    async dispose() {
        await this.disconnect();
        this.eventHandlers.clear();
        this.subscriptions.clear();
        this.connection = null;
    }
}

// Create global instance
window.analyticsSignalR = new AnalyticsSignalRClient();

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', async () => {
    if (window.analyticsSignalR && typeof signalR !== 'undefined') {
        await window.analyticsSignalR.initialize(false); // Set to true for debug logging
    }
});

// Export for module use
export default AnalyticsSignalRClient;