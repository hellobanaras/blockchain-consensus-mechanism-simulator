using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Consensus.Core.Services;
using Consensus.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace Consensus.Api.Controllers;

/// <summary>
/// API controller for analytics and metrics operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "Admin,Operator,Viewer")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive analytics summary
    /// </summary>
    /// <param name="request">Analytics request with filtering options</param>
    /// <returns>Analytics summary with metrics and distributions</returns>
    [HttpPost("summary")]
    [ProducesResponseType(typeof(AnalyticsSummary), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<AnalyticsSummary>> GetAnalyticsSummary([FromBody] AnalyticsRequest? request = null)
    {
        try
        {
            _logger.LogInformation("Getting analytics summary with request: {@Request}", request);
            
            var summary = await _analyticsService.GenerateAnalyticsSummaryAsync(request);
            return Ok(summary);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid analytics request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating analytics summary");
            return StatusCode(500, new { error = "Internal server error while generating analytics summary" });
        }
    }

    /// <summary>
    /// Get analytics summary with query parameters (simplified version)
    /// </summary>
    /// <param name="startDate">Start date for analytics period (ISO 8601 format)</param>
    /// <param name="endDate">End date for analytics period (ISO 8601 format)</param>
    /// <param name="includeNodeStats">Include detailed node statistics</param>
    /// <param name="includeTimeSeries">Include time-series data</param>
    /// <returns>Analytics summary</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(AnalyticsSummary), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<AnalyticsSummary>> GetAnalyticsSummaryQuery(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool includeNodeStats = true,
        [FromQuery] bool includeTimeSeries = true)
    {
        try
        {
            var request = new AnalyticsRequest
            {
                StartDate = startDate,
                EndDate = endDate,
                IncludeNodeStats = includeNodeStats,
                IncludeTimeSeriesData = includeTimeSeries
            };

            var summary = await _analyticsService.GenerateAnalyticsSummaryAsync(request);
            return Ok(summary);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid analytics query parameters");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating analytics summary from query");
            return StatusCode(500, new { error = "Internal server error while generating analytics summary" });
        }
    }

    /// <summary>
    /// Get winner distribution for a specific simulation
    /// </summary>
    /// <param name="simulationId">Simulation ID</param>
    /// <returns>Node winner distribution statistics</returns>
    [HttpGet("simulations/{simulationId:guid}/winners")]
    [ProducesResponseType(typeof(Dictionary<string, NodeWinnerStats>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<Dictionary<string, NodeWinnerStats>>> GetWinnerDistribution(
        [FromRoute] Guid simulationId)
    {
        try
        {
            _logger.LogInformation("Getting winner distribution for simulation {SimulationId}", simulationId);
            
            var distribution = await _analyticsService.GetWinnerDistributionAsync(simulationId);
            
            if (!distribution.Any())
            {
                return NotFound(new { error = $"No winner distribution found for simulation {simulationId}" });
            }

            return Ok(distribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting winner distribution for simulation {SimulationId}", simulationId);
            return StatusCode(500, new { error = "Internal server error while getting winner distribution" });
        }
    }

    /// <summary>
    /// Get performance metrics by consensus algorithm
    /// </summary>
    /// <returns>Algorithm performance metrics</returns>
    [HttpGet("algorithms/performance")]
    [ProducesResponseType(typeof(Dictionary<string, AlgorithmPerformanceMetrics>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<Dictionary<string, AlgorithmPerformanceMetrics>>> GetAlgorithmPerformance()
    {
        try
        {
            _logger.LogInformation("Getting algorithm performance metrics");
            
            var performance = await _analyticsService.GetAlgorithmPerformanceAsync();
            
            // Convert enum keys to strings for JSON serialization
            var result = performance.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting algorithm performance metrics");
            return StatusCode(500, new { error = "Internal server error while getting algorithm performance" });
        }
    }

    /// <summary>
    /// Get time-series data for analytics charts
    /// </summary>
    /// <param name="startDate">Start date (ISO 8601 format)</param>
    /// <param name="endDate">End date (ISO 8601 format)</param>
    /// <param name="bucketMinutes">Time bucket size in minutes (default: 30)</param>
    /// <returns>Time-series data points</returns>
    [HttpGet("timeseries")]
    [ProducesResponseType(typeof(List<TimeSeriesDataPoint>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<TimeSeriesDataPoint>>> GetTimeSeriesData(
        [FromQuery, Required] DateTime startDate,
        [FromQuery, Required] DateTime endDate,
        [FromQuery] int bucketMinutes = 30)
    {
        try
        {
            if (startDate >= endDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            if (bucketMinutes < 1 || bucketMinutes > 1440) // 1 minute to 1 day
            {
                return BadRequest(new { error = "Bucket minutes must be between 1 and 1440" });
            }

            _logger.LogInformation("Getting time-series data from {StartDate} to {EndDate} with {BucketMinutes} minute buckets",
                startDate, endDate, bucketMinutes);
            
            var timeSeriesData = await _analyticsService.GetTimeSeriesDataAsync(startDate, endDate, bucketMinutes);
            
            return Ok(timeSeriesData);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid time-series request parameters");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time-series data");
            return StatusCode(500, new { error = "Internal server error while getting time-series data" });
        }
    }

    /// <summary>
    /// Export analytics data in various formats
    /// </summary>
    /// <param name="format">Export format: csv, json</param>
    /// <param name="request">Analytics request with filtering options</param>
    /// <returns>Exported analytics data file</returns>
    [HttpPost("export/{format}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ExportAnalytics(
        [FromRoute] string format,
        [FromBody] AnalyticsRequest? request = null)
    {
        try
        {
            var supportedFormats = new[] { "csv", "json" };
            if (!supportedFormats.Contains(format.ToLower()))
            {
                return BadRequest(new { error = $"Unsupported format '{format}'. Supported formats: {string.Join(", ", supportedFormats)}" });
            }

            _logger.LogInformation("Exporting analytics data in {Format} format", format);
            
            var data = await _analyticsService.ExportAnalyticsAsync(request ?? new AnalyticsRequest(), format);
            
            var contentType = format.ToLower() switch
            {
                "csv" => "text/csv",
                "json" => "application/json",
                _ => "application/octet-stream"
            };

            var fileName = $"analytics-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{format.ToLower()}";
            
            return File(data, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid export request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting analytics data in format {Format}", format);
            return StatusCode(500, new { error = "Internal server error while exporting analytics data" });
        }
    }

    /// <summary>
    /// Get export formats available for analytics data
    /// </summary>
    /// <returns>Available export formats</returns>
    [HttpGet("export/formats")]
    [ProducesResponseType(200)]
    public IActionResult GetExportFormats()
    {
        var formats = new[]
        {
            new { format = "csv", description = "Comma-separated values", contentType = "text/csv" },
            new { format = "json", description = "JavaScript Object Notation", contentType = "application/json" }
        };

        return Ok(new { formats });
    }

    /// <summary>
    /// Health check endpoint for analytics service
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(200)]
    [ProducesResponseType(503)]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            // Basic health check - try to generate a minimal analytics summary
            var healthCheckRequest = new AnalyticsRequest
            {
                StartDate = DateTime.UtcNow.AddHours(-1),
                EndDate = DateTime.UtcNow,
                IncludeNodeStats = false,
                IncludeTimeSeriesData = false
            };

            await _analyticsService.GenerateAnalyticsSummaryAsync(healthCheckRequest);
            
            return Ok(new 
            { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                service = "Analytics API"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analytics service health check failed");
            return StatusCode(503, new 
            { 
                status = "unhealthy", 
                timestamp = DateTime.UtcNow,
                service = "Analytics API",
                error = ex.Message
            });
        }
    }
}