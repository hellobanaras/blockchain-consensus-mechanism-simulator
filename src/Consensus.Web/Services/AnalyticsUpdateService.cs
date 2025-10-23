using Microsoft.AspNetCore.SignalR;
using Consensus.Web.Hubs;
using Consensus.Web.Services;

namespace Consensus.Web.Services;

/// <summary>
/// Background service for real-time analytics updates
/// </summary>
public class AnalyticsUpdateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnalyticsUpdateService> _logger;
    private readonly IConfiguration _configuration;
    
    // Update intervals
    private readonly TimeSpan _fastUpdateInterval;
    private readonly TimeSpan _slowUpdateInterval;
    private readonly TimeSpan _cleanupInterval;
    
    // Timers for different update frequencies
    private Timer? _fastUpdateTimer;
    private Timer? _slowUpdateTimer;
    private Timer? _cleanupTimer;

    public AnalyticsUpdateService(
        IServiceProvider serviceProvider, 
        ILogger<AnalyticsUpdateService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        // Configure update intervals from appsettings or use defaults
        _fastUpdateInterval = TimeSpan.FromSeconds(_configuration.GetValue<int>("Analytics:FastUpdateIntervalSeconds", 5));
        _slowUpdateInterval = TimeSpan.FromSeconds(_configuration.GetValue<int>("Analytics:SlowUpdateIntervalSeconds", 30));
        _cleanupInterval = TimeSpan.FromMinutes(_configuration.GetValue<int>("Analytics:CleanupIntervalMinutes", 10));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Analytics Update Service starting up");
        _logger.LogInformation("Fast updates every {FastInterval}, slow updates every {SlowInterval}, cleanup every {CleanupInterval}", 
            _fastUpdateInterval, _slowUpdateInterval, _cleanupInterval);

        // Initialize timers
        _fastUpdateTimer = new Timer(async _ => await SendFastUpdates(), null, TimeSpan.Zero, _fastUpdateInterval);
        _slowUpdateTimer = new Timer(async _ => await SendSlowUpdates(), null, TimeSpan.FromSeconds(10), _slowUpdateInterval);
        _cleanupTimer = new Timer(async _ => await CleanupConnections(), null, _cleanupInterval, _cleanupInterval);

        // Keep the service running
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Analytics Update Service is stopping");
        }
    }

    /// <summary>
    /// Send fast-updating analytics data (every 5 seconds)
    /// </summary>
    private async Task SendFastUpdates()
    {
        if (AnalyticsHub.GetConnectedClientsCount() == 0)
            return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<AnalyticsHub>>();
            
            // Generate real-time statistics
            var realTimeStats = GenerateRealTimeStats();
            
            // Send to all connected analytics clients
            await hubContext.Clients.Group("AnalyticsClients").SendAsync("RealTimeStatsUpdate", realTimeStats);
            
            // Send performance updates to performance subscribers
            var performanceData = GeneratePerformanceUpdate();
            await hubContext.Clients.Group("Analytics_Performance").SendAsync("PerformanceUpdate", performanceData);
            
            _logger.LogDebug("Sent fast analytics updates to {ClientCount} clients", AnalyticsHub.GetConnectedClientsCount());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending fast analytics updates");
        }
    }

    /// <summary>
    /// Send slow-updating analytics data (every 30 seconds)
    /// </summary>
    private async Task SendSlowUpdates()
    {
        if (AnalyticsHub.GetConnectedClientsCount() == 0)
            return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<AnalyticsHub>>();
            
            // Send simulation updates
            var simulationData = GenerateSimulationUpdate();
            await hubContext.Clients.Group("Analytics_Simulation").SendAsync("SimulationUpdate", simulationData);
            
            // Send chart updates for different chart types
            await SendChartUpdates(hubContext);
            
            // Send aggregated analytics summary
            var analyticsSummary = await GenerateAnalyticsSummary();
            await hubContext.Clients.Group("AnalyticsClients").SendAsync("AnalyticsSummaryUpdate", analyticsSummary);
            
            _logger.LogDebug("Sent slow analytics updates to subscribed clients");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending slow analytics updates");
        }
    }

    /// <summary>
    /// Send chart-specific updates
    /// </summary>
    private async Task SendChartUpdates(IHubContext<AnalyticsHub> hubContext)
    {
        try
        {
            var chartTypes = new[] { "winner", "algorithm", "performance", "histogram" };
            
            foreach (var chartType in chartTypes)
            {
                var chartData = GenerateChartData(chartType);
                await hubContext.Clients.Group($"Analytics_Chart_{chartType}")
                    .SendAsync("ChartUpdate", new { chartType, data = chartData, timestamp = DateTime.UtcNow });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chart updates");
        }
    }

    /// <summary>
    /// Clean up inactive connections
    /// </summary>
    private async Task CleanupConnections()
    {
        try
        {
            var maxInactivity = TimeSpan.FromMinutes(_configuration.GetValue<int>("Analytics:MaxInactivityMinutes", 30));
            AnalyticsHub.CleanupInactiveConnections(maxInactivity);
            
            _logger.LogDebug("Cleaned up inactive analytics connections");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during analytics connection cleanup");
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Generate real-time statistics
    /// </summary>
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
            TotalThroughput = Math.Round(Random.Shared.NextDouble() * 1000 + 500, 1),
            ErrorRate = Math.Round(Random.Shared.NextDouble() * 5, 2),
            ConnectedClients = AnalyticsHub.GetConnectedClientsCount()
        };
    }

    /// <summary>
    /// Generate performance update data
    /// </summary>
    private object GeneratePerformanceUpdate()
    {
        var algorithms = new[] { "ProofOfWork", "ProofOfStake", "PBFT", "Raft", "ProofOfElapsedTime" };
        return new
        {
            Timestamp = DateTime.UtcNow,
            Metrics = algorithms.Select(alg => new
            {
                Algorithm = alg,
                ProcessingTime = Math.Round(Random.Shared.NextDouble() * 2000 + 500, 1),
                ConsensusTime = Math.Round(Random.Shared.NextDouble() * 1500 + 300, 1),
                SuccessRate = Math.Round(Random.Shared.NextDouble() * 20 + 80, 1),
                Throughput = Math.Round(Random.Shared.NextDouble() * 500 + 200, 1),
                ResourceUsage = Math.Round(Random.Shared.NextDouble() * 100, 1)
            }).ToList()
        };
    }

    /// <summary>
    /// Generate simulation update data
    /// </summary>
    private object GenerateSimulationUpdate()
    {
        return new
        {
            Timestamp = DateTime.UtcNow,
            TotalSimulations = Random.Shared.Next(50, 150),
            RunningSimulations = Random.Shared.Next(5, 15),
            CompletedToday = Random.Shared.Next(20, 50),
            FailedToday = Random.Shared.Next(0, 5),
            AverageExecutionTime = Math.Round(Random.Shared.NextDouble() * 300 + 100, 1),
            SuccessRate = Math.Round(Random.Shared.NextDouble() * 15 + 85, 1),
            QueuedSimulations = Random.Shared.Next(0, 10),
            RecentActivity = Enumerable.Range(0, 10).Select(i => new
            {
                SimulationId = $"sim-{Random.Shared.Next(1000, 9999)}",
                Algorithm = new[] { "PoW", "PoS", "PBFT", "Raft" }[Random.Shared.Next(4)],
                Status = new[] { "Running", "Completed", "Failed" }[Random.Shared.Next(3)],
                StartTime = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 60)),
                NodeCount = Random.Shared.Next(10, 100)
            }).ToList()
        };
    }

    /// <summary>
    /// Generate chart data for specific chart type
    /// </summary>
    private object GenerateChartData(string chartType)
    {
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

    /// <summary>
    /// Generate analytics summary
    /// </summary>
    private async Task<object> GenerateAnalyticsSummary()
    {
        await Task.Delay(10); // Simulate async operation

        return new
        {
            Timestamp = DateTime.UtcNow,
            Summary = new
            {
                TotalSimulations = Random.Shared.Next(100, 500),
                TotalBlocks = Random.Shared.Next(10000, 50000),
                TotalNodes = Random.Shared.Next(500, 1000),
                AverageSimulationDuration = Math.Round(Random.Shared.NextDouble() * 300 + 100, 1),
                OverallSuccessRate = Math.Round(Random.Shared.NextDouble() * 15 + 85, 1)
            },
            TopPerformingAlgorithms = new[] { "PoS", "PBFT", "Raft" }.Select(alg => new
            {
                Algorithm = alg,
                SuccessRate = Math.Round(Random.Shared.NextDouble() * 10 + 90, 1),
                AverageTime = Math.Round(Random.Shared.NextDouble() * 1000 + 500, 1)
            }).ToList(),
            SystemHealth = new
            {
                CPU = Math.Round(Random.Shared.NextDouble() * 100, 1),
                Memory = Math.Round(Random.Shared.NextDouble() * 100, 1),
                Disk = Math.Round(Random.Shared.NextDouble() * 100, 1),
                Network = Math.Round(Random.Shared.NextDouble() * 100, 1)
            }
        };
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Analytics Update Service is stopping");
        
        // Clean up timers
        _fastUpdateTimer?.Dispose();
        _slowUpdateTimer?.Dispose();
        _cleanupTimer?.Dispose();
        
        await base.StopAsync(stoppingToken);
    }

    public override void Dispose()
    {
        _fastUpdateTimer?.Dispose();
        _slowUpdateTimer?.Dispose();
        _cleanupTimer?.Dispose();
        base.Dispose();
    }
}