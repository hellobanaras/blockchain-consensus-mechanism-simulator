using Microsoft.AspNetCore.Mvc;
using Consensus.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Consensus.Web.Controllers;

/// <summary>
/// Controller for analytics dashboard data export functionality
/// </summary>
[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Admin,Operator,Viewer")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsExportService _exportService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsExportService exportService, ILogger<AnalyticsController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Get analytics summary data for dashboard
    /// </summary>
    /// <param name="timeRange">Time range filter</param>
    /// <param name="algorithm">Algorithm filter</param>
    /// <param name="nodeCount">Node count filter</param>
    /// <param name="status">Status filter</param>
    /// <param name="includeNodeStats">Include node statistics</param>
    /// <param name="includeTimeSeriesData">Include time series data</param>
    /// <returns>Analytics summary data</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(AnalyticsData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAnalyticsSummary(
        [FromQuery] string timeRange = "Last 24 Hours",
        [FromQuery] string? algorithm = null,
        [FromQuery] int? nodeCount = null,
        [FromQuery] string? status = null,
        [FromQuery] bool includeNodeStats = true,
        [FromQuery] bool includeTimeSeriesData = true)
    {
        try
        {
            _logger.LogInformation("Getting analytics summary with filters - TimeRange: {TimeRange}, Algorithm: {Algorithm}, NodeCount: {NodeCount}", 
                timeRange, algorithm, nodeCount);

            // Generate mock analytics data for now
            var analyticsData = await GenerateMockAnalyticsData(timeRange, algorithm, nodeCount, status, includeNodeStats, includeTimeSeriesData);
            
            return Ok(analyticsData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analytics summary");
            return StatusCode(500, new { message = "Internal server error retrieving analytics summary" });
        }
    }

    /// <summary>
    /// Export analytics data in specified format
    /// </summary>
    /// <param name="format">Export format (csv, json, excel, pdf)</param>
    /// <param name="dataType">Type of data to export (analytics, performance, nodes, timeseries)</param>
    /// <param name="timeRange">Time range filter</param>
    /// <param name="algorithm">Algorithm filter</param>
    /// <returns>Exported file</returns>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportAnalyticsData(
        [FromQuery] string format = "csv",
        [FromQuery] string dataType = "analytics",
        [FromQuery] string timeRange = "Last 24 Hours",
        [FromQuery] string? algorithm = null)
    {
        try
        {
            var validFormats = new[] { "csv", "json", "excel", "pdf" };
            if (!validFormats.Contains(format.ToLower()))
            {
                return BadRequest(new { message = $"Invalid format '{format}'. Valid formats: {string.Join(", ", validFormats)}" });
            }

            var validDataTypes = new[] { "analytics", "performance", "nodes", "timeseries", "dashboard" };
            if (!validDataTypes.Contains(dataType.ToLower()))
            {
                return BadRequest(new { message = $"Invalid data type '{dataType}'. Valid types: {string.Join(", ", validDataTypes)}" });
            }

            _logger.LogInformation("Exporting analytics data - Format: {Format}, DataType: {DataType}, TimeRange: {TimeRange}", 
                format, dataType, timeRange);

            // Get the data based on type
            var data = await GetExportData(dataType, timeRange, algorithm);
            
            // Export in the requested format
            byte[] exportedData;
            string contentType;
            string fileName;

            switch (format.ToLower())
            {
                case "csv":
                    exportedData = await _exportService.ExportToCsvAsync(data, dataType);
                    contentType = "text/csv";
                    fileName = $"analytics-{dataType}-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
                    break;

                case "json":
                    exportedData = await _exportService.ExportToJsonAsync(data);
                    contentType = "application/json";
                    fileName = $"analytics-{dataType}-{DateTime.Now:yyyyMMdd-HHmmss}.json";
                    break;

                case "excel":
                    exportedData = await _exportService.ExportToExcelAsync(data, dataType);
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    fileName = $"analytics-{dataType}-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
                    break;

                case "pdf":
                    if (dataType == "performance" && data is List<PerformanceMetrics> performanceData)
                    {
                        exportedData = await _exportService.ExportPerformanceReportToPdfAsync(performanceData);
                    }
                    else if (dataType == "dashboard" && data is AnalyticsData dashboardData)
                    {
                        exportedData = await _exportService.CreateDashboardReportAsync(dashboardData);
                    }
                    else
                    {
                        return BadRequest(new { message = "PDF export only supports 'performance' and 'dashboard' data types" });
                    }
                    contentType = "application/pdf";
                    fileName = $"analytics-{dataType}-report-{DateTime.Now:yyyyMMdd-HHmmss}.pdf";
                    break;

                default:
                    return BadRequest(new { message = $"Unsupported format: {format}" });
            }

            return File(exportedData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting analytics data - Format: {Format}, DataType: {DataType}", format, dataType);
            return StatusCode(500, new { message = "Internal server error exporting analytics data" });
        }
    }

    /// <summary>
    /// Export chart data as CSV
    /// </summary>
    /// <param name="chartType">Type of chart (winner, algorithm, performance, histogram, protocol)</param>
    /// <param name="timeRange">Time range filter</param>
    /// <returns>Chart data as CSV file</returns>
    [HttpGet("export/chart/{chartType}")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportChartData(
        [FromRoute] string chartType,
        [FromQuery] string timeRange = "Last 24 Hours")
    {
        try
        {
            var validChartTypes = new[] { "winner", "algorithm", "performance", "histogram", "protocol" };
            if (!validChartTypes.Contains(chartType.ToLower()))
            {
                return BadRequest(new { message = $"Invalid chart type '{chartType}'. Valid types: {string.Join(", ", validChartTypes)}" });
            }

            _logger.LogInformation("Exporting chart data - ChartType: {ChartType}, TimeRange: {TimeRange}", chartType, timeRange);

            // Generate mock chart data
            var chartData = await GenerateMockChartData(chartType, timeRange);
            
            // Export as CSV
            var exportedData = await _exportService.ExportChartDataToCsvAsync(chartData);
            var fileName = $"chart-{chartType}-{DateTime.Now:yyyyMMdd-HHmmss}.csv";

            return File(exportedData, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting chart data - ChartType: {ChartType}", chartType);
            return StatusCode(500, new { message = "Internal server error exporting chart data" });
        }
    }

    /// <summary>
    /// Get real-time statistics for dashboard
    /// </summary>
    /// <returns>Real-time system and simulation statistics</returns>
    [HttpGet("realtime")]
    [ProducesResponseType(typeof(RealTimeStats), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRealTimeStats()
    {
        try
        {
            // Generate mock real-time statistics
            var stats = new RealTimeStats
            {
                ActiveSimulations = Random.Shared.Next(5, 15),
                CurrentBlockRate = Random.Shared.NextDouble() * 10 + 5,
                ActiveNodes = Random.Shared.Next(100, 200),
                SystemLoad = Random.Shared.NextDouble() * 100,
                MemoryUsage = Random.Shared.NextDouble() * 100,
                LastUpdated = DateTime.UtcNow
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving real-time statistics");
            return StatusCode(500, new { message = "Internal server error retrieving real-time statistics" });
        }
    }

    /// <summary>
    /// Generate custom analytics report with selected metrics
    /// </summary>
    /// <param name="request">Custom report request parameters</param>
    /// <returns>Custom analytics report</returns>
    [HttpPost("report/custom")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateCustomReport([FromBody] CustomReportRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { message = "Custom report request is required" });
            }

            _logger.LogInformation("Generating custom analytics report with {MetricCount} metrics", request.SelectedMetrics?.Count ?? 0);

            // Generate analytics data based on selected metrics
            var analyticsData = await GenerateMockAnalyticsData(
                request.TimeRange ?? "Last 24 Hours",
                request.AlgorithmFilter,
                request.NodeCountFilter,
                request.StatusFilter,
                request.SelectedMetrics?.Contains("nodes") ?? true,
                request.SelectedMetrics?.Contains("timeseries") ?? true);

            // Create custom report
            var reportData = await _exportService.CreateDashboardReportAsync(analyticsData, request.Title ?? "Custom Analytics Report");
            var fileName = $"custom-report-{DateTime.Now:yyyyMMdd-HHmmss}.{request.Format?.ToLower() ?? "txt"}";
            var contentType = request.Format?.ToLower() switch
            {
                "pdf" => "application/pdf",
                "csv" => "text/csv",
                "json" => "application/json",
                _ => "text/plain"
            };

            return File(reportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating custom analytics report");
            return StatusCode(500, new { message = "Internal server error generating custom report" });
        }
    }

    #region Private Helper Methods

    private async Task<object> GetExportData(string dataType, string timeRange, string? algorithm)
    {
        return dataType.ToLower() switch
        {
            "analytics" => await GenerateMockAnalyticsData(timeRange, algorithm, null, null, true, true),
            "performance" => await GenerateMockPerformanceMetrics(timeRange, algorithm),
            "nodes" => await GenerateMockNodeStats(timeRange),
            "timeseries" => await GenerateMockTimeSeriesData(timeRange),
            "dashboard" => await GenerateMockAnalyticsData(timeRange, algorithm, null, null, true, true),
            _ => throw new ArgumentException($"Unknown data type: {dataType}")
        };
    }

    private async Task<AnalyticsData> GenerateMockAnalyticsData(string timeRange, string? algorithm, int? nodeCount, string? status, bool includeNodeStats, bool includeTimeSeriesData)
    {
        await Task.Delay(10); // Simulate async operation

        var data = new AnalyticsData
        {
            TotalSimulations = Random.Shared.Next(30, 100),
            TotalBlocks = Random.Shared.Next(1000, 5000),
            TotalNodes = nodeCount ?? Random.Shared.Next(100, 200),
            AverageSimulationDuration = Random.Shared.NextDouble() * 300 + 100
        };

        // Algorithm performance data
        var algorithms = new[] { "ProofOfWork", "ProofOfStake", "PBFT", "Raft", "ProofOfElapsedTime" };
        if (!string.IsNullOrEmpty(algorithm))
        {
            algorithms = new[] { algorithm };
        }

        data.AlgorithmPerformance = algorithms.ToDictionary(
            alg => alg,
            alg => new AlgorithmPerformanceData
            {
                TotalSimulations = Random.Shared.Next(5, 20),
                AverageProcessingTime = Random.Shared.NextDouble() * 3000 + 500,
                AverageConsensusTime = Random.Shared.NextDouble() * 2000 + 300,
                SuccessRate = Random.Shared.NextDouble() * 20 + 80
            });

        // Node statistics
        if (includeNodeStats)
        {
            data.NodeStats = Enumerable.Range(1, 10)
                .ToDictionary(
                    i => $"node-{i:D3}",
                    i => new NodeStatsData
                    {
                        BlocksProduced = Random.Shared.Next(10, 100),
                        WinRate = Random.Shared.NextDouble() * 30 + 5,
                        AverageBlockTime = Random.Shared.NextDouble() * 5 + 1,
                        EfficiencyScore = Random.Shared.NextDouble() * 3 + 7,
                        PerformanceRank = i
                    });
        }

        // Time series data
        if (includeTimeSeriesData)
        {
            var dataPoints = timeRange switch
            {
                "Last Hour" => 60,
                "Last 24 Hours" => 24,
                "Last 7 Days" => 7 * 24,
                "Last 30 Days" => 30,
                _ => 24
            };

            var baseTime = DateTime.UtcNow.AddHours(-dataPoints);
            data.TimeSeriesData = Enumerable.Range(0, dataPoints)
                .Select(i => new TimeSeriesDataPoint
                {
                    Timestamp = baseTime.AddHours(i),
                    AverageProcessingTime = Random.Shared.NextDouble() * 1000 + 500,
                    AverageConsensusTime = Random.Shared.NextDouble() * 800 + 300,
                    ActiveSimulations = Random.Shared.Next(5, 15),
                    ActiveNodes = Random.Shared.Next(80, 150),
                    BlocksCreated = Random.Shared.Next(10, 50)
                })
                .ToList();
        }

        return data;
    }

    private async Task<List<PerformanceMetrics>> GenerateMockPerformanceMetrics(string timeRange, string? algorithm)
    {
        await Task.Delay(10);

        var algorithms = new[] { "ProofOfWork", "ProofOfStake", "PBFT", "Raft", "ProofOfElapsedTime" };
        if (!string.IsNullOrEmpty(algorithm))
        {
            algorithms = new[] { algorithm };
        }

        return algorithms.Select(alg => new PerformanceMetrics
        {
            Algorithm = alg,
            TotalSimulations = Random.Shared.Next(5, 25),
            AverageProcessingTime = Random.Shared.NextDouble() * 3000 + 500,
            AverageConsensusTime = Random.Shared.NextDouble() * 2000 + 300,
            SuccessRate = Random.Shared.NextDouble() * 20 + 80,
            EfficiencyScore = Random.Shared.NextDouble() * 30 + 70
        }).ToList();
    }

    private async Task<Dictionary<string, NodeStatsData>> GenerateMockNodeStats(string timeRange)
    {
        await Task.Delay(10);

        return Enumerable.Range(1, 15)
            .ToDictionary(
                i => $"node-{i:D3}",
                i => new NodeStatsData
                {
                    BlocksProduced = Random.Shared.Next(10, 120),
                    WinRate = Random.Shared.NextDouble() * 25 + 5,
                    AverageBlockTime = Random.Shared.NextDouble() * 4 + 1,
                    EfficiencyScore = Random.Shared.NextDouble() * 3 + 7,
                    PerformanceRank = i
                });
    }

    private async Task<List<TimeSeriesDataPoint>> GenerateMockTimeSeriesData(string timeRange)
    {
        await Task.Delay(10);

        var dataPoints = timeRange switch
        {
            "Last Hour" => 60,
            "Last 24 Hours" => 24,
            "Last 7 Days" => 7 * 24,
            "Last 30 Days" => 30,
            _ => 24
        };

        var baseTime = DateTime.UtcNow.AddHours(-dataPoints);
        return Enumerable.Range(0, dataPoints)
            .Select(i => new TimeSeriesDataPoint
            {
                Timestamp = baseTime.AddHours(i),
                AverageProcessingTime = Random.Shared.NextDouble() * 1000 + 500,
                AverageConsensusTime = Random.Shared.NextDouble() * 800 + 300,
                ActiveSimulations = Random.Shared.Next(5, 15),
                ActiveNodes = Random.Shared.Next(80, 150),
                BlocksCreated = Random.Shared.Next(10, 50)
            })
            .ToList();
    }

    private async Task<ChartData> GenerateMockChartData(string chartType, string timeRange)
    {
        await Task.Delay(10);

        return chartType.ToLower() switch
        {
            "winner" => new ChartData
            {
                Type = "bar",
                Title = "Block Winner Distribution",
                Labels = new List<string> { "node-001", "node-002", "node-003", "node-004", "node-005" },
                Datasets = new List<ChartDataset>
                {
                    new ChartDataset
                    {
                        Label = "Blocks Won",
                        Data = new List<double> { 45, 42, 38, 35, 32 }
                    }
                }
            },
            "algorithm" => new ChartData
            {
                Type = "pie",
                Title = "Algorithm Distribution",
                Labels = new List<string> { "PoW", "PoS", "PBFT", "Raft" },
                Datasets = new List<ChartDataset>
                {
                    new ChartDataset
                    {
                        Label = "Simulations",
                        Data = new List<double> { 15, 12, 8, 12 }
                    }
                }
            },
            "performance" => new ChartData
            {
                Type = "line",
                Title = "Performance Trends",
                Labels = Enumerable.Range(0, 24).Select(i => $"{i:D2}:00").ToList(),
                Datasets = new List<ChartDataset>
                {
                    new ChartDataset
                    {
                        Label = "Processing Time (ms)",
                        Data = Enumerable.Range(0, 24).Select(_ => Random.Shared.NextDouble() * 1000 + 500).ToList()
                    }
                }
            },
            _ => new ChartData
            {
                Type = "bar",
                Title = $"{chartType} Chart",
                Labels = new List<string> { "Data 1", "Data 2", "Data 3" },
                Datasets = new List<ChartDataset>
                {
                    new ChartDataset
                    {
                        Label = "Values",
                        Data = new List<double> { 10, 20, 30 }
                    }
                }
            }
        };
    }

    #endregion
}

/// <summary>
/// Real-time statistics model
/// </summary>
public class RealTimeStats
{
    public int ActiveSimulations { get; set; }
    public double CurrentBlockRate { get; set; }
    public int ActiveNodes { get; set; }
    public double SystemLoad { get; set; }
    public double MemoryUsage { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Custom report request model
/// </summary>
public class CustomReportRequest
{
    public string? Title { get; set; }
    public string? Format { get; set; } = "pdf";
    public string? TimeRange { get; set; }
    public string? AlgorithmFilter { get; set; }
    public int? NodeCountFilter { get; set; }
    public string? StatusFilter { get; set; }
    public List<string>? SelectedMetrics { get; set; }
    public bool IncludeCharts { get; set; } = true;
    public bool IncludeSummary { get; set; } = true;
    public bool IncludeRecommendations { get; set; } = true;
}