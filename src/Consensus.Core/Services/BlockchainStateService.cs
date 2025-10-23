using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Consensus.Core.Services;

/// <summary>
/// Interface for blockchain state management
/// </summary>
public interface IBlockchainStateService
{
    /// <summary>
    /// Adds a new block to the blockchain
    /// </summary>
    Task<BlockAdditionResult> AddBlockAsync(BlockAdditionRequest request);
    
    /// <summary>
    /// Validates the entire blockchain integrity
    /// </summary>
    Task<ChainValidationResult> ValidateChainIntegrityAsync(Guid simulationId);
    
    /// <summary>
    /// Handles blockchain reorganization when a longer chain is discovered
    /// </summary>
    Task<ReorganizationResult> ReorganizeChainAsync(ReorganizationRequest request);
    
    /// <summary>
    /// Gets the current blockchain state
    /// </summary>
    Task<BlockchainState> GetChainStateAsync(Guid simulationId);
    
    /// <summary>
    /// Resolves fork conflicts in the blockchain
    /// </summary>
    Task<ForkResolutionResult> ResolveForkAsync(ForkResolutionRequest request);
}

/// <summary>
/// Blockchain state service implementation
/// </summary>
public class BlockchainStateService : IBlockchainStateService
{
    private readonly IBlockValidator _blockValidator;
    private readonly ILogger<BlockchainStateService> _logger;
    private readonly ConcurrentDictionary<Guid, BlockchainState> _chainStates;

    public BlockchainStateService(
        IBlockValidator blockValidator,
        ILogger<BlockchainStateService> logger)
    {
        _blockValidator = blockValidator;
        _logger = logger;
        _chainStates = new ConcurrentDictionary<Guid, BlockchainState>();
    }

    public async Task<BlockAdditionResult> AddBlockAsync(BlockAdditionRequest request)
    {
        _logger.LogInformation("Adding block {BlockNumber} to simulation {SimulationId}", 
            request.Block.BlockNumber, request.SimulationId);

        try
        {
            // Get current chain state
            var chainState = await GetChainStateAsync(request.SimulationId);
            
            // Validate the block
            var previousBlock = chainState.MainChain.LastOrDefault();
            var blockValidation = await _blockValidator.ValidateBlockAsync(
                request.Block, previousBlock, request.Algorithm);

            if (!blockValidation.IsValid)
            {
                _logger.LogWarning("Block {BlockNumber} validation failed: {Error}", 
                    request.Block.BlockNumber, blockValidation.ErrorMessage);

                return new BlockAdditionResult
                {
                    Success = false,
                    ErrorMessage = blockValidation.ErrorMessage,
                    Action = BlockAction.Rejected,
                    UpdatedChainState = chainState,
                    ChainReorganized = false
                };
            }

            // Check if block extends the main chain
            if (ExtendsMainChain(request.Block, chainState))
            {
                // Add to main chain
                chainState.MainChain.Add(request.Block);
                chainState.BlockHeight = request.Block.BlockNumber;
                chainState.LatestBlockHash = request.Block.Hash;
                chainState.LastUpdated = DateTime.UtcNow;

                // Update chain state
                _chainStates[request.SimulationId] = chainState;

                _logger.LogInformation("Block {BlockNumber} added to main chain", request.Block.BlockNumber);

                return new BlockAdditionResult
                {
                    Success = true,
                    Action = BlockAction.AddedToMainChain,
                    UpdatedChainState = chainState,
                    ChainReorganized = false
                };
            }

            // Check if block creates a fork
            if (CreatesFork(request.Block, chainState))
            {
                // Add to side chain
                if (!chainState.SideChains.ContainsKey(request.Block.PreviousHash!))
                {
                    chainState.SideChains[request.Block.PreviousHash!] = new List<Block>();
                }
                chainState.SideChains[request.Block.PreviousHash!].Add(request.Block);

                // Check if side chain is now longer than main chain
                var sideChainLength = CalculateSideChainLength(request.Block.PreviousHash!, chainState);
                if (sideChainLength > chainState.MainChain.Count)
                {
                    // Reorganization needed
                    var reorgResult = await ReorganizeChainAsync(new ReorganizationRequest
                    {
                        SimulationId = request.SimulationId,
                        NewMainChainTip = request.Block,
                        Algorithm = request.Algorithm
                    });

                    if (reorgResult.Success)
                    {
                        _logger.LogInformation("Chain reorganization completed for simulation {SimulationId}", 
                            request.SimulationId);

                        return new BlockAdditionResult
                        {
                            Success = true,
                            Action = BlockAction.AddedToMainChain,
                            UpdatedChainState = reorgResult.NewChainState,
                            ChainReorganized = true,
                            ReorganizationDetails = reorgResult
                        };
                    }
                }

                _logger.LogInformation("Block {BlockNumber} added to side chain", request.Block.BlockNumber);

                return new BlockAdditionResult
                {
                    Success = true,
                    Action = BlockAction.AddedToSideChain,
                    UpdatedChainState = chainState,
                    ChainReorganized = false
                };
            }

            // Orphan block - no known parent
            chainState.OrphanBlocks.Add(request.Block);
            _chainStates[request.SimulationId] = chainState;

            _logger.LogInformation("Block {BlockNumber} added as orphan", request.Block.BlockNumber);

            return new BlockAdditionResult
            {
                Success = true,
                Action = BlockAction.StoredAsOrphan,
                UpdatedChainState = chainState,
                ChainReorganized = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding block {BlockNumber} to simulation {SimulationId}", 
                request.Block.BlockNumber, request.SimulationId);

            return new BlockAdditionResult
            {
                Success = false,
                ErrorMessage = $"Block addition failed: {ex.Message}",
                Action = BlockAction.Rejected,
                UpdatedChainState = await GetChainStateAsync(request.SimulationId),
                ChainReorganized = false
            };
        }
    }

