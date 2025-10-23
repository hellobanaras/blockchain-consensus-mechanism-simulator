using Microsoft.AspNetCore.SignalR;
using Consensus.Web.Hubs;
using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Interfaces;
using System.Text.Json;

namespace Consensus.Web.Services;

/// <summary>
/// Service for coordinating real-time simulation updates via SignalR
/// </summary>
public interface ISimulationRealtimeService
{
    Task StartRealtimeUpdates(Guid simulationId);
    Task StopRealtimeUpdates(Guid simulationId);
    Task SendMetricsUpdate(Guid simulationId, object metrics);
    Task SendRoundUpdate(Guid simulationId, object roundData);
    Task SendBlockCreated(Guid simulationId, object blockData);
    Task SendLogEntry(Guid simulationId, string level, string category, string message);
    Task SendStatusUpdate(Guid simulationId, SimulationStatus status);
    Task SendSimulationCompleted(Guid simulationId, object results);
}

public class SimulationRealtimeService : ISimulationRealtimeService
{
    private readonly IHubContext<SimulationHub> _hubContext;
    private readonly ISimulationService _simulationService;
    private readonly ILogger<SimulationRealtimeService> _logger;
    private readonly Dictionary<Guid, Timer> _updateTimers = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _cancellationTokens = new();

    public SimulationRealtimeService(
        IHubContext<SimulationHub> hubContext,
        ISimulationService simulationService,
        ILogger<SimulationRealtimeService> logger)
    {
        _hubContext = hubContext;
        _simulationService = simulationService;
        _logger = logger;
    }

