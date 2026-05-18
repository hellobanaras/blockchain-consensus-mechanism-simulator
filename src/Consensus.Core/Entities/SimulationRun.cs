using System.ComponentModel.DataAnnotations;
using Consensus.Core.Enums;

namespace Consensus.Core.Entities;

/// <summary>
/// Represents a simulation run containing multiple nodes and consensus rounds
/// </summary>
public class SimulationRun
{
    /// <summary>
    /// Unique identifier for the simulation run
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable name of the simulation
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this simulation tests
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Current status of the simulation
    /// </summary>
    [Required]
    public SimulationStatus Status { get; set; } = SimulationStatus.Initializing;

    /// <summary>
    /// Consensus algorithm being tested in this simulation
    /// </summary>
    [Required]
    public ConsensusAlgorithm ConsensusAlgorithm { get; set; }

    /// <summary>
    /// Number of nodes participating in the simulation
    /// </summary>
    public int NodeCount { get; set; }

    /// <summary>
    /// Number of Byzantine (malicious) nodes in the simulation
    /// </summary>
    public int ByzantineNodeCount { get; set; }

    /// <summary>
    /// Target number of blocks to produce
    /// </summary>
    public int? TargetBlockCount { get; set; }

    /// <summary>
    /// Maximum number of consensus rounds to execute
    /// </summary>
    public int? MaxRounds { get; set; }

    /// <summary>
    /// Current progress of the simulation (0.0 to 1.0)
    /// </summary>
    public double Progress { get; set; } = 0.0;

    /// <summary>
    /// Error message if the simulation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Duration of the simulation in seconds (null for unlimited)
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Network topology type used in the simulation
    /// </summary>
    public NetworkTopologyType NetworkTopology { get; set; } = NetworkTopologyType.FullMesh;

    /// <summary>
    /// Target block time in milliseconds
    /// </summary>
    public int BlockTimeMs { get; set; } = 5000;

    /// <summary>
    /// Number of transactions per block
    /// </summary>
    public int TransactionsPerBlock { get; set; } = 10;

    /// <summary>
    /// Network latency in milliseconds
    /// </summary>
    public int NetworkLatencyMs { get; set; } = 100;

    /// <summary>
    /// Total number of transactions processed
    /// </summary>
    public int TotalTransactions { get; set; } = 0;

    /// <summary>
    /// Optional deterministic seed for reproducible runs. Null means non-deterministic.
    /// </summary>
    public int? RandomSeed { get; set; }

    /// <summary>
    /// Network latency simulation settings and other configuration
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }

    /// <summary>
    /// Results and metrics from the completed simulation
    /// </summary>
    public Dictionary<string, object>? Results { get; set; }

    /// <summary>
    /// When the simulation was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the simulation was started
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the simulation completed or stopped
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Last time the simulation was updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID of the user who created this simulation
    /// </summary>
    public string? CreatedById { get; set; }

    // Navigation properties
    /// <summary>
    /// User who created this simulation
    /// </summary>
    public virtual ApplicationUser? CreatedBy { get; set; }
    public virtual ICollection<Node> Nodes { get; set; } = new List<Node>();
    public virtual ICollection<Block> Blocks { get; set; } = new List<Block>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<ConsensusRound> ConsensusRounds { get; set; } = new List<ConsensusRound>();
    public virtual ICollection<NetworkTopology> NetworkTopologies { get; set; } = new List<NetworkTopology>();

    /// <summary>
    /// Starts the simulation
    /// </summary>
    public void Start()
    {
        Status = SimulationStatus.Running;
        StartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Completes the simulation
    /// </summary>
    public void Complete()
    {
        Status = SimulationStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Pauses the simulation
    /// </summary>
    public void Pause()
    {
        Status = SimulationStatus.Paused;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Stops the simulation
    /// </summary>
    public void Stop()
    {
        Status = SimulationStatus.Stopped;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates the duration of the simulation
    /// </summary>
    public TimeSpan? GetDuration()
    {
        if (StartedAt == null) return null;
        
        var endTime = CompletedAt ?? DateTime.UtcNow;
        return endTime - StartedAt.Value;
    }

    /// <summary>
    /// Gets the Byzantine fault tolerance threshold
    /// </summary>
    public int GetByzantineFaultToleranceThreshold()
    {
        return ConsensusAlgorithm switch
        {
            ConsensusAlgorithm.PracticalByzantineFaultTolerance => (NodeCount - 1) / 3,
            ConsensusAlgorithm.Tendermint => (NodeCount - 1) / 3,
            ConsensusAlgorithm.HoneyBadgerBFT => (NodeCount - 1) / 3,
            _ => 0
        };
    }

    /// <summary>
    /// Checks if the current Byzantine node count exceeds the tolerance
    /// </summary>
    public bool IsByzantineFaultToleranceExceeded()
    {
        var threshold = GetByzantineFaultToleranceThreshold();
        return threshold > 0 && ByzantineNodeCount > threshold;
    }

    public override string ToString()
    {
        return $"Simulation {Name} ({Id:D}) - Status: {Status}, Algorithm: {ConsensusAlgorithm}, Nodes: {NodeCount}";
    }
}