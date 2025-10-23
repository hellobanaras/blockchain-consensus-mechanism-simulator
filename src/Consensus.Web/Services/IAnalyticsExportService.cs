using Consensus.Web.Services;

namespace Consensus.Web.Services;

/// <summary>
/// Interface for analytics data export functionality
/// </summary>
public interface IAnalyticsExportService
{
    /// <summary>
    /// Export analytics data to CSV format
    /// </summary>
    /// <param name="data">Data to export</param>
    /// <param name="dataType">Type of data being exported</param>
    /// <returns>CSV data as byte array</returns>
    Task<byte[]> ExportToCsvAsync(object data, string dataType);

    /// <summary>
    /// Export analytics data to JSON format
    /// </summary>
    /// <param name="data">Data to export</param>
    /// <param name="formatted">Whether to format the JSON output</param>
    /// <returns>JSON data as byte array</returns>
    Task<byte[]> ExportToJsonAsync(object data, bool formatted = true);

    /// <summary>
    /// Export analytics data to Excel format
    /// </summary>
    /// <param name="data">Data to export</param>
    /// <param name="dataType">Type of data being exported</param>
    /// <returns>Excel data as byte array</returns>
    Task<byte[]> ExportToExcelAsync(object data, string dataType);

    /// <summary>
    /// Export chart data to CSV format
    /// </summary>
    /// <param name="chartData">Chart data to export</param>
    /// <returns>CSV data as byte array</returns>
    Task<byte[]> ExportChartDataToCsvAsync(ChartData chartData);

    /// <summary>
    /// Export performance metrics report to PDF format
    /// </summary>
    /// <param name="performanceData">Performance metrics data</param>
    /// <returns>PDF data as byte array</returns>
    Task<byte[]> ExportPerformanceReportToPdfAsync(List<PerformanceMetrics> performanceData);

    /// <summary>
    /// Create comprehensive dashboard report
    /// </summary>
    /// <param name="analyticsData">Analytics data for the report</param>
    /// <param name="title">Report title</param>
    /// <returns>Report data as byte array</returns>
    Task<byte[]> CreateDashboardReportAsync(AnalyticsData analyticsData, string title = "Analytics Dashboard Report");
}