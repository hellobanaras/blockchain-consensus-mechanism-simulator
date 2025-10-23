using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Models;

namespace Consensus.Core.Interfaces;

/// <summary>
/// Interface for the simulation service that manages consensus simulations
/// </summary>
public interface ISimulationService
{
    /// <summary>
    /// Creates a new simulation with the specified parameters
    /// </summary>
    Task<SimulationRun> CreateSimulationAsync(CreateSimulationRequest request);

    /// <summary>
    /// Starts an existing simulation
    /// </summary>
    Task<bool> StartSimulationAsync(Guid simulationId);

    /// <summary>
    /// Stops a running simulation
    /// </summary>
    Task<bool> StopSimulationAsync(Guid simulationId);

    /// <summary>
    /// Gets the current status of a simulation
    /// </summary>
    Task<SimulationRun?> GetSimulationAsync(Guid simulationId);

    /// <summary>
    /// Gets all simulations for a user or all simulations
    /// </summary>
    Task<IEnumerable<SimulationRun>> GetSimulationsAsync();

    /// <summary>
    /// Deletes a simulation and all its associated data
    /// </summary>
    Task<bool> DeleteSimulationAsync(Guid simulationId);

    /// <summary>
    /// Gets real-time metrics for a running simulation
    /// </summary>
    Task<SimulationMetrics?> GetSimulationMetricsAsync(Guid simulationId);

    /// <summary>
    /// Event triggered when simulation status changes
    /// </summary>
    event EventHandler<SimulationStatusChangedEventArgs>? SimulationStatusChanged;

    /// <summary>
    /// Event triggered when a consensus round completes
    /// </summary>
    event EventHandler<RoundCompletedEventArgs>? RoundCompleted;
}