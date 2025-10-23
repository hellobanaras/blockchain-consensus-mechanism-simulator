using System.ComponentModel.DataAnnotations;
using Consensus.Core.Enums;

namespace Consensus.Data.Models;

/// <summary>
/// Summary representation of a block for list views
/// </summary>
public record BlockSummary
{
    /// <summary>
    /// Unique identifier of the block
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Block number in the chain
    /// </summary>
    public required long BlockNumber { get; init; }

    /// <summary>
    /// Block hash (truncated for display)
    /// </summary>
    public required string Hash { get; init; }

    /// <summary>
    /// Previous block hash (truncated for display)
    /// </summary>
    public string? PreviousHash { get; init; }

    /// <summary>
    /// ID of the node that proposed this block
    /// </summary>
    public Guid? ProposerId { get; init; }

    /// <summary>
    /// Name or identifier of the proposer node
    /// </summary>
    public string? ProposerName { get; init; }

    /// <summary>
    /// Consensus protocol used for this block
    /// </summary>
    public required ConsensusAlgorithm Protocol { get; init; }

    /// <summary>
    /// Number of transactions in the block
    /// </summary>
    public required int TransactionCount { get; init; }

    /// <summary>
    /// Block size in bytes
    /// </summary>
    public required int Size { get; init; }

    /// <summary>
    /// When the block was created
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Whether the block is valid
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Simulation this block belongs to
    /// </summary>
    public required Guid SimulationId { get; init; }

    /// <summary>
    /// Name of the simulation
    /// </summary>
    public string? SimulationName { get; init; }
}

/// <summary>
/// Detailed representation of a block with all information
/// </summary>
public record BlockDetail
{
    /// <summary>
    /// Unique identifier of the block
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Block number in the chain
    /// </summary>
    public required long BlockNumber { get; init; }

    /// <summary>
    /// Full block hash
    /// </summary>
    public required string Hash { get; init; }

    /// <summary>
    /// Previous block hash for chain linkage
    /// </summary>
    public string? PreviousHash { get; init; }

    /// <summary>
    /// Merkle root of transactions
    /// </summary>
    public string? MerkleRoot { get; init; }

    /// <summary>
    /// Block timestamp
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Nonce used in block mining/validation
    /// </summary>
    public required long Nonce { get; init; }

    /// <summary>
    /// Difficulty target for this block
    /// </summary>
    public required long Difficulty { get; init; }

    /// <summary>
    /// Block size in bytes
    /// </summary>
    public required int Size { get; init; }

    /// <summary>
    /// Number of transactions in the block
    /// </summary>
    public required int TransactionCount { get; init; }

    /// <summary>
    /// Consensus protocol used
    /// </summary>
    public required ConsensusAlgorithm Protocol { get; init; }

    /// <summary>
    /// Whether the block is valid
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// ID of the proposing node
    /// </summary>
    public Guid? ProposerId { get; init; }

    /// <summary>
    /// Proposer node details
    /// </summary>
    public BlockProposerInfo? Proposer { get; init; }

    /// <summary>
    /// Simulation information
    /// </summary>
    public required BlockSimulationInfo Simulation { get; init; }

    /// <summary>
    /// Additional protocol-specific data
    /// </summary>
    public Dictionary<string, object>? Data { get; init; }

    /// <summary>
    /// When the block was created
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// List of transactions in this block
    /// </summary>
    public IReadOnlyList<BlockTransactionInfo> Transactions { get; init; } = Array.Empty<BlockTransactionInfo>();

    /// <summary>
    /// Navigation information to previous/next blocks
    /// </summary>
    public BlockNavigation? Navigation { get; init; }
}

/// <summary>
/// Information about the block proposer
/// </summary>
public record BlockProposerInfo
{
    /// <summary>
    /// Node ID
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Node display name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Node status at time of proposal
    /// </summary>
    public required NodeStatus Status { get; init; }

    /// <summary>
    /// Node power/stake at time of proposal
    /// </summary>
    public required double Power { get; init; }
}

/// <summary>
/// Simulation information for the block
/// </summary>
public record BlockSimulationInfo
{
    /// <summary>
    /// Simulation ID
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Simulation name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Total blocks in this simulation
    /// </summary>
    public required int TotalBlocks { get; init; }

    /// <summary>
    /// Total nodes in the simulation
    /// </summary>
    public required int TotalNodes { get; init; }
}

/// <summary>
/// Transaction information within a block
/// </summary>
public record BlockTransactionInfo
{
    /// <summary>
    /// Transaction ID
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Transaction hash
    /// </summary>
    public required string Hash { get; init; }

    /// <summary>
    /// From address
    /// </summary>
    public required string From { get; init; }

    /// <summary>
    /// To address
    /// </summary>
    public required string To { get; init; }

    /// <summary>
    /// Transaction amount
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Transaction fee
    /// </summary>
    public required decimal Fee { get; init; }

    /// <summary>
    /// Transaction status
    /// </summary>
    public required TransactionStatus Status { get; init; }

    /// <summary>
    /// Gas used for the transaction
    /// </summary>
    public required long GasUsed { get; init; }
}

/// <summary>
/// Navigation information for moving between blocks
/// </summary>
public record BlockNavigation
{
    /// <summary>
    /// Previous block ID (if exists)
    /// </summary>
    public Guid? PreviousBlockId { get; init; }

    /// <summary>
    /// Previous block number (if exists)
    /// </summary>
    public long? PreviousBlockNumber { get; init; }

    /// <summary>
    /// Next block ID (if exists)
    /// </summary>
    public Guid? NextBlockId { get; init; }

