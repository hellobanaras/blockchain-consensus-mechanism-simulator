using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Consensus.Core.Services;
using Consensus.Core.Models;
using Consensus.Core.Enums;

namespace Consensus.Api.Controllers;

/// <summary>
/// API controller for simulation results and metrics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SimulationResultsController : ControllerBase
{
    private readonly ILogger<SimulationResultsController> _logger;
    private readonly ISimulationMetricsService _metricsService;
    private readonly ISimulationResultsExportService _exportService;

    public SimulationResultsController(
        ILogger<SimulationResultsController> logger,
        ISimulationMetricsService metricsService,
        ISimulationResultsExportService exportService)
    {
        _logger = logger;
        _metricsService = metricsService;
        _exportService = exportService;
    }

    /// <summary>
    /// Get current metrics for an active simulation
    /// </summary>
    [HttpGet("{simulationId}/metrics")]
    public async Task<ActionResult<DetailedSimulationMetrics>> GetCurrentMetrics(Guid simulationId)
    {
        try
        {
            var metrics = await _metricsService.GetCurrentMetricsAsync(simulationId);
            return Ok(metrics);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics for simulation {SimulationId}", simulationId);
            return StatusCode(500, new { message = "Failed to retrieve simulation metrics" });
        }
    }

    /// <summary>
    /// Get comprehensive simulation summary
    /// </summary>
    [HttpGet("{simulationId}/summary")]
    public async Task<ActionResult<SimulationSummary>> GetSimulationSummary(Guid simulationId)
    {
        try
        {
            var summary = await _metricsService.GenerateSimulationSummaryAsync(simulationId);
            return Ok(summary);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get summary for simulation {SimulationId}", simulationId);
            return StatusCode(500, new { message = "Failed to generate simulation summary" });
        }
    }

    /// <summary>
    /// Get round metrics for a simulation
    /// </summary>
    [HttpGet("{simulationId}/rounds")]
    public async Task<ActionResult<List<RoundMetrics>>> GetRoundMetrics(
        Guid simulationId, 
        [FromQuery] int? lastN = null)
    {
        try
        {
            var rounds = await _metricsService.GetRoundMetricsAsync(simulationId, lastN);
            return Ok(rounds);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get round metrics for simulation {SimulationId}", simulationId);
            return StatusCode(500, new { message = "Failed to retrieve round metrics" });
        }
    }

    /// <summary>
    /// Get node performance metrics for a simulation
    /// </summary>
    [HttpGet("{simulationId}/nodes")]
    public async Task<ActionResult<List<NodeMetrics>>> GetNodeMetrics(Guid simulationId)
    {
        try
        {
            var nodes = await _metricsService.GetNodeMetricsAsync(simulationId);
            return Ok(nodes);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get node metrics for simulation {SimulationId}", simulationId);
            return StatusCode(500, new { message = "Failed to retrieve node metrics" });
        }
    }

    /// <summary>
    /// Export simulation results in specified format
    /// </summary>
    [HttpPost("{simulationId}/export")]
    public async Task<IActionResult> ExportSimulationResults(
        Guid simulationId,
        [FromBody] ExportRequest request)
    {
        try
        {
            var exportRequest = new SimulationExportRequest
            {
                SimulationId = simulationId,
                Format = request.Format,
                IncludeRoundData = request.IncludeRoundData,
                IncludeNodeData = request.IncludeNodeData,
                IncludeEventLog = request.IncludeEventLog,
                IncludePrettyFormatting = request.IncludePrettyFormatting,
                ExportedBy = User.Identity?.Name ?? "Anonymous"
            };

            var result = await _exportService.ExportSimulationResultsAsync(exportRequest);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return File(result.Data.ToArray(), result.ContentType, result.FileName);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export simulation {SimulationId} results", simulationId);
            return StatusCode(500, new { message = "Failed to export simulation results" });
        }
    }

    /// <summary>
    /// Generate and download a simulation report
    /// </summary>
    [HttpGet("{simulationId}/report")]
    public async Task<IActionResult> GenerateReport(
        Guid simulationId,
        [FromQuery] ReportFormat format = ReportFormat.SummaryReport)
    {
        try
        {
            var reportStream = await _exportService.GenerateSimulationReportAsync(simulationId, format);
            var fileName = $"simulation_{simulationId}_{format.ToString().ToLower()}_report.txt";
            
            return File(reportStream.ToArray(), "text/plain", fileName);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate report for simulation {SimulationId}", simulationId);
            return StatusCode(500, new { message = "Failed to generate simulation report" });
        }
    }

    /// <summary>
    /// Get metrics summaries for multiple simulations for comparison
    /// </summary>
    [HttpPost("compare")]
    public async Task<ActionResult<List<MetricsSummary>>> CompareSimulations(
        [FromBody] CompareSimulationsRequest request)
    {
        try
        {
            if (request.SimulationIds == null || !request.SimulationIds.Any())
            {
                return BadRequest(new { message = "At least one simulation ID is required" });
            }

            if (request.SimulationIds.Count > 10)
            {
                return BadRequest(new { message = "Maximum 10 simulations can be compared at once" });
            }

            var summaries = await _exportService.GetMetricsSummariesAsync(request.SimulationIds);
            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare simulations");
            return StatusCode(500, new { message = "Failed to compare simulations" });
        }
    }

    /// <summary>
    /// Get real-time metrics updates for live monitoring
    /// </summary>
    [HttpGet("{simulationId}/live")]
    public async Task<ActionResult<LiveMetricsUpdate>> GetLiveMetrics(Guid simulationId)
    {
        try
        {
            var metrics = await _metricsService.GetCurrentMetricsAsync(simulationId);
            var roundMetrics = await _metricsService.GetRoundMetricsAsync(simulationId, 1);
            var currentRound = roundMetrics.LastOrDefault()?.RoundNumber ?? 0;

            var liveUpdate = new LiveMetricsUpdate
            {
                SimulationId = simulationId,
                CurrentRound = currentRound,
                TotalBlocks = metrics.TotalBlocks,
                TotalTransactions = metrics.TotalTransactions,
                CurrentThroughput = metrics.ThroughputTps,
                ConsensusEfficiency = metrics.ConsensusEfficiency,
                ActiveNodes = metrics.NodeCount,
                Status = metrics.Status.ToString()
            };

            return Ok(liveUpdate);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get live metrics for simulation {SimulationId}", simulationId);
            return StatusCode(500, new { message = "Failed to retrieve live metrics" });
        }
    }

    /// <summary>
    /// Initialize metrics tracking for a new simulation
    /// </summary>
    [HttpPost("{simulationId}/initialize")]
    public async Task<IActionResult> InitializeMetrics(
        Guid simulationId,
        [FromBody] InitializeMetricsRequest request)
    {
        try
        {
            var metricsRequest = new SimulationMetricsRequest
            {
                SimulationId = simulationId,
                Algorithm = request.Algorithm,
                NodeCount = request.NodeCount,
                TargetRounds = request.TargetRounds
            };

            await _metricsService.InitializeSimulationMetricsAsync(metricsRequest);
            return Ok(new { message = "Metrics tracking initialized successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize metrics for simulation {SimulationId}", simulationId);
            return StatusCode(500, new { message = "Failed to initialize metrics tracking" });
        }
    }

    /// <summary>
    /// Finalize metrics when simulation completes
    /// </summary>
    [HttpPost("{simulationId}/finalize")]
    public async Task<IActionResult> FinalizeMetrics(
        Guid simulationId,
        [FromBody] FinalizeMetricsRequest request)
    {
        try
        {
            await _metricsService.FinalizeSimulationMetricsAsync(simulationId, request.FinalStatus);
            return Ok(new { message = "Metrics finalized successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to finalize metrics for simulation {SimulationId}", simulationId);
            return StatusCode(500, new { message = "Failed to finalize metrics" });
        }
    }

    /// <summary>
    /// Clean up metrics data for completed simulation
    /// </summary>
    [HttpDelete("{simulationId}/cleanup")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CleanupMetrics(Guid simulationId)
    {
        try
        {
            await _metricsService.CleanupSimulationMetricsAsync(simulationId);
            return Ok(new { message = "Metrics data cleaned up successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup metrics for simulation {SimulationId}", simulationId);
            return StatusCode(500, new { message = "Failed to cleanup metrics data" });
        }
    }
}

#region Request/Response Models

public record ExportRequest
{
    public required ExportFormat Format { get; init; }
    public bool IncludeRoundData { get; init; } = true;
    public bool IncludeNodeData { get; init; } = true;
    public bool IncludeEventLog { get; init; } = true;
    public bool IncludePrettyFormatting { get; init; } = true;
}

public record CompareSimulationsRequest
{
    public required List<Guid> SimulationIds { get; init; }
}

public record InitializeMetricsRequest
{
    public required ConsensusAlgorithm Algorithm { get; init; }
    public required int NodeCount { get; init; }
    public required int TargetRounds { get; init; }
}

public record FinalizeMetricsRequest
{
    public required SimulationStatus FinalStatus { get; init; }
}

#endregion