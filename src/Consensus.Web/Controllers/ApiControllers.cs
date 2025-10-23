using Microsoft.AspNetCore.Mvc;
using Consensus.Core.Repositories;
using Consensus.Core.Entities;

namespace Consensus.Web.Controllers;

/// <summary>
/// API controller for managing simulation runs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SimulationsController : ControllerBase
{
    private readonly ISimulationRunRepository _simulationRepository;
    private readonly ILogger<SimulationsController> _logger;

    public SimulationsController(
        ISimulationRunRepository simulationRepository,
        ILogger<SimulationsController> logger)
    {
        _simulationRepository = simulationRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all simulation runs
    /// </summary>
    /// <returns>List of simulation runs</returns>
    /// <response code="200">Returns the list of simulation runs</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SimulationRun>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SimulationRun>>> GetSimulations()
    {
        _logger.LogInformation("Getting all simulation runs");
        var simulations = await _simulationRepository.GetAllAsync();
        return Ok(simulations);
    }

    /// <summary>
    /// Get a specific simulation run by ID
    /// </summary>
    /// <param name="id">Simulation run ID</param>
    /// <returns>Simulation run details</returns>
    /// <response code="200">Returns the simulation run</response>
    /// <response code="404">If the simulation run is not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SimulationRun), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SimulationRun>> GetSimulation(Guid id)
    {
        _logger.LogInformation("Getting simulation run {SimulationId}", id);
        
        var simulation = await _simulationRepository.GetByIdAsync(id);
        if (simulation == null)
        {
            _logger.LogWarning("Simulation run {SimulationId} not found", id);
            return NotFound();
        }

        return Ok(simulation);
    }

    /// <summary>
    /// Get active simulation runs
    /// </summary>
    /// <returns>List of active simulation runs</returns>
    /// <response code="200">Returns the list of active simulation runs</response>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<SimulationRun>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SimulationRun>>> GetActiveSimulations()
    {
        _logger.LogInformation("Getting active simulation runs");
        var simulations = await _simulationRepository.GetActiveAsync();
        return Ok(simulations);
    }

    /// <summary>
    /// Get recent simulation runs
    /// </summary>
    /// <param name="count">Number of recent simulations to return (default: 10)</param>
    /// <returns>List of recent simulation runs</returns>
    /// <response code="200">Returns the list of recent simulation runs</response>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(IEnumerable<SimulationRun>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SimulationRun>>> GetRecentSimulations([FromQuery] int count = 10)
    {
        _logger.LogInformation("Getting {Count} recent simulation runs", count);
        var simulations = await _simulationRepository.GetRecentAsync(count);
        return Ok(simulations);
    }
}

/// <summary>
/// API controller for health checks and system status
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>System health status</returns>
    /// <response code="200">System is healthy</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetHealth()
    {
        _logger.LogInformation("Health check requested");
        
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }

    /// <summary>
    /// Get system information
    /// </summary>
    /// <returns>System information</returns>
    /// <response code="200">Returns system information</response>
    [HttpGet("info")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetSystemInfo()
    {
        _logger.LogInformation("System info requested");
        
        return Ok(new
        {
            application = "Consensus Mechanism Simulator",
            version = "1.0.0",
            framework = ".NET 9.0",
            serverTime = DateTime.UtcNow,
            machineName = Environment.MachineName,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }
}