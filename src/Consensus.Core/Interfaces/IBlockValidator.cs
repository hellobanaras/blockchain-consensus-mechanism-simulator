using Consensus.Core.Entities;
using Consensus.Core.Enums;

namespace Consensus.Core.Interfaces;

/// <summary>
/// Interface for block validation services
/// </summary>
public interface IBlockValidator
{
    /// <summary>
    /// Validates a block according to consensus rules
    /// </summary>
    /// <param name="block">The block to validate</param>
    /// <param name="previousBlock">The previous block in the chain</param>
    /// <param name="algorithm">The consensus algorithm being used</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateBlockAsync(Block block, Block? previousBlock, ConsensusAlgorithm algorithm);
    
    /// <summary>
    /// Validates a transaction for inclusion in a block
    /// </summary>
    /// <param name="transaction">The transaction to validate</param>
    /// <param name="existingTransactions">Already included transactions in the block</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateTransactionAsync(Transaction transaction, IEnumerable<Transaction> existingTransactions);
    
    /// <summary>
    /// Validates the entire blockchain for consistency
    /// </summary>
    /// <param name="blocks">The blocks to validate (should be in order)</param>
    /// <param name="algorithm">The consensus algorithm being used</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateChainAsync(IEnumerable<Block> blocks, ConsensusAlgorithm algorithm);
    
    /// <summary>
    /// Validates a block's merkle root
    /// </summary>
    /// <param name="block">The block to validate</param>
    /// <returns>True if the merkle root is correct</returns>
    bool ValidateMerkleRoot(Block block);
    
    /// <summary>
    /// Validates protocol-specific consensus data
    /// </summary>
    /// <param name="block">The block to validate</param>
    /// <param name="algorithm">The consensus algorithm being used</param>
    /// <param name="nodes">The participating nodes</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateConsensusDataAsync(Block block, ConsensusAlgorithm algorithm, IEnumerable<Node> nodes);
}

/// <summary>
/// Result of a validation operation
/// </summary>
public record ValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public ValidationErrorType ErrorType { get; init; }
    public Dictionary<string, object> Details { get; init; } = new();
    
    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };
    
    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <param name="errorType">The type of validation error</param>
    /// <param name="details">Additional error details</param>
    public static ValidationResult Failure(string errorMessage, ValidationErrorType errorType = ValidationErrorType.General, Dictionary<string, object>? details = null)
        => new() { IsValid = false, ErrorMessage = errorMessage, ErrorType = errorType, Details = details ?? new() };
}

/// <summary>
/// Types of validation errors
/// </summary>
public enum ValidationErrorType
{
    General,
    InvalidHash,
    InvalidMerkleRoot,
    InvalidPreviousHash,
    InvalidTimestamp,
    InvalidBlockNumber,
    InvalidTransaction,
    InvalidConsensusData,
    InvalidSignature,
    DuplicateTransaction,
    InsufficientFunds,
    InvalidNonce,
    BlockSizeExceeded,
    InvalidDifficulty
}