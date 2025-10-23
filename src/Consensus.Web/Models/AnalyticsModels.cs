using Consensus.Core.Models;

namespace Consensus.Web.Models;

/// <summary>
/// Analytics summary containing key metrics and performance data
/// </summary>
public class AnalyticsSummary
{
    public int TotalSimulations { get; set; }
    public int TotalBlocks { get; set; }
    public int TotalNodes { get; set; }
    public double AverageSimulationDuration { get; set; }
    public double OverallSuccessRate { get; set; }
    public Dictionary<string, AlgorithmPerformanceMetrics> AlgorithmPerformance { get; set; } = new();
    public Dictionary<string, NodeWinnerStats> TopNodes { get; set; } = new();
    public List<TimeSeriesDataPoint> RecentTrends { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Chart data structure for visualizations
/// </summary>
public class ChartData
{
    public string Type { get; set; } = ""; // bar, line, pie, etc.
    public string Title { get; set; } = "";
    public List<string> Labels { get; set; } = new();
    public List<ChartDataset> Datasets { get; set; } = new();
    public ChartOptions? Options { get; set; }
}

/// <summary>
/// Dataset within a chart
/// </summary>
public class ChartDataset
{
    public string Label { get; set; } = "";
    public List<double> Data { get; set; } = new();
    public List<string>? BackgroundColor { get; set; }
    public List<string>? BorderColor { get; set; }
    public string? Fill { get; set; }
    public double? BorderWidth { get; set; }
    public double? Tension { get; set; }
}

/// <summary>
/// Chart configuration options
/// </summary>
public class ChartOptions
{
    public bool Responsive { get; set; } = true;
    public bool MaintainAspectRatio { get; set; } = true;
    public Dictionary<string, object>? Plugins { get; set; }
    public Dictionary<string, object>? Scales { get; set; }
}

/// <summary>
/// Histogram data for distribution analysis
/// </summary>
public class HistogramData
{
    public string Title { get; set; } = "";
    public List<HistogramBin> Bins { get; set; } = new();
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double Mean { get; set; }
    public double StandardDeviation { get; set; }
    public int TotalSamples { get; set; }
}

/// <summary>
/// Individual bin in a histogram
/// </summary>
public class HistogramBin
{
    public double LowerBound { get; set; }
    public double UpperBound { get; set; }
    public int Count { get; set; }
    public double Frequency { get; set; }
    public string Label => $"{LowerBound:F1}-{UpperBound:F1}";
}

/// <summary>
/// Time series data for temporal analysis
/// </summary>
public class TimeSeriesData
{
    public string Title { get; set; } = "";
    public string XAxisLabel { get; set; } = "Time";
    public string YAxisLabel { get; set; } = "Value";
    public List<TimeSeriesDataPoint> DataPoints { get; set; } = new();
    public Dictionary<string, List<TimeSeriesDataPoint>>? MultiSeries { get; set; }
}

/// <summary>
/// Protocol-specific metrics
/// </summary>
public class ProtocolMetrics
{
    public string ProtocolName { get; set; } = "";
    public double AverageBlockTime { get; set; }
    public double AverageConfirmationTime { get; set; }
    public double ThroughputTps { get; set; }
    public double EnergyEfficiency { get; set; }
    public double SecurityScore { get; set; }
    public double ScalabilityScore { get; set; }
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Analytics comparison between algorithms or configurations
/// </summary>
public class AnalyticsComparison
{
    public string ComparisonTitle { get; set; } = "";
    public List<string> ComparedItems { get; set; } = new();
    public Dictionary<string, ComparisonMetric> Metrics { get; set; } = new();
    public string WinnerCategory { get; set; } = "";
    public string WinnerItem { get; set; } = "";
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Individual metric in a comparison
/// </summary>
public class ComparisonMetric
{
    public string MetricName { get; set; } = "";
    public string Unit { get; set; } = "";
    public Dictionary<string, double> Values { get; set; } = new();
    public string BestItem { get; set; } = "";
    public bool HigherIsBetter { get; set; } = true;
}

/// <summary>
/// Network-level metrics for consensus analysis
/// </summary>
public class NetworkMetrics
{
    public double NetworkHashrate { get; set; }
    public double NetworkStakeTotal { get; set; }
    public int ActiveValidators { get; set; }
    public double DecentralizationIndex { get; set; }
    public double FinalityTime { get; set; }
    public double ForkProbability { get; set; }
    public double BandwidthUsage { get; set; }
    public Dictionary<string, double> SecurityMetrics { get; set; } = new();
}

/// <summary>
/// Export options for analytics data
/// </summary>
public class ExportOptions
{
    public string Format { get; set; } = "csv"; // csv, json, pdf, excel
    public string DataType { get; set; } = "summary"; // summary, raw, charts
    public string TimeRange { get; set; } = "all";
    public List<string>? IncludeFields { get; set; }
    public List<string>? ExcludeFields { get; set; }
    public bool IncludeCharts { get; set; } = true;
    public bool IncludeMetadata { get; set; } = true;
}

/// <summary>
/// Result of an export operation
/// </summary>
public class ExportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public byte[]? Data { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}