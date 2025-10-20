using Microsoft.JSInterop;
using Consensus.Core.Models;

namespace Consensus.Web.Services;

/// <summary>
/// Service for Chart.js JavaScript interoperability
/// </summary>
public class ChartJsService
{
    private readonly IJSRuntime _jsRuntime;

    public ChartJsService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Creates a new chart with the specified configuration
    /// </summary>
    public async Task<bool> CreateChartAsync(string canvasId, object config)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("chartFunctions.createChart", canvasId, config);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating chart {canvasId}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates an existing chart with new data
    /// </summary>
    public async Task<bool> UpdateChartAsync(string canvasId, object newData, string[]? newLabels = null)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("chartFunctions.updateChart", canvasId, newData, newLabels);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating chart {canvasId}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Destroys a chart instance
    /// </summary>
    public async Task<bool> DestroyChartAsync(string canvasId)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("chartFunctions.destroyChart", canvasId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error destroying chart {canvasId}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates a bar chart configuration
    /// </summary>
    public async Task<object> GetBarChartConfigAsync(string title, string[] labels, double[] data, string[]? backgroundColor = null)
    {
        return await _jsRuntime.InvokeAsync<object>("chartFunctions.getBarChartConfig", title, labels, data, backgroundColor);
    }

    /// <summary>
    /// Creates a doughnut chart configuration
    /// </summary>
    public async Task<object> GetDoughnutChartConfigAsync(string title, string[] labels, double[] data, string[]? backgroundColor = null)
    {
        return await _jsRuntime.InvokeAsync<object>("chartFunctions.getDoughnutChartConfig", title, labels, data, backgroundColor);
    }

    /// <summary>
    /// Creates a line chart configuration with multiple datasets
    /// </summary>
    public async Task<object> GetLineChartConfigAsync(string title, string[] labels, object[] datasets)
    {
        return await _jsRuntime.InvokeAsync<object>("chartFunctions.getLineChartConfig", title, labels, datasets);
    }

    /// <summary>
    /// Creates a bar chart for winner distribution
    /// </summary>
    public async Task<bool> CreateWinnerDistributionChartAsync(string canvasId, Dictionary<string, int> winnerData)
    {
        if (!winnerData.Any()) return false;

        var labels = winnerData.Keys.Select(k => k.Length > 8 ? k.Substring(0, 8) + "..." : k).ToArray();
        var data = winnerData.Values.Select(v => (double)v).ToArray();

        var config = await GetBarChartConfigAsync("Winner Distribution", labels, data);
        return await CreateChartAsync(canvasId, config);
    }

    /// <summary>
    /// Creates a doughnut chart for algorithm distribution
    /// </summary>
    public async Task<bool> CreateAlgorithmDistributionChartAsync(string canvasId, Dictionary<string, int> algorithmData)
    {
        if (!algorithmData.Any()) return false;

        var labels = algorithmData.Keys.ToArray();
        var data = algorithmData.Values.Select(v => (double)v).ToArray();

        var config = await GetDoughnutChartConfigAsync("Algorithm Distribution", labels, data);
        return await CreateChartAsync(canvasId, config);
    }
}