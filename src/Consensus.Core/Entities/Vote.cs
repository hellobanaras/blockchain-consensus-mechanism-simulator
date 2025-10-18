using System.ComponentModel.DataAnnotations;
using Consensus.Core.Enums;

namespace Consensus.Core.Entities;

/// <summary>
/// Represents a vote cast by a node in a consensus round
/// </summary>
public class Vote
{
    /// <summary>
    /// Unique identifier for the vote
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Node that cast this vote
    /// </summary>
    [Required]
    public Guid NodeId { get; set; }

    /// <summary>
    /// Consensus round this vote belongs to
    /// </summary>
    [Required]
    public Guid ConsensusRoundId { get; set; }

    /// <summary>
    /// Type of vote (propose, approve, reject, etc.)
    /// </summary>
    [Required]
    public VoteType VoteType { get; set; }

    /// <summary>
    /// Vote value (true for approve/yes, false for reject/no)
    /// </summary>
    [Required]
    public bool Value { get; set; }

    /// <summary>
    /// Hash of the value being voted on
    /// </summary>
    [StringLength(64)]
    public string? ValueHash { get; set; }

    /// <summary>
    /// Additional vote data (algorithm-specific)
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }

    /// <summary>
    /// Digital signature of the vote (for verification)
    /// </summary>
    [StringLength(128)]
    public string? Signature { get; set; }

    /// <summary>
    /// Nonce or sequence number for this vote
    /// </summary>
    public long Nonce { get; set; }

    /// <summary>
    /// Weight of this vote (for weighted voting systems)
    /// </summary>
    public decimal Weight { get; set; } = 1m;

    /// <summary>
    /// When the vote was cast
    /// </summary>
    public DateTime CastedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the vote was received by the system
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Network delay simulation (time between cast and received)
    /// </summary>
    public int NetworkDelayMs { get; set; }

    // Navigation properties
    public virtual Node? Node { get; set; }
    public virtual ConsensusRound? ConsensusRound { get; set; }

    /// <summary>
    /// Validates the vote signature and structure
    /// </summary>
    public bool ValidateVote()
    {
        // Basic validation
        if (NodeId == Guid.Empty) return false;
        if (ConsensusRoundId == Guid.Empty) return false;
        if (Weight <= 0) return false;

        // Signature validation would go here in a real implementation
        // For simulation purposes, we'll assume valid if signature exists
        return !string.IsNullOrEmpty(Signature);
    }

    /// <summary>
    /// Calculates the hash of the vote content
    /// </summary>
    public string CalculateVoteHash()
    {
        var voteString = $"{NodeId}{ConsensusRoundId}{VoteType}{Value}{ValueHash}{Nonce}{Weight}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(voteString));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Simulates network delay for the vote
    /// </summary>
    public void SimulateNetworkDelay(int baseLatencyMs, Random? random = null)
    {
        random ??= new Random();
        
        // Add some randomness to the network delay (±50% of base latency)
        var variance = (int)(baseLatencyMs * 0.5);
        var actualDelay = baseLatencyMs + random.Next(-variance, variance + 1);
        
        NetworkDelayMs = Math.Max(0, actualDelay);
        ReceivedAt = CastedAt.AddMilliseconds(NetworkDelayMs);
    }

    /// <summary>
    /// Checks if this vote is for a specific proposal
    /// </summary>
    public bool IsForProposal(string proposalHash)
    {
        return ValueHash?.Equals(proposalHash, StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Gets the effective voting power (weight)
    /// </summary>
    public decimal GetEffectiveVotingPower()
    {
        return Weight;
    }

    /// <summary>
    /// Checks if the vote is a positive vote
    /// </summary>
    public bool IsPositiveVote()
    {
        return VoteType switch
        {
            VoteType.Approve => Value,
            VoteType.Commit => Value,
            VoteType.PreCommit => Value,
            VoteType.PreVote => Value,
            VoteType.Propose => true, // Proposals are considered positive
            VoteType.Reject => false,
            VoteType.Abort => false,
            _ => Value
        };
    }

    /// <summary>
    /// Checks if this is a final/binding vote
    /// </summary>
    public bool IsFinalVote()
    {
        return VoteType == VoteType.Commit;
    }

    /// <summary>
    /// Checks if this is a preliminary vote
    /// </summary>
    public bool IsPreliminaryVote()
    {
        return VoteType is VoteType.PreVote or VoteType.PreCommit;
    }

    /// <summary>
    /// Gets the vote delay from when it was cast
    /// </summary>
    public TimeSpan GetVoteDelay()
    {
        return ReceivedAt - CastedAt;
    }

    public override string ToString()
    {
        var nodeIdShort = NodeId.ToString()[..8];
        var roundIdShort = ConsensusRoundId.ToString()[..8];
        return $"Vote {VoteType}({Value}) by {nodeIdShort}... in round {roundIdShort}... (Weight: {Weight})";
    }
}