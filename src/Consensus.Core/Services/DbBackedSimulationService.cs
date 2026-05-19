using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Interfaces;
using Consensus.Core.Models;
using Consensus.Core.Repositories;
using Microsoft.Extensions.Logging;
using SimulationStatus = Consensus.Core.Enums.SimulationStatus;

namespace Consensus.Core.Services;

/// <summary>
/// Read-only ISimulationService implementation that serves data straight from the
/// persistence layer instead of holding an in-memory runtime. The Api host uses
/// this so its read endpoints return DB-backed truth; the Web host continues to
/// register the runtime-bearing <see cref="SimulationService"/> for orchestration.
///
/// Write/lifecycle operations deliberately throw — the Api is not the place to
/// start or stop a simulation; that responsibility lives with the Web host.
/// </summary>
public class DbBackedSimulationService : ISimulationService
{
    private readonly ISimulationRunRepository _simulationRepo;
    private readonly INodeRepository _nodeRepo;
    private readonly IBlockRepository _blockRepo;
    private readonly IConsensusRoundRepository _roundRepo;
    private readonly ILogger<DbBackedSimulationService> _logger;

    public DbBackedSimulationService(
        ISimulationRunRepository simulationRepo,
        INodeRepository nodeRepo,
        IBlockRepository blockRepo,
        IConsensusRoundRepository roundRepo,
        ILogger<DbBackedSimulationService> logger)
    {
        _simulationRepo = simulationRepo;
        _nodeRepo = nodeRepo;
        _blockRepo = blockRepo;
        _roundRepo = roundRepo;
        _logger = logger;
    }

    public event EventHandler<SimulationStatusChangedEventArgs>? SimulationStatusChanged;
    public event EventHandler<RoundCompletedEventArgs>? RoundCompleted;

    public Task<SimulationRun> CreateSimulationAsync(CreateSimulationRequest request)
        => throw new NotSupportedException(
            "Simulations cannot be created against the Api host. Use the Web host (Blazor UI) which owns the runtime.");

    public Task<bool> StartSimulationAsync(Guid simulationId)
        => throw new NotSupportedException("Start is handled by the Web host.");

    public Task<bool> StopSimulationAsync(Guid simulationId)
        => throw new NotSupportedException("Stop is handled by the Web host.");

    public Task<bool> DeleteSimulationAsync(Guid simulationId)
        => throw new NotSupportedException("Delete is handled by the Web host.");

    public async Task<SimulationRun?> GetSimulationAsync(Guid simulationId)
    {
        var sim = await _simulationRepo.GetByIdAsync(simulationId);
        if (sim == null) return null;

        // Hydrate the most important navigation collections so the response is
        // useful to a single-request consumer (UI dashboard, analytics page).
        sim.Nodes = (await _nodeRepo.GetBySimulationRunAsync(simulationId)).ToList();
        sim.Blocks = (await _blockRepo.GetBySimulationRunAsync(simulationId)).ToList();
        sim.ConsensusRounds = (await _roundRepo.GetBySimulationRunAsync(simulationId)).ToList();
        return sim;
    }

    public async Task<IEnumerable<SimulationRun>> GetSimulationsAsync()
    {
        var recent = await _simulationRepo.GetRecentAsync(100);
        return recent.OrderByDescending(s => s.CreatedAt);
    }

    public async Task<SimulationMetrics?> GetSimulationMetricsAsync(Guid simulationId)
    {
        var sim = await _simulationRepo.GetByIdAsync(simulationId);
        if (sim == null) return null;

        var nodes = (await _nodeRepo.GetBySimulationRunAsync(simulationId)).ToList();
        var rounds = (await _roundRepo.GetBySimulationRunAsync(simulationId)).ToList();
        var blocks = (await _blockRepo.GetBySimulationRunAsync(simulationId)).ToList();

        // Derive a coarse SimulationMetrics from the persisted rows. Protocol-level
        // detail (energy proxy, view changes, etc.) is intentionally absent here —
        // those live on the protocol's GetMetrics() which the Web host owns.
        double meanBlockTimeMs = 0;
        if (blocks.Count > 1)
        {
            var ordered = blocks.OrderBy(b => b.Timestamp).ToList();
            var diffs = ordered
                .Zip(ordered.Skip(1), (a, b) => (b.Timestamp - a.Timestamp).TotalMilliseconds)
                .Where(d => d >= 0)
                .ToList();
            if (diffs.Count > 0) meanBlockTimeMs = diffs.Average();
        }

        var simDurationSeconds = (sim.CompletedAt ?? DateTime.UtcNow) - (sim.StartedAt ?? sim.CreatedAt);
        var totalTx = blocks.Sum(b => b.TransactionCount);
        var tps = simDurationSeconds.TotalSeconds > 0
            ? totalTx / simDurationSeconds.TotalSeconds
            : 0;

        return new SimulationMetrics
        {
            TotalNodes = nodes.Count > 0 ? nodes.Count : sim.NodeCount,
            ActiveNodes = nodes.Count(n => n.Status == NodeStatus.Online),
            ConsensusRounds = rounds.Count,
            TotalBlocks = blocks.Count,
            AverageBlockTime = meanBlockTimeMs,
            TotalTransactions = totalTx,
            NetworkLatency = sim.NetworkLatencyMs,
            FaultTolerance = 0, // Protocol-specific; computed by the runtime, not derivable from rows alone.
            ThroughputTps = tps
        };
    }
}
