using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Consensus.Web.Services;
using System.Collections.Concurrent;

namespace Consensus.Web.Hubs;

/// <summary>
/// SignalR hub for real-time analytics updates
/// </summary>
[Authorize(Roles = "Admin,Operator,Viewer")]
public class AnalyticsHub : Hub
{
    private readonly ILogger<AnalyticsHub> _logger;
    private readonly IAnalyticsExportService _analyticsService;
    
    // Track connected clients and their subscriptions
    private static readonly ConcurrentDictionary<string, HashSet<string>> _userSubscriptions = new();
    private static readonly ConcurrentDictionary<string, DateTime> _lastActivity = new();

    public AnalyticsHub(ILogger<AnalyticsHub> logger, IAnalyticsExportService analyticsService)
    {
        _logger = logger;
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Handle client connection
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var userId = Context.UserIdentifier ?? "anonymous";
        
        _logger.LogInformation("Analytics client connected: {ConnectionId} for user {UserId}", connectionId, userId);
        
        // Initialize user subscriptions if not exists
        _userSubscriptions.TryAdd(connectionId, new HashSet<string>());
        _lastActivity[connectionId] = DateTime.UtcNow;
        
        // Send initial analytics data
        await SendInitialAnalyticsData();
        
        // Add to default analytics group
        await Groups.AddToGroupAsync(connectionId, "AnalyticsClients");
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Handle client disconnection
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        
        _logger.LogInformation("Analytics client disconnected: {ConnectionId}", connectionId);
        
        // Clean up user subscriptions
        _userSubscriptions.TryRemove(connectionId, out _);
        _lastActivity.TryRemove(connectionId, out _);
        
        // Remove from all groups
        await Groups.RemoveFromGroupAsync(connectionId, "AnalyticsClients");
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to specific analytics updates
    /// </summary>
    /// <param name="subscriptionType">Type of analytics to subscribe to</param>
    public async Task SubscribeToAnalytics(string subscriptionType)
    {
        var connectionId = Context.ConnectionId;
        
        if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
        {
            subscriptions.Add(subscriptionType);
            _lastActivity[connectionId] = DateTime.UtcNow;
            
            _logger.LogDebug("Client {ConnectionId} subscribed to {SubscriptionType}", connectionId, subscriptionType);
            
            // Add to specific group based on subscription type
            var groupName = $"Analytics_{subscriptionType}";
            await Groups.AddToGroupAsync(connectionId, groupName);
            
            // Send current data for this subscription type
            await SendSubscriptionData(subscriptionType);
        }
    }

    /// <summary>
    /// Unsubscribe from specific analytics updates
    /// </summary>
    /// <param name="subscriptionType">Type of analytics to unsubscribe from</param>
    public async Task UnsubscribeFromAnalytics(string subscriptionType)
    {
        var connectionId = Context.ConnectionId;
        
        if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
        {
            subscriptions.Remove(subscriptionType);
            _lastActivity[connectionId] = DateTime.UtcNow;
            
            _logger.LogDebug("Client {ConnectionId} unsubscribed from {SubscriptionType}", connectionId, subscriptionType);
            
            // Remove from specific group
            var groupName = $"Analytics_{subscriptionType}";
            await Groups.RemoveFromGroupAsync(connectionId, groupName);
        }
    }

    /// <summary>
    /// Request real-time statistics update
    /// </summary>
    public async Task RequestRealTimeStats()
    {
        var connectionId = Context.ConnectionId;
        _lastActivity[connectionId] = DateTime.UtcNow;
        
        try
        {
            // Generate real-time statistics
            var stats = GenerateRealTimeStats();
            
            // Send to requesting client
            await Clients.Caller.SendAsync("RealTimeStatsUpdate", stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending real-time stats to client {ConnectionId}", connectionId);
            await Clients.Caller.SendAsync("Error", "Failed to retrieve real-time statistics");
        }
    }

    /// <summary>
    /// Request chart data update
    /// </summary>
    /// <param name="chartType">Type of chart to update</param>
    /// <param name="timeRange">Time range for chart data</param>
    public async Task RequestChartUpdate(string chartType, string timeRange = "Last 24 Hours")
    {
        var connectionId = Context.ConnectionId;
        _lastActivity[connectionId] = DateTime.UtcNow;
        
        try
        {
            // Generate chart data based on type
            var chartData = await GenerateChartData(chartType, timeRange);
            
            // Send to requesting client
            await Clients.Caller.SendAsync("ChartDataUpdate", new { chartType, data = chartData, timeRange });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chart update to client {ConnectionId} for chart {ChartType}", connectionId, chartType);
            await Clients.Caller.SendAsync("Error", $"Failed to update chart: {chartType}");
        }
    }

    /// <summary>
    /// Get active client statistics
    /// </summary>
    public async Task GetClientStats()
    {
        var connectionId = Context.ConnectionId;
        _lastActivity[connectionId] = DateTime.UtcNow;
        
        var stats = new
        {
            TotalConnectedClients = _userSubscriptions.Count,
            ActiveSubscriptions = _userSubscriptions.Values.SelectMany(s => s).GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count()),
            ServerTime = DateTime.UtcNow,
            ConnectionId = connectionId
        };
        
        await Clients.Caller.SendAsync("ClientStatsUpdate", stats);
    }

    #region Hub Broadcasting Methods

    /// <summary>
    /// Broadcast analytics update to all connected clients
    /// </summary>
    /// <param name="analyticsData">Analytics data to broadcast</param>
    public async Task BroadcastAnalyticsUpdate(object analyticsData)
    {
        try
        {
            await Clients.Group("AnalyticsClients").SendAsync("AnalyticsUpdate", analyticsData);
            _logger.LogDebug("Broadcasted analytics update to all clients");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting analytics update");
        }
    }

    /// <summary>
    /// Broadcast performance metrics update
    /// </summary>
    /// <param name="performanceData">Performance metrics to broadcast</param>
    public async Task BroadcastPerformanceUpdate(object performanceData)
    {
        try
        {
            await Clients.Group("Analytics_Performance").SendAsync("PerformanceUpdate", performanceData);
            _logger.LogDebug("Broadcasted performance update to subscribed clients");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting performance update");
        }
    }

    /// <summary>
    /// Broadcast simulation status update
    /// </summary>
    /// <param name="simulationData">Simulation status data</param>
    public async Task BroadcastSimulationUpdate(object simulationData)
    {
        try
        {
            await Clients.Group("Analytics_Simulation").SendAsync("SimulationUpdate", simulationData);
            _logger.LogDebug("Broadcasted simulation update to subscribed clients");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting simulation update");
        }
    }

    /// <summary>
    /// Broadcast chart data update
    /// </summary>
    /// <param name="chartType">Type of chart being updated</param>
    /// <param name="chartData">Chart data to broadcast</param>
    public async Task BroadcastChartUpdate(string chartType, object chartData)
    {
        try
        {
            await Clients.Group($"Analytics_Chart_{chartType}").SendAsync("ChartUpdate", new { chartType, data = chartData });
            _logger.LogDebug("Broadcasted chart update for {ChartType} to subscribed clients", chartType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting chart update for {ChartType}", chartType);
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task SendInitialAnalyticsData()
    {
        try
        {
            // Send basic statistics on connection
            var initialStats = GenerateRealTimeStats();
            await Clients.Caller.SendAsync("InitialAnalyticsData", initialStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending initial analytics data");
        }
    }

    private async Task SendSubscriptionData(string subscriptionType)
    {
        try
        {
            var data = subscriptionType.ToLower() switch
            {
                "performance" => GeneratePerformanceData(),
                "simulation" => GenerateSimulationData(),
                "charts" => GenerateChartSummary(),
                "realtime" => GenerateRealTimeStats(),
                _ => null
            };

            if (data != null)
            {
                await Clients.Caller.SendAsync($"{subscriptionType}Data", data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending subscription data for {SubscriptionType}", subscriptionType);
        }
    }

    private object GenerateRealTimeStats()
    {
        return new
        {
            Timestamp = DateTime.UtcNow,
            ActiveSimulations = Random.Shared.Next(5, 15),
            CurrentBlockRate = Math.Round(Random.Shared.NextDouble() * 10 + 5, 2),
            ActiveNodes = Random.Shared.Next(100, 200),
            SystemLoad = Math.Round(Random.Shared.NextDouble() * 100, 1),
            MemoryUsage = Math.Round(Random.Shared.NextDouble() * 100, 1),
            NetworkLatency = Math.Round(Random.Shared.NextDouble() * 50 + 10, 1),
            ConsensusEfficiency = Math.Round(Random.Shared.NextDouble() * 30 + 70, 1),
            ConnectedClients = _userSubscriptions.Count
        };
    }

    private object GeneratePerformanceData()
    {
        var algorithms = new[] { "ProofOfWork", "ProofOfStake", "PBFT", "Raft", "ProofOfElapsedTime" };
        return algorithms.Select(alg => new
        {
            Algorithm = alg,
            ProcessingTime = Math.Round(Random.Shared.NextDouble() * 2000 + 500, 1),
            ConsensusTime = Math.Round(Random.Shared.NextDouble() * 1500 + 300, 1),
            SuccessRate = Math.Round(Random.Shared.NextDouble() * 20 + 80, 1),
            Timestamp = DateTime.UtcNow
        }).ToList();
    }

    private object GenerateSimulationData()
    {
        return new
        {
            TotalSimulations = Random.Shared.Next(50, 150),
            RunningSimulations = Random.Shared.Next(5, 15),
            CompletedToday = Random.Shared.Next(20, 50),
            AverageExecutionTime = Math.Round(Random.Shared.NextDouble() * 300 + 100, 1),
            SuccessRate = Math.Round(Random.Shared.NextDouble() * 15 + 85, 1),
            Timestamp = DateTime.UtcNow
        };
    }

    private object GenerateChartSummary()
    {
        return new
        {
            WinnerDistribution = Enumerable.Range(1, 5).Select(i => new { Node = $"node-{i:D3}", Wins = Random.Shared.Next(20, 60) }),
            AlgorithmUsage = new[] { "PoW", "PoS", "PBFT", "Raft" }.Select(alg => new { Algorithm = alg, Usage = Random.Shared.Next(10, 30) }),
            PerformanceTrend = Enumerable.Range(0, 12).Select(i => new { 
                Hour = DateTime.UtcNow.AddHours(-11 + i).Hour, 
                ProcessingTime = Math.Round(Random.Shared.NextDouble() * 1000 + 500, 1) 
            }),
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<object> GenerateChartData(string chartType, string timeRange)
    {
        await Task.Delay(10); // Simulate async operation

        return chartType.ToLower() switch
        {
            "winner" => new
            {
                Labels = Enumerable.Range(1, 8).Select(i => $"node-{i:D3}").ToList(),
                Data = Enumerable.Range(1, 8).Select(_ => Random.Shared.Next(20, 80)).ToList(),
                BackgroundColors = new[] { "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0", "#9966FF", "#FF9F40", "#FF6384", "#C9CBCF" }
            },
            "algorithm" => new
            {
                Labels = new[] { "PoW", "PoS", "PBFT", "Raft", "PoET" },
                Data = new[] { "PoW", "PoS", "PBFT", "Raft", "PoET" }.Select(_ => Random.Shared.Next(5, 25)).ToList(),
                BackgroundColors = new[] { "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0", "#9966FF" }
            },
            "performance" => new
            {
                Labels = Enumerable.Range(0, 24).Select(i => $"{i:D2}:00").ToList(),
                ProcessingTime = Enumerable.Range(0, 24).Select(_ => Math.Round(Random.Shared.NextDouble() * 1000 + 500, 1)).ToList(),
                ConsensusTime = Enumerable.Range(0, 24).Select(_ => Math.Round(Random.Shared.NextDouble() * 800 + 300, 1)).ToList()
            },
            "histogram" => new
            {
                Labels = Enumerable.Range(0, 10).Select(i => $"{i * 100}-{(i + 1) * 100}ms").ToList(),
                Data = Enumerable.Range(0, 10).Select(_ => Random.Shared.Next(10, 50)).ToList()
            },
            _ => new { Labels = new[] { "No Data" }, Data = new[] { 0 } }
        };
    }

    #endregion

    #region Static Utility Methods

    /// <summary>
    /// Get the number of currently connected analytics clients
    /// </summary>
    public static int GetConnectedClientsCount()
    {
        return _userSubscriptions.Count;
    }

    /// <summary>
    /// Get subscription statistics
    /// </summary>
    public static Dictionary<string, int> GetSubscriptionStats()
    {
        return _userSubscriptions.Values
            .SelectMany(s => s)
            .GroupBy(s => s)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Clean up inactive connections
    /// </summary>
    public static void CleanupInactiveConnections(TimeSpan maxInactivity)
    {
        var cutoff = DateTime.UtcNow - maxInactivity;
        var inactiveConnections = _lastActivity
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var connectionId in inactiveConnections)
        {
            _userSubscriptions.TryRemove(connectionId, out _);
            _lastActivity.TryRemove(connectionId, out _);
        }
    }

    #endregion
}