    /// <summary>
    /// Start real-time updates for a simulation
    /// </summary>
    public async Task StartRealtimeUpdates(Guid simulationId)
    {
        try
        {
            // Stop existing updates if any
            await StopRealtimeUpdates(simulationId);

            var cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokens[simulationId] = cancellationTokenSource;

            // Start periodic metrics updates
            var timer = new Timer(async _ => await UpdateSimulationMetrics(simulationId, cancellationTokenSource.Token),
                null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            
            _updateTimers[simulationId] = timer;
            
            _logger.LogInformation("Started real-time updates for simulation {SimulationId}", simulationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting real-time updates for simulation {SimulationId}", simulationId);
            throw;
        }
    }

    /// <summary>
    /// Stop real-time updates for a simulation
    /// </summary>
    public async Task StopRealtimeUpdates(Guid simulationId)
    {
        try
        {
            if (_updateTimers.TryGetValue(simulationId, out var timer))
            {
                timer.Dispose();
                _updateTimers.Remove(simulationId);
            }

            if (_cancellationTokens.TryGetValue(simulationId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _cancellationTokens.Remove(simulationId);
            }

            _logger.LogInformation("Stopped real-time updates for simulation {SimulationId}", simulationId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping real-time updates for simulation {SimulationId}", simulationId);
        }
    }

    /// <summary>
    /// Send metrics update to connected clients
    /// </summary>
    public async Task SendMetricsUpdate(Guid simulationId, object metrics)
    {
        try
        {
            await _hubContext.Clients.Group($"Simulation_{simulationId}")
                .SendAsync("ReceiveMetricsUpdate", JsonSerializer.Serialize(metrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending metrics update for simulation {SimulationId}", simulationId);
        }
    }

    /// <summary>
    /// Send round update to connected clients
    /// </summary>
    public async Task SendRoundUpdate(Guid simulationId, object roundData)
    {
        try
        {
            await _hubContext.Clients.Group($"Simulation_{simulationId}")
                .SendAsync("ReceiveRoundUpdate", JsonSerializer.Serialize(roundData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending round update for simulation {SimulationId}", simulationId);
        }
    }

    /// <summary>
    /// Send block creation update to connected clients
    /// </summary>
    public async Task SendBlockCreated(Guid simulationId, object blockData)
    {
        try
        {
            await _hubContext.Clients.Group($"Simulation_{simulationId}")
                .SendAsync("ReceiveBlockCreated", JsonSerializer.Serialize(blockData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending block creation update for simulation {SimulationId}", simulationId);
        }
    }

    /// <summary>
    /// Send log entry to connected clients
    /// </summary>
    public async Task SendLogEntry(Guid simulationId, string level, string category, string message)
    {
        try
        {
            var logEntry = new
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Category = category,
                Message = message
            };

            await _hubContext.Clients.Group($"Simulation_{simulationId}")
                .SendAsync("ReceiveLogEntry", JsonSerializer.Serialize(logEntry));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending log entry for simulation {SimulationId}", simulationId);
        }
    }

    /// <summary>
    /// Send status update to connected clients
    /// </summary>
    public async Task SendStatusUpdate(Guid simulationId, SimulationStatus status)
    {
        try
        {
            await _hubContext.Clients.Group($"Simulation_{simulationId}")
                .SendAsync("ReceiveStatusUpdate", status.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending status update for simulation {SimulationId}", simulationId);
        }
    }

    /// <summary>
    /// Send simulation completion notification
    /// </summary>
    public async Task SendSimulationCompleted(Guid simulationId, object results)
    {
        try
        {
            await _hubContext.Clients.Group($"Simulation_{simulationId}")
                .SendAsync("ReceiveSimulationCompleted", JsonSerializer.Serialize(results));

            // Stop real-time updates
            await StopRealtimeUpdates(simulationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending simulation completed notification for {SimulationId}", simulationId);
        }
    }

    /// <summary>
    /// Update simulation metrics periodically
    /// </summary>
    private async Task UpdateSimulationMetrics(Guid simulationId, CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var simulation = await _simulationService.GetSimulationAsync(simulationId);
            if (simulation == null || simulation.Status != SimulationStatus.Running)
                return;

            // Generate realistic mock metrics based on simulation state
            var metrics = await GenerateMockMetrics(simulation);
            await SendMetricsUpdate(simulationId, metrics);

            // Occasionally send activity updates
            var random = new Random();
            if (random.NextDouble() < 0.3)
            {
                await SendMockActivity(simulationId, simulation);
            }

            // Occasionally send log entries
            if (random.NextDouble() < 0.4)
            {
                await SendMockLogEntry(simulationId, simulation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating metrics for simulation {SimulationId}", simulationId);
        }
    }

    /// <summary>
    /// Generate mock metrics for demonstration
    /// </summary>
    private async Task<object> GenerateMockMetrics(SimulationRun simulation)
    {
        var random = new Random();
        var elapsedTime = DateTime.UtcNow - (simulation.StartedAt ?? simulation.CreatedAt);
        
        // Calculate dynamic values based on algorithm and time
        var blockHeight = Math.Max(1, (int)(elapsedTime.TotalSeconds / 2)); // ~1 block every 2 seconds
        var currentRound = blockHeight + random.Next(0, 3);

        return new
        {
            ActiveNodes = simulation.NodeCount - random.Next(0, Math.Max(1, simulation.ByzantineNodeCount)),
            TotalNodes = simulation.NodeCount,
            BlockHeight = blockHeight,
            CurrentRound = currentRound,
            BlocksPerSecond = Math.Round(random.NextDouble() * 2 + 0.3, 1),
            RoundsPerMinute = Math.Round(random.NextDouble() * 120 + 30, 1),
            AverageLatency = random.Next(50, 250),
            MessageCount = random.Next(100, 1000) + (int)(elapsedTime.TotalSeconds * 10),
            ThroughputTps = Math.Round(random.NextDouble() * 50 + 10, 1),
            PendingTransactions = random.Next(0, 100)
        };
    }

    /// <summary>
    /// Send mock activity for demonstration
    /// </summary>
    private async Task SendMockActivity(Guid simulationId, SimulationRun simulation)
    {
        var random = new Random();
        var activities = new[]
        {
            ("consensus", "Consensus round completed successfully"),
            ("block", $"Block validated and added to chain"),
            ("network", "All nodes responding normally"),
            ("consensus", "Leader election in progress"),
            ("block", "Block proposal received from leader"),
            ("network", "Peer synchronization completed"),
            ("consensus", "Vote aggregation completed")
        };

        var activity = activities[random.Next(activities.Length)];
        await SendLogEntry(simulationId, "INFO", activity.Item1, activity.Item2);
    }

    /// <summary>
    /// Send mock log entry for demonstration
    /// </summary>
    private async Task SendMockLogEntry(Guid simulationId, SimulationRun simulation)
    {
        var random = new Random();
        var logEntries = new[]
        {
            ("INFO", "consensus", "Consensus protocol proceeding normally"),
            ("DEBUG", "blocks", "Block validation passed all checks"),
            ("INFO", "network", "Network connectivity stable"),
            ("WARN", "consensus", "Minor delay in consensus protocol"),
            ("INFO", "blocks", "Transaction pool updated"),
            ("DEBUG", "network", "Peer discovery completed"),
            ("INFO", "consensus", "Round timeout handled gracefully")
        };

        var entry = logEntries[random.Next(logEntries.Length)];
        await SendLogEntry(simulationId, entry.Item1, entry.Item2, entry.Item3);
    }

    /// <summary>
    /// Cleanup resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var kvp in _updateTimers.ToList())
        {
            await StopRealtimeUpdates(kvp.Key);
        }
    }
}