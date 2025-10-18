using System.ComponentModel.DataAnnotations;
using Consensus.Core.Enums;

namespace Consensus.Core.Entities;

/// <summary>
/// Represents a transaction in the blockchain
/// </summary>
public class Transaction
{
    /// <summary>
    /// Unique identifier for the transaction
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Cryptographic hash of the transaction
    /// </summary>
    [Required]
    [StringLength(64)]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Sender's address
    /// </summary>
    [StringLength(42)]
    public string? FromAddress { get; set; }

    /// <summary>
    /// Recipient's address
    /// </summary>
    [StringLength(42)]
    public string? ToAddress { get; set; }

    /// <summary>
    /// Amount being transferred
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Transaction fee
    /// </summary>
    public decimal Fee { get; set; }

    /// <summary>
    /// Transaction nonce (for preventing replay attacks)
    /// </summary>
    public long Nonce { get; set; }

    /// <summary>
    /// Gas limit for smart contract execution
    /// </summary>
    public long GasLimit { get; set; }

    /// <summary>
    /// Gas price per unit
    /// </summary>
    public decimal GasPrice { get; set; }

    /// <summary>
    /// Actual gas used in execution
    /// </summary>
    public long GasUsed { get; set; }

    /// <summary>
    /// Transaction input data (for smart contracts)
    /// </summary>
    public byte[]? InputData { get; set; }

    /// <summary>
    /// Additional transaction data
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }

    /// <summary>
    /// Digital signature of the transaction
    /// </summary>
    [StringLength(128)]
    public string? Signature { get; set; }

    /// <summary>
    /// Current status of the transaction
    /// </summary>
    [Required]
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    /// <summary>
    /// Block that contains this transaction (if confirmed)
    /// </summary>
    public Guid? BlockId { get; set; }

    /// <summary>
    /// Position of this transaction within the block
    /// </summary>
    public int? TransactionIndex { get; set; }

    /// <summary>
    /// Simulation run this transaction belongs to
    /// </summary>
    [Required]
    public Guid SimulationRunId { get; set; }

    /// <summary>
    /// When the transaction was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the transaction was confirmed (included in a block)
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    // Navigation properties
    public virtual Block? Block { get; set; }
    public virtual SimulationRun? SimulationRun { get; set; }

    /// <summary>
    /// Calculates the transaction hash based on its contents
    /// </summary>
    public string CalculateHash()
    {
        var txString = $"{FromAddress}{ToAddress}{Amount}{Nonce}{GasLimit}{GasPrice}{Convert.ToBase64String(InputData ?? Array.Empty<byte>())}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(txString));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Validates the transaction hash
    /// </summary>
    public bool ValidateHash()
    {
        return Hash.Equals(CalculateHash(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Calculates the total cost of the transaction (amount + fee)
    /// </summary>
    public decimal GetTotalCost()
    {
        return Amount + Fee;
    }

    /// <summary>
    /// Calculates the maximum possible gas cost
    /// </summary>
    public decimal GetMaxGasCost()
    {
        return GasLimit * GasPrice;
    }

    /// <summary>
    /// Calculates the actual gas cost based on usage
    /// </summary>
    public decimal GetActualGasCost()
    {
        return GasUsed * GasPrice;
    }

    /// <summary>
    /// Validates the transaction structure and signature
    /// </summary>
    public bool ValidateTransaction()
    {
        // Basic validation
        if (string.IsNullOrEmpty(Hash)) return false;
        if (!ValidateHash()) return false;
        if (Amount < 0) return false;
        if (Fee < 0) return false;
        if (GasLimit <= 0) return false;
        if (GasPrice < 0) return false;

        // Address validation (simplified)
        if (string.IsNullOrEmpty(FromAddress) && string.IsNullOrEmpty(ToAddress))
            return false;

        // Signature validation would go here in a real implementation
        // For simulation purposes, we'll assume valid if signature exists
        return !string.IsNullOrEmpty(Signature);
    }

    /// <summary>
    /// Marks the transaction as confirmed
    /// </summary>
    public void Confirm(Guid blockId, int transactionIndex)
    {
        Status = TransactionStatus.Confirmed;
        BlockId = blockId;
        TransactionIndex = transactionIndex;
        ConfirmedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the transaction as rejected
    /// </summary>
    public void Reject()
    {
        Status = TransactionStatus.Rejected;
    }

    /// <summary>
    /// Marks the transaction as failed
    /// </summary>
    public void Fail()
    {
        Status = TransactionStatus.Failed;
    }

    /// <summary>
    /// Checks if the transaction is a coinbase transaction (mining reward)
    /// </summary>
    public bool IsCoinbaseTransaction()
    {
        return string.IsNullOrEmpty(FromAddress) && !string.IsNullOrEmpty(ToAddress);
    }

    /// <summary>
    /// Checks if the transaction is a smart contract deployment
    /// </summary>
    public bool IsContractDeployment()
    {
        return string.IsNullOrEmpty(ToAddress) && InputData != null && InputData.Length > 0;
    }

    public override string ToString()
    {
        var shortHash = Hash.Length > 8 ? Hash[..8] + "..." : Hash;
        return $"Transaction {shortHash} - {FromAddress?[..8]}...→{ToAddress?[..8]}... Amount: {Amount}, Status: {Status}";
    }
}