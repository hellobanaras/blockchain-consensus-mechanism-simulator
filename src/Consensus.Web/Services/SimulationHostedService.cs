using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using Consensus.Core.Interfaces;
using Consensus.Core.Models;
using Consensus.Web.Hubs;
using Consensus.Web.Models;
using Consensus.Core.Enums;

namespace Consensus.Web.Services;

/// <summary>
/// Background service that manages simulation execution and provides real-time updates via SignalR
/// </summary>
public class SimulationHostedService : BackgroundService
{
    private readonly ILogger<SimulationHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<SimulationHub> _hubContext;

    public SimulationHostedService(
        ILogger<SimulationHostedService> logger,
        IServiceProvider serviceProvider,
        IHubContext<SimulationHub> hubContext)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SimulationHostedService started");

        try
        {
            // Subscribe to simulation service events
            using var scope = _serviceProvider.CreateScope();
            var simulationService = scope.ServiceProvider.GetRequiredService<ISimulationService>();

            // Subscribe to simulation events
            simulationService.SimulationStatusChanged += OnSimulationStatusChanged;
            simulationService.RoundCompleted += OnRoundCompleted;

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Periodically send heartbeat and update metrics for active simulations
                    await UpdateActiveSimulations(simulationService);
                    
                    // Wait for 1 second before next update
                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error during simulation updates");
                    // Continue processing even if individual updates fail
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SimulationHostedService is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in SimulationHostedService");
        }
        finally
        {
            _logger.LogInformation("SimulationHostedService stopped");
        }
    }

    private async Task UpdateActiveSimulations(ISimulationService simulationService)
    {
        try
        {
            var simulations = await simulationService.GetSimulationsAsync();
            var activeSimulations = simulations.Where(s => 
                s.Status == SimulationStatus.Running || 
                s.Status == SimulationStatus.Initializing);

            foreach (var simulation in activeSimulations)
            {
                await SendSimulationUpdate(simulation, simulationService);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating active simulations");
        }
    }

    private async Task SendSimulationUpdate(Core.Entities.SimulationRun simulation, ISimulationService simulationService)
    {
        try
        {
            var metrics = await simulationService.GetSimulationMetricsAsync(simulation.Id);
            var currentRound = simulation.ConsensusRounds?.LastOrDefault();

            var update = new SimulationUpdate
            {
                SimulationId = simulation.Id,
                Name = simulation.Name,
                Status = simulation.Status,
                Algorithm = simulation.ConsensusAlgorithm,
                TotalRounds = simulation.ConsensusRounds?.Count ?? 0,
                BlocksCreated = simulation.ConsensusRounds?.Count(r => r.Status == ConsensusRoundStatus.Completed) ?? 0,
                TransactionsProcessed = simulation.TotalTransactions,
                RunningDuration = simulation.StartedAt.HasValue 
                    ? DateTime.UtcNow - simulation.StartedAt.Value 
                    : TimeSpan.Zero,
                EstimatedTimeRemaining = CalculateEstimatedTimeRemaining(simulation),
                ProgressPercentage = CalculateProgressPercentage(simulation),
                CurrentRound = currentRound != null ? CreateRoundUpdate(simulation, currentRound) : null,
                Metrics = CreateSimulationMetrics(metrics),
                ActiveNodes = simulation.Nodes.Count(n => n.Status == NodeStatus.Online),
                FailedNodes = simulation.Nodes.Count(n => n.Status == NodeStatus.Failed),
                LastUpdated = DateTime.UtcNow
            };

            // Send update to all clients subscribed to this simulation
            await _hubContext.Clients.Group($"Simulation_{simulation.Id}")
                .SendAsync("SimulationUpdate", update);

            // Also send to general dashboard
            await _hubContext.Clients.Group("dashboard")
                .SendAsync("SimulationListUpdate", update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending simulation update for {SimulationId}", simulation.Id);
        }
    }

    private RoundUpdate CreateRoundUpdate(Core.Entities.SimulationRun simulation, Core.Entities.ConsensusRound round)
    {
        var events = new List<RoundEvent>();

        // Add some synthetic events based on round status
        if (round.Status == ConsensusRoundStatus.InProgress)
        {
            events.Add(new RoundEvent
            {
                Timestamp = round.StartedAt,
                EventType = "RoundStarted",
                Message = $"Consensus round {round.RoundNumber} started",
                Severity = "Info"
            });
        }
        else if (round.Status == ConsensusRoundStatus.Completed)
        {
            events.Add(new RoundEvent
            {
                Timestamp = round.CompletedAt ?? DateTime.UtcNow,
                EventType = "RoundCompleted",
                Message = $"Consensus round {round.RoundNumber} completed successfully",
                Severity = "Info"
            });

            if (round.LeaderId.HasValue)
            {
                var leader = simulation.Nodes.FirstOrDefault(n => n.Id == round.LeaderId.Value);
                events.Add(new RoundEvent
                {
                    Timestamp = round.CompletedAt ?? DateTime.UtcNow,
                    EventType = "LeaderSelected",
                    NodeId = round.LeaderId,
                    Message = $"Node {leader?.Name ?? "Unknown"} selected as leader",
                    Severity = "Info"
                });
            }
        }

        var duration = round.CompletedAt.HasValue
            ? round.CompletedAt.Value - round.StartedAt
            : DateTime.UtcNow - round.StartedAt;

        return new RoundUpdate
        {
            SimulationId = simulation.Id,
            RoundNumber = round.RoundNumber,
            RoundStatus = round.Status,
            LeaderId = round.LeaderId,
            LeaderName = round.LeaderId.HasValue 
                ? simulation.Nodes.FirstOrDefault(n => n.Id == round.LeaderId.Value)?.Name 
                : null,
            ParticipatingNodes = round.ParticipatingNodes,
            VotesReceived = round.VotesReceived,
            PositiveVotes = round.PositiveVotes,
            ConsensusPercentage = round.GetConsensusPercentage(),
            RoundDuration = duration,
            RoundStartTime = round.StartedAt,
            ProposedValue = round.ProposedValue?.ToString(),
            Events = events,
            Metrics = new Consensus.Web.Models.RoundMetrics
            {
                AverageResponseTime = Random.Shared.NextDouble() * 100 + 50, // Simulated
                NetworkThroughput = Random.Shared.NextDouble() * 1000 + 500, // Simulated
                MessageCount = round.ParticipatingNodes * 2, // Estimated
                NetworkLatency = simulation.NetworkLatencyMs,
                ConsensusEfficiency = round.Status == ConsensusRoundStatus.Completed ? 0.95 : 0.0
            },
            IsCompleted = round.Status == ConsensusRoundStatus.Completed || round.Status == ConsensusRoundStatus.Failed,
            ErrorMessage = round.Status == ConsensusRoundStatus.Failed ? "Consensus round failed" : null
        };
    }

    private Models.SimulationMetrics CreateSimulationMetrics(Core.Models.SimulationMetrics? coreMetrics)
    {
        if (coreMetrics == null)
        {
            return new Models.SimulationMetrics();
        }

        return new Models.SimulationMetrics
        {
            AverageRoundTime = coreMetrics.AverageBlockTime,
            AverageBlockTime = coreMetrics.AverageBlockTime,
            TransactionThroughput = coreMetrics.ThroughputTps,
            ConsensusSuccessRate = 0.95, // Simulated - would be calculated from actual round results
            NetworkUtilization = Random.Shared.NextDouble() * 0.8 + 0.1, // Simulated
            TotalByzantineFaults = 0, // Would be tracked from actual faults
            FaultTolerance = coreMetrics.FaultTolerance,
            EnergyEfficiency = Random.Shared.NextDouble() * 0.5 + 0.5 // Simulated
        };
    }

    private double CalculateProgressPercentage(Core.Entities.SimulationRun simulation)
    {
        if (!simulation.StartedAt.HasValue)
        {
            return 0.0;
        }

        var elapsed = DateTime.UtcNow - simulation.StartedAt.Value;
        var total = TimeSpan.FromSeconds(simulation.DurationSeconds ?? 300);
        
        return Math.Min(100.0, (elapsed.TotalSeconds / total.TotalSeconds) * 100.0);
    }

    private TimeSpan? CalculateEstimatedTimeRemaining(Core.Entities.SimulationRun simulation)
    {
        if (!simulation.StartedAt.HasValue || simulation.Status != SimulationStatus.Running)
        {
            return null;
        }

        var elapsed = DateTime.UtcNow - simulation.StartedAt.Value;
        var total = TimeSpan.FromSeconds(simulation.DurationSeconds ?? 300);
        var remaining = total - elapsed;

        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private async void OnSimulationStatusChanged(object? sender, SimulationStatusChangedEventArgs e)
    {
        try
        {
            _logger.LogDebug("Simulation {SimulationId} status changed to {Status}", 
                e.Simulation.Id, e.Simulation.Status);

            // Create a status change event
            var statusUpdate = new
            {
                SimulationId = e.Simulation.Id,
                Name = e.Simulation.Name,
                Status = e.Simulation.Status,
                Timestamp = DateTime.UtcNow,
                Message = $"Simulation status changed to {e.Simulation.Status}"
            };

            // Notify all clients subscribed to this simulation
            await _hubContext.Clients.Group($"simulation-{e.Simulation.Id}")
                .SendAsync("SimulationStatusChanged", statusUpdate);

            // Also notify dashboard
            await _hubContext.Clients.Group("dashboard")
                .SendAsync("SimulationStatusChanged", statusUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling simulation status change for {SimulationId}", 
                e.Simulation.Id);
        }
    }

    private async void OnRoundCompleted(object? sender, RoundCompletedEventArgs e)
    {
        try
        {
            _logger.LogDebug("Round {RoundNumber} completed for simulation {SimulationId}", 
                e.Round.RoundNumber, e.Simulation.Id);

            // Create round update
            var roundUpdate = CreateRoundUpdate(e.Simulation, e.Round);

            // Send to clients subscribed to this simulation
            await _hubContext.Clients.Group($"simulation-{e.Simulation.Id}")
                .SendAsync("RoundCompleted", roundUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling round completion for simulation {SimulationId}, round {RoundNumber}", 
                e.Simulation.Id, e.Round.RoundNumber);
        }
    }
}