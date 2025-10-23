using System.ComponentModel.DataAnnotations;

namespace Consensus.Web.Models.Api;

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