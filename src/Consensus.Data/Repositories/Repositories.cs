using Consensus.Core.Entities;
using Consensus.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Consensus.Data.Repositories;

/// <summary>
/// Generic repository implementation
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ConsensusDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ConsensusDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        return await _dbSet.FindAsync(id) != null;
    }

    public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var totalCount = await _dbSet.CountAsync();
        var items = await _dbSet
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}

/// <summary>
/// Repository implementation for SimulationRun entities
/// </summary>
public class SimulationRunRepository : Repository<SimulationRun>, ISimulationRunRepository
{
    public SimulationRunRepository(ConsensusDbContext context) : base(context) { }

    public async Task<IEnumerable<SimulationRun>> GetByStatusAsync(Consensus.Core.Enums.SimulationStatus status)
    {
        return await _dbSet
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SimulationRun>> GetByAlgorithmAsync(Consensus.Core.Enums.ConsensusAlgorithm algorithm)
    {
        return await _dbSet
            .Where(s => s.ConsensusAlgorithm == algorithm)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SimulationRun>> GetActiveAsync()
    {
        return await _dbSet
            .Where(s => s.Status == Consensus.Core.Enums.SimulationStatus.Running || 
                       s.Status == Consensus.Core.Enums.SimulationStatus.Paused)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SimulationRun>> GetRecentAsync(int count = 10)
    {
        return await _dbSet
            .OrderByDescending(s => s.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}

/// <summary>
/// Repository implementation for Node entities
/// </summary>
public class NodeRepository : Repository<Node>, INodeRepository
{
    public NodeRepository(ConsensusDbContext context) : base(context) { }

    public async Task<IEnumerable<Node>> GetBySimulationRunAsync(Guid simulationRunId)
    {
        return await _dbSet
            .Where(n => n.SimulationRunId == simulationRunId)
            .OrderBy(n => n.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Node>> GetByStatusAsync(Consensus.Core.Enums.NodeStatus status)
    {
        return await _dbSet
            .Where(n => n.Status == status)
            .OrderBy(n => n.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Node>> GetByAlgorithmAsync(Consensus.Core.Enums.ConsensusAlgorithm algorithm)
    {
        return await _dbSet
            .Where(n => n.ConsensusAlgorithm == algorithm)
            .OrderBy(n => n.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Node>> GetActiveNodesAsync(Guid simulationRunId)
    {
        return await _dbSet
            .Where(n => n.SimulationRunId == simulationRunId && 
                       n.IsActive && 
                       n.Status == Consensus.Core.Enums.NodeStatus.Online)
            .OrderBy(n => n.Name)
            .ToListAsync();
    }
}

/// <summary>
/// Repository implementation for ConsensusRound entities
/// </summary>
public class ConsensusRoundRepository : Repository<ConsensusRound>, IConsensusRoundRepository
{
    public ConsensusRoundRepository(ConsensusDbContext context) : base(context) { }

    public async Task<IEnumerable<ConsensusRound>> GetBySimulationRunAsync(Guid simulationRunId)
    {
        return await _dbSet
            .Where(r => r.SimulationRunId == simulationRunId)
            .OrderBy(r => r.RoundNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConsensusRound>> GetByStatusAsync(Consensus.Core.Enums.ConsensusRoundStatus status)
    {
        return await _dbSet
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync();
    }

    public async Task<ConsensusRound?> GetActiveRoundAsync(Guid simulationRunId)
    {
        return await _dbSet
            .Where(r => r.SimulationRunId == simulationRunId && 
                       r.Status == Consensus.Core.Enums.ConsensusRoundStatus.InProgress)
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<ConsensusRound?> GetLatestRoundAsync(Guid simulationRunId)
    {
        return await _dbSet
            .Where(r => r.SimulationRunId == simulationRunId)
            .OrderByDescending(r => r.RoundNumber)
            .FirstOrDefaultAsync();
    }
}

/// <summary>
/// Repository implementation for EventLog entities
/// </summary>
public class EventLogRepository : Repository<EventLog>, IEventLogRepository
{
    public EventLogRepository(ConsensusDbContext context) : base(context) { }

    public async Task<IEnumerable<EventLog>> GetBySimulationRunAsync(Guid simulationRunId)
    {
        return await _dbSet
            .Where(e => e.SimulationRunId == simulationRunId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<EventLog>> GetByEventTypeAsync(string eventType)
    {
        return await _dbSet
            .Where(e => e.EventType == eventType)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<EventLog>> GetByLevelAsync(string level)
    {
        return await _dbSet
            .Where(e => e.Level == level)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<EventLog>> GetByTimeRangeAsync(DateTime startTime, DateTime endTime)
    {
        return await _dbSet
            .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<EventLog>> GetRecentAsync(int count = 100)
    {
        return await _dbSet
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToListAsync();
    }
}