    public async Task<ChainValidationResult> ValidateChainIntegrityAsync(Guid simulationId)
    {
        _logger.LogInformation("Validating chain integrity for simulation {SimulationId}", simulationId);

        try
        {
            var chainState = await GetChainStateAsync(simulationId);
            var mainChain = chainState.MainChain;

            if (!mainChain.Any())
            {
                return new ChainValidationResult
                {
                    IsValid = true,
                    TotalBlocks = 0,
                    ValidationDetails = "Empty chain is valid"
                };
            }

            // Validate the main chain
            var chainValidation = await _blockValidator.ValidateChainAsync(mainChain, chainState.Algorithm);
            
            var validationResult = new ChainValidationResult
            {
                IsValid = chainValidation.IsValid,
                TotalBlocks = mainChain.Count,
                ValidationDetails = chainValidation.ErrorMessage ?? "Chain validation successful",
                ErrorType = chainValidation.ErrorType,
                BlocksValidated = mainChain.Count,
                ValidationDuration = TimeSpan.FromMilliseconds(100), // Mock timing
                LastValidatedAt = DateTime.UtcNow,
                AdditionalChecks = new List<IntegrityCheck>()
            };

            // Additional integrity checks
            if (chainValidation.IsValid)
            {
                var integrityChecks = PerformAdditionalIntegrityChecks(chainState);
                validationResult = validationResult with { AdditionalChecks = integrityChecks };
                
                if (integrityChecks.Any(c => !c.Passed))
                {
                    validationResult = validationResult with 
                    { 
                        IsValid = false,
                        ValidationDetails = "Additional integrity checks failed"
                    };
                }
            }

            _logger.LogInformation("Chain integrity validation completed for simulation {SimulationId}: {Result}", 
                simulationId, validationResult.IsValid ? "Valid" : "Invalid");

            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating chain integrity for simulation {SimulationId}", simulationId);
            
            return new ChainValidationResult
            {
                IsValid = false,
                ValidationDetails = $"Validation error: {ex.Message}",
                TotalBlocks = 0
            };
        }
    }

