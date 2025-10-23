using Microsoft.Extensions.Logging;
using Consensus.Core.Models;
using Consensus.Core.Enums;
using System.Text.Json;
using System.Text;
using System.Globalization;

namespace Consensus.Core.Services;

/// <summary>
/// Service for exporting simulation results in various formats
/// </summary>
public class SimulationResultsExportService : ISimulationResultsExportService
{
    private readonly ILogger<SimulationResultsExportService> _logger;
    private readonly ISimulationMetricsService _metricsService;

    public SimulationResultsExportService(
        ILogger<SimulationResultsExportService> logger,
        ISimulationMetricsService metricsService)
    {
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task<ExportResult> ExportSimulationResultsAsync(SimulationExportRequest request)
    {
        try
        {
            _logger.LogInformation("Exporting simulation {SimulationId} results in {Format} format", 
                request.SimulationId, request.Format);

            // Get simulation data
            var summary = await _metricsService.GenerateSimulationSummaryAsync(request.SimulationId);
            var roundMetrics = await _metricsService.GetRoundMetricsAsync(request.SimulationId);
            var nodeMetrics = await _metricsService.GetNodeMetricsAsync(request.SimulationId);

            var exportData = new ExportableSimulationResults
            {
                Summary = summary,
                RoundData = roundMetrics,
                NodeData = nodeMetrics,
                EventLog = summary.ConsensusEvents,
                Metadata = CreateMetadata(request),
                ExportFormat = request.Format.ToString(),
                ExportedBy = request.ExportedBy
            };

            var exportResult = request.Format switch
            {
                ExportFormat.JSON => await ExportAsJsonAsync(exportData, request),
                ExportFormat.CSV => await ExportAsCsvAsync(exportData, request),
                ExportFormat.Excel => await ExportAsExcelAsync(exportData, request),
                ExportFormat.PDF => await ExportAsPdfAsync(exportData, request),
                _ => throw new ArgumentException($"Unsupported export format: {request.Format}")
            };

            _logger.LogInformation("Successfully exported simulation {SimulationId} results", request.SimulationId);
            return exportResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export simulation {SimulationId} results", request.SimulationId);
            throw;
        }
    }

    public async Task<MemoryStream> GenerateSimulationReportAsync(Guid simulationId, ReportFormat format)
    {
        try
        {
            _logger.LogInformation("Generating {Format} report for simulation {SimulationId}", format, simulationId);

            var summary = await _metricsService.GenerateSimulationSummaryAsync(simulationId);
            var roundMetrics = await _metricsService.GetRoundMetricsAsync(simulationId);

            return format switch
            {
                ReportFormat.SummaryReport => await GenerateSummaryReportAsync(summary),
                ReportFormat.DetailedReport => await GenerateDetailedReportAsync(summary, roundMetrics),
                ReportFormat.PerformanceReport => await GeneratePerformanceReportAsync(summary),
                ReportFormat.ComparisonReport => await GenerateComparisonReportAsync(summary),
                _ => throw new ArgumentException($"Unsupported report format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate report for simulation {SimulationId}", simulationId);
            throw;
        }
    }

    public async Task<List<MetricsSummary>> GetMetricsSummariesAsync(List<Guid> simulationIds)
    {
        try
        {
            var summaries = new List<MetricsSummary>();

            foreach (var simulationId in simulationIds)
            {
                try
                {
                    var summary = await _metricsService.GenerateSimulationSummaryAsync(simulationId);
                    summaries.Add(new MetricsSummary
                    {
                        SimulationId = simulationId,
                        ConsensusAlgorithm = summary.ConsensusAlgorithm,
                        Duration = summary.Duration,
                        TotalRounds = summary.TotalRounds,
                        SuccessRate = summary.SuccessRate,
                        ThroughputTps = summary.ThroughputTps,
                        ConsensusEfficiency = summary.ConsensusEfficiency,
                        NodeCount = summary.NodeCount,
                        TotalBlocks = summary.TotalBlocks,
                        AverageBlockTime = summary.AverageBlockTime,
                        PerformanceGrade = summary.PerformanceGrade
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get summary for simulation {SimulationId}", simulationId);
                }
            }

            return summaries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics summaries");
            throw;
        }
    }

    #region Export Format Implementations

    private async Task<ExportResult> ExportAsJsonAsync(ExportableSimulationResults data, SimulationExportRequest request)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = request.IncludePrettyFormatting,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, options);
        var stream = new MemoryStream(jsonBytes);

        return new ExportResult
        {
            Success = true,
            Format = ExportFormat.JSON,
            Data = stream,
            FileName = $"simulation_{request.SimulationId}_results.json",
            ContentType = "application/json",
            Size = jsonBytes.Length
        };
    }

    private async Task<ExportResult> ExportAsCsvAsync(ExportableSimulationResults data, SimulationExportRequest request)
    {
        var csv = new StringBuilder();

        // Summary section
        csv.AppendLine("Simulation Summary");
        csv.AppendLine($"Simulation ID,{data.Summary.SimulationId}");
        csv.AppendLine($"Algorithm,{data.Summary.ConsensusAlgorithm}");
        csv.AppendLine($"Duration,{data.Summary.Duration}");
        csv.AppendLine($"Success Rate,{data.Summary.SuccessRate:F2}%");
        csv.AppendLine($"Throughput TPS,{data.Summary.ThroughputTps:F2}");
        csv.AppendLine($"Consensus Efficiency,{data.Summary.ConsensusEfficiency:F2}%");
        csv.AppendLine();

        // Round metrics
        if (request.IncludeRoundData && data.RoundData.Any())
        {
            csv.AppendLine("Round Metrics");
            csv.AppendLine("Round,Duration(ms),Blocks Proposed,Blocks Accepted,Transactions,Success,Failure Reason");
            
            foreach (var round in data.RoundData)
            {
                csv.AppendLine($"{round.RoundNumber},{round.Duration.TotalMilliseconds:F0}," +
                             $"{round.BlocksProposed},{round.BlocksAccepted},{round.TransactionsProcessed}," +
                             $"{round.Success},{round.FailureReason ?? ""}");
            }
            csv.AppendLine();
        }

        // Node performance
        if (request.IncludeNodeData && data.Summary.NodePerformance.Any())
        {
            csv.AppendLine("Node Performance");
            csv.AppendLine("Node ID,Node Name,Blocks Proposed,Blocks Accepted,Success Rate,Uptime %,Final Stake");
            
            foreach (var node in data.Summary.NodePerformance.Values)
            {
                csv.AppendLine($"{node.NodeId},{node.NodeName},{node.TotalBlocksProposed}," +
                             $"{node.TotalBlocksAccepted},{node.BlockSuccessRate:F2}," +
                             $"{node.UptimePercentage:F2},{node.FinalStake}");
            }
        }

        var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
        var stream = new MemoryStream(csvBytes);

        return new ExportResult
        {
            Success = true,
            Format = ExportFormat.CSV,
            Data = stream,
            FileName = $"simulation_{request.SimulationId}_results.csv",
            ContentType = "text/csv",
            Size = csvBytes.Length
        };
    }

    private async Task<ExportResult> ExportAsExcelAsync(ExportableSimulationResults data, SimulationExportRequest request)
    {
        // For now, return CSV format with Excel MIME type
        // In a full implementation, you would use a library like EPPlus or ClosedXML
        var csvResult = await ExportAsCsvAsync(data, request);
        
        return csvResult with
        {
            Format = ExportFormat.Excel,
            FileName = $"simulation_{request.SimulationId}_results.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    private async Task<ExportResult> ExportAsPdfAsync(ExportableSimulationResults data, SimulationExportRequest request)
    {
        // For now, return a simple text-based PDF content
        // In a full implementation, you would use a library like iTextSharp or PdfSharp
        var report = await GenerateTextReportAsync(data);
        var reportBytes = Encoding.UTF8.GetBytes(report);
        var stream = new MemoryStream(reportBytes);

        return new ExportResult
        {
            Success = true,
            Format = ExportFormat.PDF,
            Data = stream,
            FileName = $"simulation_{request.SimulationId}_report.txt", // Would be .pdf in real implementation
            ContentType = "text/plain", // Would be application/pdf in real implementation
            Size = reportBytes.Length
        };
    }

    #endregion

    #region Report Generation

    private async Task<MemoryStream> GenerateSummaryReportAsync(SimulationSummary summary)
    {
        var report = new StringBuilder();
        report.AppendLine("=== BLOCKCHAIN CONSENSUS SIMULATION SUMMARY ===");
        report.AppendLine();
        report.AppendLine($"Simulation ID: {summary.SimulationId}");
        report.AppendLine($"Algorithm: {summary.ConsensusAlgorithm}");
        report.AppendLine($"Duration: {summary.Duration}");
        report.AppendLine($"Performance Grade: {summary.PerformanceGrade}");
        report.AppendLine();
        report.AppendLine("KEY METRICS:");
        report.AppendLine($"  • Total Rounds: {summary.TotalRounds}");
        report.AppendLine($"  • Success Rate: {summary.SuccessRate:F1}%");
        report.AppendLine($"  • Total Blocks: {summary.TotalBlocks}");
        report.AppendLine($"  • Throughput: {summary.ThroughputTps:F2} TPS");
        report.AppendLine($"  • Consensus Efficiency: {summary.ConsensusEfficiency:F1}%");
        report.AppendLine($"  • Average Block Time: {summary.AverageBlockTime.TotalSeconds:F2}s");
        
        var bytes = Encoding.UTF8.GetBytes(report.ToString());
        return new MemoryStream(bytes);
    }

    private async Task<MemoryStream> GenerateDetailedReportAsync(SimulationSummary summary, List<RoundMetrics> rounds)
    {
        var report = new StringBuilder();
        report.AppendLine("=== DETAILED SIMULATION REPORT ===");
        report.AppendLine();
        
        // Include summary
        var summaryReport = await GenerateSummaryReportAsync(summary);
        summaryReport.Position = 0;
        using var reader = new StreamReader(summaryReport);
        report.AppendLine(await reader.ReadToEndAsync());
        
        report.AppendLine();
        report.AppendLine("ROUND-BY-ROUND ANALYSIS:");
        
        foreach (var round in rounds.Take(20)) // Limit to first 20 rounds for readability
        {
            report.AppendLine($"Round {round.RoundNumber}: " +
                            $"{round.Duration.TotalMilliseconds:F0}ms, " +
                            $"{round.BlocksAccepted} blocks, " +
                            $"{round.TransactionsProcessed} txns, " +
                            $"{(round.Success ? "SUCCESS" : "FAILED")}");
        }
        
        if (rounds.Count > 20)
        {
            report.AppendLine($"... and {rounds.Count - 20} more rounds");
        }
        
        var bytes = Encoding.UTF8.GetBytes(report.ToString());
        return new MemoryStream(bytes);
    }

    private async Task<MemoryStream> GeneratePerformanceReportAsync(SimulationSummary summary)
    {
        var report = new StringBuilder();
        report.AppendLine("=== PERFORMANCE ANALYSIS REPORT ===");
        report.AppendLine();
        report.AppendLine($"Algorithm: {summary.ConsensusAlgorithm}");
        report.AppendLine($"Overall Grade: {summary.PerformanceGrade}");
        report.AppendLine();
        
        report.AppendLine("PERFORMANCE BREAKDOWN:");
        report.AppendLine($"  Consensus Efficiency: {summary.ConsensusEfficiency:F1}% " +
                         $"({GetPerformanceRating(summary.ConsensusEfficiency)})");
        report.AppendLine($"  Throughput: {summary.ThroughputTps:F2} TPS " +
                         $"({GetThroughputRating(summary.ThroughputTps)})");
        report.AppendLine($"  Network Latency: {summary.NetworkLatency.TotalMilliseconds:F0}ms " +
                         $"({GetLatencyRating(summary.NetworkLatency)})");
        
        report.AppendLine();
        report.AppendLine("NODE PERFORMANCE:");
        foreach (var node in summary.NodePerformance.Values.Take(10))
        {
            report.AppendLine($"  {node.NodeName}: {node.PerformanceRating} " +
                            $"({node.UptimePercentage:F1}% uptime, " +
                            $"{node.BlockSuccessRate:F1}% block success)");
        }
        
        var bytes = Encoding.UTF8.GetBytes(report.ToString());
        return new MemoryStream(bytes);
    }

    private async Task<MemoryStream> GenerateComparisonReportAsync(SimulationSummary summary)
    {
        var report = new StringBuilder();
        report.AppendLine("=== CONSENSUS ALGORITHM COMPARISON ===");
        report.AppendLine();
        report.AppendLine($"Current Algorithm: {summary.ConsensusAlgorithm}");
        report.AppendLine();
        
        // Add algorithm-specific analysis
        report.AppendLine("ALGORITHM CHARACTERISTICS:");
        switch (summary.ConsensusAlgorithm)
        {
            case ConsensusAlgorithm.ProofOfWork:
                report.AppendLine("  • Energy-intensive but highly secure");
                report.AppendLine("  • Slower but deterministic finality");
                break;
            case ConsensusAlgorithm.ProofOfStake:
                report.AppendLine("  • Energy-efficient with economic security");
                report.AppendLine("  • Faster finality with staking incentives");
                break;
            case ConsensusAlgorithm.PracticalByzantineFaultTolerance:
                report.AppendLine("  • Immediate finality with Byzantine fault tolerance");
                report.AppendLine("  • Optimal for permissioned networks");
                break;
            case ConsensusAlgorithm.ProofOfElapsedTime:
                report.AppendLine("  • Fair leader selection with trusted execution");
                report.AppendLine("  • Low energy consumption");
                break;
        }
        
        var bytes = Encoding.UTF8.GetBytes(report.ToString());
        return new MemoryStream(bytes);
    }

    #endregion

    #region Helper Methods

    private Dictionary<string, object> CreateMetadata(SimulationExportRequest request)
    {
        return new Dictionary<string, object>
        {
            ["exportVersion"] = "1.0",
            ["exportedAt"] = DateTime.UtcNow,
            ["exportedBy"] = request.ExportedBy,
            ["includeRoundData"] = request.IncludeRoundData,
            ["includeNodeData"] = request.IncludeNodeData,
            ["includeEventLog"] = request.IncludeEventLog,
            ["format"] = request.Format.ToString()
        };
    }

    private async Task<string> GenerateTextReportAsync(ExportableSimulationResults data)
    {
        var report = new StringBuilder();
        report.AppendLine("BLOCKCHAIN CONSENSUS SIMULATION REPORT");
        report.AppendLine("=====================================");
        report.AppendLine();
        report.AppendLine($"Simulation: {data.Summary.SimulationId}");
        report.AppendLine($"Algorithm: {data.Summary.ConsensusAlgorithm}");
        report.AppendLine($"Generated: {data.ExportedAt:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine();
        report.AppendLine($"Duration: {data.Summary.Duration}");
        report.AppendLine($"Success Rate: {data.Summary.SuccessRate:F1}%");
        report.AppendLine($"Throughput: {data.Summary.ThroughputTps:F2} TPS");
        report.AppendLine($"Performance Grade: {data.Summary.PerformanceGrade}");
        
        return report.ToString();
    }

    private string GetPerformanceRating(double efficiency)
    {
        return efficiency switch
        {
            >= 95 => "Excellent",
            >= 85 => "Good",
            >= 75 => "Average",
            >= 60 => "Poor",
            _ => "Critical"
        };
    }

    private string GetThroughputRating(double tps)
    {
        return tps switch
        {
            >= 1000 => "Very High",
            >= 100 => "High",
            >= 10 => "Moderate",
            >= 1 => "Low",
            _ => "Very Low"
        };
    }

    private string GetLatencyRating(TimeSpan latency)
    {
        return latency.TotalMilliseconds switch
        {
            < 10 => "Excellent",
            < 50 => "Good",
            < 100 => "Average",
            < 500 => "Poor",
            _ => "Critical"
        };
    }

    #endregion
}

#region Supporting Models and Enums

public enum ExportFormat
{
    JSON,
    CSV,
    Excel,
    PDF
}

public enum ReportFormat
{
    SummaryReport,
    DetailedReport,
    PerformanceReport,
    ComparisonReport
}

public record SimulationExportRequest
{
    public required Guid SimulationId { get; init; }
    public required ExportFormat Format { get; init; }
    public bool IncludeRoundData { get; init; } = true;
    public bool IncludeNodeData { get; init; } = true;
    public bool IncludeEventLog { get; init; } = true;
    public bool IncludePrettyFormatting { get; init; } = true;
    public string ExportedBy { get; init; } = "System";
}

public record ExportResult
{
    public required bool Success { get; init; }
    public required ExportFormat Format { get; init; }
    public required MemoryStream Data { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long Size { get; init; }
    public string? ErrorMessage { get; init; }
}

public record MetricsSummary
{
    public required Guid SimulationId { get; init; }
    public required ConsensusAlgorithm ConsensusAlgorithm { get; init; }
    public required TimeSpan Duration { get; init; }
    public required int TotalRounds { get; init; }
    public required double SuccessRate { get; init; }
    public required double ThroughputTps { get; init; }
    public required double ConsensusEfficiency { get; init; }
    public required int NodeCount { get; init; }
    public required int TotalBlocks { get; init; }
    public required TimeSpan AverageBlockTime { get; init; }
    public required string PerformanceGrade { get; init; }
}

#endregion