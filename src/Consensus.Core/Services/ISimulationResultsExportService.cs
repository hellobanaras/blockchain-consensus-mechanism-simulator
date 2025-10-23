using Consensus.Core.Models;

namespace Consensus.Core.Services;

/// <summary>
/// Interface for simulation results export and reporting service
/// </summary>
public interface ISimulationResultsExportService
{
    /// <summary>
    /// Export simulation results in the specified format
    /// </summary>
    Task<ExportResult> ExportSimulationResultsAsync(SimulationExportRequest request);

    /// <summary>
    /// Generate a formatted simulation report
    /// </summary>
    Task<MemoryStream> GenerateSimulationReportAsync(Guid simulationId, ReportFormat format);

    /// <summary>
    /// Get summary metrics for multiple simulations for comparison
    /// </summary>
    Task<List<MetricsSummary>> GetMetricsSummariesAsync(List<Guid> simulationIds);
}