    public async Task<ReorganizationResult> ReorganizeChainAsync(ReorganizationRequest request)
    {
        _logger.LogInformation("Starting chain reorganization for simulation {SimulationId}", request.SimulationId);

        try
        {
            var chainState = await GetChainStateAsync(request.SimulationId);
            
            // Build the new main chain from the tip block
            var newMainChain = BuildChainFromTip(request.NewMainChainTip, chainState);
            
            if (!newMainChain.Any())
            {
                return new ReorganizationResult
                {
                    Success = false,
                    ErrorMessage = "Unable to build valid chain from new tip",
                    NewChainState = chainState
                };
            }

            // Validate the new chain
            var chainValidation = await _blockValidator.ValidateChainAsync(newMainChain, request.Algorithm);
            if (!chainValidation.IsValid)
            {
                return new ReorganizationResult
                {
                    Success = false,
                    ErrorMessage = $"New chain validation failed: {chainValidation.ErrorMessage}",
                    NewChainState = chainState
                };
            }

            // Perform reorganization
            var oldMainChain = chainState.MainChain.ToList();
            var revertedBlocks = new List<Block>();
            var appliedBlocks = new List<Block>();

            // Find common ancestor
            var commonAncestor = FindCommonAncestor(oldMainChain, newMainChain);
            
            // Revert blocks after common ancestor
            for (int i = oldMainChain.Count - 1; i >= 0; i--)
            {
                var block = oldMainChain[i];
                if (block.Hash == commonAncestor?.Hash)
                    break;
                
                revertedBlocks.Add(block);
                // Move reverted transactions back to pending
                foreach (var transaction in block.Transactions)
                {
                    transaction.Status = TransactionStatus.Pending;
                    transaction.BlockId = null;
                    transaction.TransactionIndex = null;
                    transaction.ConfirmedAt = null;
                }
            }

            // Apply new blocks after common ancestor
            var commonAncestorIndex = commonAncestor != null 
                ? newMainChain.FindIndex(b => b.Hash == commonAncestor.Hash)
                : -1;

            for (int i = commonAncestorIndex + 1; i < newMainChain.Count; i++)
            {
                var block = newMainChain[i];
                appliedBlocks.Add(block);
                
                // Confirm transactions in the new blocks
                for (int txIndex = 0; txIndex < block.Transactions.Count; txIndex++)
                {
                    var transaction = block.Transactions.ElementAt(txIndex);
                    transaction.Confirm(block.Id, txIndex);
                }
            }

            // Update chain state
            chainState.MainChain = newMainChain;
            chainState.BlockHeight = newMainChain.Last().BlockNumber;
            chainState.LatestBlockHash = newMainChain.Last().Hash;
            chainState.LastUpdated = DateTime.UtcNow;

            // Move old main chain blocks to side chains if they're not already there
            foreach (var revertedBlock in revertedBlocks)
            {
                if (revertedBlock.PreviousHash != null)
                {
                    if (!chainState.SideChains.ContainsKey(revertedBlock.PreviousHash))
                    {
                        chainState.SideChains[revertedBlock.PreviousHash] = new List<Block>();
                    }
                    if (!chainState.SideChains[revertedBlock.PreviousHash].Contains(revertedBlock))
                    {
                        chainState.SideChains[revertedBlock.PreviousHash].Add(revertedBlock);
                    }
                }
            }

            // Update state
            _chainStates[request.SimulationId] = chainState;

            _logger.LogInformation("Chain reorganization completed: reverted {RevertedCount} blocks, applied {AppliedCount} blocks", 
                revertedBlocks.Count, appliedBlocks.Count);

            return new ReorganizationResult
            {
                Success = true,
                NewChainState = chainState,
                RevertedBlocks = revertedBlocks,
                AppliedBlocks = appliedBlocks,
                CommonAncestor = commonAncestor,
                ReorganizationDepth = revertedBlocks.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during chain reorganization for simulation {SimulationId}", request.SimulationId);
            
            return new ReorganizationResult
            {
                Success = false,
                ErrorMessage = $"Reorganization failed: {ex.Message}",
                NewChainState = await GetChainStateAsync(request.SimulationId)
            };
        }
    }

    public async Task<BlockchainState> GetChainStateAsync(Guid simulationId)
    {
        if (_chainStates.TryGetValue(simulationId, out var existingState))
        {
            return existingState;
        }

        // Initialize new chain state
        var newState = new BlockchainState
        {
            SimulationId = simulationId,
            MainChain = new List<Block>(),
            SideChains = new Dictionary<string, List<Block>>(),
            OrphanBlocks = new List<Block>(),
            BlockHeight = 0,
            LatestBlockHash = string.Empty,
            Algorithm = ConsensusAlgorithm.ProofOfElapsedTime, // Default
            LastUpdated = DateTime.UtcNow
        };

        _chainStates[simulationId] = newState;
        return newState;
    }

    public async Task<ForkResolutionResult> ResolveForkAsync(ForkResolutionRequest request)
    {
        _logger.LogInformation("Resolving fork for simulation {SimulationId}", request.SimulationId);

        try
        {
            var chainState = await GetChainStateAsync(request.SimulationId);
            var resolutionActions = new List<ForkResolutionAction>();

            // Analyze all side chains
            foreach (var sideChain in chainState.SideChains)
            {
                var sideChainBlocks = sideChain.Value;
                var sideChainLength = CalculateSideChainTotalLength(sideChain.Key, chainState);

                if (sideChainLength > chainState.MainChain.Count)
                {
                    // Side chain is longer - should become main chain
                    resolutionActions.Add(new ForkResolutionAction
                    {
                        ActionType = ForkActionType.Reorganize,
                        TargetChain = sideChainBlocks,
                        Reason = $"Side chain is longer ({sideChainLength} vs {chainState.MainChain.Count})"
                    });
                }
                else if (sideChainLength == chainState.MainChain.Count)
                {
                    // Equal length - use tie-breaking rules
                    var tieBreaker = ResolveTieBreaker(chainState.MainChain, sideChainBlocks, request.Algorithm);
                    if (tieBreaker == TieBreakResult.UseSideChain)
                    {
                        resolutionActions.Add(new ForkResolutionAction
                        {
                            ActionType = ForkActionType.Reorganize,
                            TargetChain = sideChainBlocks,
                            Reason = "Side chain wins tie-breaker"
                        });
                    }
                    else
                    {
                        resolutionActions.Add(new ForkResolutionAction
                        {
                            ActionType = ForkActionType.KeepSideChain,
                            TargetChain = sideChainBlocks,
                            Reason = "Main chain wins tie-breaker"
                        });
                    }
                }
                else
                {
                    // Side chain is shorter - can be pruned if old
                    if (ShouldPruneSideChain(sideChainBlocks, chainState))
                    {
                        resolutionActions.Add(new ForkResolutionAction
                        {
                            ActionType = ForkActionType.Prune,
                            TargetChain = sideChainBlocks,
                            Reason = "Side chain is shorter and old"
                        });
                    }
                    else
                    {
                        resolutionActions.Add(new ForkResolutionAction
                        {
                            ActionType = ForkActionType.KeepSideChain,
                            TargetChain = sideChainBlocks,
                            Reason = "Side chain kept for potential future extension"
                        });
                    }
                }
            }

            // Execute resolution actions
            var updatedChainState = chainState;
            foreach (var action in resolutionActions.Where(a => a.ActionType == ForkActionType.Reorganize))
            {
                var lastBlock = action.TargetChain.LastOrDefault();
                if (lastBlock != null)
                {
                    var reorgResult = await ReorganizeChainAsync(new ReorganizationRequest
                    {
                        SimulationId = request.SimulationId,
                        NewMainChainTip = lastBlock,
                        Algorithm = request.Algorithm
                    });

                    if (reorgResult.Success)
                    {
                        updatedChainState = reorgResult.NewChainState;
                        break; // Only one reorganization per resolution
                    }
                }
            }

            // Prune side chains marked for pruning
            foreach (var action in resolutionActions.Where(a => a.ActionType == ForkActionType.Prune))
            {
                var firstBlock = action.TargetChain.FirstOrDefault();
                if (firstBlock?.PreviousHash != null)
                {
                    updatedChainState.SideChains.Remove(firstBlock.PreviousHash);
                }
            }

            _chainStates[request.SimulationId] = updatedChainState;

            return new ForkResolutionResult
            {
                Success = true,
                ResolutionActions = resolutionActions,
                UpdatedChainState = updatedChainState,
                ForksResolved = resolutionActions.Count(a => a.ActionType == ForkActionType.Reorganize),
                ChainsPruned = resolutionActions.Count(a => a.ActionType == ForkActionType.Prune)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving fork for simulation {SimulationId}", request.SimulationId);
            
            return new ForkResolutionResult
            {
                Success = false,
                ErrorMessage = $"Fork resolution failed: {ex.Message}",
                UpdatedChainState = await GetChainStateAsync(request.SimulationId)
            };
        }
    }

    // Private helper methods
    private bool ExtendsMainChain(Block block, BlockchainState chainState)
    {
        if (!chainState.MainChain.Any())
        {
            return block.IsGenesisBlock();
        }

        var latestBlock = chainState.MainChain.Last();
        return block.PreviousHash == latestBlock.Hash && 
               block.BlockNumber == latestBlock.BlockNumber + 1;
    }

    private bool CreatesFork(Block block, BlockchainState chainState)
    {
        if (string.IsNullOrEmpty(block.PreviousHash))
        {
            return false; // Genesis blocks don't create forks
        }

        // Check if previous hash exists in main chain (but not as the latest block)
        return chainState.MainChain.Any(b => b.Hash == block.PreviousHash) ||
               chainState.SideChains.Values.Any(sideChain => sideChain.Any(b => b.Hash == block.PreviousHash));
    }

    private int CalculateSideChainLength(string parentHash, BlockchainState chainState)
    {
        var length = 0;
        
        // Find parent in main chain
        var parentInMainChain = chainState.MainChain.FirstOrDefault(b => b.Hash == parentHash);
        if (parentInMainChain != null)
        {
            length = (int)parentInMainChain.BlockNumber + 1;
        }

        // Add side chain length
        if (chainState.SideChains.TryGetValue(parentHash, out var sideChain))
        {
            length += sideChain.Count;
        }

        return length;
    }

    private int CalculateSideChainTotalLength(string rootHash, BlockchainState chainState)
    {
        // Calculate total length including blocks before the fork point
        var baseLength = 0;
        
        // Find the fork point in main chain
        var forkPoint = chainState.MainChain.FirstOrDefault(b => b.Hash == rootHash);
        if (forkPoint != null)
        {
            baseLength = (int)forkPoint.BlockNumber + 1;
        }

        // Add side chain blocks
        if (chainState.SideChains.TryGetValue(rootHash, out var sideChain))
        {
            baseLength += sideChain.Count;
        }

        return baseLength;
    }

    private List<Block> BuildChainFromTip(Block tipBlock, BlockchainState chainState)
    {
        var chain = new List<Block> { tipBlock };
        var currentBlock = tipBlock;

        while (!string.IsNullOrEmpty(currentBlock.PreviousHash))
        {
            // Look for parent in main chain
            var parentInMainChain = chainState.MainChain.FirstOrDefault(b => b.Hash == currentBlock.PreviousHash);
            if (parentInMainChain != null)
            {
                // Add all blocks from main chain up to and including the parent
                var parentIndex = chainState.MainChain.IndexOf(parentInMainChain);
                var blocksToAdd = chainState.MainChain.Take(parentIndex + 1).ToList();
                chain.InsertRange(0, blocksToAdd);
                break;
            }

            // Look for parent in side chains
            Block? parentInSideChain = null;
            foreach (var sideChain in chainState.SideChains.Values)
            {
                parentInSideChain = sideChain.FirstOrDefault(b => b.Hash == currentBlock.PreviousHash);
                if (parentInSideChain != null)
                {
                    chain.Insert(0, parentInSideChain);
                    currentBlock = parentInSideChain;
                    break;
                }
            }

            if (parentInSideChain == null)
            {
                // No parent found - incomplete chain
                break;
            }
        }

        return chain.OrderBy(b => b.BlockNumber).ToList();
    }

    private Block? FindCommonAncestor(List<Block> chain1, List<Block> chain2)
    {
        var chain1Hashes = chain1.Select(b => b.Hash).ToHashSet();
        
        // Find the latest common block
        for (int i = chain2.Count - 1; i >= 0; i--)
        {
            if (chain1Hashes.Contains(chain2[i].Hash))
            {
                return chain2[i];
            }
        }

        return null;
    }

    private TieBreakResult ResolveTieBreaker(List<Block> mainChain, List<Block> sideChain, ConsensusAlgorithm algorithm)
    {
        // Implement algorithm-specific tie-breaking rules
        return algorithm switch
        {
            ConsensusAlgorithm.ProofOfWork => ResolvePoWTieBreaker(mainChain, sideChain),
            ConsensusAlgorithm.ProofOfStake => ResolvePoSTieBreaker(mainChain, sideChain),
            _ => ResolveLexicographicTieBreaker(mainChain, sideChain)
        };
    }

    private TieBreakResult ResolvePoWTieBreaker(List<Block> mainChain, List<Block> sideChain)
    {
        // In PoW, choose chain with higher cumulative difficulty
        var mainChainDifficulty = mainChain.Sum(b => b.Difficulty);
        var sideChainDifficulty = sideChain.Sum(b => b.Difficulty);
        
        return sideChainDifficulty > mainChainDifficulty ? TieBreakResult.UseSideChain : TieBreakResult.UseMainChain;
    }

    private TieBreakResult ResolvePoSTieBreaker(List<Block> mainChain, List<Block> sideChain)
    {
        // In PoS, might use earliest timestamp or other criteria
        var mainChainTimestamp = mainChain.LastOrDefault()?.Timestamp ?? DateTime.MaxValue;
        var sideChainTimestamp = sideChain.LastOrDefault()?.Timestamp ?? DateTime.MaxValue;
        
        return sideChainTimestamp < mainChainTimestamp ? TieBreakResult.UseSideChain : TieBreakResult.UseMainChain;
    }

    private TieBreakResult ResolveLexicographicTieBreaker(List<Block> mainChain, List<Block> sideChain)
    {
        // Lexicographic comparison of block hashes
        var mainChainHash = mainChain.LastOrDefault()?.Hash ?? string.Empty;
        var sideChainHash = sideChain.LastOrDefault()?.Hash ?? string.Empty;
        
        return string.Compare(sideChainHash, mainChainHash, StringComparison.OrdinalIgnoreCase) < 0 
            ? TieBreakResult.UseSideChain 
            : TieBreakResult.UseMainChain;
    }

    private bool ShouldPruneSideChain(List<Block> sideChain, BlockchainState chainState)
    {
        // Prune side chains that are significantly behind and old
        var latestSideChainBlock = sideChain.LastOrDefault();
        if (latestSideChainBlock == null) return true;

        var blockAgeCutoff = DateTime.UtcNow.AddHours(-1); // 1 hour cutoff
        var heightDifferenceCutoff = 10; // 10 blocks behind

        return latestSideChainBlock.Timestamp < blockAgeCutoff ||
               (chainState.BlockHeight - latestSideChainBlock.BlockNumber) > heightDifferenceCutoff;
    }

    private List<IntegrityCheck> PerformAdditionalIntegrityChecks(BlockchainState chainState)
    {
        var checks = new List<IntegrityCheck>();

        // Check 1: Verify block number sequence
        checks.Add(new IntegrityCheck
        {
            CheckName = "Block Number Sequence",
            Passed = VerifyBlockNumberSequence(chainState.MainChain),
            Details = "Verify that block numbers are sequential"
        });

        // Check 2: Verify hash chain integrity
        checks.Add(new IntegrityCheck
        {
            CheckName = "Hash Chain Integrity",
            Passed = VerifyHashChainIntegrity(chainState.MainChain),
            Details = "Verify that each block's previous hash matches the previous block's hash"
        });

        // Check 3: Verify timestamp ordering
        checks.Add(new IntegrityCheck
        {
            CheckName = "Timestamp Ordering",
            Passed = VerifyTimestampOrdering(chainState.MainChain),
            Details = "Verify that block timestamps are generally increasing"
        });

        return checks;
    }

    private bool VerifyBlockNumberSequence(List<Block> chain)
    {
        for (int i = 1; i < chain.Count; i++)
        {
            if (chain[i].BlockNumber != chain[i - 1].BlockNumber + 1)
            {
                return false;
            }
        }
        return true;
    }

    private bool VerifyHashChainIntegrity(List<Block> chain)
    {
        for (int i = 1; i < chain.Count; i++)
        {
            if (chain[i].PreviousHash != chain[i - 1].Hash)
            {
                return false;
            }
        }
        return true;
    }

    private bool VerifyTimestampOrdering(List<Block> chain)
    {
        for (int i = 1; i < chain.Count; i++)
        {
            if (chain[i].Timestamp < chain[i - 1].Timestamp)
            {
                return false;
            }
        }
        return true;
    }
}

// Supporting models and enums
public record BlockAdditionRequest
{
    public required Block Block { get; init; }
    public required Guid SimulationId { get; init; }
    public required ConsensusAlgorithm Algorithm { get; init; }
}

public record BlockAdditionResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public required BlockAction Action { get; init; }
    public required BlockchainState UpdatedChainState { get; init; }
    public required bool ChainReorganized { get; init; }
    public ReorganizationResult? ReorganizationDetails { get; init; }
}

public record ReorganizationRequest
{
    public required Guid SimulationId { get; init; }
    public required Block NewMainChainTip { get; init; }
    public required ConsensusAlgorithm Algorithm { get; init; }
}

public record ReorganizationResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public required BlockchainState NewChainState { get; init; }
    public List<Block>? RevertedBlocks { get; init; }
    public List<Block>? AppliedBlocks { get; init; }
    public Block? CommonAncestor { get; init; }
    public int ReorganizationDepth { get; init; }
}

