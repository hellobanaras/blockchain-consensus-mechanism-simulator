using Microsoft.AspNetCore.Mvc;
using Consensus.Core.Interfaces;
using Consensus.Core.Models;
using Consensus.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Consensus.Core.Entities;

namespace Consensus.Api.Controllers;

/// <summary>
/// API controller for managing consensus simulations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SimulationsController : ControllerBase
{
    private readonly ISimulationService _simulationService;
    private readonly ILogger<SimulationsController> _logger;

    public SimulationsController(ISimulationService simulationService, ILogger<SimulationsController> logger)
    {
        _simulationService = simulationService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new simulation
    /// </summary>
    /// <param name="request">Simulation creation request</param>
    /// <returns>Created simulation details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SimulationRun), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SimulationRun>> CreateSimulation([FromBody] CreateSimulationRequest request)
    {
        try
        {
            _logger.LogInformation("Creating new simulation: {Name} with {NodeCount} nodes using {Algorithm}", 
                request.Name, request.NodeCount, request.Algorithm);

            var simulation = await _simulationService.CreateSimulationAsync(request);
            
            _logger.LogInformation("Successfully created simulation {SimulationId}", simulation.Id);
            
            return CreatedAtAction(
                nameof(GetSimulation), 
                new { id = simulation.Id }, 
                simulation);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid simulation parameters: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot create simulation: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating simulation");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while creating the simulation" });
        }
    }

    /// <summary>
    /// Starts a simulation
    /// </summary>
    /// <param name="id">Simulation ID</param>
    /// <returns>Operation result</returns>
    [HttpPost("{id}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartSimulation(Guid id)
    {
        try
        {
            _logger.LogInformation("Starting simulation {SimulationId}", id);

            var result = await _simulationService.StartSimulationAsync(id);
            
            if (!result)
            {
                return NotFound(new { error = "Simulation not found or cannot be started" });
            }

            _logger.LogInformation("Successfully started simulation {SimulationId}", id);
            return Ok(new { message = "Simulation started successfully", simulationId = id });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid simulation start request: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot start simulation: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting simulation {SimulationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while starting the simulation" });
        }
    }

    /// <summary>
    /// Stops a running simulation
    /// </summary>
    /// <param name="id">Simulation ID</param>
    /// <returns>Operation result</returns>
    [HttpPost("{id}/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StopSimulation(Guid id)
    {
        try
        {
            _logger.LogInformation("Stopping simulation {SimulationId}", id);

            var result = await _simulationService.StopSimulationAsync(id);
            
            if (!result)
            {
                return NotFound(new { error = "Simulation not found or cannot be stopped" });
            }

            _logger.LogInformation("Successfully stopped simulation {SimulationId}", id);
            return Ok(new { message = "Simulation stopped successfully", simulationId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping simulation {SimulationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while stopping the simulation" });
        }
    }

    /// <summary>
    /// Gets a simulation by ID
    /// </summary>
    /// <param name="id">Simulation ID</param>
    /// <returns>Simulation details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SimulationRun), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SimulationRun>> GetSimulation(Guid id)
    {
        try
        {
            var simulation = await _simulationService.GetSimulationAsync(id);
            
            if (simulation == null)
            {
                return NotFound(new { error = "Simulation not found" });
            }

            return Ok(simulation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving simulation {SimulationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while retrieving the simulation" });
        }
    }

    /// <summary>
    /// Gets all simulations
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="algorithm">Optional algorithm filter</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>List of simulations</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SimulationRun>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<SimulationRun>>> GetSimulations(
        [FromQuery] SimulationStatus? status = null,
        [FromQuery] ConsensusAlgorithm? algorithm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var simulations = await _simulationService.GetSimulationsAsync();
            
            // Apply filters
            if (status.HasValue)
            {
                simulations = simulations.Where(s => s.Status == status.Value);
            }
            
            if (algorithm.HasValue)
            {
                simulations = simulations.Where(s => s.ConsensusAlgorithm == algorithm.Value);
            }

            // Apply pagination
            var totalCount = simulations.Count();
            var pagedSimulations = simulations
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Add pagination headers
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());
            Response.Headers.Add("X-Total-Pages", ((int)Math.Ceiling(totalCount / (double)pageSize)).ToString());

            return Ok(pagedSimulations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving simulations");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while retrieving simulations" });
        }
    }

    /// <summary>
    /// Deletes a simulation
    /// </summary>
    /// <param name="id">Simulation ID</param>
    /// <returns>Operation result</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteSimulation(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting simulation {SimulationId}", id);

            var result = await _simulationService.DeleteSimulationAsync(id);
            
            if (!result)
            {
                return NotFound(new { error = "Simulation not found" });
            }

            _logger.LogInformation("Successfully deleted simulation {SimulationId}", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot delete simulation: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting simulation {SimulationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while deleting the simulation" });
        }
    }

    /// <summary>
    /// Gets simulation metrics and statistics
    /// </summary>
    /// <param name="id">Simulation ID</param>
    /// <returns>Simulation metrics</returns>
    [HttpGet("{id}/metrics")]
    [ProducesResponseType(typeof(SimulationMetrics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SimulationMetrics>> GetSimulationMetrics(Guid id)
    {
        try
        {
            var metrics = await _simulationService.GetSimulationMetricsAsync(id);
            
            if (metrics == null)
            {
                return NotFound(new { error = "Simulation not found or no metrics available" });
            }

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving simulation metrics for {SimulationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while retrieving simulation metrics" });
        }
    }

    /// <summary>
    /// Gets the blocks produced by a simulation
    /// </summary>
    /// <param name="id">Simulation ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of blocks per page</param>
    /// <returns>List of blocks</returns>
    [HttpGet("{id}/blocks")]
    [ProducesResponseType(typeof(IEnumerable<Block>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Block>>> GetSimulationBlocks(
        Guid id, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var simulation = await _simulationService.GetSimulationAsync(id);
            
            if (simulation == null)
            {
                return NotFound(new { error = "Simulation not found" });
            }

            var blocks = simulation.Blocks.ToList();
            var totalCount = blocks.Count;
            
            // Apply pagination
            var pagedBlocks = blocks
                .OrderBy(b => b.BlockNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Add pagination headers
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());
            Response.Headers.Add("X-Total-Pages", ((int)Math.Ceiling(totalCount / (double)pageSize)).ToString());

            return Ok(pagedBlocks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blocks for simulation {SimulationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while retrieving simulation blocks" });
        }
    }

    /// <summary>
    /// Gets the consensus rounds from a simulation
    /// </summary>
    /// <param name="id">Simulation ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of rounds per page</param>
    /// <returns>List of consensus rounds</returns>
    [HttpGet("{id}/rounds")]
    [ProducesResponseType(typeof(IEnumerable<ConsensusRound>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ConsensusRound>>> GetSimulationRounds(
        Guid id, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var simulation = await _simulationService.GetSimulationAsync(id);
            
            if (simulation == null)
            {
                return NotFound(new { error = "Simulation not found" });
            }

            var rounds = simulation.ConsensusRounds.ToList();
            var totalCount = rounds.Count;
            
            // Apply pagination
            var pagedRounds = rounds
                .OrderBy(r => r.RoundNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Add pagination headers
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());
            Response.Headers.Add("X-Total-Pages", ((int)Math.Ceiling(totalCount / (double)pageSize)).ToString());

            return Ok(pagedRounds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rounds for simulation {SimulationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while retrieving simulation rounds" });
        }
    }

    /// <summary>
    /// Gets available consensus algorithms
    /// </summary>
    /// <returns>List of available consensus algorithms</returns>
    [HttpGet("algorithms")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<object>>> GetAvailableAlgorithms()
    {
        var algorithms = Enum.GetValues<ConsensusAlgorithm>()
            .Select(a => new
            {
                value = a,
                name = a.ToString(),
                displayName = GetAlgorithmDisplayName(a),
                description = GetAlgorithmDescription(a),
                minNodes = GetAlgorithmMinNodes(a),
                supportsByzantineFaults = GetAlgorithmByzantineSupport(a)
            })
            .ToList();

        return await Task.FromResult(Ok(algorithms));
    }

    private static string GetAlgorithmDisplayName(ConsensusAlgorithm algorithm)
    {
        return algorithm switch
        {
            ConsensusAlgorithm.ProofOfWork => "Proof of Work",
            ConsensusAlgorithm.ProofOfStake => "Proof of Stake",
            ConsensusAlgorithm.DelegatedProofOfStake => "Delegated Proof of Stake",
            ConsensusAlgorithm.PracticalByzantineFaultTolerance => "Practical Byzantine Fault Tolerance",
            ConsensusAlgorithm.ProofOfElapsedTime => "Proof of Elapsed Time",
            ConsensusAlgorithm.Raft => "Raft",
            ConsensusAlgorithm.HoneyBadgerBFT => "HoneyBadger BFT",
            ConsensusAlgorithm.Tendermint => "Tendermint",
            ConsensusAlgorithm.Algorand => "Algorand",
            ConsensusAlgorithm.StellarConsensusProtocol => "Stellar Consensus Protocol",
            ConsensusAlgorithm.FedCoin => "FedCoin",
            _ => algorithm.ToString()
        };
    }

    private static string GetAlgorithmDescription(ConsensusAlgorithm algorithm)
    {
        return algorithm switch
        {
            ConsensusAlgorithm.ProofOfWork => "Mining-based consensus using computational puzzles",
            ConsensusAlgorithm.ProofOfStake => "Validator selection based on stake ownership",
            ConsensusAlgorithm.DelegatedProofOfStake => "Delegated validators voted by stakeholders",
            ConsensusAlgorithm.PracticalByzantineFaultTolerance => "Byzantine fault tolerant consensus algorithm",
            ConsensusAlgorithm.ProofOfElapsedTime => "Random leader selection based on wait times",
            ConsensusAlgorithm.Raft => "Leader-based consensus for distributed systems",
            ConsensusAlgorithm.HoneyBadgerBFT => "Asynchronous Byzantine fault tolerant protocol",
            ConsensusAlgorithm.Tendermint => "Byzantine fault tolerant consensus with instant finality",
            ConsensusAlgorithm.Algorand => "Pure proof-of-stake with verifiable random functions",
            ConsensusAlgorithm.StellarConsensusProtocol => "Federated Byzantine Agreement protocol",
            ConsensusAlgorithm.FedCoin => "Simplified consensus for testing purposes",
            _ => "Consensus algorithm"
        };
    }

    private static int GetAlgorithmMinNodes(ConsensusAlgorithm algorithm)
    {
        return algorithm switch
        {
            ConsensusAlgorithm.ProofOfWork => 1,
            ConsensusAlgorithm.ProofOfStake => 3,
            ConsensusAlgorithm.DelegatedProofOfStake => 3,
            ConsensusAlgorithm.PracticalByzantineFaultTolerance => 4,
            ConsensusAlgorithm.ProofOfElapsedTime => 3,
            ConsensusAlgorithm.Raft => 3,
            ConsensusAlgorithm.HoneyBadgerBFT => 4,
            ConsensusAlgorithm.Tendermint => 4,
            ConsensusAlgorithm.Algorand => 3,
            ConsensusAlgorithm.StellarConsensusProtocol => 3,
            ConsensusAlgorithm.FedCoin => 1,
            _ => 3
        };
    }

    private static bool GetAlgorithmByzantineSupport(ConsensusAlgorithm algorithm)
    {
        return algorithm switch
        {
            ConsensusAlgorithm.PracticalByzantineFaultTolerance => true,
            ConsensusAlgorithm.ProofOfElapsedTime => true,
            _ => false
        };
    }
}