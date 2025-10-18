using Consensus.Core.Entities;
using Consensus.Core.Enums;

namespace Consensus.Core.Interfaces;

/// <summary>
/// Interface for simulation management services
/// </summary>
public interface ISimulationService
{
    /// <summary>
    /// Starts a new consensus simulation
    /// </summary>
    /// <param name="request">Simulation configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created simulation run</returns>
    Task<SimulationRun> StartSimulationAsync(StartSimulationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops a running simulation
    /// </summary>
    /// <param name="simulationId">ID of the simulation to stop</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StopSimulationAsync(Guid simulationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current status of a simulation
    /// </summary>
    /// <param name="simulationId">ID of the simulation</param>
    /// <returns>Current simulation status</returns>
    Task<SimulationStatus> GetSimulationStatusAsync(Guid simulationId);
    
    /// <summary>
    /// Gets all simulation runs
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of results per page</param>
    /// <returns>Paginated list of simulation runs</returns>
    Task<PaginatedResult<SimulationRun>> GetSimulationRunsAsync(int pageNumber = 1, int pageSize = 20);
    
    /// <summary>
    /// Gets a specific simulation run with its details
    /// </summary>
    /// <param name="simulationId">ID of the simulation</param>
    /// <returns>Simulation run details</returns>
    Task<SimulationRun?> GetSimulationRunAsync(Guid simulationId);
    
    /// <summary>
    /// Gets real-time metrics for an active simulation
    /// </summary>
    /// <param name="simulationId">ID of the simulation</param>
    /// <returns>Current simulation metrics</returns>
    Task<SimulationMetrics> GetSimulationMetricsAsync(Guid simulationId);
    
    /// <summary>
    /// Gets event logs for a simulation
    /// </summary>
    /// <param name="simulationId">ID of the simulation</param>
    /// <param name="level">Minimum log level to retrieve</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of results per page</param>
    /// <returns>Paginated event logs</returns>
    Task<PaginatedResult<EventLog>> GetSimulationLogsAsync(
        Guid simulationId, 
        string? level = null, 
        int pageNumber = 1, 
        int pageSize = 50);
        
    /// <summary>
    /// Event raised when simulation status changes
    /// </summary>
    event EventHandler<SimulationStatusChangedEventArgs> SimulationStatusChanged;
    
    /// <summary>
    /// Event raised when new consensus round completes
    /// </summary>
    event EventHandler<ConsensusRoundCompletedEventArgs> ConsensusRoundCompleted;
}

/// <summary>
/// Request model for starting a simulation
/// </summary>
public record StartSimulationRequest
{
    public required ConsensusAlgorithm Algorithm { get; init; }
    public required int NodeCount { get; init; }
    public required int MaxRounds { get; init; }
    public TimeSpan? MaxDuration { get; init; }
    public Dictionary<string, object> ProtocolConfiguration { get; init; } = new();
    public Dictionary<string, object> NetworkConfiguration { get; init; } = new();
    public int? FaultyNodeCount { get; init; }
    public List<FaultType> FaultTypes { get; init; } = new();
}

/// <summary>
/// Current status of a simulation
/// </summary>
public record SimulationStatus
{
    public Guid Id { get; init; }
    public required SimulationStatus State { get; init; }
    public int CurrentRound { get; init; }
    public int MaxRounds { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public TimeSpan? Duration => CompletedAt?.Subtract(StartedAt) ?? DateTime.UtcNow.Subtract(StartedAt);
    public int ParticipatingNodes { get; init; }
    public int FaultyNodes { get; init; }
    public int BlocksProduced { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Real-time simulation metrics
/// </summary>
public record SimulationMetrics
{
    public double AverageRoundDuration { get; init; }
    public double Throughput { get; init; } // Blocks per second
    public double SuccessRate { get; init; }
    public int TotalTransactions { get; init; }
    public Dictionary<string, object> ProtocolSpecificMetrics { get; init; } = new();
}

/// <summary>
/// Paginated result wrapper
/// </summary>
public record PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = new List<T>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}

/// <summary>
/// Event arguments for simulation status changes
/// </summary>
public class SimulationStatusChangedEventArgs : EventArgs
{
    public required Guid SimulationId { get; init; }
    public required SimulationStatus OldState { get; init; }
    public required SimulationStatus NewState { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Event arguments for consensus round completion
/// </summary>
public class ConsensusRoundCompletedEventArgs : EventArgs
{
    public required Guid SimulationId { get; init; }
    public required ConsensusRound Round { get; init; }
    public required ConsensusResult Result { get; init; }
}