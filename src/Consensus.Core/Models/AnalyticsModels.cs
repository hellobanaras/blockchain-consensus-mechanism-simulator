namespace Consensus.Core.Models;

/// <summary>
/// Request model for analytics queries with filtering and configuration options
/// </summary>
public class AnalyticsRequest
{
    /// <summary>
    /// Start date for the analytics period (optional, defaults to beginning of data)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for the analytics period (optional, defaults to current time)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Include detailed node statistics in the response
    /// </summary>
    public bool IncludeNodeStats { get; set; } = true;

    /// <summary>
    /// Include time-series data for charting
    /// </summary>
    public bool IncludeTimeSeriesData { get; set; } = true;

    /// <summary>
    /// Time bucket size for time-series data in minutes (default: 30 minutes)
    /// </summary>
    public int TimeBucketMinutes { get; set; } = 30;

    /// <summary>
    /// Filter by specific consensus algorithms (optional)
    /// </summary>
    public List<string>? AlgorithmFilter { get; set; }

    /// <summary>
    /// Minimum simulation duration to include (in seconds)
    /// </summary>
    public int? MinSimulationDuration { get; set; }

    /// <summary>
    /// Maximum number of results to return for time-series data
    /// </summary>
    public int MaxDataPoints { get; set; } = 1000;
}

/// <summary>
/// Comprehensive analytics summary containing all key metrics and statistics
/// </summary>
public class AnalyticsSummary
{
    /// <summary>
    /// Total number of simulations analyzed
    /// </summary>
    public int TotalSimulations { get; set; }

    /// <summary>
    /// Total consensus rounds across all included simulations. Distinct from
    /// TotalBlocks because a failed round counts toward TotalRounds but
    /// produces no block.
    /// </summary>
    public long TotalRounds { get; set; }

    /// <summary>
    /// Total number of blocks created across all simulations
    /// </summary>
    public long TotalBlocks { get; set; }

    /// <summary>
    /// Total number of transactions processed
    /// </summary>
    public long TotalTransactions { get; set; }

    /// <summary>
    /// Total number of nodes across all simulations
    /// </summary>
    public int TotalNodes { get; set; }

    /// <summary>
    /// Average blocks per simulation
    /// </summary>
    public double AverageBlocksPerSimulation { get; set; }

    /// <summary>
    /// Average transactions per block
    /// </summary>
    public double AverageTransactionsPerBlock { get; set; }

    /// <summary>
    /// Average simulation duration in seconds
    /// </summary>
    public double AverageSimulationDuration { get; set; }

    /// <summary>
    /// Statistics for each node showing winner distribution
    /// </summary>
    public Dictionary<string, NodeWinnerStats> NodeStats { get; set; } = new();

    /// <summary>
    /// Performance metrics by consensus algorithm
    /// </summary>
    public Dictionary<string, AlgorithmPerformanceMetrics> AlgorithmPerformance { get; set; } = new();

    /// <summary>
    /// Time-series data for visualization (when requested)
    /// </summary>
    public List<TimeSeriesDataPoint> TimeSeriesData { get; set; } = new();

    /// <summary>
    /// Date range of the analyzed data
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the analyzed data
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Timestamp when this summary was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Most active consensus algorithm by simulation count
    /// </summary>
    public string? MostUsedAlgorithm { get; set; }

    /// <summary>
    /// Best performing algorithm by average block generation rate
    /// </summary>
    public string? BestPerformingAlgorithm { get; set; }

    /// <summary>
    /// Node with highest block production rate
    /// </summary>
    public string? TopPerformingNode { get; set; }

    /// <summary>
    /// Distribution of simulation statuses
    /// </summary>
    public Dictionary<string, int> StatusDistribution { get; set; } = new();
}

/// <summary>
/// Statistics for individual node performance and block production
/// </summary>
public class NodeWinnerStats
{
    /// <summary>
    /// Node identifier/name
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Total number of blocks produced by this node
    /// </summary>
    public int BlocksProduced { get; set; }

    /// <summary>
    /// Percentage of total blocks produced by this node
    /// </summary>
    public double WinRate { get; set; }

    /// <summary>
    /// Average time between block productions (in seconds)
    /// </summary>
    public double AverageBlockTime { get; set; }

    /// <summary>
    /// Fastest block production time (in seconds)
    /// </summary>
    public double FastestBlockTime { get; set; }