    /// <summary>
    /// Next block number (if exists)
    /// </summary>
    public long? NextBlockNumber { get; init; }

    /// <summary>
    /// Whether this is the genesis block
    /// </summary>
    public required bool IsGenesis { get; init; }

    /// <summary>
    /// Whether this is the latest block
    /// </summary>
    public required bool IsLatest { get; init; }
}

/// <summary>
/// Request parameters for listing blocks
/// </summary>
public record ListBlocksRequest
{
    /// <summary>
    /// Filter by simulation ID
    /// </summary>
    public Guid? SimulationId { get; init; }

    /// <summary>
    /// Filter by consensus protocol
    /// </summary>
    public ConsensusAlgorithm? Protocol { get; init; }

    /// <summary>
    /// Filter by proposer node ID
    /// </summary>
    public Guid? ProposerId { get; init; }

    /// <summary>
    /// Filter by minimum block number
    /// </summary>
    public long? MinBlockNumber { get; init; }

    /// <summary>
    /// Filter by maximum block number
    /// </summary>
    public long? MaxBlockNumber { get; init; }

    /// <summary>
    /// Filter by date range start
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Filter by date range end
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Search term for block hash or other identifiers
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Show only valid blocks (default: null = all)
    /// </summary>
    public bool? IsValid { get; init; }

    /// <summary>
    /// Sort field
    /// </summary>
    public BlockSortField SortBy { get; init; } = BlockSortField.BlockNumber;

    /// <summary>
    /// Sort direction
    /// </summary>
    public SortDirection SortDirection { get; init; } = SortDirection.Descending;

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Page size (max 100)
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Response containing paginated block list
/// </summary>
public record ListBlocksResponse
{
    /// <summary>
    /// List of blocks in the current page
    /// </summary>
    public required IReadOnlyList<BlockSummary> Blocks { get; init; }

    /// <summary>
    /// Pagination information
    /// </summary>
    public required PaginationInfo Pagination { get; init; }

    /// <summary>
    /// Applied filters summary
    /// </summary>
    public required BlockFiltersApplied Filters { get; init; }
}

/// <summary>
/// Information about applied filters
/// </summary>
public record BlockFiltersApplied
{
    /// <summary>
    /// Total blocks before filtering
    /// </summary>
    public required int TotalBlocksInSystem { get; init; }

    /// <summary>
    /// Blocks matching the filters
    /// </summary>
    public required int FilteredBlockCount { get; init; }

    /// <summary>
    /// Applied simulation filter
    /// </summary>
    public string? SimulationName { get; init; }

    /// <summary>
    /// Applied protocol filter
    /// </summary>
    public ConsensusAlgorithm? Protocol { get; init; }

    /// <summary>
    /// Applied search term
    /// </summary>
    public string? SearchTerm { get; init; }
}

/// <summary>
/// Pagination metadata
/// </summary>
public record PaginationInfo
{
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public required int CurrentPage { get; init; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Total number of items available
    /// </summary>
    public required int TotalItems { get; init; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public required int TotalPages { get; init; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public required bool HasPreviousPage { get; init; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public required bool HasNextPage { get; init; }
}

/// <summary>
/// Sort fields for block listing
/// </summary>
public enum BlockSortField
{
    BlockNumber,
    CreatedAt,
    TransactionCount,
    Size,
    Protocol
}

/// <summary>
/// Sort direction
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}

/// <summary>
/// Block statistics for a simulation
/// </summary>
public record BlockStatistics
{
    /// <summary>
    /// Total number of blocks
    /// </summary>
    public required int TotalBlocks { get; init; }

    /// <summary>
    /// Number of valid blocks
    /// </summary>
    public required int ValidBlocks { get; init; }

    /// <summary>
    /// Number of invalid blocks
    /// </summary>
    public required int InvalidBlocks { get; init; }

    /// <summary>
    /// Total number of transactions across all blocks
    /// </summary>
    public required int TotalTransactions { get; init; }

    /// <summary>
    /// Average block size in bytes
    /// </summary>
    public required double AverageBlockSize { get; init; }

    /// <summary>
    /// Average transactions per block
    /// </summary>
    public required double AverageTransactionsPerBlock { get; init; }

    /// <summary>
    /// Average time between blocks
    /// </summary>
    public required TimeSpan AverageBlockTime { get; init; }

    /// <summary>
    /// Chain height (latest block number)
    /// </summary>
    public required long ChainHeight { get; init; }

    /// <summary>
    /// First block timestamp
    /// </summary>
    public DateTime? FirstBlockTime { get; init; }

    /// <summary>
    /// Latest block timestamp  
    /// </summary>
    public DateTime? LatestBlockTime { get; init; }

    /// <summary>
    /// Total chain duration
    /// </summary>
    public TimeSpan? ChainDuration { get; init; }
}

/// <summary>
/// Chain information for a simulation
/// </summary>
public record ChainInfo
{
    /// <summary>
    /// Simulation identifier
    /// </summary>
    public required Guid SimulationId { get; init; }

    /// <summary>
    /// Current chain height
    /// </summary>
    public required long Height { get; init; }

    /// <summary>
    /// Genesis block hash
    /// </summary>
    public string? GenesisHash { get; init; }

    /// <summary>
    /// Latest block hash
    /// </summary>
    public string? LatestHash { get; init; }

    /// <summary>
    /// Chain validity status
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Number of orphaned blocks (if any)
    /// </summary>
    public required int OrphanedBlocks { get; init; }

    /// <summary>
    /// Hash rate or throughput metric
    /// </summary>
    public double? Throughput { get; init; }

    /// <summary>
    /// Average block creation time
    /// </summary>
    public TimeSpan? AverageBlockTime { get; init; }
}