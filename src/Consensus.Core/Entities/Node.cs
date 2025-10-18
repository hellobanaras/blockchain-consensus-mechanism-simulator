using System.ComponentModel.DataAnnotations;
using Consensus.Core.Enums;

namespace Consensus.Core.Entities;

/// <summary>
/// Represents a node in the blockchain network
/// </summary>
public class Node
{
    /// <summary>
    /// Unique identifier for the node
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable name of the node
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current operational status of the node
    /// </summary>
    [Required]
    public NodeStatus Status { get; set; } = NodeStatus.Offline;

    /// <summary>
    /// Consensus algorithm this node is participating in
    /// </summary>
    [Required]
    public ConsensusAlgorithm ConsensusAlgorithm { get; set; }

    /// <summary>
    /// Network connection information (IP:Port, etc.)
    /// </summary>
    [StringLength(500)]
    public string? ConnectionInfo { get; set; }

    /// <summary>
    /// Indicates if the node is currently active in the simulation
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Stake amount for PoS algorithms (if applicable)
    /// </summary>
    public decimal StakeAmount { get; set; }

    /// <summary>
    /// Computational power for PoW algorithms (if applicable)
    /// </summary>
    public int ComputationalPower { get; set; }

    /// <summary>
    /// Node's reputation score (for reputation-based algorithms)
    /// </summary>
    public decimal ReputationScore { get; set; } = 100m;

    /// <summary>
    /// Network latency in milliseconds (for simulation purposes)
    /// </summary>
    public int NetworkLatency { get; set; } = 100;

    /// <summary>
    /// Byzantine fault tolerance - indicates if node is byzantine/malicious
    /// </summary>
    public bool IsByzantine { get; set; } = false;

    /// <summary>
    /// Additional configuration specific to the node
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }

    /// <summary>
    /// Simulation run this node belongs to
    /// </summary>
    [Required]
    public Guid SimulationRunId { get; set; }

    /// <summary>
    /// When the node was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the node was seen active
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the node was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual SimulationRun? SimulationRun { get; set; }

    /// <summary>
    /// Updates the node's last seen timestamp
    /// </summary>
    public void UpdateLastSeen()
    {
        LastSeen = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the node status and updates timestamps
    /// </summary>
    public void UpdateStatus(NodeStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        
        if (newStatus == NodeStatus.Online)
        {
            UpdateLastSeen();
        }
    }

    /// <summary>
    /// Validates if the node can participate in consensus for the given algorithm
    /// </summary>
    public bool CanParticipateInConsensus()
    {
        return IsActive && 
               Status == NodeStatus.Online && 
               !IsByzantine &&
               ConsensusAlgorithm switch
               {
                   ConsensusAlgorithm.ProofOfStake => StakeAmount > 0,
                   ConsensusAlgorithm.ProofOfWork => ComputationalPower > 0,
                   ConsensusAlgorithm.PracticalByzantineFaultTolerance => true,
                   ConsensusAlgorithm.DelegatedProofOfStake => StakeAmount > 0,
                   _ => true
               };
    }

    public override string ToString()
    {
        return $"Node {Name} ({Id:D}) - Status: {Status}, Algorithm: {ConsensusAlgorithm}";
    }
}