    /// <summary>
    /// Slowest block production time (in seconds)
    /// </summary>
    public double SlowestBlockTime { get; set; }

    /// <summary>
    /// Number of simulations this node participated in
    /// </summary>
    public int SimulationsParticipated { get; set; }

    /// <summary>
    /// Average blocks per simulation for this node
    /// </summary>
    public double AverageBlocksPerSimulation { get; set; }

    /// <summary>
    /// Total uptime across all simulations (in seconds)
    /// </summary>
    public double TotalUptime { get; set; }

    /// <summary>
    /// Performance ranking compared to other nodes (1 = best)
    /// </summary>
    public int PerformanceRank { get; set; }

    /// <summary>
    /// Efficiency score (blocks produced per unit time)
    /// </summary>
    public double EfficiencyScore { get; set; }
}

/// <summary>
/// Performance metrics and statistics for consensus algorithms
/// </summary>
public class AlgorithmPerformanceMetrics
{
    /// <summary>
    /// Algorithm name/type
    /// </summary>
    public string AlgorithmName { get; set; } = string.Empty;

    /// <summary>
    /// Total number of simulations using this algorithm
    /// </summary>
    public int TotalSimulations { get; set; }

    /// <summary>
    /// Average blocks produced per simulation
    /// </summary>
    public double AverageBlocksPerSimulation { get; set; }

    /// <summary>
    /// Average simulation duration in seconds
    /// </summary>
    public double AverageSimulationDuration { get; set; }

    /// <summary>
    /// Average block generation rate (blocks per second)
    /// </summary>
    public double AverageBlockRate { get; set; }

    /// <summary>
    /// Average transaction throughput (transactions per second)
    /// </summary>
    public double AverageTransactionThroughput { get; set; }

    /// <summary>
    /// Success rate (percentage of completed simulations)
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Total computing time spent on this algorithm (in seconds)
    /// </summary>
    public double TotalComputingTime { get; set; }

    /// <summary>
    /// Average number of nodes per simulation
    /// </summary>
    public double AverageNodesPerSimulation { get; set; }

    /// <summary>
    /// Network efficiency score (throughput per node)
    /// </summary>
    public double NetworkEfficiency { get; set; }

    /// <summary>
    /// Algorithm stability score based on variance in performance
    /// </summary>
    public double StabilityScore { get; set; }

    /// <summary>
    /// Best performing simulation ID for this algorithm
    /// </summary>
    public Guid? BestSimulationId { get; set; }

    /// <summary>
    /// Worst performing simulation ID for this algorithm
    /// </summary>
    public Guid? WorstSimulationId { get; set; }

    /// <summary>
    /// Gini coefficient of leader/proposer distribution (0 = perfect equality, 1 = one node leads all).
    /// </summary>
    public double LeaderGini { get; set; }

    /// <summary>
    /// Shannon entropy (bits) of leader/proposer distribution. Higher = more decentralized.
    /// </summary>
    public double LeaderEntropy { get; set; }

    /// <summary>
    /// 95th-percentile inter-block time in milliseconds across all blocks for this algorithm.
    /// </summary>
    public double P95BlockTimeMs { get; set; }

    /// <summary>
    /// 99th-percentile inter-block time in milliseconds.
    /// </summary>
    public double P99BlockTimeMs { get; set; }
}

/// <summary>
/// Time-series data point for analytics charts and visualizations
/// </summary>
public class TimeSeriesDataPoint
{
    /// <summary>
    /// Timestamp for this data point
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Number of active simulations at this time
    /// </summary>
    public int ActiveSimulations { get; set; }

    /// <summary>
    /// Number of blocks created in this time period
    /// </summary>
    public int BlocksCreated { get; set; }

    /// <summary>
    /// Number of transactions processed in this time period
    /// </summary>
    public int TransactionsProcessed { get; set; }

    /// <summary>
    /// Average block generation rate for this time period
    /// </summary>
    public double AverageBlockRate { get; set; }

    /// <summary>
    /// Number of nodes active in this time period
    /// </summary>
    public int ActiveNodes { get; set; }

    /// <summary>
    /// Distribution of algorithms active in this time period
    /// </summary>
    public Dictionary<string, int> AlgorithmDistribution { get; set; } = new();

    /// <summary>
    /// System performance metrics for this time period
    /// </summary>
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();

    /// <summary>
    /// Custom metrics that can be added dynamically
    /// </summary>
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}