using Consensus.Core.Entities;

namespace Consensus.Core.Repositories;

/// <summary>
/// Generic repository interface for common CRUD operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Get entity by ID
    /// </summary>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get all entities
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Add new entity
    /// </summary>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Update existing entity
    /// </summary>
    Task<T> UpdateAsync(T entity);

    /// <summary>
    /// Delete entity by ID
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Check if entity exists
    /// </summary>
    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    /// Get entities with pagination
    /// </summary>
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
}

/// <summary>
/// Repository interface for SimulationRun entities
/// </summary>
public interface ISimulationRunRepository : IRepository<SimulationRun>
{
    /// <summary>
    /// Get simulation runs by status
    /// </summary>
    Task<IEnumerable<SimulationRun>> GetByStatusAsync(Consensus.Core.Enums.SimulationStatus status);

    /// <summary>
    /// Get simulation runs by consensus algorithm
    /// </summary>
    Task<IEnumerable<SimulationRun>> GetByAlgorithmAsync(Consensus.Core.Enums.ConsensusAlgorithm algorithm);

    /// <summary>
    /// Get active simulation runs
    /// </summary>
    Task<IEnumerable<SimulationRun>> GetActiveAsync();

    /// <summary>
    /// Get recent simulation runs
    /// </summary>
    Task<IEnumerable<SimulationRun>> GetRecentAsync(int count = 10);
}

/// <summary>
/// Repository interface for Node entities
/// </summary>
public interface INodeRepository : IRepository<Node>
{
    /// <summary>
    /// Get nodes by simulation run ID
    /// </summary>
    Task<IEnumerable<Node>> GetBySimulationRunAsync(Guid simulationRunId);

    /// <summary>
    /// Get nodes by status
    /// </summary>
    Task<IEnumerable<Node>> GetByStatusAsync(Consensus.Core.Enums.NodeStatus status);

    /// <summary>
    /// Get nodes by consensus algorithm
    /// </summary>
    Task<IEnumerable<Node>> GetByAlgorithmAsync(Consensus.Core.Enums.ConsensusAlgorithm algorithm);

    /// <summary>
    /// Get active nodes for a simulation
    /// </summary>
    Task<IEnumerable<Node>> GetActiveNodesAsync(Guid simulationRunId);
}

/// <summary>
/// Repository interface for Block entities
/// </summary>
public interface IBlockRepository : IRepository<Block>
{
    /// <summary>
    /// Get blocks by simulation run ID
    /// </summary>
    Task<IEnumerable<Block>> GetBySimulationRunAsync(Guid simulationRunId);

    /// <summary>
    /// Get blockchain for a simulation run
    /// </summary>
    Task<IEnumerable<Block>> GetBlockchainAsync(Guid simulationRunId);

    /// <summary>
    /// Get latest block for simulation
    /// </summary>
    Task<Block?> GetLatestBlockAsync(Guid simulationRunId);

    /// <summary>
    /// Get block by hash
    /// </summary>
    Task<Block?> GetByHashAsync(string hash);

    /// <summary>
    /// Get blocks by proposer
    /// </summary>
    Task<IEnumerable<Block>> GetByProposerAsync(Guid proposerId);
}

/// <summary>
/// Repository interface for ConsensusRound entities
/// </summary>
public interface IConsensusRoundRepository : IRepository<ConsensusRound>
{
    /// <summary>
    /// Get rounds by simulation run ID
    /// </summary>
    Task<IEnumerable<ConsensusRound>> GetBySimulationRunAsync(Guid simulationRunId);

    /// <summary>
    /// Get rounds by status
    /// </summary>
    Task<IEnumerable<ConsensusRound>> GetByStatusAsync(Consensus.Core.Enums.ConsensusRoundStatus status);

    /// <summary>
    /// Get current active round for simulation
    /// </summary>
    Task<ConsensusRound?> GetActiveRoundAsync(Guid simulationRunId);

    /// <summary>
    /// Get latest round for simulation
    /// </summary>
    Task<ConsensusRound?> GetLatestRoundAsync(Guid simulationRunId);
}

/// <summary>
/// Repository interface for EventLog entities
/// </summary>
public interface IEventLogRepository : IRepository<EventLog>
{
    /// <summary>
    /// Get events by simulation run ID
    /// </summary>
    Task<IEnumerable<EventLog>> GetBySimulationRunAsync(Guid simulationRunId);

    /// <summary>
    /// Get events by type
    /// </summary>
    Task<IEnumerable<EventLog>> GetByEventTypeAsync(string eventType);

    /// <summary>
    /// Get events by level
    /// </summary>
    Task<IEnumerable<EventLog>> GetByLevelAsync(string level);

    /// <summary>
    /// Get events for a time range
    /// </summary>
    Task<IEnumerable<EventLog>> GetByTimeRangeAsync(DateTime startTime, DateTime endTime);

    /// <summary>
    /// Get recent events
    /// </summary>
    Task<IEnumerable<EventLog>> GetRecentAsync(int count = 100);
}