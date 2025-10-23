using System.ComponentModel.DataAnnotations;

namespace Consensus.Api.Models;

/// <summary>
/// Summary analytics data for a simulation
/// </summary>
public class AnalyticsSummary
{
    /// <summary>
    /// Unique identifier for the simulation
    /// </summary>
    public Guid SimulationId { get; set; }

    /// <summary>
    /// Consensus protocol used in the simulation
    /// </summary>
    [Required]
    public string Protocol { get; set; } = string.Empty;

    /// <summary>
    /// Total number of blocks generated
    /// </summary>
    [Range(0, int.MaxValue)]
    public int TotalBlocks { get; set; }

    /// <summary>
    /// Total number of nodes participating
    /// </summary>
    [Range(1, int.MaxValue)]
    public int TotalNodes { get; set; }

    /// <summary>
    /// Total simulation duration in milliseconds
    /// </summary>
    [Range(0, double.MaxValue)]
    public double SimulationDurationMs { get; set; }

    /// <summary>
    /// Average time between blocks in milliseconds
    /// </summary>
    [Range(0, double.MaxValue)]
    public double AverageBlockTimeMs { get; set; }

    /// <summary>
    /// Distribution of block winners as percentages
    /// Key: Node identifier, Value: Percentage of blocks won
    /// </summary>
    public Dictionary<string, double> WinnerDistribution { get; set; } = new();

    /// <summary>
    /// Protocol-specific metrics and statistics
    /// </summary>
    public Dictionary<string, object> ProtocolMetrics { get; set; } = new();

    /// <summary>
    /// Network performance metrics
    /// </summary>
    public NetworkPerformance? NetworkPerformance { get; set; }

    /// <summary>
    /// Fairness and decentralization metrics
    /// </summary>
    public FairnessMetrics? FairnessMetrics { get; set; }
}

/// <summary>
/// Network performance metrics for simulation analysis
/// </summary>
public class NetworkPerformance
{
    /// <summary>
    /// Average network latency in milliseconds
    /// </summary>
    [Range(0, double.MaxValue)]
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Total number of messages exchanged
    /// </summary>
    [Range(0, int.MaxValue)]
    public int TotalMessages { get; set; }

    /// <summary>
    /// Network throughput in blocks per second
    /// </summary>
    [Range(0, double.MaxValue)]
    public double ThroughputBps { get; set; }

    /// <summary>
    /// Network efficiency score (0-1)
    /// </summary>
    [Range(0, 1)]
    public double EfficiencyScore { get; set; }
}

/// <summary>
/// Fairness and decentralization metrics
/// </summary>
public class FairnessMetrics
{
    /// <summary>
    /// Gini coefficient for measuring inequality (0 = perfect equality, 1 = maximum inequality)
    /// </summary>
    [Range(0, 1)]
    public double GiniCoefficient { get; set; }

    /// <summary>
    /// Entropy measure of block distribution
    /// </summary>
    [Range(0, double.MaxValue)]
    public double BlockDistributionEntropy { get; set; }

    /// <summary>
    /// Nakamoto coefficient (minimum nodes needed to control 51% of blocks)
    /// </summary>
    [Range(1, int.MaxValue)]
    public int NakamotoCoefficient { get; set; }

    /// <summary>
    /// Percentage of blocks controlled by top 3 nodes
    /// </summary>
    [Range(0, 100)]
    public double Top3NodesPercentage { get; set; }
}

/// <summary>
/// Chart data for visualization components
/// </summary>
public class ChartData
{
    /// <summary>
    /// Chart type (bar, line, pie, doughnut, histogram)
    /// </summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Chart title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Data labels for X-axis
    /// </summary>
    public List<string> Labels { get; set; } = new();

    /// <summary>
    /// Chart datasets containing data series
    /// </summary>
    public List<ChartDataset> Datasets { get; set; } = new();

    /// <summary>
    /// Chart configuration options
    /// </summary>
    public ChartOptions? Options { get; set; }
}

/// <summary>
/// Dataset for chart visualization
/// </summary>
public class ChartDataset
{
    /// <summary>
    /// Dataset label
    /// </summary>
    [Required]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Data values
    /// </summary>
    public List<double> Data { get; set; } = new();

    /// <summary>
    /// Background colors for data points
    /// </summary>
    public List<string> BackgroundColor { get; set; } = new();

