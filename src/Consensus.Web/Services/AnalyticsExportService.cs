using System.Text;
using System.Text.Json;

namespace Consensus.Web.Services;

/// <summary>
/// Implementation of analytics data export functionality
/// </summary>
public class AnalyticsExportService : IAnalyticsExportService
{
    private readonly ILogger<AnalyticsExportService> _logger;

    public AnalyticsExportService(ILogger<AnalyticsExportService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Export analytics data to CSV format
    /// </summary>
    public async Task<byte[]> ExportToCsvAsync(object data, string dataType)
    {
        try
        {
            var csvContent = dataType.ToLower() switch
            {
                "analytics" when data is AnalyticsData analyticsData => ConvertAnalyticsDataToCsv(analyticsData),
                "performance" when data is List<PerformanceMetrics> performanceData => ConvertPerformanceMetricsToCsv(performanceData),
                "nodes" when data is Dictionary<string, NodeStatsData> nodeData => ConvertNodeStatsToCsv(nodeData),
                "timeseries" when data is List<TimeSeriesDataPoint> timeSeriesData => ConvertTimeSeriesDataToCsv(timeSeriesData),
                _ => throw new ArgumentException($"Unsupported data type for CSV export: {dataType}")
            };

            return Encoding.UTF8.GetBytes(csvContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data to CSV for type {DataType}", dataType);
            throw;
        }
    }

    /// <summary>
    /// Export analytics data to JSON format
    /// </summary>
    public async Task<byte[]> ExportToJsonAsync(object data, bool formatted = true)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = formatted,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonString = JsonSerializer.Serialize(data, options);
            return Encoding.UTF8.GetBytes(jsonString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data to JSON");
            throw;
        }
    }

    /// <summary>
    /// Export analytics data to Excel format (CSV-based for now)
    /// </summary>
    public async Task<byte[]> ExportToExcelAsync(object data, string dataType)
    {
        // For now, use CSV format for Excel export
        // In the future, could implement proper Excel format using libraries like EPPlus
        return await ExportToCsvAsync(data, dataType);
    }

    /// <summary>
    /// Export chart data to CSV format
    /// </summary>
    public async Task<byte[]> ExportChartDataToCsvAsync(ChartData chartData)
    {
        try
        {
            var csv = new StringBuilder();
            
            // Add header with chart info
            csv.AppendLine($"Chart Type,{chartData.Type}");
            csv.AppendLine($"Chart Title,{EscapeCsvValue(chartData.Title)}");
            csv.AppendLine();

            // Add data headers
            var headers = new List<string> { "Label" };
            headers.AddRange(chartData.Datasets.Select(ds => EscapeCsvValue(ds.Label)));
            csv.AppendLine(string.Join(",", headers));

            // Add data rows
            for (int i = 0; i < chartData.Labels.Count; i++)
            {
                var row = new List<string> { EscapeCsvValue(chartData.Labels[i]) };
                foreach (var dataset in chartData.Datasets)
                {
                    var value = i < dataset.Data.Count ? dataset.Data[i].ToString() : "0";
                    row.Add(value);
                }
                csv.AppendLine(string.Join(",", row));
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting chart data to CSV");
            throw;
        }
    }

    /// <summary>
    /// Export performance metrics report to PDF format
    /// </summary>
    public async Task<byte[]> ExportPerformanceReportToPdfAsync(List<PerformanceMetrics> performanceData)
    {
        try
        {
            // For now, create a text-based report
            // In the future, could implement proper PDF generation using libraries like iTextSharp
            var report = new StringBuilder();
            
            report.AppendLine("CONSENSUS ALGORITHM PERFORMANCE REPORT");
            report.AppendLine("=" + new string('=', 40));
            report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            report.AppendLine();

            report.AppendLine("EXECUTIVE SUMMARY");
            report.AppendLine("-" + new string('-', 16));
            report.AppendLine($"Total Algorithms Analyzed: {performanceData.Count}");
            report.AppendLine($"Average Success Rate: {performanceData.Average(p => p.SuccessRate):F2}%");
            report.AppendLine($"Best Performing Algorithm: {performanceData.OrderByDescending(p => p.EfficiencyScore).First().Algorithm}");
            report.AppendLine();

            report.AppendLine("DETAILED ANALYSIS");
            report.AppendLine("-" + new string('-', 17));
            
            foreach (var metrics in performanceData.OrderByDescending(p => p.EfficiencyScore))
            {
                report.AppendLine($"\nAlgorithm: {metrics.Algorithm}");
                report.AppendLine($"  Total Simulations: {metrics.TotalSimulations}");
                report.AppendLine($"  Avg Processing Time: {metrics.AverageProcessingTime:F2} ms");
                report.AppendLine($"  Avg Consensus Time: {metrics.AverageConsensusTime:F2} ms");
                report.AppendLine($"  Success Rate: {metrics.SuccessRate:F2}%");
                report.AppendLine($"  Efficiency Score: {metrics.EfficiencyScore:F2}");
            }

            report.AppendLine("\nRECOMMendATIONS");
            report.AppendLine("-" + new string('-', 15));
            var bestAlgorithm = performanceData.OrderByDescending(p => p.EfficiencyScore).First();
            report.AppendLine($"• Consider using {bestAlgorithm.Algorithm} for optimal performance");
            report.AppendLine($"• Focus on algorithms with success rates above {performanceData.Average(p => p.SuccessRate):F0}%");
            report.AppendLine($"• Monitor processing times to maintain sub-{performanceData.Average(p => p.AverageProcessingTime):F0}ms performance");

            return Encoding.UTF8.GetBytes(report.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating performance report PDF");
            throw;
        }
    }

    /// <summary>
    /// Create comprehensive dashboard report
    /// </summary>
    public async Task<byte[]> CreateDashboardReportAsync(AnalyticsData analyticsData, string title = "Analytics Dashboard Report")
    {
        try
        {
            var report = new StringBuilder();
            
            report.AppendLine(title.ToUpper());
            report.AppendLine("=" + new string('=', title.Length));
            report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            report.AppendLine();

            // Summary Statistics
            report.AppendLine("SUMMARY STATISTICS");
            report.AppendLine("-" + new string('-', 18));
            report.AppendLine($"Total Simulations: {analyticsData.TotalSimulations:N0}");
            report.AppendLine($"Total Blocks: {analyticsData.TotalBlocks:N0}");
            report.AppendLine($"Total Nodes: {analyticsData.TotalNodes:N0}");
            report.AppendLine($"Avg Simulation Duration: {analyticsData.AverageSimulationDuration:F2} seconds");
            report.AppendLine();

            // Algorithm Performance
            if (analyticsData.AlgorithmPerformance?.Any() == true)
            {
                report.AppendLine("ALGORITHM PERFORMANCE");
                report.AppendLine("-" + new string('-', 20));
                foreach (var alg in analyticsData.AlgorithmPerformance.OrderByDescending(kvp => kvp.Value.SuccessRate))
                {
                    var perf = alg.Value;
                    report.AppendLine($"\n{alg.Key}:");
                    report.AppendLine($"  Simulations: {perf.TotalSimulations}");
                    report.AppendLine($"  Avg Processing Time: {perf.AverageProcessingTime:F2} ms");
                    report.AppendLine($"  Avg Consensus Time: {perf.AverageConsensusTime:F2} ms");
                    report.AppendLine($"  Success Rate: {perf.SuccessRate:F2}%");
                }
                report.AppendLine();
            }

            // Top Performing Nodes
            if (analyticsData.NodeStats?.Any() == true)
            {
                report.AppendLine("TOP PERFORMING NODES");
                report.AppendLine("-" + new string('-', 20));
                var topNodes = analyticsData.NodeStats
                    .OrderByDescending(kvp => kvp.Value.EfficiencyScore)
                    .Take(5);
                
                foreach (var node in topNodes)
                {
                    var stats = node.Value;
                    report.AppendLine($"{node.Key}: {stats.BlocksProduced} blocks, {stats.WinRate:F1}% win rate, {stats.EfficiencyScore:F2} efficiency");
                }
                report.AppendLine();
            }

            // Recent Trends
            if (analyticsData.TimeSeriesData?.Any() == true)
            {
                report.AppendLine("RECENT TRENDS");
                report.AppendLine("-" + new string('-', 13));
                var recent = analyticsData.TimeSeriesData.TakeLast(24);
                report.AppendLine($"Avg Processing Time (last 24h): {recent.Average(t => t.AverageProcessingTime):F2} ms");
                report.AppendLine($"Avg Consensus Time (last 24h): {recent.Average(t => t.AverageConsensusTime):F2} ms");
                report.AppendLine($"Avg Active Simulations: {recent.Average(t => t.ActiveSimulations):F1}");
                report.AppendLine($"Avg Active Nodes: {recent.Average(t => t.ActiveNodes):F0}");
                report.AppendLine();
            }

            report.AppendLine("END OF REPORT");

            return Encoding.UTF8.GetBytes(report.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dashboard report");
            throw;
        }
    }

    #region Private Helper Methods

    private string ConvertAnalyticsDataToCsv(AnalyticsData data)
    {
        var csv = new StringBuilder();
        
        // Summary section
        csv.AppendLine("ANALYTICS SUMMARY");
        csv.AppendLine("Metric,Value");
        csv.AppendLine($"Total Simulations,{data.TotalSimulations}");
        csv.AppendLine($"Total Blocks,{data.TotalBlocks}");
        csv.AppendLine($"Total Nodes,{data.TotalNodes}");
        csv.AppendLine($"Average Simulation Duration,{data.AverageSimulationDuration}");
        csv.AppendLine();

        // Algorithm performance section
        if (data.AlgorithmPerformance?.Any() == true)
        {
            csv.AppendLine("ALGORITHM PERFORMANCE");
            csv.AppendLine("Algorithm,Total Simulations,Avg Processing Time,Avg Consensus Time,Success Rate");
            foreach (var alg in data.AlgorithmPerformance)
            {
                var perf = alg.Value;
                csv.AppendLine($"{alg.Key},{perf.TotalSimulations},{perf.AverageProcessingTime},{perf.AverageConsensusTime},{perf.SuccessRate}");
            }
            csv.AppendLine();
        }

        return csv.ToString();
    }

    private string ConvertPerformanceMetricsToCsv(List<PerformanceMetrics> metrics)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Algorithm,Total Simulations,Avg Processing Time,Avg Consensus Time,Success Rate,Efficiency Score");
        
        foreach (var metric in metrics)
        {
            csv.AppendLine($"{metric.Algorithm},{metric.TotalSimulations},{metric.AverageProcessingTime},{metric.AverageConsensusTime},{metric.SuccessRate},{metric.EfficiencyScore}");
        }

        return csv.ToString();
    }

    private string ConvertNodeStatsToCsv(Dictionary<string, NodeStatsData> nodeStats)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Node ID,Blocks Produced,Win Rate,Avg Block Time,Efficiency Score,Performance Rank");
        
        foreach (var node in nodeStats)
        {
            var stats = node.Value;
            csv.AppendLine($"{node.Key},{stats.BlocksProduced},{stats.WinRate},{stats.AverageBlockTime},{stats.EfficiencyScore},{stats.PerformanceRank}");
        }

        return csv.ToString();
    }

    private string ConvertTimeSeriesDataToCsv(List<TimeSeriesDataPoint> timeSeriesData)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Timestamp,Avg Processing Time,Avg Consensus Time,Active Simulations,Active Nodes,Blocks Created");
        
        foreach (var point in timeSeriesData)
        {
            csv.AppendLine($"{point.Timestamp:yyyy-MM-dd HH:mm:ss},{point.AverageProcessingTime},{point.AverageConsensusTime},{point.ActiveSimulations},{point.ActiveNodes},{point.BlocksCreated}");
        }

        return csv.ToString();
    }

    private string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    #endregion
}

// Supporting data models
public class AnalyticsData
{
    public int TotalSimulations { get; set; }
    public int TotalBlocks { get; set; }
    public int TotalNodes { get; set; }
    public double AverageSimulationDuration { get; set; }
    public Dictionary<string, AlgorithmPerformanceData>? AlgorithmPerformance { get; set; }
    public Dictionary<string, NodeStatsData>? NodeStats { get; set; }
    public List<TimeSeriesDataPoint>? TimeSeriesData { get; set; }
}

public class AlgorithmPerformanceData
{
    public int TotalSimulations { get; set; }
    public double AverageProcessingTime { get; set; }
    public double AverageConsensusTime { get; set; }
    public double SuccessRate { get; set; }
}

public class NodeStatsData
{
    public int BlocksProduced { get; set; }
    public double WinRate { get; set; }
    public double AverageBlockTime { get; set; }
    public double EfficiencyScore { get; set; }
    public int PerformanceRank { get; set; }
}

public class TimeSeriesDataPoint
{
    public DateTime Timestamp { get; set; }
    public double AverageProcessingTime { get; set; }
    public double AverageConsensusTime { get; set; }
    public int ActiveSimulations { get; set; }
    public int ActiveNodes { get; set; }
    public int BlocksCreated { get; set; }
}

public class PerformanceMetrics
{
    public string Algorithm { get; set; } = "";
    public int TotalSimulations { get; set; }
    public double AverageProcessingTime { get; set; }
    public double AverageConsensusTime { get; set; }
    public double SuccessRate { get; set; }
    public double EfficiencyScore { get; set; }
}

public class ChartData
{
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public List<string> Labels { get; set; } = new();
    public List<ChartDataset> Datasets { get; set; } = new();
}

public class ChartDataset
{
    public string Label { get; set; } = "";
    public List<double> Data { get; set; } = new();
}