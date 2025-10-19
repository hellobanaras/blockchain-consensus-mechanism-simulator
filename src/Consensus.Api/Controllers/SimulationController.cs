using Microsoft.AspNetCore.Mvc;
using Consensus.Core.Services;
using Consensus.Api.Models;
using Consensus.Core.Enums;

namespace Consensus.Api.Controllers;

/// <summary>
/// API controller for managing consensus simulations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SimulationController : ControllerBase
{
    private readonly ISimulationService _simulationService;
    private readonly ILogger<SimulationController> _logger;

    public SimulationController(ISimulationService simulationService, ILogger<SimulationController> logger)
    {
        _simulationService = simulationService;
        _logger = logger;
    }

    /// <summary>
    /// Creates and optionally starts a new consensus simulation
    /// </summary>
    /// <param name="request">Simulation configuration parameters</param>
    /// <returns>Simulation details and status</returns>
    [HttpPost("start")]
    [ProducesResponseType(typeof(StartSimulationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StartSimulationResponse>> StartSimulation([FromBody] StartSimulationRequest request)
    {
        try
        {
            _logger.LogInformation("Starting simulation request: {Name} with algorithm {Algorithm}", 
                request.Name, request.Algorithm);

            // Validate the request
            var validationErrors = request.ValidateRequest();
            if (validationErrors.Any())
            {
                var problemDetails = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["ValidationErrors"] = validationErrors.ToArray()
                })
                {
                    Title = "Validation Failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "One or more validation errors occurred."
                };

                return BadRequest(problemDetails);
            }

            // Create the simulation
            var createRequest = new CreateSimulationRequest
            {
                Name = request.Name,
                Algorithm = request.Algorithm,
                NodeCount = request.NodeCount,
                ByzantineNodeCount = request.ByzantineNodeCount,
                DurationSeconds = request.DurationSeconds,
                NetworkTopology = request.NetworkTopology,
                BlockTimeMs = request.BlockTimeMs,
                TransactionsPerBlock = request.TransactionsPerBlock,
                NetworkLatencyMs = request.NetworkLatencyMs,
                AlgorithmConfiguration = request.AlgorithmConfiguration
            };

            var simulation = await _simulationService.CreateSimulationAsync(createRequest);

            // Optionally start the simulation immediately
            bool started = false;
            if (request.AutoStart)
            {
                started = await _simulationService.StartSimulationAsync(simulation.Id);
                if (!started)
                {
                    _logger.LogWarning("Failed to auto-start simulation {SimulationId}", simulation.Id);
                }
            }

            // Prepare response
            var response = new StartSimulationResponse
            {
                SimulationId = simulation.Id,
                Name = simulation.Name,
                Status = simulation.Status,
                CreatedAt = simulation.CreatedAt,
                EstimatedCompletionAt = request.AutoStart && started
                    ? DateTime.UtcNow.AddSeconds(request.DurationSeconds)
                    : null,
                WebSocketEndpoint = $"/simulationHub",
                Success = true
            };

            // Add warnings if any
            var warnings = new List<string>();
            if (request.ByzantineNodeCount > 0)
            {
                warnings.Add($"Simulation includes {request.ByzantineNodeCount} Byzantine (faulty) nodes");
            }
            
            if (request.NetworkLatencyMs > 500)
            {
                warnings.Add($"High network latency ({request.NetworkLatencyMs}ms) may significantly impact consensus performance");
            }

            if (request.NodeCount > 50)
            {
                warnings.Add($"Large network size ({request.NodeCount} nodes) may require more time to reach consensus");
            }

            if (!started && request.AutoStart)
            {
                warnings.Add("Auto-start was requested but simulation failed to start automatically");
            }

            // Create new response with warnings
            response = response with { Warnings = warnings };

            _logger.LogInformation("Simulation {SimulationId} created successfully with status {Status}", 
                simulation.Id, simulation.Status);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid simulation request: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create simulation: {Name}", request.Name);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An error occurred while creating the simulation"
            });
        }
    }

    /// <summary>
    /// Starts an existing simulation
    /// </summary>
    /// <param name="simulationId">ID of the simulation to start</param>
    /// <returns>Success status</returns>
    [HttpPost("{simulationId:guid}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartExistingSimulation(Guid simulationId)
    {
        try
        {
            var simulation = await _simulationService.GetSimulationAsync(simulationId);
            if (simulation == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Simulation Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"No simulation found with ID {simulationId}"
                });
            }

            var started = await _simulationService.StartSimulationAsync(simulationId);
            if (!started)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Cannot Start Simulation",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = $"Simulation {simulationId} cannot be started in its current state: {simulation.Status}"
                });
            }

            _logger.LogInformation("Simulation {SimulationId} started manually", simulationId);
            return Ok(new { success = true, message = "Simulation started successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start simulation {SimulationId}", simulationId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An error occurred while starting the simulation"
            });
        }
    }

    /// <summary>
    /// Stops a running simulation
    /// </summary>
    /// <param name="simulationId">ID of the simulation to stop</param>
    /// <returns>Success status</returns>
    [HttpPost("{simulationId:guid}/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopSimulation(Guid simulationId)
    {
        try
        {
            var simulation = await _simulationService.GetSimulationAsync(simulationId);
            if (simulation == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Simulation Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"No simulation found with ID {simulationId}"
                });
            }

            var stopped = await _simulationService.StopSimulationAsync(simulationId);
            if (!stopped)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Cannot Stop Simulation",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = $"Simulation {simulationId} could not be stopped"
                });
            }

            _logger.LogInformation("Simulation {SimulationId} stopped manually", simulationId);
            return Ok(new { success = true, message = "Simulation stopped successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop simulation {SimulationId}", simulationId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An error occurred while stopping the simulation"
            });
        }
    }

    /// <summary>
    /// Gets details of a specific simulation
    /// </summary>
    /// <param name="simulationId">ID of the simulation</param>
    /// <returns>Simulation details</returns>
    [HttpGet("{simulationId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSimulation(Guid simulationId)
    {
        try
        {
            var simulation = await _simulationService.GetSimulationAsync(simulationId);
            if (simulation == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Simulation Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"No simulation found with ID {simulationId}"
                });
            }

            var metrics = await _simulationService.GetSimulationMetricsAsync(simulationId);

            var response = new
            {
                Id = simulation.Id,
                Name = simulation.Name,
                Description = simulation.Description,
                Status = simulation.Status,
                Algorithm = simulation.ConsensusAlgorithm,
                NodeCount = simulation.NodeCount,
                ByzantineNodeCount = simulation.ByzantineNodeCount,
                DurationSeconds = simulation.DurationSeconds,
                NetworkTopology = simulation.NetworkTopology,
                BlockTimeMs = simulation.BlockTimeMs,
                TransactionsPerBlock = simulation.TransactionsPerBlock,
                NetworkLatencyMs = simulation.NetworkLatencyMs,
                CreatedAt = simulation.CreatedAt,
                StartedAt = simulation.StartedAt,
                CompletedAt = simulation.CompletedAt,
                TotalRounds = simulation.ConsensusRounds?.Count ?? 0,
                TotalTransactions = simulation.TotalTransactions,
                Metrics = metrics,
                Configuration = simulation.Configuration
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get simulation {SimulationId}", simulationId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An error occurred while retrieving the simulation"
            });
        }
    }

    /// <summary>
    /// Gets a list of all simulations
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="algorithm">Optional algorithm filter</param>
    /// <returns>List of simulations</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSimulations(
        [FromQuery] SimulationStatus? status = null,
        [FromQuery] ConsensusAlgorithm? algorithm = null)
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

            var response = simulations.Select(s => new
            {
                Id = s.Id,
                Name = s.Name,
                Status = s.Status,
                Algorithm = s.ConsensusAlgorithm,
                NodeCount = s.NodeCount,
                ByzantineNodeCount = s.ByzantineNodeCount,
                CreatedAt = s.CreatedAt,
                StartedAt = s.StartedAt,
                CompletedAt = s.CompletedAt,
                Duration = s.GetDuration(),
                TotalRounds = s.ConsensusRounds?.Count ?? 0
            }).OrderByDescending(s => s.CreatedAt);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get simulations list");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An error occurred while retrieving simulations"
            });
        }
    }

    /// <summary>
    /// Deletes a simulation and all its data
    /// </summary>
    /// <param name="simulationId">ID of the simulation to delete</param>
    /// <returns>Success status</returns>
    [HttpDelete("{simulationId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSimulation(Guid simulationId)
    {
        try
        {
            var simulation = await _simulationService.GetSimulationAsync(simulationId);
            if (simulation == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Simulation Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"No simulation found with ID {simulationId}"
                });
            }

            var deleted = await _simulationService.DeleteSimulationAsync(simulationId);
            if (!deleted)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Cannot Delete Simulation",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = $"Simulation {simulationId} could not be deleted"
                });
            }

            _logger.LogInformation("Simulation {SimulationId} deleted", simulationId);
            return Ok(new { success = true, message = "Simulation deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete simulation {SimulationId}", simulationId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An error occurred while deleting the simulation"
            });
        }
    }

    /// <summary>
    /// Gets real-time metrics for a simulation
    /// </summary>
    /// <param name="simulationId">ID of the simulation</param>
    /// <returns>Current simulation metrics</returns>
    [HttpGet("{simulationId:guid}/metrics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSimulationMetrics(Guid simulationId)
    {
        try
        {
            var simulation = await _simulationService.GetSimulationAsync(simulationId);
            if (simulation == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Simulation Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"No simulation found with ID {simulationId}"
                });
            }

            var metrics = await _simulationService.GetSimulationMetricsAsync(simulationId);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics for simulation {SimulationId}", simulationId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An error occurred while retrieving simulation metrics"
            });
        }
    }
}