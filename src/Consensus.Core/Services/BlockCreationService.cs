using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Consensus.Core.Services;

/// <summary>
/// Interface for comprehensive block creation and management services
/// </summary>
public interface IBlockCreationService
{
    /// <summary>
    /// Creates a new block with transactions and consensus data
    /// </summary>
    Task<Block> CreateBlockAsync(BlockCreationRequest request);
    
    /// <summary>
    /// Selects transactions for inclusion in a block
    /// </summary>
    Task<IList<Transaction>> SelectTransactionsAsync(TransactionSelectionRequest request);
    
    /// <summary>
    /// Calculates the merkle root for a list of transactions
    /// </summary>
    string CalculateMerkleRoot(IEnumerable<Transaction> transactions);
    
    /// <summary>
    /// Bundles transactions into a block
    /// </summary>
    Task<Block> BundleTransactionsAsync(TransactionBundlingRequest request);
    
    /// <summary>
    /// Handles orphan blocks and blockchain reorganization
    /// </summary>
    Task<OrphanBlockResult> HandleOrphanBlockAsync(OrphanBlockRequest request);
}

/// <summary>
/// Comprehensive block validator implementation
/// </summary>
public class BlockValidator : IBlockValidator
{
    private readonly ILogger<BlockValidator> _logger;
    
