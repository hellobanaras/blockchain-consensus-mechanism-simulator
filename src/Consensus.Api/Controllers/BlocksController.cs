using Consensus.Core.Models;
using Consensus.Core.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Consensus.Api.Controllers;

/// <summary>
/// API controller for blockchain block operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BlocksController : ControllerBase
{
    private readonly IBlockRepository _blockRepository;
    private readonly ILogger<BlocksController> _logger;

    public BlocksController(IBlockRepository blockRepository, ILogger<BlocksController> logger)
    {
        _blockRepository = blockRepository ?? throw new ArgumentNullException(nameof(blockRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a paginated list of blocks with optional filtering
    /// </summary>
    /// <param name="simulationId">Filter by simulation ID</param>
    /// <param name="protocol">Filter by consensus protocol</param>
    /// <param name="proposerId">Filter by proposer node ID</param>
    /// <param name="minBlockNumber">Minimum block number</param>
    /// <param name="maxBlockNumber">Maximum block number</param>
    /// <param name="startDate">Start date filter</param>
    /// <param name="endDate">End date filter</param>
    /// <param name="searchTerm">Search term for hash or identifiers</param>
    /// <param name="isValid">Filter by block validity</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDirection">Sort direction</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Page size (max 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of block summaries</returns>
    [HttpGet]
    public async Task<ActionResult<ListBlocksResponse>> GetBlocks(
        [FromQuery] Guid? simulationId = null,
        [FromQuery] string? protocol = null,
        [FromQuery] Guid? proposerId = null,
        [FromQuery] long? minBlockNumber = null,
        [FromQuery] long? maxBlockNumber = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isValid = null,
        [FromQuery] BlockSortField sortBy = BlockSortField.BlockNumber,
        [FromQuery] SortDirection sortDirection = SortDirection.Descending,
        [FromQuery] [Range(1, int.MaxValue)] int page = 1,
        [FromQuery] [Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse protocol if provided
            Core.Enums.ConsensusAlgorithm? consensusProtocol = null;
            if (!string.IsNullOrWhiteSpace(protocol))
            {
                if (Enum.TryParse<Core.Enums.ConsensusAlgorithm>(protocol, true, out var parsed))
                {
                    consensusProtocol = parsed;
                }
                else
                {
                    return BadRequest($"Invalid protocol: {protocol}");
                }
            }

            var request = new ListBlocksRequest
            {
                SimulationId = simulationId,
                Protocol = consensusProtocol,
                ProposerId = proposerId,
                MinBlockNumber = minBlockNumber,
                MaxBlockNumber = maxBlockNumber,
                StartDate = startDate,
                EndDate = endDate,
                SearchTerm = searchTerm,
                IsValid = isValid,
                SortBy = sortBy,
                SortDirection = sortDirection,
                Page = page,
                PageSize = pageSize
            };

            var response = await _blockRepository.GetBlocksAsync(request, cancellationToken);

            _logger.LogInformation("Retrieved {BlockCount} blocks for page {Page} (total: {Total})", 
                response.Blocks.Count, page, response.Pagination.TotalItems);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blocks for page {Page}", page);
            return StatusCode(500, "An error occurred while retrieving blocks");
        }
    }

    /// <summary>
    /// Gets detailed information about a specific block
    /// </summary>
    /// <param name="id">Block identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed block information</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BlockDetail>> GetBlock(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var block = await _blockRepository.GetBlockDetailAsync(id, cancellationToken);

            if (block == null)
            {
                _logger.LogWarning("Block {BlockId} not found", id);
                return NotFound($"Block {id} not found");
            }

            _logger.LogInformation("Retrieved block {BlockId} (#{BlockNumber})", id, block.BlockNumber);
            return Ok(block);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving block {BlockId}", id);
            return StatusCode(500, "An error occurred while retrieving the block");
        }
    }

    /// <summary>
    /// Gets a block by its hash
    /// </summary>
    /// <param name="hash">Block hash</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Block information</returns>
    [HttpGet("hash/{hash}")]
    public async Task<ActionResult<BlockDetail>> GetBlockByHash(string hash, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                return BadRequest("Block hash is required");
            }

            var block = await _blockRepository.GetBlockSummaryByHashAsync(hash, cancellationToken);

            if (block == null)
            {
                _logger.LogWarning("Block with hash {Hash} not found", hash);
                return NotFound($"Block with hash {hash} not found");
            }

            // Get detailed information
            var blockDetail = await _blockRepository.GetBlockDetailAsync(block.Id, cancellationToken);

            _logger.LogInformation("Retrieved block by hash {Hash} (#{BlockNumber})", hash, block.BlockNumber);
            return Ok(blockDetail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving block by hash {Hash}", hash);
            return StatusCode(500, "An error occurred while retrieving the block");
        }
    }

    /// <summary>
    /// Gets a block by simulation and block number
    /// </summary>
    /// <param name="simulationId">Simulation identifier</param>
    /// <param name="blockNumber">Block number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Block information</returns>
    [HttpGet("simulation/{simulationId:guid}/block/{blockNumber:long}")]
    public async Task<ActionResult<BlockDetail>> GetBlockByNumber(
        Guid simulationId, long blockNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            if (blockNumber < 0)
            {
                return BadRequest("Block number must be non-negative");
            }

            var block = await _blockRepository.GetBlockSummaryByNumberAsync(simulationId, blockNumber, cancellationToken);

            if (block == null)
            {
                _logger.LogWarning("Block #{BlockNumber} not found in simulation {SimulationId}", blockNumber, simulationId);
                return NotFound($"Block #{blockNumber} not found in simulation {simulationId}");
            }

            // Get detailed information
            var blockDetail = await _blockRepository.GetBlockDetailAsync(block.Id, cancellationToken);

            _logger.LogInformation("Retrieved block #{BlockNumber} from simulation {SimulationId}", blockNumber, simulationId);
            return Ok(blockDetail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving block #{BlockNumber} from simulation {SimulationId}", blockNumber, simulationId);
            return StatusCode(500, "An error occurred while retrieving the block");
        }
    }

    /// <summary>
    /// Gets the latest block in a simulation
    /// </summary>
    /// <param name="simulationId">Simulation identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest block information</returns>
    [HttpGet("simulation/{simulationId:guid}/latest")]
    public async Task<ActionResult<BlockDetail>> GetLatestBlock(Guid simulationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var block = await _blockRepository.GetLatestBlockSummaryAsync(simulationId, cancellationToken);

            if (block == null)
            {
                _logger.LogWarning("No blocks found in simulation {SimulationId}", simulationId);
                return NotFound($"No blocks found in simulation {simulationId}");
            }

            // Get detailed information
            var blockDetail = await _blockRepository.GetBlockDetailAsync(block.Id, cancellationToken);

            _logger.LogInformation("Retrieved latest block #{BlockNumber} from simulation {SimulationId}", block.BlockNumber, simulationId);
            return Ok(blockDetail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest block from simulation {SimulationId}", simulationId);
            return StatusCode(500, "An error occurred while retrieving the latest block");
        }
    }

    /// <summary>
    /// Searches for blocks by hash, block number, or transaction hash
    /// </summary>
    /// <param name="q">Search query</param>
    /// <param name="simulationId">Optional simulation filter</param>
    /// <param name="limit">Maximum number of results (default: 10, max: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching block summaries</returns>
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<BlockSummary>>> SearchBlocks(
        [FromQuery] [Required] string q,
        [FromQuery] Guid? simulationId = null,
        [FromQuery] [Range(1, 50)] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest("Search query is required");
            }

            var blocks = await _blockRepository.SearchBlocksAsync(q, simulationId, limit, cancellationToken);

            _logger.LogInformation("Search for '{Query}' returned {ResultCount} blocks", q, blocks.Count);
            return Ok(blocks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching blocks with query '{Query}'", q);
            return StatusCode(500, "An error occurred while searching blocks");
        }
    }

    /// <summary>
    /// Gets block statistics for a simulation
    /// </summary>
    /// <param name="simulationId">Simulation identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Block statistics</returns>
    [HttpGet("simulation/{simulationId:guid}/statistics")]
    public async Task<ActionResult<BlockStatistics>> GetBlockStatistics(Guid simulationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var statistics = await _blockRepository.GetBlockStatisticsAsync(simulationId, cancellationToken);

            _logger.LogInformation("Retrieved block statistics for simulation {SimulationId}: {TotalBlocks} blocks", 
                simulationId, statistics.TotalBlocks);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving block statistics for simulation {SimulationId}", simulationId);
            return StatusCode(500, "An error occurred while retrieving block statistics");
        }
    }

    /// <summary>
    /// Gets chain information for a simulation
    /// </summary>
    /// <param name="simulationId">Simulation identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chain information</returns>
    [HttpGet("simulation/{simulationId:guid}/chain-info")]
    public async Task<ActionResult<ChainInfo>> GetChainInfo(Guid simulationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var chainInfo = await _blockRepository.GetChainInfoAsync(simulationId, cancellationToken);

            _logger.LogInformation("Retrieved chain info for simulation {SimulationId}: height {Height}, valid: {IsValid}", 
                simulationId, chainInfo.Height, chainInfo.IsValid);

            return Ok(chainInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chain info for simulation {SimulationId}", simulationId);
            return StatusCode(500, "An error occurred while retrieving chain information");
        }
    }

    /// <summary>
    /// Gets blocks proposed by a specific node
    /// </summary>
    /// <param name="nodeId">Node identifier</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Page size (max 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated blocks proposed by the node</returns>
    [HttpGet("proposer/{nodeId:guid}")]
    public async Task<ActionResult<object>> GetBlocksByProposer(
        Guid nodeId,
        [FromQuery] [Range(1, int.MaxValue)] int page = 1,
        [FromQuery] [Range(1, 50)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (blocks, totalCount) = await _blockRepository.GetBlocksByProposerAsync(nodeId, page, pageSize, cancellationToken);

            var response = new
            {
                Blocks = blocks,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPreviousPage = page > 1,
                    HasNextPage = page * pageSize < totalCount
                }
            };

            _logger.LogInformation("Retrieved {BlockCount} blocks proposed by node {NodeId} (page {Page})", 
                blocks.Count, nodeId, page);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blocks by proposer {NodeId}", nodeId);
            return StatusCode(500, "An error occurred while retrieving blocks by proposer");
        }
    }
}