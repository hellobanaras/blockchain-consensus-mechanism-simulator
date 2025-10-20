using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Interfaces;
using Consensus.Core.Models;
using Consensus.Core.Protocols;
using Microsoft.Extensions.Logging;
using SimulationStatus = Consensus.Core.Enums.SimulationStatus;

namespace Consensus.Core.Services;

/// <summary>
/// Core simulation service implementation
/// </summary>
public class SimulationService : ISimulationService
{
    private readonly ILogger<SimulationService> _logger;
    private readonly Dictionary<Guid, SimulationRuntime> _activeSimulations;
    private readonly object _lock = new();

    public SimulationService(ILogger<SimulationService> logger)
    {
        _logger = logger;
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
                Status = SimulationStatus.Initializing,
                CreatedAt = DateTime.UtcNow,
                Configuration = request.AlgorithmConfiguration
            };

            // Create nodes for the simulation
            simulation.Nodes = await CreateNodesAsync(request);

            // Initialize the consensus protocol
            var protocol = CreateConsensusProtocol(request.Algorithm);
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
            return;
        }

        try
        {
            var simulation = runtime.Simulation;
            var protocol = runtime.Protocol;
            var cancellationToken = runtime.CancellationTokenSource.Token;

            _logger.LogInformation("Executing simulation {SimulationId} for {Duration} seconds", 
                simulationId, simulation.DurationSeconds);

            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddSeconds(simulation.DurationSeconds ?? 300); // Default 5 minutes
            var roundNumber = 1L;

            while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
            {
                // Create a new consensus round
                var round = new ConsensusRound
                {
                    Id = Guid.NewGuid(),
                    RoundNumber = roundNumber,
                    SimulationRunId = simulation.Id,
                    Algorithm = simulation.ConsensusAlgorithm,
                    ParticipatingNodes = simulation.Nodes.Count(n => !n.IsByzantine && n.Status == NodeStatus.Online),
                    StartedAt = DateTime.UtcNow
                };

                try
                {
                    // Execute the consensus round
                    var result = await protocol.ExecuteRoundAsync(round, cancellationToken);
                    
                    round.Status = result.Success ? ConsensusRoundStatus.Completed : ConsensusRoundStatus.Failed;
                    round.CompletedAt = DateTime.UtcNow;
                    round.LeaderId = result.LeaderId != null ? Guid.Parse(result.LeaderId) : null;

                    // Add the round to simulation
                    simulation.ConsensusRounds ??= new List<ConsensusRound>();
                    simulation.ConsensusRounds.Add(round);

                    // Notify subscribers
                    OnRoundCompleted(simulation, round, result);

                    _logger.LogDebug("Round {RoundNumber} completed for simulation {SimulationId}: {Success}", 
                        roundNumber, simulationId, result.Success);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in round {RoundNumber} for simulation {SimulationId}", 
                        roundNumber, simulationId);
                    
                    round.Status = ConsensusRoundStatus.Failed;
                    round.CompletedAt = DateTime.UtcNow;
                }

                roundNumber++;

                // Wait for the next round (simulate block time)
                await Task.Delay(simulation.BlockTimeMs, cancellationToken);
            }

            // Mark simulation as completed
            simulation.Status = cancellationToken.IsCancellationRequested 
                ? SimulationStatus.Stopped 
                : SimulationStatus.Completed;
            simulation.CompletedAt = DateTime.UtcNow;

            OnSimulationStatusChanged(simulation);

            _logger.LogInformation("Simulation {SimulationId} completed after {Duration} with {Rounds} rounds", 
                simulationId, DateTime.UtcNow - startTime, roundNumber - 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in simulation {SimulationId}", simulationId);
            
            var simulation = runtime.Simulation;
            simulation.Status = SimulationStatus.Failed;
            simulation.CompletedAt = DateTime.UtcNow;
            
            OnSimulationStatusChanged(simulation);
        }
    }

    private async Task<List<Node>> CreateNodesAsync(CreateSimulationRequest request)
    {
        var nodes = new List<Node>();

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
                ComputationalPower = Random.Shared.Next(80, 120), // Vary slightly for realism
                ReputationScore = 100m,
                CreatedAt = DateTime.UtcNow
            };

            nodes.Add(node);
        }

        return nodes;
    }

    private IConsensusProtocol CreateConsensusProtocol(ConsensusAlgorithm algorithm)
    {
        return algorithm switch
        {
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
        SimulationStatusChanged?.Invoke(this, new SimulationStatusChangedEventArgs(simulation));
    }

    private void OnRoundCompleted(SimulationRun simulation, ConsensusRound round, ConsensusResult result)
    {
        RoundCompleted?.Invoke(this, new RoundCompletedEventArgs(simulation, round, result));
    }
}