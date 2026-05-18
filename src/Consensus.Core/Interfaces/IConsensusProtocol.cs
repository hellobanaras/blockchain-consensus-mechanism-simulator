using Consensus.Core.Entities;
using Consensus.Core.Enums;

namespace Consensus.Core.Interfaces;

/// <summary>
/// Interface for consensus protocol implementations
/// </summary>
public interface IConsensusProtocol
{
    /// <summary>
    /// Gets the name of the consensus protocol
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the consensus algorithm type
    /// </summary>
    ConsensusAlgorithm Algorithm { get; }
    
    /// <summary>
    /// Gets the minimum number of nodes required for this protocol
    /// </summary>
    int MinimumNodes { get; }
    
    /// <summary>
    /// Initializes the protocol with the given configuration
    /// </summary>
    /// <param name="nodes">The participating nodes</param>
    /// <param name="configuration">Protocol-specific configuration</param>
    Task InitializeAsync(IEnumerable<Node> nodes, Dictionary<string, object> configuration);
    
    /// <summary>
    /// Starts a new consensus round
    /// </summary>
    /// <param name="round">The consensus round to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the consensus round</returns>
    Task<ConsensusResult> ExecuteRoundAsync(ConsensusRound round, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates if a node can participate in consensus
    /// </summary>
    /// <param name="node">The node to validate</param>
    /// <returns>True if the node can participate</returns>
    bool CanNodeParticipate(Node node);
    
    /// <summary>
    /// Gets protocol-specific metrics
    /// </summary>
    /// <returns>Dictionary of metric names and values</returns>
    Dictionary<string, object> GetMetrics();
    
    /// <summary>
    /// Handles a node becoming faulty during consensus
    /// </summary>
    /// <param name="node">The faulty node</param>
    /// <param name="faultType">The type of fault</param>
    Task HandleNodeFaultAsync(Node node, FaultType faultType);

    /// <summary>
    /// Injects a deterministic random source so leader/proposer selection is reproducible
    /// across runs sharing the same seed. Default no-op preserves prior non-deterministic behaviour.
    /// </summary>
    void SetRandom(Random rng) { }
}

/// <summary>
/// Result of a consensus round execution
/// </summary>
public record ConsensusResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
    public int ParticipatingNodes { get; init; }
    public Dictionary<string, object> Metrics { get; init; } = new();
    public Block? ProducedBlock { get; init; }
    public string? LeaderId { get; init; }
    public List<ConsensusEvent> Events { get; init; } = new();
}

/// <summary>
/// Represents an event that occurred during consensus
/// </summary>
public record ConsensusEvent
{
    public DateTime Timestamp { get; init; }
    public EventType Type { get; init; }
    public string NodeId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, object> Data { get; init; } = new();
}

/// <summary>
/// Types of consensus events
/// </summary>
public enum EventType
{
    NodeParticipation,
    LeaderSelection,
    ProposalCreated,
    VoteCast,
    BlockCreated,
    ConsensusReached,
    ConsensusFailed,
    NetworkPartition,
    NodeFault
}

/// <summary>
/// Types of node faults that can occur
/// </summary>
public enum FaultType
{
    NetworkPartition,
    Byzantine,
    Crash,
    SlowResponse,
    InvalidMessage
}