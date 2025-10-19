using Consensus.Core.Models;

namespace Consensus.Web.Services;

/// <summary>
/// Service for interacting with the Block Explorer API
/// </summary>
public interface IBlockExplorerService
{
    Task<ListBlocksResponse> GetBlocksAsync(ListBlocksRequest request, CancellationToken cancellationToken = default);
    Task<BlockDetail?> GetBlockDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BlockSummary?> GetBlockSummaryByHashAsync(string hash, CancellationToken cancellationToken = default);
    Task<ListBlocksResponse> SearchBlocksAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<BlockStatistics> GetBlockStatisticsAsync(Guid? simulationId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// HTTP client implementation for Block Explorer API
/// </summary>
public class BlockExplorerService : IBlockExplorerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BlockExplorerService> _logger;

    public BlockExplorerService(HttpClient httpClient, ILogger<BlockExplorerService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ListBlocksResponse> GetBlocksAsync(ListBlocksRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryString = BuildQueryString(request);
            var response = await _httpClient.GetFromJsonAsync<ListBlocksResponse>($"api/blocks{queryString}", cancellationToken);
            return response ?? new ListBlocksResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blocks");
            return new ListBlocksResponse();
        }
    }

    public async Task<BlockDetail?> GetBlockDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<BlockDetail>($"api/blocks/{id}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting block detail for {BlockId}", id);
            return null;
        }
    }

    public async Task<BlockSummary?> GetBlockSummaryByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<BlockSummary>($"api/blocks/hash/{hash}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting block summary by hash {Hash}", hash);
            return null;
        }
    }

    public async Task<ListBlocksResponse> SearchBlocksAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryString = $"?query={Uri.EscapeDataString(query)}&page={page}&pageSize={pageSize}";
            var response = await _httpClient.GetFromJsonAsync<ListBlocksResponse>($"api/blocks/search{queryString}", cancellationToken);
            return response ?? new ListBlocksResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching blocks with query {Query}", query);
            return new ListBlocksResponse();
        }
    }

    public async Task<BlockStatistics> GetBlockStatisticsAsync(Guid? simulationId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryString = simulationId.HasValue ? $"?simulationId={simulationId}" : "";
            var response = await _httpClient.GetFromJsonAsync<BlockStatistics>($"api/blocks/statistics{queryString}", cancellationToken);
            return response ?? new BlockStatistics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting block statistics");
            return new BlockStatistics();
        }
    }

    private static string BuildQueryString(ListBlocksRequest request)
    {
        var parameters = new List<string>();
        
        if (request.Page > 1)
            parameters.Add($"page={request.Page}");
        
        if (request.PageSize != 20)
            parameters.Add($"pageSize={request.PageSize}");
        
        if (request.SimulationId.HasValue)
            parameters.Add($"simulationId={request.SimulationId}");
        
        if (!string.IsNullOrEmpty(request.SortBy))
            parameters.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");
        
        if (request.SortDescending)
            parameters.Add("sortDescending=true");

        return parameters.Count > 0 ? "?" + string.Join("&", parameters) : "";
    }
}