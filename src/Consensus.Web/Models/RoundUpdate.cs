using Consensus.Core.Enums;

namespace Consensus.Web.Models;

/// <summary>
/// Real-time update model for consensus round progress sent via SignalR
/// </summary>
public record RoundUpdate
{
    /// <summary>
    /// Unique identifier of the simulation
    /// </summary>
    public Guid SimulationId { get; init; }

    /// <summary>
    /// Current round number in the consensus process
    /// </summary>
    public long RoundNumber { get; init; }

    /// <summary>
    /// Current status of the round
    /// </summary>
    public ConsensusRoundStatus RoundStatus { get; init; }

    /// <summary>
    /// Current status of the round (simplified for UI)
    /// </summary>
    public RoundStatus Status { get; init; }

    /// <summary>
    /// Node ID of the current leader/proposer (if applicable)
    /// </summary>
    public Guid? LeaderId { get; init; }

    /// <summary>
    /// Node ID of the current leader/proposer (alias for LeaderId)
    /// </summary>
    public Guid? LeaderNodeId => LeaderId;

    /// <summary>
    /// Name of the leader node for display
    /// </summary>
    public string? LeaderName { get; init; }

    /// <summary>
    /// Total number of nodes participating in this round
    /// </summary>
    public int ParticipatingNodes { get; init; }

    /// <summary>
    /// Number of votes received so far
    /// </summary>
    public int VotesReceived { get; init; }

    /// <summary>
    /// Number of positive votes
    /// </summary>
    public int PositiveVotes { get; init; }

    /// <summary>
    /// Percentage of consensus achieved (0-100)
    /// </summary>
    public double ConsensusPercentage { get; init; }

    /// <summary>
    /// Duration of the current round so far
    /// </summary>
    public TimeSpan RoundDuration { get; init; }

    /// <summary>
    /// Timestamp when the round started
    /// </summary>
    public DateTime RoundStartTime { get; init; }

    /// <summary>
    /// Timestamp when the round started (alias for RoundStartTime)
    /// </summary>
    public DateTime Timestamp => RoundStartTime;

    /// <summary>
    /// Proposed value or block hash for this round
    /// </summary>
    public string? ProposedValue { get; init; }

    /// <summary>
    /// Recent events that occurred in this round
    /// </summary>
    public List<RoundEvent> Events { get; init; } = new();

    /// <summary>
    /// Current metrics for this round
    /// </summary>
    public RoundMetrics Metrics { get; init; } = new();

    /// <summary>
    /// Whether this round has completed (success or failure)
    /// </summary>
    public bool IsCompleted { get; init; }

    /// <summary>
    /// Error message if the round failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Individual event that occurred during a consensus round
/// </summary>
public record RoundEvent
{
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Type of event
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Node ID associated with the event
    /// </summary>
    public Guid? NodeId { get; init; }

    /// <summary>
    /// Human-readable description of the event
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Additional data associated with the event
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();

    /// <summary>
    /// Severity level of the event (Info, Warning, Error)
    /// </summary>
    public string Severity { get; init; } = "Info";
}

/// <summary>
/// Performance and statistical metrics for a consensus round
/// </summary>
public record RoundMetrics
{
    /// <summary>
    /// Average response time of nodes in milliseconds
    /// </summary>
    public double AverageResponseTime { get; init; }

    /// <summary>
    /// Network throughput in messages per second
    /// </summary>
    public double NetworkThroughput { get; init; }

    /// <summary>
    /// Number of message exchanges in this round
    /// </summary>
    public int MessageCount { get; init; }

    /// <summary>
    /// CPU usage percentage during this round
    /// </summary>
    public double CpuUsage { get; init; }

    /// <summary>
    /// Memory usage in MB during this round
    /// </summary>
    public double MemoryUsage { get; init; }

    /// <summary>
    /// Network latency in milliseconds
    /// </summary>
    public double NetworkLatency { get; init; }

    /// <summary>
    /// Consensus efficiency (0-1, where 1 is optimal)
    /// </summary>
    public double ConsensusEfficiency { get; init; }

    /// <summary>
    /// Number of Byzantine faults detected
    /// </summary>
    public int ByzantineFaultsDetected { get; init; }
}

/// <summary>
/// Overall simulation status update sent via SignalR
/// </summary>
public record SimulationUpdate
{
    /// <summary>
    /// Unique identifier of the simulation
    /// </summary>
    public Guid SimulationId { get; init; }

    /// <summary>
    /// Name of the simulation
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Current status of the simulation
    /// </summary>
    public SimulationStatus Status { get; init; }

    /// <summary>
    /// Consensus algorithm being used
    /// </summary>
    public ConsensusAlgorithm Algorithm { get; init; }

    /// <summary>
    /// Total number of rounds completed
    /// </summary>
    public long TotalRounds { get; init; }

    /// <summary>
    /// Total number of blocks created
    /// </summary>
    public int BlocksCreated { get; init; }

    /// <summary>
    /// Total number of transactions processed
    /// </summary>
    public int TransactionsProcessed { get; init; }

    /// <summary>
    /// Duration the simulation has been running
    /// </summary>
    public TimeSpan RunningDuration { get; init; }

    /// <summary>
    /// Elapsed time (alias for RunningDuration)
    /// </summary>
    public TimeSpan ElapsedTime => RunningDuration;

    /// <summary>
    /// Estimated time remaining (if applicable)
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }

    /// <summary>
    /// Overall simulation progress (0-100)
    /// </summary>
    public double ProgressPercentage { get; init; }

    /// <summary>
    /// Current round update (latest round information)
    /// </summary>
    public RoundUpdate? CurrentRound { get; init; }

    /// <summary>
    /// Overall simulation metrics
    /// </summary>
    public SimulationMetrics Metrics { get; init; } = new();

    /// <summary>
    /// Number of currently active nodes
    /// </summary>
    public int ActiveNodes { get; init; }

    /// <summary>
    /// Number of nodes that have failed or gone offline
    /// </summary>
    public int FailedNodes { get; init; }

    /// <summary>
    /// Timestamp of the last update
    /// </summary>
    public DateTime LastUpdated { get; init; }

    /// <summary>
    /// Timestamp (alias for LastUpdated)
    /// </summary>
    public DateTime Timestamp => LastUpdated;
}

/// <summary>
/// Overall metrics for the entire simulation
/// </summary>
public record SimulationMetrics
{
    /// <summary>
    /// Average time per consensus round in milliseconds
    /// </summary>
    public double AverageRoundTime { get; init; }

    /// <summary>
    /// Average time per block creation in milliseconds
    /// </summary>
    public double AverageBlockTime { get; init; }

    /// <summary>
    /// Transaction throughput in transactions per second
    /// </summary>
    public double TransactionThroughput { get; init; }

    /// <summary>
    /// Overall consensus success rate (0-1)
    /// </summary>
    public double ConsensusSuccessRate { get; init; }

    /// <summary>
    /// Network utilization percentage
    /// </summary>
    public double NetworkUtilization { get; init; }

    /// <summary>
    /// Total number of Byzantine faults detected
    /// </summary>
    public int TotalByzantineFaults { get; init; }

    /// <summary>
    /// Fault tolerance achieved (0-1)
    /// </summary>
    public double FaultTolerance { get; init; }

    /// <summary>
    /// Energy efficiency metric (simulated)
    /// </summary>
    public double EnergyEfficiency { get; init; }
}