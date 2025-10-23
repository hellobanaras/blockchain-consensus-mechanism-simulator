using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Models;

namespace Consensus.Core.Services;

/// <summary>
/// Interface for simulation metrics tracking and analysis service
/// </summary>
public interface ISimulationMetricsService
{
    /// <summary>
    /// Initialize metrics tracking for a new simulation
    /// </summary>
    Task InitializeSimulationMetricsAsync(SimulationMetricsRequest request);

    /// <summary>
    /// Record metrics for a completed consensus round
    /// </summary>
    Task RecordRoundMetricsAsync(RoundMetricsData roundData);

    /// <summary>
    /// Record performance metrics for a specific node
    /// </summary>
    Task RecordNodeMetricsAsync(NodeMetricsData nodeData);

    /// <summary>
    /// Record a consensus-related event for tracking
    /// </summary>
    Task RecordConsensusEventAsync(ConsensusEventData eventData);

    /// <summary>
    /// Generate comprehensive simulation summary with all metrics
    /// </summary>
    Task<SimulationSummary> GenerateSimulationSummaryAsync(Guid simulationId);

    /// <summary>
    /// Get current real-time metrics for an active simulation
    /// </summary>
    Task<DetailedSimulationMetrics> GetCurrentMetricsAsync(Guid simulationId);

    /// <summary>
    /// Get round-by-round metrics for analysis
    /// </summary>
    Task<List<RoundMetrics>> GetRoundMetricsAsync(Guid simulationId, int? lastN = null);

    /// <summary>
    /// Get node performance metrics
    /// </summary>
    Task<List<NodeMetrics>> GetNodeMetricsAsync(Guid simulationId);

    /// <summary>
    /// Finalize metrics when simulation completes
    /// </summary>
    Task FinalizeSimulationMetricsAsync(Guid simulationId, SimulationStatus finalStatus);

    /// <summary>
    /// Clean up metrics data for completed simulation
    /// </summary>
    Task CleanupSimulationMetricsAsync(Guid simulationId);
}