using Microsoft.AspNetCore.SignalR;
using Consensus.Core.Interfaces;
using Consensus.Core.Models;
using Consensus.Web.Models;
using Consensus.Core.Enums;

namespace Consensus.Web.Hubs;

/// <summary>
/// SignalR hub for real-time simulation updates
/// </summary>
public class SimulationHub : Hub
{
    private readonly ISimulationService _simulationService;
    private readonly ILogger<SimulationHub> _logger;

    public SimulationHub(ISimulationService simulationService, ILogger<SimulationHub> logger)
    {
        _simulationService = simulationService;
        _logger = logger;
    }

    /// <summary>
    /// Joins a simulation group to receive updates for a specific simulation
    /// </summary>
    /// <param name="simulationId">ID of the simulation to monitor</param>
    public async Task JoinSimulation(string simulationId)
    {
        try
        {
            if (!Guid.TryParse(simulationId, out var id))
            {
                await Clients.Caller.SendAsync("Error", "Invalid simulation ID format");
                return;
            }

            var simulation = await _simulationService.GetSimulationAsync(id);
            if (simulation == null)
            {
                await Clients.Caller.SendAsync("Error", $"Simulation {simulationId} not found");
                return;
            }

            var groupName = GetSimulationGroupName(simulationId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            _logger.LogDebug("Client {ConnectionId} joined simulation {SimulationId}", 
                Context.ConnectionId, simulationId);

            // Send current simulation state to the newly joined client
            var metrics = await _simulationService.GetSimulationMetricsAsync(id);
            var currentState = new SimulationUpdate
            {
                SimulationId = id,
                Name = simulation.Name,
                Status = simulation.Status,
                Algorithm = simulation.ConsensusAlgorithm,
                CurrentRound = null, // We'll implement round tracking later
                TotalRounds = simulation.ConsensusRounds?.Count ?? 0,
                BlocksCreated = simulation.Blocks?.Count ?? 0,
                TransactionsProcessed = simulation.TotalTransactions,
                RunningDuration = simulation.GetDuration() ?? TimeSpan.Zero,
                ProgressPercentage = CalculateProgress(simulation),
                Metrics = new Consensus.Web.Models.SimulationMetrics
                {
                    AverageRoundTime = 0.0, // Not available in current core metrics
                    AverageBlockTime = metrics?.AverageBlockTime ?? 0.0,
                    TransactionThroughput = metrics?.ThroughputTps ?? 0.0,
                    ConsensusSuccessRate = 0.95, // Default for now
                    NetworkUtilization = 0.8, // Default for now
                    FaultTolerance = metrics?.FaultTolerance ?? 0.67,
                    TotalByzantineFaults = 0,
                    EnergyEfficiency = 0.9 // Default for now
                },
                ActiveNodes = simulation.Nodes?.Count(n => n.Status == NodeStatus.Online) ?? 0,
                FailedNodes = simulation.Nodes?.Count(n => n.Status == NodeStatus.Failed) ?? 0,
                LastUpdated = DateTime.UtcNow
            };

            await Clients.Caller.SendAsync("SimulationUpdate", currentState);

            await Clients.Caller.SendAsync("JoinedSimulation", new
            {
                SimulationId = simulationId,
                Message = $"Successfully joined simulation {simulation.Name}",
                Status = simulation.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining simulation {SimulationId} for connection {ConnectionId}", 
                simulationId, Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", "Failed to join simulation");
        }
    }

    /// <summary>
    /// Leaves a simulation group to stop receiving updates
    /// </summary>
    /// <param name="simulationId">ID of the simulation to stop monitoring</param>
    public async Task LeaveSimulation(string simulationId)
    {
        try
        {
            if (!Guid.TryParse(simulationId, out _))
            {
                await Clients.Caller.SendAsync("Error", "Invalid simulation ID format");
                return;
            }

            var groupName = GetSimulationGroupName(simulationId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            _logger.LogDebug("Client {ConnectionId} left simulation {SimulationId}", 
                Context.ConnectionId, simulationId);

            await Clients.Caller.SendAsync("LeftSimulation", new
            {
                SimulationId = simulationId,
                Message = "Successfully left simulation"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving simulation {SimulationId} for connection {ConnectionId}", 
                simulationId, Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", "Failed to leave simulation");
        }
    }

    /// <summary>
    /// Gets current status of a simulation
    /// </summary>
    /// <param name="simulationId">ID of the simulation</param>
    public async Task GetSimulationStatus(string simulationId)
    {
        try
        {
            if (!Guid.TryParse(simulationId, out var id))
            {
                await Clients.Caller.SendAsync("Error", "Invalid simulation ID format");
                return;
            }

            var simulation = await _simulationService.GetSimulationAsync(id);
            if (simulation == null)
            {
                await Clients.Caller.SendAsync("Error", $"Simulation {simulationId} not found");
                return;
            }

            var metrics = await _simulationService.GetSimulationMetricsAsync(id);
            var status = new
            {
                SimulationId = simulationId,
                Name = simulation.Name,
                Status = simulation.Status.ToString(),
                Algorithm = simulation.ConsensusAlgorithm.ToString(),
                CurrentRound = simulation.ConsensusRounds?.Count ?? 0,
                TotalRounds = CalculateExpectedRounds(simulation),
                ElapsedTime = simulation.GetDuration(),
                NodeCount = simulation.NodeCount,
                ByzantineNodeCount = simulation.ByzantineNodeCount,
                CreatedAt = simulation.CreatedAt,
                StartedAt = simulation.StartedAt,
                CompletedAt = simulation.CompletedAt,
                Metrics = metrics
            };

            await Clients.Caller.SendAsync("SimulationStatus", status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for simulation {SimulationId}", simulationId);
            await Clients.Caller.SendAsync("Error", "Failed to get simulation status");
        }
    }

    /// <summary>
    /// Subscribes to all simulation updates (for dashboard overview)
    /// </summary>
    public async Task SubscribeToAllSimulations()
    {
        try
        {
            const string allSimulationsGroup = "AllSimulations";
            await Groups.AddToGroupAsync(Context.ConnectionId, allSimulationsGroup);

            _logger.LogDebug("Client {ConnectionId} subscribed to all simulations", Context.ConnectionId);

            // Send current state of all active simulations
            var simulations = await _simulationService.GetSimulationsAsync();
            var activeSimulations = simulations
                .Where(s => s.Status == SimulationStatus.Running || s.Status == SimulationStatus.Starting)
                .ToList();

            await Clients.Caller.SendAsync("AllSimulationsUpdate", new
            {
                ActiveSimulations = activeSimulations.Count,
                TotalSimulations = simulations.Count(),
                Simulations = activeSimulations.Select(s => new
                {
                    Id = s.Id,
                    Name = s.Name,
                    Status = s.Status.ToString(),
                    Algorithm = s.ConsensusAlgorithm.ToString(),
                    NodeCount = s.NodeCount,
                    CurrentRound = s.ConsensusRounds?.Count ?? 0,
                    ElapsedTime = s.GetDuration()?.TotalSeconds ?? 0
                })
            });

            await Clients.Caller.SendAsync("SubscribedToAllSimulations", new
            {
                Message = "Successfully subscribed to all simulation updates",
                ActiveSimulations = activeSimulations.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to all simulations for connection {ConnectionId}", 
                Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", "Failed to subscribe to all simulations");
        }
    }

    /// <summary>
    /// Unsubscribes from all simulation updates
    /// </summary>
    public async Task UnsubscribeFromAllSimulations()
    {
        try
        {
            const string allSimulationsGroup = "AllSimulations";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, allSimulationsGroup);

            _logger.LogDebug("Client {ConnectionId} unsubscribed from all simulations", Context.ConnectionId);

            await Clients.Caller.SendAsync("UnsubscribedFromAllSimulations", new
            {
                Message = "Successfully unsubscribed from all simulation updates"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from all simulations for connection {ConnectionId}", 
                Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", "Failed to unsubscribe from all simulations");
        }
    }

    /// <summary>
    /// Handles client disconnection
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("Client {ConnectionId} disconnected", Context.ConnectionId);

        if (exception != null)
        {
            _logger.LogWarning(exception, "Client {ConnectionId} disconnected with error", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Handles client connection
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("Client {ConnectionId} connected", Context.ConnectionId);
        
        await Clients.Caller.SendAsync("Connected", new
        {
            ConnectionId = Context.ConnectionId,
            Message = "Successfully connected to simulation hub",
            Timestamp = DateTime.UtcNow
        });

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Sends a round update to all clients monitoring a specific simulation
    /// </summary>
    /// <param name="simulationId">ID of the simulation</param>
    /// <param name="update">Round update details</param>
    public async Task SendRoundUpdate(Guid simulationId, RoundUpdate update)
    {
        try
        {
            var groupName = GetSimulationGroupName(simulationId.ToString());
            await Clients.Group(groupName).SendAsync("RoundUpdate", update);

            // Also send to all simulations subscribers
            await Clients.Group("AllSimulations").SendAsync("GlobalRoundUpdate", new
            {
                SimulationId = simulationId,
                RoundNumber = update.RoundNumber,
                Status = update.Status.ToString(),
                LeaderNodeId = update.LeaderNodeId,
                Timestamp = update.Timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending round update for simulation {SimulationId}", simulationId);
        }
    }

    /// <summary>
    /// Sends a simulation update to all clients monitoring a specific simulation
    /// </summary>
    /// <param name="simulationId">ID of the simulation</param>
    /// <param name="update">Simulation update details</param>
    public async Task SendSimulationUpdate(Guid simulationId, SimulationUpdate update)
    {
        try
        {
            var groupName = GetSimulationGroupName(simulationId.ToString());
            await Clients.Group(groupName).SendAsync("SimulationUpdate", update);

            // Also send to all simulations subscribers
            await Clients.Group("AllSimulations").SendAsync("GlobalSimulationUpdate", new
            {
                SimulationId = simulationId,
                Status = update.Status.ToString(),
                CurrentRound = update.CurrentRound,
                ElapsedTime = update.ElapsedTime.TotalSeconds,
                Timestamp = update.Timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending simulation update for simulation {SimulationId}", simulationId);
        }
    }

    /// <summary>
    /// Gets the SignalR group name for a simulation
    /// </summary>
    /// <param name="simulationId">ID of the simulation</param>
    /// <returns>Group name string</returns>
    private static string GetSimulationGroupName(string simulationId)
    {
        return $"Simulation_{simulationId}";
    }

    /// <summary>
    /// Calculates simulation progress percentage based on time elapsed or rounds completed
    /// </summary>
    /// <param name="simulation">Simulation entity</param>
    /// <returns>Progress percentage (0-100)</returns>
    private static double CalculateProgress(Core.Entities.SimulationRun simulation)
    {
        if (simulation.Status == Core.Enums.SimulationStatus.Completed)
            return 100.0;

        if (simulation.DurationSeconds.HasValue && simulation.StartedAt.HasValue)
        {
            var elapsedTime = DateTime.UtcNow - simulation.StartedAt.Value;
            var totalDuration = TimeSpan.FromSeconds(simulation.DurationSeconds.Value);
            return Math.Min(100.0, (elapsedTime.TotalSeconds / totalDuration.TotalSeconds) * 100.0);
        }

        if (simulation.TargetBlockCount.HasValue)
        {
            var completedBlocks = simulation.Blocks?.Count ?? 0;
            return Math.Min(100.0, ((double)completedBlocks / simulation.TargetBlockCount.Value) * 100.0);
        }

        // Default fallback based on rounds
        var completedRounds = simulation.ConsensusRounds?.Count ?? 0;
        var expectedRounds = CalculateExpectedRounds(simulation);
        return Math.Min(100.0, ((double)completedRounds / expectedRounds) * 100.0);
    }

    /// <summary>
    /// Calculates expected number of rounds based on simulation configuration
    /// </summary>
    /// <param name="simulation">Simulation entity</param>
    /// <returns>Expected number of rounds</returns>
    private static int CalculateExpectedRounds(Core.Entities.SimulationRun simulation)
    {
        if (simulation.DurationSeconds.HasValue)
        {
            var durationMs = simulation.DurationSeconds.Value * 1000;
            return (int)Math.Ceiling((double)durationMs / simulation.BlockTimeMs);
        }

        // Default estimation if duration or block time is not specified
        return 100;
    }
}