    public BlockValidator(ILogger<BlockValidator> logger)
    {
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateBlockAsync(Block block, Block? previousBlock, ConsensusAlgorithm algorithm)
    {
        _logger.LogDebug("Validating block {BlockNumber} with algorithm {Algorithm}", 
            block.BlockNumber, algorithm);

        try
        {
            // Basic block structure validation
            var basicValidation = ValidateBasicStructure(block);
            if (!basicValidation.IsValid)
                return basicValidation;

            // Hash validation
            var hashValidation = ValidateBlockHash(block);
            if (!hashValidation.IsValid)
                return hashValidation;

            // Chain linkage validation
            var linkageValidation = ValidateChainLinkage(block, previousBlock);
            if (!linkageValidation.IsValid)
                return linkageValidation;

            // Transaction validation
            var transactionValidation = await ValidateBlockTransactionsAsync(block);
            if (!transactionValidation.IsValid)
                return transactionValidation;

            // Merkle root validation
            if (!ValidateMerkleRoot(block))
            {
                return ValidationResult.Failure("Invalid merkle root", ValidationErrorType.InvalidMerkleRoot);
            }

            // Algorithm-specific validation
            var consensusValidation = ValidateAlgorithmSpecificData(block, algorithm);
            if (!consensusValidation.IsValid)
                return consensusValidation;

            _logger.LogDebug("Block {BlockNumber} validation successful", block.BlockNumber);
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating block {BlockNumber}", block.BlockNumber);
            return ValidationResult.Failure($"Validation error: {ex.Message}");
        }
    }

    public async Task<ValidationResult> ValidateTransactionAsync(Transaction transaction, IEnumerable<Transaction> existingTransactions)
    {
        _logger.LogDebug("Validating transaction {TransactionId}", transaction.Id);

        try
        {
            // Basic transaction validation
            if (!transaction.ValidateTransaction())
            {
                return ValidationResult.Failure("Transaction failed basic validation", ValidationErrorType.InvalidTransaction);
            }

            // Check for duplicate transactions
            if (existingTransactions.Any(t => t.Hash == transaction.Hash))
            {
                return ValidationResult.Failure("Duplicate transaction", ValidationErrorType.DuplicateTransaction);
            }

            // Check transaction nonce (prevent replay attacks)
            var sameAccountTxs = existingTransactions
                .Where(t => t.FromAddress == transaction.FromAddress && t.FromAddress != null)
                .OrderBy(t => t.Nonce);

            if (sameAccountTxs.Any() && transaction.Nonce <= sameAccountTxs.Last().Nonce)
            {
                return ValidationResult.Failure("Invalid transaction nonce", ValidationErrorType.InvalidNonce);
            }

            // Validate transaction signature
            if (!ValidateTransactionSignature(transaction))
            {
                return ValidationResult.Failure("Invalid transaction signature", ValidationErrorType.InvalidSignature);
            }

            // Gas validation
            if (transaction.GasLimit <= 0 || transaction.GasPrice < 0)
            {
                return ValidationResult.Failure("Invalid gas parameters", ValidationErrorType.InvalidTransaction);
            }

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating transaction {TransactionId}", transaction.Id);
            return ValidationResult.Failure($"Transaction validation error: {ex.Message}");
        }
    }

    public async Task<ValidationResult> ValidateChainAsync(IEnumerable<Block> blocks, ConsensusAlgorithm algorithm)
    {
        _logger.LogDebug("Validating blockchain with {BlockCount} blocks", blocks.Count());

        try
        {
            var blockList = blocks.OrderBy(b => b.BlockNumber).ToList();
            
            if (!blockList.Any())
            {
                return ValidationResult.Success(); // Empty chain is valid
            }

            // Validate genesis block
            var genesisBlock = blockList.First();
            if (!genesisBlock.IsGenesisBlock())
            {
                return ValidationResult.Failure("First block is not a valid genesis block", ValidationErrorType.InvalidBlockNumber);
            }

            // Validate sequential block chain
            for (int i = 1; i < blockList.Count; i++)
            {
                var currentBlock = blockList[i];
                var previousBlock = blockList[i - 1];

                // Validate block number sequence
                if (currentBlock.BlockNumber != previousBlock.BlockNumber + 1)
                {
                    return ValidationResult.Failure(
                        $"Invalid block sequence: Block {currentBlock.BlockNumber} should be {previousBlock.BlockNumber + 1}",
                        ValidationErrorType.InvalidBlockNumber);
                }

                // Validate individual block
                var blockValidation = await ValidateBlockAsync(currentBlock, previousBlock, algorithm);
                if (!blockValidation.IsValid)
                {
                    return blockValidation;
                }
            }

            // Validate chain integrity
            var integrityValidation = ValidateChainIntegrity(blockList);
            if (!integrityValidation.IsValid)
                return integrityValidation;

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating blockchain");
            return ValidationResult.Failure($"Chain validation error: {ex.Message}");
        }
    }

    public bool ValidateMerkleRoot(Block block)
    {
        try
        {
            if (block.Transactions?.Any() != true)
            {
                // Block with no transactions should have empty merkle root
                return string.IsNullOrEmpty(block.MerkleRoot);
            }

            var calculatedRoot = CalculateMerkleRoot(block.Transactions.Select(t => t.Hash));
            return string.Equals(block.MerkleRoot, calculatedRoot, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating merkle root for block {BlockNumber}", block.BlockNumber);
            return false;
        }
    }

    public async Task<ValidationResult> ValidateConsensusDataAsync(Block block, ConsensusAlgorithm algorithm, IEnumerable<Node> nodes)
    {
        _logger.LogDebug("Validating consensus data for block {BlockNumber} with algorithm {Algorithm}", 
            block.BlockNumber, algorithm);

        try
        {
            return algorithm switch
            {
                ConsensusAlgorithm.ProofOfWork => ValidateProofOfWorkData(block),
                ConsensusAlgorithm.ProofOfStake => ValidateProofOfStakeData(block, nodes),
                ConsensusAlgorithm.ProofOfElapsedTime => ValidatePoetData(block, nodes),
                ConsensusAlgorithm.PracticalByzantineFaultTolerance => ValidatePbftData(block, nodes),
                _ => ValidationResult.Failure($"Unsupported consensus algorithm: {algorithm}", ValidationErrorType.InvalidConsensusData)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating consensus data for block {BlockNumber}", block.BlockNumber);
            return ValidationResult.Failure($"Consensus validation error: {ex.Message}");
        }
    }

    // Private validation helper methods
    private ValidationResult ValidateBasicStructure(Block block)
    {
        if (block.BlockNumber < 0)
            return ValidationResult.Failure("Invalid block number", ValidationErrorType.InvalidBlockNumber);

        if (string.IsNullOrEmpty(block.Hash))
            return ValidationResult.Failure("Block hash is required", ValidationErrorType.InvalidHash);

        if (block.Timestamp > DateTime.UtcNow.AddMinutes(10))
            return ValidationResult.Failure("Block timestamp is too far in the future", ValidationErrorType.InvalidTimestamp);

        if (block.Size <= 0)
            return ValidationResult.Failure("Invalid block size", ValidationErrorType.BlockSizeExceeded);

        return ValidationResult.Success();
    }

    private ValidationResult ValidateBlockHash(Block block)
    {
        var calculatedHash = block.CalculateHash();
        if (!string.Equals(block.Hash, calculatedHash, StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Failure("Block hash mismatch", ValidationErrorType.InvalidHash);
        }

        return ValidationResult.Success();
    }

    private ValidationResult ValidateChainLinkage(Block block, Block? previousBlock)
    {
        if (block.IsGenesisBlock())
        {
            if (!string.IsNullOrEmpty(block.PreviousHash))
            {
                return ValidationResult.Failure("Genesis block should not have previous hash", ValidationErrorType.InvalidPreviousHash);
            }
            return ValidationResult.Success();
        }

        if (string.IsNullOrEmpty(block.PreviousHash))
        {
            return ValidationResult.Failure("Non-genesis block must have previous hash", ValidationErrorType.InvalidPreviousHash);
        }

        if (previousBlock != null && !string.Equals(block.PreviousHash, previousBlock.Hash, StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Failure("Previous hash mismatch", ValidationErrorType.InvalidPreviousHash);
        }

        return ValidationResult.Success();
    }

    private async Task<ValidationResult> ValidateBlockTransactionsAsync(Block block)
    {
        if (block.Transactions?.Any() != true)
        {
            return ValidationResult.Success(); // Empty block is valid
        }

        var transactions = block.Transactions.ToList();

        // Validate transaction count matches
        if (block.TransactionCount != transactions.Count)
        {
            return ValidationResult.Failure("Transaction count mismatch", ValidationErrorType.InvalidTransaction);
        }

        // Validate each transaction
        for (int i = 0; i < transactions.Count; i++)
        {
            var transaction = transactions[i];
            var existingTransactions = transactions.Take(i);

            var validationResult = await ValidateTransactionAsync(transaction, existingTransactions);
            if (!validationResult.IsValid)
            {
                return ValidationResult.Failure(
                    $"Transaction {i} validation failed: {validationResult.ErrorMessage}",
                    validationResult.ErrorType);
            }
        }

        return ValidationResult.Success();
    }

    private ValidationResult ValidateChainIntegrity(List<Block> blocks)
    {
        // Check for duplicate block numbers
        var blockNumbers = blocks.Select(b => b.BlockNumber).ToList();
        if (blockNumbers.Count != blockNumbers.Distinct().Count())
        {
            return ValidationResult.Failure("Duplicate block numbers in chain", ValidationErrorType.InvalidBlockNumber);
        }

        // Check for hash consistency
        for (int i = 1; i < blocks.Count; i++)
        {
            if (!string.Equals(blocks[i].PreviousHash, blocks[i - 1].Hash, StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Failure(
                    $"Hash chain broken at block {blocks[i].BlockNumber}",
                    ValidationErrorType.InvalidPreviousHash);
            }
        }

        return ValidationResult.Success();
    }

    private ValidationResult ValidateAlgorithmSpecificData(Block block, ConsensusAlgorithm algorithm)
    {
        return algorithm switch
        {
            ConsensusAlgorithm.ProofOfWork => ValidateProofOfWorkData(block),
            ConsensusAlgorithm.ProofOfStake => ValidationResult.Success(), // Basic validation for now
            ConsensusAlgorithm.ProofOfElapsedTime => ValidateBasicPoetData(block),
            ConsensusAlgorithm.PracticalByzantineFaultTolerance => ValidationResult.Success(), // Basic validation for now
            _ => ValidationResult.Success() // Unknown algorithms pass basic validation
        };
    }

    private ValidationResult ValidateProofOfWorkData(Block block)
    {
        if (block.Difficulty <= 0)
        {
            return ValidationResult.Failure("Invalid difficulty for PoW block", ValidationErrorType.InvalidDifficulty);
        }

        if (block.Nonce < 0)
        {
            return ValidationResult.Failure("Invalid nonce for PoW block", ValidationErrorType.InvalidConsensusData);
        }

        return ValidationResult.Success();
    }

    private ValidationResult ValidateProofOfStakeData(Block block, IEnumerable<Node> nodes)
    {
        if (!block.ProposerId.HasValue)
        {
            return ValidationResult.Failure("PoS block must have a proposer", ValidationErrorType.InvalidConsensusData);
        }

        var proposer = nodes.FirstOrDefault(n => n.Id == block.ProposerId.Value);
        if (proposer == null)
        {
            return ValidationResult.Failure("Unknown proposer for PoS block", ValidationErrorType.InvalidConsensusData);
        }

        return ValidationResult.Success();
    }

    private ValidationResult ValidatePoetData(Block block, IEnumerable<Node> nodes)
    {
        if (block.Data == null || !block.Data.ContainsKey("waitTime") || !block.Data.ContainsKey("proof"))
        {
            return ValidationResult.Failure("PoET block missing required data", ValidationErrorType.InvalidConsensusData);
        }

        if (!block.ProposerId.HasValue)
        {
            return ValidationResult.Failure("PoET block must have a proposer", ValidationErrorType.InvalidConsensusData);
        }

        var proposer = nodes.FirstOrDefault(n => n.Id == block.ProposerId.Value);
        if (proposer == null)
        {
            return ValidationResult.Failure("Unknown proposer for PoET block", ValidationErrorType.InvalidConsensusData);
        }

        if (proposer.IsByzantine)
        {
            return ValidationResult.Failure("Byzantine node cannot propose valid PoET block", ValidationErrorType.InvalidConsensusData);
        }

        return ValidationResult.Success();
    }

    private ValidationResult ValidateBasicPoetData(Block block)
    {
        if (block.Data == null || !block.Data.ContainsKey("waitTime"))
        {
            return ValidationResult.Failure("PoET block missing wait time data", ValidationErrorType.InvalidConsensusData);
        }

        return ValidationResult.Success();
    }

    private ValidationResult ValidatePbftData(Block block, IEnumerable<Node> nodes)
    {
        if (!block.ProposerId.HasValue)
        {
            return ValidationResult.Failure("PBFT block must have a proposer", ValidationErrorType.InvalidConsensusData);
        }

        var proposer = nodes.FirstOrDefault(n => n.Id == block.ProposerId.Value);
        if (proposer == null)
        {
            return ValidationResult.Failure("Unknown proposer for PBFT block", ValidationErrorType.InvalidConsensusData);
        }

        return ValidationResult.Success();
    }

    private bool ValidateTransactionSignature(Transaction transaction)
    {
        // Simplified signature validation for simulation purposes
        // In a real implementation, this would verify cryptographic signatures
        return !string.IsNullOrEmpty(transaction.Signature);
    }

    private string CalculateMerkleRoot(IEnumerable<string> hashes)
    {
        var hashList = hashes.ToList();
        
        if (!hashList.Any())
            return string.Empty;

        if (hashList.Count == 1)
            return hashList[0];

        while (hashList.Count > 1)
        {
            var newLevel = new List<string>();

            for (int i = 0; i < hashList.Count; i += 2)
            {
                var left = hashList[i];
                var right = i + 1 < hashList.Count ? hashList[i + 1] : left; // Duplicate if odd number

                var combined = left + right;
                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                newLevel.Add(Convert.ToHexString(hashBytes).ToLowerInvariant());
            }

            hashList = newLevel;
        }

        return hashList[0];
    }
}

/// <summary>
/// Comprehensive block creation service implementation
/// </summary>
public class BlockCreationService : IBlockCreationService
{
    private readonly IBlockValidator _validator;
    private readonly ILogger<BlockCreationService> _logger;

    public BlockCreationService(IBlockValidator validator, ILogger<BlockCreationService> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    public async Task<Block> CreateBlockAsync(BlockCreationRequest request)
    {
        _logger.LogInformation("Creating block {BlockNumber} for simulation {SimulationId}", 
            request.BlockNumber, request.SimulationId);

        try
        {
            // Select transactions for the block
            var transactions = await SelectTransactionsAsync(new TransactionSelectionRequest
            {
                AvailableTransactions = request.AvailableTransactions,
                MaxTransactions = request.MaxTransactions,
                MaxBlockSize = request.MaxBlockSize,
                Algorithm = request.Algorithm
            });

            // Calculate merkle root
            var merkleRoot = CalculateMerkleRoot(transactions);

            // Create the block
            var block = new Block
            {
                Id = Guid.NewGuid(),
                BlockNumber = request.BlockNumber,
                PreviousHash = request.PreviousBlockHash,
                MerkleRoot = merkleRoot,
                Timestamp = DateTime.UtcNow,
                ProposerId = request.ProposerId,
                SimulationRunId = request.SimulationId,
                Nonce = request.Nonce ?? 0,
                Difficulty = request.Difficulty ?? CalculateDifficulty(request.Algorithm),
                Data = request.ConsensusData ?? new Dictionary<string, object>(),
                Transactions = transactions,
                TransactionCount = transactions.Count,
                Size = CalculateBlockSize(transactions),
                CreatedAt = DateTime.UtcNow
            };

            // Calculate and set the block hash
            block.Hash = block.CalculateHash();

            // Validate the created block
            var validation = await _validator.ValidateBlockAsync(block, null, request.Algorithm);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Created block failed validation: {validation.ErrorMessage}");
            }

            _logger.LogInformation("Successfully created block {BlockNumber} with {TransactionCount} transactions", 
                block.BlockNumber, block.TransactionCount);

            return block;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating block {BlockNumber}", request.BlockNumber);
            throw;
        }
    }

    public async Task<IList<Transaction>> SelectTransactionsAsync(TransactionSelectionRequest request)
    {
        _logger.LogDebug("Selecting transactions for block creation");

        try
        {
            var availableTransactions = request.AvailableTransactions
                .Where(t => t.Status == TransactionStatus.Pending)
                .OrderByDescending(t => t.Fee) // Prioritize higher fee transactions
                .ThenBy(t => t.CreatedAt) // Then by creation time (FIFO)
                .ToList();

            var selectedTransactions = new List<Transaction>();
            int totalSize = 0;

            foreach (var transaction in availableTransactions)
            {
                if (selectedTransactions.Count >= request.MaxTransactions)
                    break;

                var transactionSize = CalculateTransactionSize(transaction);
                if (totalSize + transactionSize > request.MaxBlockSize)
                    break;

                // Validate transaction before including
                var validation = await _validator.ValidateTransactionAsync(transaction, selectedTransactions);
                if (validation.IsValid)
                {
                    selectedTransactions.Add(transaction);
                    totalSize += transactionSize;
                }
                else
                {
                    _logger.LogWarning("Skipping invalid transaction {TransactionId}: {Error}", 
                        transaction.Id, validation.ErrorMessage);
                }
            }

            _logger.LogDebug("Selected {Count} transactions for block inclusion", selectedTransactions.Count);
            return selectedTransactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting transactions for block");
            throw;
        }
    }

    public string CalculateMerkleRoot(IEnumerable<Transaction> transactions)
    {
        var hashes = transactions.Select(t => t.Hash);
        return CalculateMerkleRootFromHashes(hashes);
    }

    public async Task<Block> BundleTransactionsAsync(TransactionBundlingRequest request)
    {
        _logger.LogInformation("Bundling {TransactionCount} transactions into block", 
            request.Transactions.Count());

        try
        {
            var transactions = request.Transactions.ToList();

            // Validate all transactions
            for (int i = 0; i < transactions.Count; i++)
            {
                var transaction = transactions[i];
                var existingTransactions = transactions.Take(i);

                var validation = await _validator.ValidateTransactionAsync(transaction, existingTransactions);
                if (!validation.IsValid)
                {
                    throw new InvalidOperationException($"Transaction {transaction.Id} validation failed: {validation.ErrorMessage}");
                }

                // Mark transaction as confirmed
                transaction.Confirm(request.BlockId, i);
            }

            // Calculate merkle root
            var merkleRoot = CalculateMerkleRoot(transactions);

            // Create block with bundled transactions
            var block = new Block
            {
                Id = request.BlockId,
                BlockNumber = request.BlockNumber,
                PreviousHash = request.PreviousBlockHash,
                MerkleRoot = merkleRoot,
                Timestamp = DateTime.UtcNow,
                ProposerId = request.ProposerId,
                SimulationRunId = request.SimulationId,
                Transactions = transactions,
                TransactionCount = transactions.Count,
                Size = CalculateBlockSize(transactions),
                Data = request.ConsensusData ?? new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow
            };

            // Calculate and set block hash
            block.Hash = block.CalculateHash();

            _logger.LogInformation("Successfully bundled transactions into block {BlockNumber}", block.BlockNumber);
            return block;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bundling transactions into block");
            throw;
        }
    }

    public async Task<OrphanBlockResult> HandleOrphanBlockAsync(OrphanBlockRequest request)
    {
        _logger.LogInformation("Handling orphan block {BlockId}", request.OrphanBlock.Id);

        try
        {
            var result = new OrphanBlockResult
            {
                OrphanBlock = request.OrphanBlock,
                Action = OrphanBlockAction.Store,
                ReorganizationRequired = false
            };

            // Check if the orphan block extends a known chain
            var parentBlock = request.KnownBlocks
                .FirstOrDefault(b => b.Hash == request.OrphanBlock.PreviousHash);

            if (parentBlock == null)
            {
                // No known parent - store as orphan
                return result with 
                { 
                    Action = OrphanBlockAction.Store,
                    Reason = "No known parent block found"
                };
            }

            // Check if this creates a longer chain than the current main chain
            var mainChainLength = request.MainChainBlocks.Count();
            var orphanChainLength = CalculateChainLength(request.OrphanBlock, request.KnownBlocks);

            if (orphanChainLength > mainChainLength)
            {
                // Reorganization required
                return result with 
                { 
                    Action = OrphanBlockAction.Reorganize,
                    ReorganizationRequired = true,
                    NewMainChain = BuildChainFromBlock(request.OrphanBlock, request.KnownBlocks),
                    Reason = $"Orphan chain is longer ({orphanChainLength} vs {mainChainLength})"
                };
            }
            else
            {
                // Keep as side chain
                return result with 
                { 
                    Action = OrphanBlockAction.KeepAsSideChain,
                    Reason = "Chain not longer than main chain"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling orphan block {BlockId}", request.OrphanBlock.Id);
            throw;
        }
    }

    // Private helper methods
    private string CalculateMerkleRootFromHashes(IEnumerable<string> hashes)
    {
        var hashList = hashes.ToList();
        
        if (!hashList.Any())
            return string.Empty;

        if (hashList.Count == 1)
            return hashList[0];

        while (hashList.Count > 1)
        {
            var newLevel = new List<string>();

            for (int i = 0; i < hashList.Count; i += 2)
            {
                var left = hashList[i];
                var right = i + 1 < hashList.Count ? hashList[i + 1] : left;

                var combined = left + right;
                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                newLevel.Add(Convert.ToHexString(hashBytes).ToLowerInvariant());
            }

            hashList = newLevel;
        }

        return hashList[0];
    }

    private int CalculateTransactionSize(Transaction transaction)
    {
        // Simplified size calculation
        var baseSize = 200; // Base transaction overhead
        var dataSize = transaction.InputData?.Length ?? 0;
        var addressSize = (transaction.FromAddress?.Length ?? 0) + (transaction.ToAddress?.Length ?? 0);
        
        return baseSize + dataSize + addressSize;
    }

    private int CalculateBlockSize(IEnumerable<Transaction> transactions)
    {
        var baseBlockSize = 512; // Block header and metadata
        var transactionSizes = transactions.Sum(CalculateTransactionSize);
        
        return baseBlockSize + transactionSizes;
    }

    private long CalculateDifficulty(ConsensusAlgorithm algorithm)
    {
        return algorithm switch
        {
            ConsensusAlgorithm.ProofOfWork => 1000, // Starting difficulty for PoW
            ConsensusAlgorithm.ProofOfStake => 1,
            ConsensusAlgorithm.ProofOfElapsedTime => 1,
            ConsensusAlgorithm.PracticalByzantineFaultTolerance => 1,
            _ => 1
        };
    }

    private int CalculateChainLength(Block block, IEnumerable<Block> knownBlocks)
    {
        var currentBlock = block;
        int length = 1;

        while (currentBlock.PreviousHash != null)
        {
            var parentBlock = knownBlocks.FirstOrDefault(b => b.Hash == currentBlock.PreviousHash);
            if (parentBlock == null)
                break;

            currentBlock = parentBlock;
            length++;
        }

        return length;
    }

    private IList<Block> BuildChainFromBlock(Block leafBlock, IEnumerable<Block> knownBlocks)
    {
        var chain = new List<Block> { leafBlock };
        var currentBlock = leafBlock;

        while (currentBlock.PreviousHash != null)
        {
            var parentBlock = knownBlocks.FirstOrDefault(b => b.Hash == currentBlock.PreviousHash);
            if (parentBlock == null)
                break;

            chain.Insert(0, parentBlock);
            currentBlock = parentBlock;
        }

        return chain;
    }
}

// Request/Response models for block creation services
public record BlockCreationRequest
{
    public required Guid SimulationId { get; init; }
    public required long BlockNumber { get; init; }
    public string? PreviousBlockHash { get; init; }
    public Guid? ProposerId { get; init; }
    public required ConsensusAlgorithm Algorithm { get; init; }
    public IEnumerable<Transaction> AvailableTransactions { get; init; } = Array.Empty<Transaction>();
    public int MaxTransactions { get; init; } = 1000;
    public int MaxBlockSize { get; init; } = 1024 * 1024; // 1MB
    public long? Nonce { get; init; }
    public long? Difficulty { get; init; }
    public Dictionary<string, object>? ConsensusData { get; init; }
}

public record TransactionSelectionRequest
{
    public required IEnumerable<Transaction> AvailableTransactions { get; init; }
    public int MaxTransactions { get; init; } = 1000;
    public int MaxBlockSize { get; init; } = 1024 * 1024;
    public required ConsensusAlgorithm Algorithm { get; init; }
}

public record TransactionBundlingRequest
{
    public required Guid BlockId { get; init; }
    public required Guid SimulationId { get; init; }
    public required long BlockNumber { get; init; }
    public string? PreviousBlockHash { get; init; }
    public Guid? ProposerId { get; init; }
    public required IEnumerable<Transaction> Transactions { get; init; }
    public Dictionary<string, object>? ConsensusData { get; init; }
}

public record OrphanBlockRequest
{
    public required Block OrphanBlock { get; init; }
    public required IEnumerable<Block> KnownBlocks { get; init; }
    public required IEnumerable<Block> MainChainBlocks { get; init; }
}

public record OrphanBlockResult
{
    public required Block OrphanBlock { get; init; }
    public required OrphanBlockAction Action { get; init; }
    public required bool ReorganizationRequired { get; init; }
    public IList<Block>? NewMainChain { get; init; }
    public string? Reason { get; init; }
}

public enum OrphanBlockAction
{
    Store,
    Reorganize,
    KeepAsSideChain,
    Discard
}