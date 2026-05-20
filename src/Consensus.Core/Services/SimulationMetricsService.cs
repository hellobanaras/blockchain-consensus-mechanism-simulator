using Microsoft.Extensions.Logging;
using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Models;
using Consensus.Core.Repositories;
using System.Collections.Concurrent;

namespace Consensus.Core.Services;

/// <summary>
/// Service for tracking and analyzing simulation metrics and performance data
/// </summary>
public class SimulationMetricsService : ISimulationMetricsService
{
    private readonly ILogger<SimulationMetricsService> _logger;
    private readonly ConcurrentDictionary<Guid, DetailedSimulationMetrics> _simulationMetrics;
    private readonly ConcurrentDictionary<Guid, List<RoundMetrics>> _roundMetrics;
    private readonly ConcurrentDictionary<Guid, List<NodeMetrics>> _nodeMetrics;
    private readonly ConcurrentDictionary<Guid, List<ConsensusEvent>> _consensusEvents;

    // Repos are optional — when present (Api host registration), an on-demand
    // hydration path loads completed-sim metrics directly from Postgres so
    // SimulationResultsExportService.ExportSimulationResultsAsync stops
    // throwing "No metrics found" for sims that were never seen live by this
    // process. When null (legacy call sites), the in-memory dictionary
    // remains the only source.
    private readonly ISimulationRunRepository? _simRepo;
    private readonly IConsensusRoundRepository? _roundRepo;
    private readonly IBlockRepository? _blockRepo;
    private readonly INodeRepository? _nodeRepo;

    public SimulationMetricsService(ILogger<SimulationMetricsService> logger)
        : this(logger, null, null, null, null) { }

    public SimulationMetricsService(
        ILogger<SimulationMetricsService> logger,
        ISimulationRunRepository? simRepo,
        IConsensusRoundRepository? roundRepo,
        IBlockRepository? blockRepo,
        INodeRepository? nodeRepo)
    {
        _logger = logger;
        _simulationMetrics = new ConcurrentDictionary<Guid, DetailedSimulationMetrics>();
        _roundMetrics = new ConcurrentDictionary<Guid, List<RoundMetrics>>();
        _nodeMetrics = new ConcurrentDictionary<Guid, List<NodeMetrics>>();
        _consensusEvents = new ConcurrentDictionary<Guid, List<ConsensusEvent>>();
        _simRepo = simRepo;
        _roundRepo = roundRepo;
        _blockRepo = blockRepo;
        _nodeRepo = nodeRepo;
    }

    /// <summary>
    /// Hydrate the in-memory caches from persisted rows for one simulation.
    /// Called from GenerateSimulationSummaryAsync / GetRoundMetricsAsync etc.
    /// when the cache miss path fires AND repos are wired (Api host).
    /// </summary>
    private async Task<bool> TryHydrateFromDbAsync(Guid simulationId)
    {
        if (_simRepo == null || _roundRepo == null || _blockRepo == null || _nodeRepo == null)
        {
            return false;
        }
        try
        {
            var sim = await _simRepo.GetByIdAsync(simulationId);
            if (sim == null) return false;

            var rounds = (await _roundRepo.GetBySimulationRunAsync(simulationId)).ToList();
            var blocks = (await _blockRepo.GetBySimulationRunAsync(simulationId)).ToList();
            var nodes = (await _nodeRepo.GetBySimulationRunAsync(simulationId)).ToList();

            var detailed = new DetailedSimulationMetrics
            {
                SimulationId = sim.Id,
                ConsensusAlgorithm = sim.ConsensusAlgorithm,
                NodeCount = nodes.Count > 0 ? nodes.Count : sim.NodeCount,
                TargetRounds = sim.MaxRounds ?? rounds.Count,
                StartTime = sim.StartedAt ?? sim.CreatedAt,
                EndTime = sim.CompletedAt,
                Status = sim.Status,
                TotalBlocks = blocks.Count,
                TotalTransactions = blocks.Sum(b => b.TransactionCount),
                SuccessfulRounds = rounds.Count(r => r.Status == ConsensusRoundStatus.Completed),
                FailedRounds = rounds.Count(r => r.Status == ConsensusRoundStatus.Failed),
            };
            _simulationMetrics[simulationId] = detailed;

            // Build a one-per-round metrics list — best-effort. Only fields
            // SimulationSummary actually consumes are populated; the rest
            // stay at defaults. Block→round correlation uses BlockNumber
            // (each round produces at most one block on the chains we
            // simulate) since Block doesn't carry a ConsensusRoundId.
            var roundList = rounds
                .OrderBy(r => r.RoundNumber)
                .Select(r => new RoundMetrics
                {
                    RoundNumber = (int)r.RoundNumber,
                    Duration = (r.CompletedAt ?? r.StartedAt) - r.StartedAt,
                    Success = r.Status == ConsensusRoundStatus.Completed,
                    ProposerNodeId = r.LeaderId ?? Guid.Empty,
                    BlocksAccepted = blocks.Count(b => b.ProposerId == r.LeaderId && r.LeaderId.HasValue) > 0 ? 1 : 0,
                    Timestamp = r.StartedAt,
                })
                .ToList();
            _roundMetrics[simulationId] = roundList;

            var nodeList = nodes
                .Select(n => new NodeMetrics
                {
                    NodeId = n.Id,
                    NodeName = n.Name,
                    BlocksAccepted = blocks.Count(b => b.ProposerId == n.Id && b.IsValid),
                })
                .ToList();
            _nodeMetrics[simulationId] = nodeList;

            _consensusEvents.TryAdd(simulationId, new List<ConsensusEvent>());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DB-hydration of metrics for {SimulationId} failed", simulationId);
            return false;
        }
    }

