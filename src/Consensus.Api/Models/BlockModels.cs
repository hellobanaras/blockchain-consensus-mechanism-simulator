using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Consensus.Api.Models;

/// <summary>
/// Block summary model for list views and basic information display
/// </summary>
public class BlockSummary
{
    /// <summary>
    /// Block height in the blockchain
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Unique block hash identifier
    /// </summary>
    [Required]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the previous block in the chain
    /// </summary>
    [Required]
    public string PreviousHash { get; set; } = string.Empty;

    /// <summary>
    /// Consensus protocol used for this block
    /// </summary>
    [Required]
    public string Protocol { get; set; } = string.Empty;

    /// <summary>
    /// Name of the node that proposed this block
    /// </summary>
    [Required]
    public string ProposerName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the block was created
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Experiment ID this block belongs to
    /// </summary>
    public Guid ExperimentId { get; set; }

    /// <summary>
    /// Name of the experiment this block belongs to
    /// </summary>
    public string? ExperimentName { get; set; }

    /// <summary>
    /// Size of the block payload in bytes
    /// </summary>
    public int PayloadSize { get; set; }

    /// <summary>
    /// Number of transactions or events in this block
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Block processing time in milliseconds (for performance metrics)
    /// </summary>
    public double ProcessingTimeMs { get; set; }
}

/// <summary>
/// Detailed block model for individual block views with complete information
/// </summary>
public class BlockDetail
{
    /// <summary>
    /// Block height in the blockchain
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Unique block hash identifier
    /// </summary>
    [Required]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the previous block in the chain
    /// </summary>
    [Required]
    public string PreviousHash { get; set; } = string.Empty;

    /// <summary>
    /// ID of the node that proposed this block
    /// </summary>
    public Guid ProposerNodeId { get; set; }

    /// <summary>
    /// Name of the node that proposed this block
    /// </summary>
    [Required]
    public string ProposerName { get; set; } = string.Empty;

    /// <summary>
    /// Consensus protocol used for this block
    /// </summary>
    [Required]
    public string Protocol { get; set; } = string.Empty;

    /// <summary>
    /// Block payload data (can be large)
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// Timestamp when the block was created
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Experiment ID this block belongs to
    /// </summary>
    public Guid ExperimentId { get; set; }

    /// <summary>
    /// Name of the experiment this block belongs to
    /// </summary>
    public string? ExperimentName { get; set; }

    /// <summary>
    /// Size of the block payload in bytes
    /// </summary>
    public int PayloadSize { get; set; }

    /// <summary>
    /// Number of transactions or events in this block
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Block processing time in milliseconds
    /// </summary>
    public double ProcessingTimeMs { get; set; }

    /// <summary>
    /// Proposer node's power/stake at time of block creation
    /// </summary>
    public decimal ProposerPower { get; set; }

    /// <summary>
    /// Proposer node's stake at time of block creation (for PoS protocols)
    /// </summary>
    public decimal ProposerStake { get; set; }

    /// <summary>
    /// Wait time for this block in milliseconds (relevant for PoET)
    /// </summary>
    public double? WaitTimeMs { get; set; }

    /// <summary>
    /// Number of votes received for this block (relevant for voting protocols)
    /// </summary>
    public int? VoteCount { get; set; }

    /// <summary>
    /// Whether this block achieved quorum (relevant for PBFT)
    /// </summary>
    public bool? HasQuorum { get; set; }

    /// <summary>
    /// Protocol-specific metadata as JSON
    /// </summary>
    public string? ProtocolMetadata { get; set; }

    /// <summary>
    /// Navigation: Previous block details (null for genesis)
    /// </summary>
    public BlockNavigationInfo? PreviousBlock { get; set; }

    /// <summary>
    /// Navigation: Next block details (null for latest)
    /// </summary>
    public BlockNavigationInfo? NextBlock { get; set; }
}

/// <summary>
/// Block navigation information for previous/next links
/// </summary>
public class BlockNavigationInfo
{
    /// <summary>
    /// Block height
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Block hash
    /// </summary>
    [Required]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Block timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Proposer name
    /// </summary>
    [Required]
    public string ProposerName { get; set; } = string.Empty;
}

/// <summary>
/// Block filtering options for API queries
/// </summary>
public class BlockFilterOptions
{
    /// <summary>
    /// Filter by consensus protocol
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    /// Filter by experiment ID
    /// </summary>
    public Guid? ExperimentId { get; set; }

    /// <summary>
    /// Filter by proposer node ID
    /// </summary>
    public Guid? ProposerNodeId { get; set; }

    /// <summary>
    /// Filter by minimum block height
    /// </summary>
    public int? MinHeight { get; set; }

    /// <summary>
    /// Filter by maximum block height
    /// </summary>
    public int? MaxHeight { get; set; }

    /// <summary>
    /// Filter by start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Filter by end date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Sort field options
    /// </summary>
    public BlockSortField SortBy { get; set; } = BlockSortField.Height;

    /// <summary>
    /// Sort direction
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}

/// <summary>
/// Block sorting field options
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BlockSortField
{
    Height,
    Timestamp,
    Protocol,
    ProposerName,
    PayloadSize,
    ProcessingTime
}

/// <summary>
/// Sort direction options
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortDirection
{
    Ascending,
    Descending
}

/// <summary>
/// Block statistics summary for explorer overview
/// </summary>
public class BlockStatistics
{
    /// <summary>
    /// Total number of blocks in the blockchain
    /// </summary>
    public int TotalBlocks { get; set; }

    /// <summary>
    /// Height of the latest block
    /// </summary>
    public int LatestBlockHeight { get; set; }

    /// <summary>
    /// Average block processing time in milliseconds
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// Total size of all blocks in bytes
    /// </summary>
    public long TotalBlockchainSizeBytes { get; set; }

    /// <summary>
    /// Number of unique proposer nodes
    /// </summary>
    public int UniqueProposers { get; set; }

    /// <summary>
    /// Distribution of blocks by protocol
    /// </summary>
    public Dictionary<string, int> ProtocolDistribution { get; set; } = new();

    /// <summary>
    /// Blocks created in the last 24 hours
    /// </summary>
    public int BlocksLast24Hours { get; set; }

    /// <summary>
    /// Average block size in bytes
    /// </summary>
    public double AverageBlockSizeBytes { get; set; }

    /// <summary>
    /// Timestamp of the latest block
    /// </summary>
    public DateTime? LatestBlockTimestamp { get; set; }
}

/// <summary>
/// Paged result container for API responses
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// List of items for the current page
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Whether there is a next page available
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Whether there is a previous page available
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Applied filter options for this result set
    /// </summary>
    public BlockFilterOptions? Filters { get; set; }
}