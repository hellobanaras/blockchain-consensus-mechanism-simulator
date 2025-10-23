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
            return response ?? CreateEmptyListBlocksResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blocks");
            return CreateEmptyListBlocksResponse();
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
            return response ?? CreateEmptyListBlocksResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching blocks with query {Query}", query);
            return CreateEmptyListBlocksResponse();
        }
    }

    public async Task<BlockStatistics> GetBlockStatisticsAsync(Guid? simulationId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryString = simulationId.HasValue ? $"?simulationId={simulationId}" : "";
            var response = await _httpClient.GetFromJsonAsync<BlockStatistics>($"api/blocks/statistics{queryString}", cancellationToken);
            return response ?? CreateEmptyBlockStatistics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting block statistics");
            return CreateEmptyBlockStatistics();
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
        
        if (request.SortBy != BlockSortField.BlockNumber)
            parameters.Add($"sortBy={Uri.EscapeDataString(request.SortBy.ToString())}");
        
        if (request.SortDirection == SortDirection.Ascending)
            parameters.Add("sortDirection=Ascending");

        return parameters.Count > 0 ? "?" + string.Join("&", parameters) : "";
    }

    private static ListBlocksResponse CreateEmptyListBlocksResponse()
    {
        return new ListBlocksResponse
        {
            Blocks = Array.Empty<BlockSummary>(),
            Pagination = new PaginationInfo
            {
                CurrentPage = 1,
                PageSize = 20,
                TotalItems = 0,
                TotalPages = 0,
                HasPreviousPage = false,
                HasNextPage = false
            },
            Filters = new BlockFiltersApplied
            {
                TotalBlocksInSystem = 0,
                FilteredBlockCount = 0
            }
        };
    }

    private static BlockStatistics CreateEmptyBlockStatistics()
    {
        return new BlockStatistics
        {
            TotalBlocks = 0,
            ValidBlocks = 0,
            InvalidBlocks = 0,
            TotalTransactions = 0,
            AverageBlockSize = 0,
            AverageTransactionsPerBlock = 0,
            AverageBlockTime = TimeSpan.Zero,
            ChainHeight = 0
        };
    }
}