    /// <summary>
    /// Border colors for data points
    /// </summary>
    public List<string> BorderColor { get; set; } = new();

    /// <summary>
    /// Border width
    /// </summary>
    public int BorderWidth { get; set; } = 1;

    /// <summary>
    /// Fill area under line (for line charts)
    /// </summary>
    public bool Fill { get; set; } = false;

    /// <summary>
    /// Line tension for smooth curves (0-1)
    /// </summary>
    [Range(0, 1)]
    public double Tension { get; set; } = 0;
}

/// <summary>
/// Chart configuration options
/// </summary>
public class ChartOptions
{
    /// <summary>
    /// Whether the chart is responsive
    /// </summary>
    public bool Responsive { get; set; } = true;

    /// <summary>
    /// Maintain aspect ratio
    /// </summary>
    public bool MaintainAspectRatio { get; set; } = true;

    /// <summary>
    /// Chart plugins configuration
    /// </summary>
    public ChartPlugins? Plugins { get; set; }

    /// <summary>
    /// Chart scales configuration
    /// </summary>
    public ChartScales? Scales { get; set; }
}

/// <summary>
/// Chart plugins configuration
/// </summary>
public class ChartPlugins
{
    /// <summary>
    /// Legend configuration
    /// </summary>
    public ChartLegend? Legend { get; set; }

    /// <summary>
    /// Tooltip configuration
    /// </summary>
    public ChartTooltip? Tooltip { get; set; }
}

/// <summary>
/// Chart legend configuration
/// </summary>
public class ChartLegend
{
    /// <summary>
    /// Display legend
    /// </summary>
    public bool Display { get; set; } = true;

    /// <summary>
    /// Legend position (top, bottom, left, right)
    /// </summary>
    public string Position { get; set; } = "top";
}

/// <summary>
/// Chart tooltip configuration
/// </summary>
public class ChartTooltip
{
    /// <summary>
    /// Enable tooltips
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Tooltip mode (point, nearest, index)
    /// </summary>
    public string Mode { get; set; } = "nearest";
}

/// <summary>
/// Chart scales configuration
/// </summary>
public class ChartScales
{
    /// <summary>
    /// X-axis configuration
    /// </summary>
    public ChartAxis? X { get; set; }

    /// <summary>
    /// Y-axis configuration
    /// </summary>
    public ChartAxis? Y { get; set; }
}

/// <summary>
/// Chart axis configuration
/// </summary>
public class ChartAxis
{
    /// <summary>
    /// Display axis
    /// </summary>
    public bool Display { get; set; } = true;

    /// <summary>
    /// Axis title
    /// </summary>
    public ChartAxisTitle? Title { get; set; }

    /// <summary>
    /// Minimum value
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// Maximum value
    /// </summary>
    public double? Max { get; set; }
}

/// <summary>
/// Chart axis title configuration
/// </summary>
public class ChartAxisTitle
{
    /// <summary>
    /// Display title
    /// </summary>
    public bool Display { get; set; } = true;

    /// <summary>
    /// Title text
    /// </summary>
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Histogram data for frequency analysis
/// </summary>
public class HistogramData
{
    /// <summary>
    /// Histogram bin labels
    /// </summary>
    public List<string> Bins { get; set; } = new();

    /// <summary>
    /// Frequency counts for each bin
    /// </summary>
    public List<int> Frequencies { get; set; } = new();

    /// <summary>
    /// Statistical summary
    /// </summary>
    public Dictionary<string, double> Statistics { get; set; } = new();

    /// <summary>
    /// Bin edges for precise analysis
    /// </summary>
    public List<double> BinEdges { get; set; } = new();
}

/// <summary>
/// Time series data for temporal analysis
/// </summary>
public class TimeSeriesData
{
    /// <summary>
    /// Time interval (second, minute, hour, day)
    /// </summary>
    [Required]
    public string Interval { get; set; } = string.Empty;

    /// <summary>
    /// Start time of the series
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time of the series
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Time series data points
    /// </summary>
    public List<TimeSeriesPoint> DataPoints { get; set; } = new();