public record ForkResolutionRequest
{
    public required Guid SimulationId { get; init; }
    public required ConsensusAlgorithm Algorithm { get; init; }
}

public record ForkResolutionResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public List<ForkResolutionAction>? ResolutionActions { get; init; }
    public required BlockchainState UpdatedChainState { get; init; }
    public int ForksResolved { get; init; }
    public int ChainsPruned { get; init; }
}

public record ForkResolutionAction
{
    public required ForkActionType ActionType { get; init; }
    public required List<Block> TargetChain { get; init; }
    public required string Reason { get; init; }
}

public record BlockchainState
{
    public required Guid SimulationId { get; init; }
    public required List<Block> MainChain { get; set; }
    public required Dictionary<string, List<Block>> SideChains { get; set; }
    public required List<Block> OrphanBlocks { get; set; }
    public required long BlockHeight { get; set; }
    public required string LatestBlockHash { get; set; }
    public required ConsensusAlgorithm Algorithm { get; set; }
    public required DateTime LastUpdated { get; set; }
}

public record ChainValidationResult
{
    public required bool IsValid { get; init; }
    public required int TotalBlocks { get; init; }
    public required string ValidationDetails { get; init; }
    public ValidationErrorType ErrorType { get; init; }
    public int BlocksValidated { get; init; }
    public TimeSpan ValidationDuration { get; init; }
    public DateTime LastValidatedAt { get; init; }
    public List<IntegrityCheck>? AdditionalChecks { get; init; }
}

public record IntegrityCheck
{
    public required string CheckName { get; init; }
    public required bool Passed { get; init; }
    public required string Details { get; init; }
}

public enum BlockAction
{
    AddedToMainChain,
    AddedToSideChain,
    StoredAsOrphan,
    Rejected
}

public enum ForkActionType
{
    Reorganize,
    KeepSideChain,
    Prune
}

public enum TieBreakResult
{
    UseMainChain,
    UseSideChain
}