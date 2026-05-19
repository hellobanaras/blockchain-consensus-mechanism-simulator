using Consensus.Core.Models;
using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Consensus.Data.Repositories;

/// <summary>
/// Repository implementation for block data access operations
/// </summary>
public class BlockRepository : Repository<Block>, IBlockRepository
{
    private new readonly ConsensusDbContext _context;

    public BlockRepository(ConsensusDbContext context) : base(context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ListBlocksResponse> GetBlocksAsync(ListBlocksRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Blocks
            .Include(b => b.Proposer)
            .Include(b => b.SimulationRun)
            .AsQueryable();

        // Apply filters
        if (request.SimulationId.HasValue)
            query = query.Where(b => b.SimulationRunId == request.SimulationId.Value);

        if (request.ProposerId.HasValue)
            query = query.Where(b => b.ProposerId == request.ProposerId.Value);

        if (request.MinBlockNumber.HasValue)
            query = query.Where(b => b.BlockNumber >= request.MinBlockNumber.Value);

        if (request.MaxBlockNumber.HasValue)
            query = query.Where(b => b.BlockNumber <= request.MaxBlockNumber.Value);

        if (request.StartDate.HasValue)
            query = query.Where(b => b.CreatedAt >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(b => b.CreatedAt <= request.EndDate.Value);

        if (request.IsValid.HasValue)
            query = query.Where(b => b.IsValid == request.IsValid.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim().ToLower();
            query = query.Where(b => 
                b.Hash.ToLower().Contains(searchTerm) ||
                b.BlockNumber.ToString().Contains(searchTerm) ||
                (b.Proposer != null && b.Proposer.Name.ToLower().Contains(searchTerm)));
        }

        // Get total count before sorting and paging
        var totalCount = await query.CountAsync(cancellationToken);
        var totalSystemBlocks = await _context.Blocks.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortBy switch
        {
            BlockSortField.BlockNumber => request.SortDirection == SortDirection.Ascending 
                ? query.OrderBy(b => b.BlockNumber) 
                : query.OrderByDescending(b => b.BlockNumber),
            BlockSortField.CreatedAt => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(b => b.CreatedAt)
                : query.OrderByDescending(b => b.CreatedAt),
            BlockSortField.TransactionCount => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(b => b.TransactionCount)
                : query.OrderByDescending(b => b.TransactionCount),
            BlockSortField.Size => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(b => b.Size)
                : query.OrderByDescending(b => b.Size),
            _ => query.OrderByDescending(b => b.BlockNumber)
        };

        // Apply pagination
        var blocks = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BlockSummary
            {
                Id = b.Id,
                BlockNumber = b.BlockNumber,
                Hash = b.Hash.Length > 16 ? b.Hash.Substring(0, 16) + "..." : b.Hash,
                PreviousHash = !string.IsNullOrEmpty(b.PreviousHash) && b.PreviousHash.Length > 16 
                    ? b.PreviousHash.Substring(0, 16) + "..." 
                    : b.PreviousHash,
                ProposerId = b.ProposerId,
                ProposerName = b.Proposer != null ? b.Proposer.Name : null,
                Protocol = b.SimulationRun!.ConsensusAlgorithm,
                TransactionCount = b.TransactionCount,
                Size = b.Size,
                CreatedAt = b.CreatedAt,
                IsValid = b.IsValid,
                SimulationId = b.SimulationRunId,
                SimulationName = b.SimulationRun!.Name
            })
            .ToListAsync(cancellationToken);

        // Get simulation name for filters if applicable
        string? simulationName = null;
        if (request.SimulationId.HasValue)
        {
            simulationName = await _context.SimulationRuns
                .Where(s => s.Id == request.SimulationId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ListBlocksResponse
        {
            Blocks = blocks,
            Pagination = new PaginationInfo
            {
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                HasPreviousPage = request.Page > 1,
                HasNextPage = request.Page * request.PageSize < totalCount
            },
            Filters = new BlockFiltersApplied
            {
                TotalBlocksInSystem = totalSystemBlocks,
                FilteredBlockCount = totalCount,
                SimulationName = simulationName,
                Protocol = request.Protocol,
                SearchTerm = request.SearchTerm
            }
        };
    }

    public async Task<BlockDetail?> GetBlockDetailAsync(Guid blockId, CancellationToken cancellationToken = default)
    {
        var block = await _context.Blocks
            .Include(b => b.Proposer)
            .Include(b => b.SimulationRun)
            .Include(b => b.Transactions)
            .FirstOrDefaultAsync(b => b.Id == blockId, cancellationToken);

        if (block == null)
            return null;

        // Get navigation info
        var prevBlock = await _context.Blocks
            .Where(b => b.SimulationRunId == block.SimulationRunId && b.BlockNumber == block.BlockNumber - 1)
            .Select(b => new { b.Id, b.BlockNumber })
            .FirstOrDefaultAsync(cancellationToken);

        var nextBlock = await _context.Blocks
            .Where(b => b.SimulationRunId == block.SimulationRunId && b.BlockNumber == block.BlockNumber + 1)
            .Select(b => new { b.Id, b.BlockNumber })
            .FirstOrDefaultAsync(cancellationToken);

        // Get simulation stats
        var simulationStats = await _context.Blocks
            .Where(b => b.SimulationRunId == block.SimulationRunId)
            .GroupBy(b => b.SimulationRunId)
            .Select(g => new { TotalBlocks = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        var nodeStats = await _context.Nodes
            .Where(n => n.SimulationRunId == block.SimulationRunId)
            .CountAsync(cancellationToken);

        return new BlockDetail
        {
            Id = block.Id,
            BlockNumber = block.BlockNumber,
            Hash = block.Hash,
            PreviousHash = block.PreviousHash,
            MerkleRoot = block.MerkleRoot,
            Timestamp = block.Timestamp,
            Nonce = block.Nonce,
            Difficulty = block.Difficulty,
            Size = block.Size,
            TransactionCount = block.TransactionCount,
            Protocol = block.SimulationRun!.ConsensusAlgorithm,
            IsValid = block.IsValid,
            ProposerId = block.ProposerId,
            Proposer = block.Proposer != null ? new BlockProposerInfo
            {
                Id = block.Proposer.Id,
                Name = block.Proposer.Name,
                Status = block.Proposer.Status,
                Power = block.Proposer.ComputationalPower
            } : null,
            Simulation = new BlockSimulationInfo
            {
                Id = block.SimulationRun.Id,
                Name = block.SimulationRun.Name,
                TotalBlocks = simulationStats?.TotalBlocks ?? 0,
                TotalNodes = nodeStats
            },
            Data = block.Data,
            CreatedAt = block.CreatedAt,
            Transactions = block.Transactions.Select(t => new BlockTransactionInfo
            {
                Id = t.Id,
                Hash = t.Hash,
                From = t.FromAddress,
                To = t.ToAddress,
                Amount = t.Amount,
                Fee = t.Fee,
                Status = t.Status,
                GasUsed = t.GasUsed
            }).ToList(),
            Navigation = new BlockNavigation
            {
                PreviousBlockId = prevBlock?.Id,
                PreviousBlockNumber = prevBlock?.BlockNumber,
                NextBlockId = nextBlock?.Id,
                NextBlockNumber = nextBlock?.BlockNumber,
                IsGenesis = block.BlockNumber == 0 || string.IsNullOrEmpty(block.PreviousHash),
                IsLatest = nextBlock == null
            }
        };
    }

    public async Task<Block?> GetBlockByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await _context.Blocks
            .Include(b => b.Proposer)
            .Include(b => b.SimulationRun)
            .Include(b => b.Transactions)
            .FirstOrDefaultAsync(b => b.Hash == hash, cancellationToken);
    }

    public async Task<Block?> GetBlockByNumberAsync(Guid simulationId, long blockNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Blocks
            .Include(b => b.Proposer)
            .Include(b => b.SimulationRun)
            .Include(b => b.Transactions)
            .FirstOrDefaultAsync(b => b.SimulationRunId == simulationId && b.BlockNumber == blockNumber, cancellationToken);
    }

    public async Task<Block?> GetLatestBlockAsync(Guid simulationId, CancellationToken cancellationToken = default)
    {
        return await _context.Blocks
            .Include(b => b.Proposer)
            .Include(b => b.SimulationRun)
            .Where(b => b.SimulationRunId == simulationId)
            .OrderByDescending(b => b.BlockNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<BlockStatistics> GetGlobalBlockStatisticsAsync(CancellationToken cancellationToken = default)
        => GetBlockStatisticsInternalAsync(simulationId: null, cancellationToken);

    public Task<BlockStatistics> GetBlockStatisticsAsync(Guid simulationId, CancellationToken cancellationToken = default)
        => GetBlockStatisticsInternalAsync(simulationId, cancellationToken);

    private async Task<BlockStatistics> GetBlockStatisticsInternalAsync(Guid? simulationId, CancellationToken cancellationToken)
    {
        var blocksQuery = _context.Blocks.AsQueryable();
        if (simulationId.HasValue)
        {
            blocksQuery = blocksQuery.Where(b => b.SimulationRunId == simulationId.Value);
        }
        var blocks = await blocksQuery.ToListAsync(cancellationToken);

        if (!blocks.Any())
        {
            return new BlockStatistics
            {
                TotalBlocks = 0,
                ValidBlocks = 0,
                InvalidBlocks = 0,
                TotalTransactions = 0,
                AverageBlockSize = 0,
                AverageTransactionsPerBlock = 0,
                AverageBlockTime = TimeSpan.Zero,
                ChainHeight = -1
            };
        }

        var sortedBlocks = blocks.OrderBy(b => b.CreatedAt).ToList();
        // The Transactions table is unused by the current simulator (PoW/PoS
        // etc. just stash a synthetic TransactionCount on each Block row).
        // Counting from Transactions returned 0 even when blocks existed,
        // which was the Statistics-page "all zeros" bug; sum the per-block
        // counts instead.
        var totalTransactions = blocks.Sum(b => b.TransactionCount);

        // Calculate average block time
        var avgBlockTime = TimeSpan.Zero;
        if (sortedBlocks.Count > 1)
        {
            var totalTime = sortedBlocks.Last().CreatedAt - sortedBlocks.First().CreatedAt;
            avgBlockTime = TimeSpan.FromMilliseconds(totalTime.TotalMilliseconds / (sortedBlocks.Count - 1));
        }

        return new BlockStatistics
        {
            TotalBlocks = blocks.Count,
            ValidBlocks = blocks.Count(b => b.IsValid),
            InvalidBlocks = blocks.Count(b => !b.IsValid),
            TotalTransactions = totalTransactions,
            AverageBlockSize = blocks.Average(b => b.Size),
            AverageTransactionsPerBlock = blocks.Average(b => (double)b.TransactionCount),
            AverageBlockTime = avgBlockTime,
            ChainHeight = blocks.Max(b => b.BlockNumber),
            FirstBlockTime = sortedBlocks.FirstOrDefault()?.CreatedAt,
            LatestBlockTime = sortedBlocks.LastOrDefault()?.CreatedAt,
            ChainDuration = sortedBlocks.Count > 1 
                ? sortedBlocks.Last().CreatedAt - sortedBlocks.First().CreatedAt 
                : null
        };
    }

    public async Task<BlockSummary?> GetBlockSummaryByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return null;
        }

        var block = await _context.Blocks
            .Include(b => b.Proposer)
            .Include(b => b.SimulationRun)
            .Where(b => b.Hash == hash)
            .Select(b => new BlockSummary
            {
                Id = b.Id,
                BlockNumber = b.BlockNumber,
                Hash = b.Hash.Length > 16 ? b.Hash.Substring(0, 16) + "..." : b.Hash,
                PreviousHash = !string.IsNullOrEmpty(b.PreviousHash) && b.PreviousHash.Length > 16 
                    ? b.PreviousHash.Substring(0, 16) + "..." 
                    : b.PreviousHash,
                ProposerId = b.ProposerId,
                ProposerName = b.Proposer != null ? b.Proposer.Name : null,
                Protocol = b.SimulationRun!.ConsensusAlgorithm,
                TransactionCount = b.TransactionCount,
                Size = b.Size,
                CreatedAt = b.CreatedAt,
                IsValid = b.IsValid,
                SimulationId = b.SimulationRunId,
                SimulationName = b.SimulationRun!.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        return block;
    }

    public async Task<BlockSummary?> GetBlockSummaryByNumberAsync(Guid simulationId, long blockNumber, CancellationToken cancellationToken = default)
    {
        var block = await _context.Blocks
            .Include(b => b.Proposer)
            .Include(b => b.SimulationRun)
            .Where(b => b.SimulationRunId == simulationId && b.BlockNumber == blockNumber)
            .Select(b => new BlockSummary
            {
                Id = b.Id,
                BlockNumber = b.BlockNumber,
                Hash = b.Hash.Length > 16 ? b.Hash.Substring(0, 16) + "..." : b.Hash,
                PreviousHash = !string.IsNullOrEmpty(b.PreviousHash) && b.PreviousHash.Length > 16 
                    ? b.PreviousHash.Substring(0, 16) + "..." 
                    : b.PreviousHash,
                ProposerId = b.ProposerId,
                ProposerName = b.Proposer != null ? b.Proposer.Name : null,
                Protocol = b.SimulationRun!.ConsensusAlgorithm,
                TransactionCount = b.TransactionCount,
                Size = b.Size,
                CreatedAt = b.CreatedAt,
                IsValid = b.IsValid,
                SimulationId = b.SimulationRunId,
                SimulationName = b.SimulationRun!.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        return block;
    }

    public async Task<BlockSummary?> GetLatestBlockSummaryAsync(Guid simulationId, CancellationToken cancellationToken = default)
    {
        var block = await _context.Blocks
            .Include(b => b.Proposer)
            .Include(b => b.SimulationRun)
            .Where(b => b.SimulationRunId == simulationId)
            .OrderByDescending(b => b.BlockNumber)
            .Select(b => new BlockSummary
            {
                Id = b.Id,
                BlockNumber = b.BlockNumber,
                Hash = b.Hash.Length > 16 ? b.Hash.Substring(0, 16) + "..." : b.Hash,
                PreviousHash = !string.IsNullOrEmpty(b.PreviousHash) && b.PreviousHash.Length > 16 
                    ? b.PreviousHash.Substring(0, 16) + "..." 
                    : b.PreviousHash,
                ProposerId = b.ProposerId,
                ProposerName = b.Proposer != null ? b.Proposer.Name : null,
                Protocol = b.SimulationRun!.ConsensusAlgorithm,
                TransactionCount = b.TransactionCount,
                Size = b.Size,
                CreatedAt = b.CreatedAt,
                IsValid = b.IsValid,
                SimulationId = b.SimulationRunId,
                SimulationName = b.SimulationRun!.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        return block;
    }

    public async Task<(IReadOnlyList<BlockSummary> Blocks, int TotalCount)> GetBlocksByProposerAsync(
        Guid nodeId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Blocks
            .Include(b => b.Proposer)
            .Include(b => b.SimulationRun)
            .Where(b => b.ProposerId == nodeId);

        var totalCount = await query.CountAsync(cancellationToken);

        var blocks = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BlockSummary
            {
                Id = b.Id,
                BlockNumber = b.BlockNumber,
                Hash = b.Hash.Length > 16 ? b.Hash.Substring(0, 16) + "..." : b.Hash,
                PreviousHash = !string.IsNullOrEmpty(b.PreviousHash) && b.PreviousHash.Length > 16 
                    ? b.PreviousHash.Substring(0, 16) + "..." 
                    : b.PreviousHash,
                ProposerId = b.ProposerId,
                ProposerName = b.Proposer != null ? b.Proposer.Name : null,
                Protocol = b.SimulationRun!.ConsensusAlgorithm,
                TransactionCount = b.TransactionCount,
                Size = b.Size,
                CreatedAt = b.CreatedAt,
                IsValid = b.IsValid,
                SimulationId = b.SimulationRunId,
                SimulationName = b.SimulationRun!.Name
            })
            .ToListAsync(cancellationToken);

        return (blocks, totalCount);
    }

    public async Task<IReadOnlyList<BlockSummary>> SearchBlocksAsync(
        string searchTerm, Guid? simulationId = null, int limit = 10, CancellationToken cancellationToken = default)
    {
        var query = _context.Blocks
            .Include(b => b.Proposer)
            .Include(b => b.SimulationRun)
            .AsQueryable();

        if (simulationId.HasValue)
            query = query.Where(b => b.SimulationRunId == simulationId.Value);

        var normalizedSearch = searchTerm.Trim().ToLower();

        // Search by hash (exact or partial), block number, or transaction hash
        var blocks = await query
            .Where(b =>
                b.Hash.ToLower().Contains(normalizedSearch) ||
                b.BlockNumber.ToString().Contains(normalizedSearch) ||
                b.Transactions.Any(t => t.Hash.ToLower().Contains(normalizedSearch)))
            .Take(limit)
            .Select(b => new BlockSummary
            {
                Id = b.Id,
                BlockNumber = b.BlockNumber,
                Hash = b.Hash.Length > 16 ? b.Hash.Substring(0, 16) + "..." : b.Hash,
                PreviousHash = !string.IsNullOrEmpty(b.PreviousHash) && b.PreviousHash.Length > 16 
                    ? b.PreviousHash.Substring(0, 16) + "..." 
                    : b.PreviousHash,
                ProposerId = b.ProposerId,
                ProposerName = b.Proposer != null ? b.Proposer.Name : null,
                Protocol = b.SimulationRun!.ConsensusAlgorithm,
                TransactionCount = b.TransactionCount,
                Size = b.Size,
                CreatedAt = b.CreatedAt,
                IsValid = b.IsValid,
                SimulationId = b.SimulationRunId,
                SimulationName = b.SimulationRun!.Name
            })
            .ToListAsync(cancellationToken);

        return blocks;
    }

    public async Task<ChainInfo> GetChainInfoAsync(Guid simulationId, CancellationToken cancellationToken = default)
    {
        var blocks = await _context.Blocks
            .Where(b => b.SimulationRunId == simulationId)
            .OrderBy(b => b.BlockNumber)
            .ToListAsync(cancellationToken);

        if (!blocks.Any())
        {
            return new ChainInfo
            {
                SimulationId = simulationId,
                Height = -1,
                IsValid = true,
                OrphanedBlocks = 0
            };
        }

        // Validate chain integrity
        var isValid = ValidateChainIntegrity(blocks);

        // Calculate average block time
        TimeSpan? avgBlockTime = null;
        if (blocks.Count > 1)
        {
            var totalTime = blocks.Last().CreatedAt - blocks.First().CreatedAt;
            avgBlockTime = TimeSpan.FromMilliseconds(totalTime.TotalMilliseconds / (blocks.Count - 1));
        }

        // For simplicity, assume no orphaned blocks in current implementation
        var orphanedBlocks = 0;

        return new ChainInfo
        {
            SimulationId = simulationId,
            Height = blocks.Max(b => b.BlockNumber),
            GenesisHash = blocks.FirstOrDefault()?.Hash,
            LatestHash = blocks.LastOrDefault()?.Hash,
            IsValid = isValid,
            OrphanedBlocks = orphanedBlocks,
            AverageBlockTime = avgBlockTime,
            Throughput = avgBlockTime?.TotalSeconds > 0 ? 1.0 / avgBlockTime.Value.TotalSeconds : null
        };
    }

    private static bool ValidateChainIntegrity(List<Block> blocks)
    {
        if (!blocks.Any()) return true;

        var sortedBlocks = blocks.OrderBy(b => b.BlockNumber).ToList();

        // Check genesis block
        var genesis = sortedBlocks.First();
        if (genesis.BlockNumber != 0 || !string.IsNullOrEmpty(genesis.PreviousHash))
            return false;

        // Check chain continuity
        for (int i = 1; i < sortedBlocks.Count; i++)
        {
            var current = sortedBlocks[i];
            var previous = sortedBlocks[i - 1];

            // Check block number sequence
            if (current.BlockNumber != previous.BlockNumber + 1)
                return false;

            // Check hash linkage
            if (current.PreviousHash != previous.Hash)
                return false;

            // Validate individual block
            if (!current.ValidateBlock())
                return false;
        }

        return true;
    }

    // Original IBlockRepository methods
    public async Task<IEnumerable<Block>> GetBySimulationRunAsync(Guid simulationRunId)
    {
        return await _context.Blocks
            .Where(b => b.SimulationRunId == simulationRunId)
            .OrderBy(b => b.BlockNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<Block>> GetBlockchainAsync(Guid simulationRunId)
    {
        return await _context.Blocks
            .Where(b => b.SimulationRunId == simulationRunId)
            .OrderBy(b => b.BlockNumber)
            .ToListAsync();
    }

    Task<Block?> IBlockRepository.GetLatestBlockAsync(Guid simulationRunId)
    {
        return _context.Blocks
            .Where(b => b.SimulationRunId == simulationRunId)
            .OrderByDescending(b => b.BlockNumber)
            .FirstOrDefaultAsync();
    }

    Task<Block?> IBlockRepository.GetByHashAsync(string hash)
    {
        return _context.Blocks
            .FirstOrDefaultAsync(b => b.Hash == hash);
    }

    Task<IEnumerable<Block>> IBlockRepository.GetByProposerAsync(Guid proposerId)
    {
        return Task.FromResult(_context.Blocks
            .Where(b => b.ProposerId == proposerId)
            .OrderByDescending(b => b.CreatedAt)
            .AsEnumerable());
    }
}