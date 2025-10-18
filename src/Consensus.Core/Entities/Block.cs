using System.ComponentModel.DataAnnotations;
using Consensus.Core.Enums;

namespace Consensus.Core.Entities;

/// <summary>
/// Represents a block in the blockchain
/// </summary>
public class Block
{
    /// <summary>
    /// Unique identifier for the block
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Sequential block number in the chain
    /// </summary>
    [Required]
    public long BlockNumber { get; set; }

    /// <summary>
    /// Cryptographic hash of this block
    /// </summary>
    [Required]
    [StringLength(64)]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the previous block in the chain
    /// </summary>
    [StringLength(64)]
    public string? PreviousHash { get; set; }

    /// <summary>
    /// Merkle root of transactions in this block
    /// </summary>
    [StringLength(64)]
    public string? MerkleRoot { get; set; }

    /// <summary>
    /// Timestamp when the block was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Nonce value for Proof of Work (if applicable)
    /// </summary>
    public long Nonce { get; set; }

    /// <summary>
    /// Difficulty target for this block (PoW)
    /// </summary>
    public long Difficulty { get; set; }

    /// <summary>
    /// Size of the block in bytes
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Number of transactions in this block
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Additional block data (consensus-specific)
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }

    /// <summary>
    /// Indicates if the block is valid according to consensus rules
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Node that proposed this block
    /// </summary>
    public Guid? ProposerId { get; set; }

    /// <summary>
    /// Simulation run this block belongs to
    /// </summary>
    [Required]
    public Guid SimulationRunId { get; set; }

    /// <summary>
    /// When the block was created in the simulation
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Node? Proposer { get; set; }
    public virtual SimulationRun? SimulationRun { get; set; }
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    /// <summary>
    /// Calculates the block hash based on its contents
    /// </summary>
    public string CalculateHash()
    {
        var blockString = $"{BlockNumber}{PreviousHash}{MerkleRoot}{Timestamp:O}{Nonce}{Difficulty}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(blockString));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Validates the block hash
    /// </summary>
    public bool ValidateHash()
    {
        return Hash.Equals(CalculateHash(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if this block is the genesis block
    /// </summary>
    public bool IsGenesisBlock()
    {
        return BlockNumber == 0 || string.IsNullOrEmpty(PreviousHash);
    }

    /// <summary>
    /// Validates the block structure and rules
    /// </summary>
    public bool ValidateBlock()
    {
        // Basic validation rules
        if (BlockNumber < 0) return false;
        if (string.IsNullOrEmpty(Hash)) return false;
        if (!ValidateHash()) return false;
        
        // Genesis block validation
        if (IsGenesisBlock())
        {
            return BlockNumber == 0 && string.IsNullOrEmpty(PreviousHash);
        }
        
        // Non-genesis block must have previous hash
        if (string.IsNullOrEmpty(PreviousHash)) return false;
        
        return true;
    }

    /// <summary>
    /// Gets the block reward for miners/validators
    /// </summary>
    public decimal GetBlockReward()
    {
        // Simplified block reward calculation
        return BlockNumber switch
        {
            < 100 => 50m,
            < 1000 => 25m,
            < 10000 => 12.5m,
            _ => 6.25m
        };
    }

    public override string ToString()
    {
        return $"Block #{BlockNumber} ({Hash[..8]}...) - Proposer: {ProposerId?.ToString()[..8]}..., Txns: {TransactionCount}";
    }
}