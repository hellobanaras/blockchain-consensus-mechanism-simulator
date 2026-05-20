using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Interfaces;
using Consensus.Core.Models;
using Consensus.Core.Protocols;
using Consensus.Core.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimulationStatus = Consensus.Core.Enums.SimulationStatus;

namespace Consensus.Core.Services;

/// <summary>
/// Core simulation service implementation
/// </summary>
public class SimulationService : ISimulationService
{
    private readonly ILogger<SimulationService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly bool _persistToDb;
    private readonly Dictionary<Guid, SimulationRuntime> _activeSimulations;
    private readonly object _lock = new();

    public SimulationService(
        ILogger<SimulationService> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        // Read feature flag without taking a dependency on Configuration.Binder. Defaults to true.
        _persistToDb = !bool.TryParse(configuration["Simulation:PersistToDb"], out var flag) || flag;
        _activeSimulations = new Dictionary<Guid, SimulationRuntime>();
    }

    public event EventHandler<SimulationStatusChangedEventArgs>? SimulationStatusChanged;
    public event EventHandler<RoundCompletedEventArgs>? RoundCompleted;

    public async Task<SimulationRun> CreateSimulationAsync(CreateSimulationRequest request)
    {
        _logger.LogInformation("Creating new simulation: {Name} with {Algorithm}", 
            request.Name, request.Algorithm);

        try
        {
            // Validate the request
            var validationErrors = request.Validate();
            if (validationErrors.Any())
            {
                throw new ArgumentException($"Invalid simulation parameters: {string.Join(", ", validationErrors)}");
            }

            // Create the simulation entity
            var simulation = new SimulationRun
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                ConsensusAlgorithm = request.Algorithm,
                NodeCount = request.NodeCount,
                ByzantineNodeCount = request.ByzantineNodeCount,
                DurationSeconds = request.DurationSeconds,
                NetworkTopology = request.NetworkTopology,
                BlockTimeMs = request.BlockTimeMs,
                TransactionsPerBlock = request.TransactionsPerBlock,
                NetworkLatencyMs = request.NetworkLatencyMs,
                RandomSeed = request.RandomSeed,
                MaxRounds = request.MaxRounds,
                Status = SimulationStatus.Initializing,
                CreatedAt = DateTime.UtcNow,
                Configuration = request.AlgorithmConfiguration
            };

            // One seeded RNG per simulation — shared across node-power and protocol leader selection
            // so the same seed yields identical traces.
            var rng = request.RandomSeed.HasValue ? new Random(request.RandomSeed.Value) : Random.Shared;

            // Create nodes for the simulation
            simulation.Nodes = await CreateNodesAsync(request, rng);

            // Initialize the consensus protocol
            var protocol = CreateConsensusProtocol(request.Algorithm);
            protocol.SetRandom(rng);
            await protocol.InitializeAsync(simulation.Nodes, request.AlgorithmConfiguration);

            // Create the simulation runtime
            var runtime = new SimulationRuntime
            {
                Simulation = simulation,
                Protocol = protocol,
                CancellationTokenSource = new CancellationTokenSource()
            };

            lock (_lock)
            {
                _activeSimulations[simulation.Id] = runtime;
            }

            simulation.Status = SimulationStatus.Ready;

            // Persist simulation + nodes so post-run queries (export, dashboard refresh) have data.
            // Best-effort: a DB failure must not break the live in-memory run.
            await PersistSimulationCreatedAsync(simulation);

            OnSimulationStatusChanged(simulation);

            _logger.LogInformation("Simulation {SimulationId} created successfully", simulation.Id);
            return simulation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create simulation: {Name}", request.Name);
            throw;
        }
    }

    public async Task<bool> StartSimulationAsync(Guid simulationId)
    {
        _logger.LogInformation("Starting simulation {SimulationId}", simulationId);

        try
        {
            lock (_lock)
            {
                if (!_activeSimulations.TryGetValue(simulationId, out var runtime))
                {
                    _logger.LogWarning("Simulation {SimulationId} not found", simulationId);
                    return false;
                }

                if (runtime.Simulation.Status != SimulationStatus.Ready)
                {
                    _logger.LogWarning("Simulation {SimulationId} is not in Ready status: {Status}", 
                        simulationId, runtime.Simulation.Status);
                    return false;
                }

                runtime.Simulation.Status = SimulationStatus.Running;
                runtime.Simulation.StartedAt = DateTime.UtcNow;
            }

            // Persist the Running status to the DB so the dashboard counter
            // cards (Running / Completed / Failed) reflect reality during a
            // live run. Without this, the DB row stayed at Ready throughout
            // and the live run never appeared in any counter bucket until
            // PersistSimulationCompletedAsync fired at the very end.
            await PersistSimulationStatusAsync(simulationId);

            // Start the simulation execution in the background
            _ = Task.Run(() => ExecuteSimulationAsync(simulationId));

            OnSimulationStatusChanged(GetSimulationRuntime(simulationId)?.Simulation!);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start simulation {SimulationId}", simulationId);
            return false;
        }
    }

    public async Task<bool> StopSimulationAsync(Guid simulationId)
    {
        _logger.LogInformation("Stopping simulation {SimulationId}", simulationId);

        try
        {
            var runtime = GetSimulationRuntime(simulationId);
            if (runtime == null)
            {
                return false;
            }

            runtime.CancellationTokenSource.Cancel();
            runtime.Simulation.Status = SimulationStatus.Stopped;
            runtime.Simulation.CompletedAt = DateTime.UtcNow;

            OnSimulationStatusChanged(runtime.Simulation);
            
            _logger.LogInformation("Simulation {SimulationId} stopped", simulationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop simulation {SimulationId}", simulationId);
            return false;
        }
    }

    public async Task<SimulationRun?> GetSimulationAsync(Guid simulationId)
    {
        var runtime = GetSimulationRuntime(simulationId);
        return runtime?.Simulation;
    }

    public async Task<IEnumerable<SimulationRun>> GetSimulationsAsync()
    {
        lock (_lock)
        {
            return _activeSimulations.Values.Select(r => r.Simulation).ToList();
        }
    }

    public async Task<bool> DeleteSimulationAsync(Guid simulationId)
    {
        _logger.LogInformation("Deleting simulation {SimulationId}", simulationId);

        try
        {
            var runtime = GetSimulationRuntime(simulationId);
            if (runtime == null)
            {
                return false;
            }

            // Stop the simulation if it's running
            if (runtime.Simulation.Status == SimulationStatus.Running)
            {
                await StopSimulationAsync(simulationId);
            }

            // Cleanup the protocol
            if (runtime.Protocol is IDisposable disposableProtocol)
            {
                disposableProtocol.Dispose();
            }

            // Remove from active simulations
            lock (_lock)
            {
                _activeSimulations.Remove(simulationId);
            }

            _logger.LogInformation("Simulation {SimulationId} deleted", simulationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete simulation {SimulationId}", simulationId);
            return false;
        }
    }

    public async Task<SimulationMetrics?> GetSimulationMetricsAsync(Guid simulationId)
    {
        var runtime = GetSimulationRuntime(simulationId);
        if (runtime?.Protocol == null)
        {
            return null;
        }

        var protocolMetrics = runtime.Protocol.GetMetrics();
        var simulation = runtime.Simulation;

        return new SimulationMetrics
        {
            TotalNodes = simulation.NodeCount,
            ActiveNodes = simulation.Nodes.Count(n => n.Status == NodeStatus.Online),
            ConsensusRounds = simulation.ConsensusRounds?.Count ?? 0,
            TotalBlocks = simulation.Blocks?.Count ?? 0,
            AverageBlockTime = protocolMetrics.TryGetValue("averageBlockTime", out var blockTime)
                ? Convert.ToDouble(blockTime) : 0,
            TotalTransactions = simulation.TotalTransactions,
            NetworkLatency = simulation.NetworkLatencyMs,
            FaultTolerance = protocolMetrics.TryGetValue("faultTolerance", out var ft) 
                ? Convert.ToDouble(ft) : 0,
            ThroughputTps = protocolMetrics.TryGetValue("throughputTps", out var tps) 
                ? Convert.ToDouble(tps) : 0
        };
    }

    private async Task ExecuteSimulationAsync(Guid simulationId)
    {
        var runtime = GetSimulationRuntime(simulationId);
        if (runtime == null)
        {
            _logger.LogWarning("No runtime found for simulation {SimulationId}", simulationId);
            return;
        }

        try
        {
            var simulation = runtime.Simulation;
            var protocol = runtime.Protocol;
            var cancellationToken = runtime.CancellationTokenSource.Token;

            _logger.LogInformation("Starting simulation {SimulationId} with {NodeCount} nodes for {MaxRounds} rounds", 
                simulationId, simulation.NodeCount, simulation.MaxRounds);

            // Update simulation status
            simulation.Status = SimulationStatus.Running;
            simulation.StartedAt = DateTime.UtcNow;
            OnSimulationStatusChanged(simulation);

            // Create genesis block if this is the first block
            if (simulation.Blocks == null || !simulation.Blocks.Any())
            {
                var genesisBlock = await CreateGenesisBlock(simulation);
                simulation.Blocks = new List<Block> { genesisBlock };
                _logger.LogInformation("Created genesis block {BlockHash} for simulation {SimulationId}", 
                    genesisBlock.Hash, simulationId);
            }

            var roundNumber = 1L;
            var maxRounds = simulation.MaxRounds ?? 100; // Default to 100 rounds if not specified
            var blockTime = simulation.BlockTimeMs > 0 ? simulation.BlockTimeMs : 2000; // Default 2 seconds

            while (roundNumber <= maxRounds && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Starting round {RoundNumber}/{MaxRounds} for simulation {SimulationId}", 
                    roundNumber, maxRounds, simulationId);

                // Create a new consensus round
                var round = new ConsensusRound
                {
                    Id = Guid.NewGuid(),
                    RoundNumber = roundNumber,
                    SimulationRunId = simulation.Id,
                    Algorithm = simulation.ConsensusAlgorithm,
                    ParticipatingNodes = simulation.Nodes.Count(n => !n.IsByzantine && n.Status == NodeStatus.Online),
                    StartedAt = DateTime.UtcNow,
                    Status = ConsensusRoundStatus.InProgress
                };

                try
                {
                    // Execute the consensus round
                    var result = await protocol.ExecuteRoundAsync(round, cancellationToken);
                    
                    // Update round status
                    round.Status = result.Success ? ConsensusRoundStatus.Completed : ConsensusRoundStatus.Failed;
                    round.CompletedAt = DateTime.UtcNow;
                    round.LeaderId = result.LeaderId != null ? Guid.Parse(result.LeaderId) : null;

                    // Create a block if consensus was successful
                    Block? newBlock = null;
                    if (result.Success && result.LeaderId != null)
                    {
                        newBlock = await CreateBlock(simulation, round, result);
                        if (newBlock != null)
                        {
                            simulation.Blocks.Add(newBlock);
                            _logger.LogInformation("Created block {BlockNumber} with hash {BlockHash} by leader {LeaderId}", 
                                newBlock.BlockNumber, newBlock.Hash, result.LeaderId);
                        }
                    }

                    // Add the round to simulation
                    simulation.ConsensusRounds ??= new List<ConsensusRound>();
                    simulation.ConsensusRounds.Add(round);

                    // Update simulation progress
                    simulation.Progress = (double)roundNumber / maxRounds;

                    // Persist round + block + event log row (best-effort, see PersistRoundResultAsync)
                    await PersistRoundResultAsync(simulation, round, newBlock, result);

                    // Notify subscribers about round completion
                    OnRoundCompleted(simulation, round, result);

                    _logger.LogDebug("Round {RoundNumber} completed for simulation {SimulationId}: {Success} " +
                                   "(Leader: {LeaderId}, Block: {HasBlock})", 
                        roundNumber, simulationId, result.Success, result.LeaderId, newBlock != null);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Round {RoundNumber} cancelled for simulation {SimulationId}", 
                        roundNumber, simulationId);
                    round.Status = ConsensusRoundStatus.Cancelled;
                    round.CompletedAt = DateTime.UtcNow;
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in round {RoundNumber} for simulation {SimulationId}", 
                        roundNumber, simulationId);
                    
                    round.Status = ConsensusRoundStatus.Failed;
                    round.CompletedAt = DateTime.UtcNow;
                    round.ErrorMessage = ex.Message;

                    // Add failed round to simulation
                    simulation.ConsensusRounds ??= new List<ConsensusRound>();
                    simulation.ConsensusRounds.Add(round);

                    // Continue with next round unless it's a critical error
                    if (ex is InvalidOperationException || ex is ArgumentException)
                    {
                        _logger.LogError("Critical error in simulation {SimulationId}, stopping execution", simulationId);
                        break;
                    }
                }

                roundNumber++;

                // Wait for the next round (simulate block time)
                if (roundNumber <= maxRounds && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(blockTime, cancellationToken);
                }
            }

            // Mark simulation as completed
            simulation.Status = cancellationToken.IsCancellationRequested 
                ? SimulationStatus.Stopped 
                : SimulationStatus.Completed;
            simulation.CompletedAt = DateTime.UtcNow;
            simulation.Progress = 1.0;

            // Calculate final metrics
            var successfulRounds = simulation.ConsensusRounds?.Count(r => r.Status == ConsensusRoundStatus.Completed) ?? 0;
            var totalRounds = simulation.ConsensusRounds?.Count ?? 0;
            var finalBlockHeight = simulation.Blocks?.Count ?? 0;

            _logger.LogInformation("Simulation {SimulationId} completed: {Status}, " +
                                 "Successful rounds: {SuccessfulRounds}/{TotalRounds}, " +
                                 "Final block height: {BlockHeight}",
                simulationId, simulation.Status, successfulRounds, totalRounds, finalBlockHeight);

            // Persist final simulation state so exports reflect Completed/Stopped status.
            await PersistSimulationCompletedAsync(simulation);

            OnSimulationStatusChanged(simulation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in simulation {SimulationId}", simulationId);
            
            var simulation = runtime.Simulation;
            simulation.Status = SimulationStatus.Failed;
            simulation.CompletedAt = DateTime.UtcNow;
            simulation.ErrorMessage = ex.Message;
            
            OnSimulationStatusChanged(simulation);
        }
        finally
        {
            // Clean up runtime
            lock (_lock)
            {
                if (_activeSimulations.ContainsKey(simulationId))
                {
                    runtime.CancellationTokenSource.Dispose();
                    _activeSimulations.Remove(simulationId);
                }
            }
        }
    }

    private async Task<Block> CreateGenesisBlock(SimulationRun simulation)
    {
        var genesisBlock = new Block
        {
            Id = Guid.NewGuid(),
            BlockNumber = 0,
            Hash = GenerateBlockHash("GENESIS", null, DateTime.UtcNow, new Dictionary<string, object>()),
            PreviousHash = null,
            SimulationRunId = simulation.Id,
            Timestamp = DateTime.UtcNow,
            ProposerId = null, // Genesis has no proposer
            Data = new Dictionary<string, object>
            {
                { "genesis", true },
                { "protocol", simulation.ConsensusAlgorithm.ToString() },
                { "nodeCount", simulation.NodeCount },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            },
            Nonce = 0,
            Difficulty = 1,
            Size = 512, // Genesis block size
            TransactionCount = 0
        };

        return await Task.FromResult(genesisBlock);
    }

    private async Task<Block?> CreateBlock(SimulationRun simulation, ConsensusRound round, ConsensusResult result)
    {
        try
        {
            var previousBlock = simulation.Blocks.LastOrDefault();
            if (previousBlock == null)
            {
                _logger.LogError("Cannot create block: no previous block found for simulation {SimulationId}", simulation.Id);
                return null;
            }

            var blockData = new Dictionary<string, object>
            {
                { "round", round.RoundNumber },
                { "algorithm", simulation.ConsensusAlgorithm.ToString() },
                { "leader", result.LeaderId ?? "unknown" },
                { "participatingNodes", result.ParticipatingNodes },
                { "consensusDuration", result.Duration.TotalMilliseconds },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            };

            // Add protocol-specific metrics
            foreach (var metric in result.Metrics)
            {
                blockData[$"metric_{metric.Key}"] = metric.Value;
            }

            var newBlock = new Block
            {
                Id = Guid.NewGuid(),
                BlockNumber = previousBlock.BlockNumber + 1,
                PreviousHash = previousBlock.Hash,
                SimulationRunId = simulation.Id,
                Timestamp = DateTime.UtcNow,
                ProposerId = result.LeaderId != null ? Guid.Parse(result.LeaderId) : null,
                Data = blockData,
                Nonce = (int)(round.RoundNumber % int.MaxValue),
                Difficulty = 1, // Simplified for simulation
                Size = 1024 + (blockData.Count * 50), // Estimated size
                TransactionCount = 1 // One consensus transaction per block
            };

            // Generate block hash
            newBlock.Hash = GenerateBlockHash(
                newBlock.BlockNumber.ToString(),
                newBlock.PreviousHash,
                newBlock.Timestamp,
                newBlock.Data
            );

            return await Task.FromResult(newBlock);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating block for round {RoundNumber} in simulation {SimulationId}", 
                round.RoundNumber, simulation.Id);
            return null;
        }
    }

    private string GenerateBlockHash(string blockData, string? previousHash, DateTime timestamp, Dictionary<string, object> data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        
        var combined = $"{blockData}:{previousHash ?? ""}:{timestamp:O}:{string.Join(",", data.Select(kv => $"{kv.Key}={kv.Value}"))}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(combined);
        var hashBytes = sha256.ComputeHash(bytes);
        
        return Convert.ToHexString(hashBytes).ToLower();
    }

    private async Task<List<Node>> CreateNodesAsync(CreateSimulationRequest request, Random rng)
    {
        var nodes = new List<Node>();

        // PoS / DPoS protocols filter out nodes whose StakeAmount falls
        // below the configured minimum (100 in PosProtocol). Default
        // Node.StakeAmount is 0, so a PoS sim with otherwise-valid inputs
        // failed at InitializeAsync with "PoS requires at least 3 nodes
        // with minimum stake of 100". Seed stake so newly-created PoS/DPoS
        // sims pass that gate by default; the values are deterministic per
        // seed so reproducibility holds.
        var needsStake = request.Algorithm == ConsensusAlgorithm.ProofOfStake
                         || request.Algorithm == ConsensusAlgorithm.DelegatedProofOfStake;

        for (int i = 0; i < request.NodeCount; i++)
        {
            var node = new Node
            {
                Id = Guid.NewGuid(),
                Name = $"Node_{i + 1}",
                Status = NodeStatus.Online,
                ConsensusAlgorithm = request.Algorithm,
                IsActive = true,
                IsByzantine = i < request.ByzantineNodeCount, // First N nodes are Byzantine
                ComputationalPower = rng.Next(80, 120), // Vary slightly for realism
                ReputationScore = 100m,
                // Range 100–600 keeps every node above the 100-coin minimum
                // and gives the weighted-lottery enough spread to be visibly
                // weighted (not uniform).
                StakeAmount = needsStake ? rng.Next(100, 601) : 0m,
                CreatedAt = DateTime.UtcNow
            };

            nodes.Add(node);
        }

        return await Task.FromResult(nodes);
    }

    private IConsensusProtocol CreateConsensusProtocol(ConsensusAlgorithm algorithm)
    {
        return algorithm switch
        {
            ConsensusAlgorithm.ProofOfWork => new PowProtocol(_logger as ILogger<PowProtocol> ?? 
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<PowProtocol>()),
            ConsensusAlgorithm.ProofOfStake => new PosProtocol(_logger as ILogger<PosProtocol> ?? 
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<PosProtocol>()),
            ConsensusAlgorithm.DelegatedProofOfStake => new DposProtocol(_logger as ILogger<DposProtocol> ?? 
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<DposProtocol>()),
            ConsensusAlgorithm.PracticalByzantineFaultTolerance => new PbftProtocol(_logger as ILogger<PbftProtocol> ?? 
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<PbftProtocol>()),
            ConsensusAlgorithm.ProofOfElapsedTime => new PoetProtocol(_logger as ILogger<PoetProtocol> ?? 
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<PoetProtocol>()),
            _ => throw new NotSupportedException($"Consensus algorithm {algorithm} is not yet implemented")
        };
    }

    private SimulationRuntime? GetSimulationRuntime(Guid simulationId)
    {
        lock (_lock)
        {
            return _activeSimulations.TryGetValue(simulationId, out var runtime) ? runtime : null;
        }
    }

    private void OnSimulationStatusChanged(SimulationRun simulation)
    {
        try
        {
            SimulationStatusChanged?.Invoke(this, new SimulationStatusChangedEventArgs(simulation));
            _logger.LogDebug("Simulation status changed event fired for {SimulationId}: {Status}", 
                simulation.Id, simulation.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firing simulation status changed event for {SimulationId}", simulation.Id);
        }
    }

    private void OnRoundCompleted(SimulationRun simulation, ConsensusRound round, ConsensusResult result)
    {
        try
        {
            RoundCompleted?.Invoke(this, new RoundCompletedEventArgs(simulation, round, result));
            _logger.LogDebug("Round completed event fired for simulation {SimulationId}, round {RoundNumber}",
                simulation.Id, round.RoundNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firing round completed event for simulation {SimulationId}, round {RoundNumber}",
                simulation.Id, round.RoundNumber);
        }
    }

    private async Task PersistSimulationCreatedAsync(SimulationRun simulation)
    {
        if (!_persistToDb) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var simRepo = scope.ServiceProvider.GetRequiredService<ISimulationRunRepository>();
            var nodeRepo = scope.ServiceProvider.GetRequiredService<INodeRepository>();

            // Detach Nodes before saving the parent to avoid cascade-insert collisions; persist them explicitly.
            var nodes = simulation.Nodes?.ToList() ?? new List<Node>();
            simulation.Nodes = new List<Node>();

            await simRepo.AddAsync(simulation);
            foreach (var node in nodes)
            {
                node.SimulationRunId = simulation.Id;
                await nodeRepo.AddAsync(node);
            }

            simulation.Nodes = nodes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Persistence of simulation {SimulationId} failed; continuing with in-memory state",
                simulation.Id);
        }
    }

    private async Task PersistRoundResultAsync(SimulationRun simulation, ConsensusRound round, Block? newBlock, ConsensusResult result)
    {
        if (!_persistToDb) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var roundRepo = scope.ServiceProvider.GetRequiredService<IConsensusRoundRepository>();
            var blockRepo = scope.ServiceProvider.GetRequiredService<IBlockRepository>();
            var eventRepo = scope.ServiceProvider.GetRequiredService<IEventLogRepository>();

            await roundRepo.AddAsync(round);

            if (newBlock != null)
            {
                await blockRepo.AddAsync(newBlock);
            }

            var eventLog = EventLog.Info(
                simulation.Id,
                "RoundCompleted",
                $"Round {round.RoundNumber} {(result.Success ? "succeeded" : "failed")} (leader={result.LeaderId ?? "n/a"})",
                result.Metrics);
            eventLog.ConsensusRoundId = round.Id;
            eventLog.BlockId = newBlock?.Id;
            eventLog.Source = nameof(SimulationService);
            await eventRepo.AddAsync(eventLog);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Persistence of round {RoundNumber} for simulation {SimulationId} failed",
                round.RoundNumber, simulation.Id);
        }
    }

    private async Task PersistSimulationStatusAsync(Guid simulationId)
    {
        if (!_persistToDb) return;

        try
        {
            var runtime = GetSimulationRuntime(simulationId);
            if (runtime == null) return;

            using var scope = _scopeFactory.CreateScope();
            var simRepo = scope.ServiceProvider.GetRequiredService<ISimulationRunRepository>();
            var stored = await simRepo.GetByIdAsync(simulationId);
            if (stored == null) return;

            stored.Status = runtime.Simulation.Status;
            stored.StartedAt = runtime.Simulation.StartedAt;
            stored.UpdatedAt = DateTime.UtcNow;
            await simRepo.UpdateAsync(stored);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Status persistence update for simulation {SimulationId} failed", simulationId);
        }
    }

    private async Task PersistSimulationCompletedAsync(SimulationRun simulation)
    {
        if (!_persistToDb) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var simRepo = scope.ServiceProvider.GetRequiredService<ISimulationRunRepository>();
            var stored = await simRepo.GetByIdAsync(simulation.Id);
            if (stored == null)
            {
                _logger.LogWarning("Cannot finalize persistence for simulation {SimulationId}: row not found",
                    simulation.Id);
                return;
            }

            stored.Status = simulation.Status;
            stored.CompletedAt = simulation.CompletedAt;
            stored.StartedAt = simulation.StartedAt;
            stored.Progress = simulation.Progress;
            stored.TotalTransactions = simulation.TotalTransactions;
            stored.ErrorMessage = simulation.ErrorMessage;
            await simRepo.UpdateAsync(stored);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Final persistence update for simulation {SimulationId} failed", simulation.Id);
        }
    }
}