    /// <summary>
    /// Aggregation method used (avg, sum, min, max)
    /// </summary>
    public string AggregationMethod { get; set; } = "avg";
}

/// <summary>
/// Individual time series data point
/// </summary>
public class TimeSeriesPoint
{
    /// <summary>
    /// Timestamp of the data point
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Metric value
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Metric name/type
    /// </summary>
    [Required]
    public string Metric { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata for the data point
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Protocol-specific metrics container
/// </summary>
public class ProtocolMetrics
{
    /// <summary>
    /// Protocol name
    /// </summary>
    [Required]
    public string Protocol { get; set; } = string.Empty;

    /// <summary>
    /// Protocol-specific metrics and values
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();

    /// <summary>
    /// Timestamp when metrics were calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Multi-simulation comparison data
/// </summary>
public class AnalyticsComparison
{
    /// <summary>
    /// List of simulations being compared
    /// </summary>
    public List<SimulationSummary> Simulations { get; set; } = new();

    /// <summary>
    /// Comparison metrics between simulations
    /// </summary>
    public Dictionary<string, object> ComparisonMetrics { get; set; } = new();

    /// <summary>
    /// Comparison charts and visualizations
    /// </summary>
    public List<ChartData> ComparisonCharts { get; set; } = new();

    /// <summary>
    /// Recommendations based on comparison
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Simulation summary for comparison
/// </summary>
public class SimulationSummary
{
    /// <summary>
    /// Simulation unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Simulation name
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Protocol used
    /// </summary>
    [Required]
    public string Protocol { get; set; } = string.Empty;

    /// <summary>
    /// Total blocks generated
    /// </summary>
    [Range(0, int.MaxValue)]
    public int TotalBlocks { get; set; }

    /// <summary>
    /// Average block time in milliseconds
    /// </summary>
    [Range(0, double.MaxValue)]
    public double AverageBlockTime { get; set; }

    /// <summary>
    /// When the simulation was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Simulation duration in milliseconds
    /// </summary>
    [Range(0, double.MaxValue)]
    public double DurationMs { get; set; }

    /// <summary>
    /// Key performance indicators
    /// </summary>
    public Dictionary<string, double> KeyMetrics { get; set; } = new();
}

/// <summary>
/// Network topology and performance metrics
/// </summary>
public class NetworkMetrics
{
    /// <summary>
    /// Total number of nodes in the network
    /// </summary>
    [Range(1, int.MaxValue)]
    public int TotalNodes { get; set; }

    /// <summary>
    /// Number of actively participating nodes
    /// </summary>
    [Range(0, int.MaxValue)]
    public int ActiveNodes { get; set; }

    /// <summary>
    /// Network efficiency score (0-1)
    /// </summary>
    [Range(0, 1)]
    public double NetworkEfficiency { get; set; }

    /// <summary>
    /// Total messages passed between nodes
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MessagesPassed { get; set; }

    /// <summary>
    /// Average network latency in milliseconds
    /// </summary>
    [Range(0, double.MaxValue)]
    public double AverageLatency { get; set; }

    /// <summary>
    /// Average time to reach consensus in milliseconds
    /// </summary>
    [Range(0, double.MaxValue)]
    public double ConsensusTime { get; set; }

    /// <summary>
    /// Partition tolerance score (0-1)
    /// </summary>
    [Range(0, 1)]
    public double PartitionTolerance { get; set; }

    /// <summary>
    /// Node connectivity metrics
    /// </summary>
    public Dictionary<string, double> ConnectivityMetrics { get; set; } = new();
}

/// <summary>
/// Data export options and metadata
/// </summary>
public class ExportOptions
{
    /// <summary>
    /// Export format (csv, json, excel)
    /// </summary>
    [Required]
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Include raw data
    /// </summary>
    public bool IncludeRawData { get; set; } = true;

    /// <summary>
    /// Include charts as images
    /// </summary>
    public bool IncludeCharts { get; set; } = false;

    /// <summary>
    /// Include metadata and configuration
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Compression level (0-9)
    /// </summary>
    [Range(0, 9)]
    public int CompressionLevel { get; set; } = 6;

    /// <summary>
    /// Custom fields to include in export
    /// </summary>
    public List<string> CustomFields { get; set; } = new();
}

/// <summary>
/// Export result information
/// </summary>
public class ExportResult
{
    /// <summary>
    /// Download URL for the exported file
    /// </summary>
    [Required]
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    [Range(0, long.MaxValue)]
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Export format used
    /// </summary>
    [Required]
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// When the export was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Export expiration time
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Number of records exported
    /// </summary>
    [Range(0, int.MaxValue)]
    public int RecordCount { get; set; }
}