    public async Task InitializeSimulationMetricsAsync(SimulationMetricsRequest request)
    {
        try
        {
            var metrics = new DetailedSimulationMetrics
            {
                SimulationId = request.SimulationId,
                ConsensusAlgorithm = request.Algorithm,
                NodeCount = request.NodeCount,
                TargetRounds = request.TargetRounds,
                StartTime = DateTime.UtcNow,
                Status = SimulationStatus.Running,
                TotalBlocks = 0,
                TotalTransactions = 0,
                SuccessfulRounds = 0,
                FailedRounds = 0,
                AverageBlockTime = TimeSpan.Zero,
                ThroughputTps = 0.0,
                ConsensusEfficiency = 0.0,
                NetworkLatency = TimeSpan.Zero,
                ForkCount = 0,
                OrphanBlocks = 0
            };

            _simulationMetrics[request.SimulationId] = metrics;
            _roundMetrics[request.SimulationId] = new List<RoundMetrics>();
            _nodeMetrics[request.SimulationId] = new List<NodeMetrics>();
            _consensusEvents[request.SimulationId] = new List<ConsensusEvent>();

            _logger.LogInformation("Initialized metrics tracking for simulation {SimulationId}", request.SimulationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize metrics for simulation {SimulationId}", request.SimulationId);
            throw;
        }
    }

    public async Task RecordRoundMetricsAsync(RoundMetricsData roundData)
    {
        try
        {
            if (!_roundMetrics.ContainsKey(roundData.SimulationId))
            {
                _logger.LogWarning("No metrics container found for simulation {SimulationId}", roundData.SimulationId);
                return;
            }

            var metrics = new RoundMetrics
            {
                RoundNumber = roundData.RoundNumber,
                Duration = roundData.Duration,
                ProposerNodeId = roundData.ProposerNodeId,
                ProposerAlgorithm = roundData.ProposerAlgorithm,
                BlocksProposed = roundData.BlocksProposed,
                BlocksAccepted = roundData.BlocksAccepted,
                BlocksRejected = roundData.BlocksRejected,
                TransactionsProcessed = roundData.TransactionsProcessed,
                ConsensusReached = roundData.ConsensusReached,
                ParticipatingNodes = roundData.ParticipatingNodes,
                VotesReceived = roundData.VotesReceived,
                NetworkMessages = roundData.NetworkMessages,
                AverageLatency = roundData.AverageLatency,
                Success = roundData.Success,
                FailureReason = roundData.FailureReason,
                Timestamp = DateTime.UtcNow
            };

            _roundMetrics[roundData.SimulationId].Add(metrics);

            // Update overall simulation metrics
            await UpdateSimulationMetricsAsync(roundData.SimulationId, metrics);

            _logger.LogDebug("Recorded round {RoundNumber} metrics for simulation {SimulationId}", 
                roundData.RoundNumber, roundData.SimulationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record round metrics for simulation {SimulationId}", roundData.SimulationId);
            throw;
        }
    }

    public async Task RecordNodeMetricsAsync(NodeMetricsData nodeData)
    {
        try
        {
            if (!_nodeMetrics.ContainsKey(nodeData.SimulationId))
            {
                _logger.LogWarning("No metrics container found for simulation {SimulationId}", nodeData.SimulationId);
                return;
            }

            var metrics = new NodeMetrics
            {
                NodeId = nodeData.NodeId,
                NodeName = nodeData.NodeName,
                BlocksProposed = nodeData.BlocksProposed,
                BlocksAccepted = nodeData.BlocksAccepted,
                VotesCast = nodeData.VotesCast,
                MessagesReceived = nodeData.MessagesReceived,
                MessagesSent = nodeData.MessagesSent,
                AverageResponseTime = nodeData.AverageResponseTime,
                NetworkLatency = nodeData.NetworkLatency,
                Uptime = nodeData.Uptime,
                Status = nodeData.Status,
                LastActivity = nodeData.LastActivity,
                ConsensusParticipation = nodeData.ConsensusParticipation,
                StakeAmount = nodeData.StakeAmount,
                ReputationScore = nodeData.ReputationScore,
                Timestamp = DateTime.UtcNow
            };

            _nodeMetrics[nodeData.SimulationId].Add(metrics);

            _logger.LogDebug("Recorded node metrics for node {NodeId} in simulation {SimulationId}", 
                nodeData.NodeId, nodeData.SimulationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record node metrics for simulation {SimulationId}", nodeData.SimulationId);
            throw;
        }
    }

    public async Task RecordConsensusEventAsync(ConsensusEventData eventData)
    {
        try
        {
            if (!_consensusEvents.ContainsKey(eventData.SimulationId))
            {
                _logger.LogWarning("No events container found for simulation {SimulationId}", eventData.SimulationId);
                return;
            }

            var consensusEvent = new ConsensusEvent
            {
                EventType = eventData.EventType,
                RoundNumber = eventData.RoundNumber,
                NodeId = eventData.NodeId,
                Description = eventData.Description,
                AdditionalData = eventData.AdditionalData,
                Severity = eventData.Severity,
                Timestamp = DateTime.UtcNow
            };

            _consensusEvents[eventData.SimulationId].Add(consensusEvent);

            _logger.LogDebug("Recorded consensus event {EventType} for simulation {SimulationId}", 
                eventData.EventType, eventData.SimulationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record consensus event for simulation {SimulationId}", eventData.SimulationId);
            throw;
        }
    }

    public async Task<SimulationSummary> GenerateSimulationSummaryAsync(Guid simulationId)
    {
        try
        {
            if (!_simulationMetrics.TryGetValue(simulationId, out var metrics))
            {
                // Cache miss — try to hydrate from persisted rows. This is
                // the Api-host path: the export endpoint runs in a process
                // that never saw the simulation live, so the in-memory
                // dictionary is empty.
                if (await TryHydrateFromDbAsync(simulationId)
                    && _simulationMetrics.TryGetValue(simulationId, out metrics))
                {
                    // fall through with the just-hydrated metrics
                }
                else
                {
                    throw new ArgumentException($"No metrics found for simulation {simulationId}");
                }
            }

            var roundMetrics = _roundMetrics.GetValueOrDefault(simulationId, new List<RoundMetrics>());
            var nodeMetrics = _nodeMetrics.GetValueOrDefault(simulationId, new List<NodeMetrics>());
            var events = _consensusEvents.GetValueOrDefault(simulationId, new List<ConsensusEvent>());

            var summary = new SimulationSummary
            {
                SimulationId = simulationId,
                ConsensusAlgorithm = metrics.ConsensusAlgorithm,
                Duration = metrics.EndTime.HasValue ? 
                    metrics.EndTime.Value - metrics.StartTime : 
                    DateTime.UtcNow - metrics.StartTime,
                TotalRounds = roundMetrics.Count,
                SuccessfulRounds = roundMetrics.Count(r => r.Success),
                FailedRounds = roundMetrics.Count(r => !r.Success),
                TotalBlocks = metrics.TotalBlocks,
                TotalTransactions = metrics.TotalTransactions,
                AverageBlockTime = CalculateAverageBlockTime(roundMetrics),
                ThroughputTps = CalculateThroughput(roundMetrics, metrics.StartTime, metrics.EndTime),
                ConsensusEfficiency = CalculateConsensusEfficiency(roundMetrics),
                NetworkLatency = CalculateAverageNetworkLatency(roundMetrics),
                NodeCount = metrics.NodeCount,
                ForkCount = metrics.ForkCount,
                OrphanBlocks = metrics.OrphanBlocks,
                NodePerformance = CalculateNodePerformance(nodeMetrics),
                RoundStatistics = CalculateRoundStatistics(roundMetrics),
                ConsensusEvents = events.OrderBy(e => e.Timestamp).ToList(),
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Generated simulation summary for {SimulationId}: {Rounds} rounds, {Blocks} blocks", 
                simulationId, summary.TotalRounds, summary.TotalBlocks);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate simulation summary for {SimulationId}", simulationId);
            throw;
        }
    }

    public async Task<DetailedSimulationMetrics> GetCurrentMetricsAsync(Guid simulationId)
    {
        try
        {
            if (!_simulationMetrics.TryGetValue(simulationId, out var metrics))
            {
                throw new ArgumentException($"No metrics found for simulation {simulationId}");
            }

            // Update real-time metrics
            var roundMetrics = _roundMetrics.GetValueOrDefault(simulationId, new List<RoundMetrics>());
            if (roundMetrics.Any())
            {
                metrics.TotalBlocks = roundMetrics.Sum(r => r.BlocksAccepted);
                metrics.TotalTransactions = roundMetrics.Sum(r => r.TransactionsProcessed);
                metrics.SuccessfulRounds = roundMetrics.Count(r => r.Success);
                metrics.FailedRounds = roundMetrics.Count(r => !r.Success);
                metrics.AverageBlockTime = CalculateAverageBlockTime(roundMetrics);
                metrics.ThroughputTps = CalculateThroughput(roundMetrics, metrics.StartTime, metrics.EndTime);
                metrics.ConsensusEfficiency = CalculateConsensusEfficiency(roundMetrics);
                metrics.NetworkLatency = CalculateAverageNetworkLatency(roundMetrics);
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current metrics for simulation {SimulationId}", simulationId);
            throw;
        }
    }

    public async Task<List<RoundMetrics>> GetRoundMetricsAsync(Guid simulationId, int? lastN = null)
    {
        try
        {
            if (!_roundMetrics.ContainsKey(simulationId))
            {
                await TryHydrateFromDbAsync(simulationId);
            }
            var metrics = _roundMetrics.GetValueOrDefault(simulationId, new List<RoundMetrics>());

            if (lastN.HasValue && lastN.Value > 0)
            {
                return metrics.TakeLast(lastN.Value).ToList();
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get round metrics for simulation {SimulationId}", simulationId);
            throw;
        }
    }

    public async Task<List<NodeMetrics>> GetNodeMetricsAsync(Guid simulationId)
    {
        try
        {
            if (!_nodeMetrics.ContainsKey(simulationId))
            {
                await TryHydrateFromDbAsync(simulationId);
            }
            return _nodeMetrics.GetValueOrDefault(simulationId, new List<NodeMetrics>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get node metrics for simulation {SimulationId}", simulationId);
            throw;
        }
    }

    public async Task FinalizeSimulationMetricsAsync(Guid simulationId, SimulationStatus finalStatus)
    {
        try
        {
            if (_simulationMetrics.TryGetValue(simulationId, out var metrics))
            {
                metrics.EndTime = DateTime.UtcNow;
                metrics.Status = finalStatus;

                // Calculate final metrics
                var roundMetrics = _roundMetrics.GetValueOrDefault(simulationId, new List<RoundMetrics>());
                if (roundMetrics.Any())
                {
                    metrics.TotalBlocks = roundMetrics.Sum(r => r.BlocksAccepted);
                    metrics.TotalTransactions = roundMetrics.Sum(r => r.TransactionsProcessed);
                    metrics.SuccessfulRounds = roundMetrics.Count(r => r.Success);
                    metrics.FailedRounds = roundMetrics.Count(r => !r.Success);
                    metrics.AverageBlockTime = CalculateAverageBlockTime(roundMetrics);
                    metrics.ThroughputTps = CalculateThroughput(roundMetrics, metrics.StartTime, metrics.EndTime);
                    metrics.ConsensusEfficiency = CalculateConsensusEfficiency(roundMetrics);
                    metrics.NetworkLatency = CalculateAverageNetworkLatency(roundMetrics);
                }

                _logger.LogInformation("Finalized metrics for simulation {SimulationId} with status {Status}", 
                    simulationId, finalStatus);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to finalize metrics for simulation {SimulationId}", simulationId);
            throw;
        }
    }

    public async Task CleanupSimulationMetricsAsync(Guid simulationId)
    {
        try
        {
            _simulationMetrics.TryRemove(simulationId, out _);
            _roundMetrics.TryRemove(simulationId, out _);
            _nodeMetrics.TryRemove(simulationId, out _);
            _consensusEvents.TryRemove(simulationId, out _);

            _logger.LogInformation("Cleaned up metrics for simulation {SimulationId}", simulationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup metrics for simulation {SimulationId}", simulationId);
            throw;
        }
    }

    #region Private Helper Methods

    private async Task UpdateSimulationMetricsAsync(Guid simulationId, RoundMetrics roundMetrics)
    {
        if (_simulationMetrics.TryGetValue(simulationId, out var metrics))
        {
            metrics.TotalBlocks += roundMetrics.BlocksAccepted;
            metrics.TotalTransactions += roundMetrics.TransactionsProcessed;
            
            if (roundMetrics.Success)
                metrics.SuccessfulRounds++;
            else
                metrics.FailedRounds++;

            // Update real-time averages
            var allRounds = _roundMetrics.GetValueOrDefault(simulationId, new List<RoundMetrics>());
            if (allRounds.Any())
            {
                metrics.AverageBlockTime = CalculateAverageBlockTime(allRounds);
                metrics.ThroughputTps = CalculateThroughput(allRounds, metrics.StartTime, metrics.EndTime);
                metrics.ConsensusEfficiency = CalculateConsensusEfficiency(allRounds);
                metrics.NetworkLatency = CalculateAverageNetworkLatency(allRounds);
            }
        }
    }

    private TimeSpan CalculateAverageBlockTime(List<RoundMetrics> rounds)
    {
        if (!rounds.Any()) return TimeSpan.Zero;
        
        var avgTicks = rounds.Where(r => r.Success && r.Duration > TimeSpan.Zero)
            .Select(r => r.Duration.Ticks)
            .DefaultIfEmpty(0)
            .Average();
            
        return new TimeSpan((long)avgTicks);
    }

    private double CalculateThroughput(List<RoundMetrics> rounds, DateTime startTime, DateTime? endTime)
    {
        if (!rounds.Any()) return 0.0;

        var totalTransactions = rounds.Sum(r => r.TransactionsProcessed);
        var duration = (endTime ?? DateTime.UtcNow) - startTime;
        
        return duration.TotalSeconds > 0 ? totalTransactions / duration.TotalSeconds : 0.0;
    }

    private double CalculateConsensusEfficiency(List<RoundMetrics> rounds)
    {
        if (!rounds.Any()) return 0.0;
        
        var successfulRounds = rounds.Count(r => r.Success);
        return (double)successfulRounds / rounds.Count * 100.0;
    }

    private TimeSpan CalculateAverageNetworkLatency(List<RoundMetrics> rounds)
    {
        if (!rounds.Any()) return TimeSpan.Zero;
        
        var avgTicks = rounds.Where(r => r.AverageLatency > TimeSpan.Zero)
            .Select(r => r.AverageLatency.Ticks)
            .DefaultIfEmpty(0)
            .Average();
            
        return new TimeSpan((long)avgTicks);
    }

    private Dictionary<Guid, NodePerformanceMetrics> CalculateNodePerformance(List<NodeMetrics> nodeMetrics)
    {
        var performance = new Dictionary<Guid, NodePerformanceMetrics>();
        
        var nodeGroups = nodeMetrics.GroupBy(n => n.NodeId);
        foreach (var group in nodeGroups)
        {
            var latestMetrics = group.OrderByDescending(n => n.Timestamp).FirstOrDefault();
            if (latestMetrics != null)
            {
                performance[group.Key] = new NodePerformanceMetrics
                {
                    NodeId = group.Key,
                    NodeName = latestMetrics.NodeName,
                    TotalBlocksProposed = group.Sum(n => n.BlocksProposed),
                    TotalBlocksAccepted = group.Sum(n => n.BlocksAccepted),
                    TotalVotes = group.Sum(n => n.VotesCast),
                    AverageResponseTime = TimeSpan.FromTicks((long)group.Average(n => n.AverageResponseTime.Ticks)),
                    // Guard against single-sample groups: Max - Min = 0 makes
                    // this divide-by-zero, producing Infinity that the JSON
                    // serializer refuses. When the time window is zero we
                    // can't compute a meaningful uptime percentage, so report
                    // 100 (the node is up for the only sample we saw).
                    UptimePercentage = ComputeUptimePercentage(group),
                    ConsensusParticipation = group.Average(n => n.ConsensusParticipation),
                    FinalStake = latestMetrics.StakeAmount,
                    FinalReputation = latestMetrics.ReputationScore
                };
            }
        }
        
        return performance;
    }

    private static double ComputeUptimePercentage(IEnumerable<NodeMetrics> group)
    {
        var list = group.ToList();
        if (list.Count == 0) return 0;
        var window = list.Max(n => n.Timestamp).Subtract(list.Min(n => n.Timestamp)).TotalSeconds;
        if (window <= 0) return 100; // single sample → assume always-up for that point
        var pct = list.Average(n => n.Uptime.TotalSeconds) / window * 100;
        return double.IsFinite(pct) ? Math.Clamp(pct, 0, 100) : 0;
    }

    private RoundStatistics CalculateRoundStatistics(List<RoundMetrics> rounds)
    {
        if (!rounds.Any())
        {
            return new RoundStatistics
            {
                TotalRounds = 0,
                SuccessfulRounds = 0,
                FailedRounds = 0,
                AverageRoundDuration = TimeSpan.Zero,
                MinRoundDuration = TimeSpan.Zero,
                MaxRoundDuration = TimeSpan.Zero,
                AverageBlocksPerRound = 0,
                AverageTransactionsPerRound = 0,
                AverageParticipatingNodes = 0
            };
        }

        return new RoundStatistics
        {
            TotalRounds = rounds.Count,
            SuccessfulRounds = rounds.Count(r => r.Success),
            FailedRounds = rounds.Count(r => !r.Success),
            AverageRoundDuration = TimeSpan.FromTicks((long)rounds.Average(r => r.Duration.Ticks)),
            MinRoundDuration = rounds.Min(r => r.Duration),
            MaxRoundDuration = rounds.Max(r => r.Duration),
            AverageBlocksPerRound = rounds.Average(r => r.BlocksAccepted),
            AverageTransactionsPerRound = rounds.Average(r => r.TransactionsProcessed),
            AverageParticipatingNodes = rounds.Average(r => r.ParticipatingNodes)
        };
    }

    #endregion
}

#region Supporting Models

public record SimulationMetricsRequest
{
    public required Guid SimulationId { get; init; }
    public required ConsensusAlgorithm Algorithm { get; init; }
    public required int NodeCount { get; init; }
    public required int TargetRounds { get; init; }
}

public record RoundMetricsData
{
    public required Guid SimulationId { get; init; }
    public required int RoundNumber { get; init; }
    public required TimeSpan Duration { get; init; }
    public required Guid ProposerNodeId { get; init; }
    public required ConsensusAlgorithm ProposerAlgorithm { get; init; }
    public required int BlocksProposed { get; init; }
    public required int BlocksAccepted { get; init; }
    public required int BlocksRejected { get; init; }
    public required int TransactionsProcessed { get; init; }
    public required bool ConsensusReached { get; init; }
    public required int ParticipatingNodes { get; init; }
    public required int VotesReceived { get; init; }
    public required int NetworkMessages { get; init; }
    public required TimeSpan AverageLatency { get; init; }
    public required bool Success { get; init; }
    public string? FailureReason { get; init; }
}

public record NodeMetricsData
{
    public required Guid SimulationId { get; init; }
    public required Guid NodeId { get; init; }
    public required string NodeName { get; init; }
    public required int BlocksProposed { get; init; }
    public required int BlocksAccepted { get; init; }
    public required int VotesCast { get; init; }
    public required int MessagesReceived { get; init; }
    public required int MessagesSent { get; init; }
    public required TimeSpan AverageResponseTime { get; init; }
    public required TimeSpan NetworkLatency { get; init; }
    public required TimeSpan Uptime { get; init; }
    public required NodeStatus Status { get; init; }
    public required DateTime LastActivity { get; init; }
    public required double ConsensusParticipation { get; init; }
    public required decimal StakeAmount { get; init; }
    public required decimal ReputationScore { get; init; }
}

public record ConsensusEventData
{
    public required Guid SimulationId { get; init; }
    public required string EventType { get; init; }
    public required int RoundNumber { get; init; }
    public Guid? NodeId { get; init; }
    public required string Description { get; init; }
    public Dictionary<string, object> AdditionalData { get; init; } = new();
    public required string Severity { get; init; }
}

#endregion