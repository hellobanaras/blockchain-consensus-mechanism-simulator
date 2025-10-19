using Microsoft.Extensions.Logging;
using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Consensus.Core.Protocols;

/// <summary>
/// Proof of Elapsed Time (PoET) consensus protocol implementation
/// PoET is a lottery-style consensus mechanism that randomly selects nodes to create blocks
/// based on a randomly generated wait time for each node
/// </summary>
public class PoetProtocol : IConsensusProtocol
{
    private readonly ILogger<PoetProtocol> _logger;
    private readonly Random _random;
    private readonly Dictionary<Guid, double> _nodeWaitTimes;
    private readonly Dictionary<Guid, int> _leaderCounts;
    private readonly List<double> _roundTimes;

    public ConsensusAlgorithm Algorithm => ConsensusAlgorithm.ProofOfElapsedTime;
    public string Name => "PoET";
    public int MinimumNodes => 3;
    public string Description => "Proof of Elapsed Time - Random leader election based on wait times";
    public bool SupportsByzantineFaultTolerance => true;
    public List<Node> ParticipatingNodes { get; private set; } = new();

    // Configuration parameters
    private int _minWaitTimeMs = 1000;
    private int _maxWaitTimeMs = 5000;
    private int _blockTimeMs = 2000;
    private int _timeoutMs = 10000;

    public PoetProtocol(ILogger<PoetProtocol> logger)
    {
        _logger = logger;
        _random = new Random();
        _nodeWaitTimes = new Dictionary<Guid, double>();
        _leaderCounts = new Dictionary<Guid, int>();
        _roundTimes = new List<double>();
    }

    public async Task InitializeAsync(IEnumerable<Node> nodes, Dictionary<string, object> configuration)
    {
        _logger.LogInformation("Initializing PoET consensus protocol");

        // Validate nodes
        var nodeList = nodes.ToList();
        if (nodeList.Count < MinimumNodes)
        {
            throw new ArgumentException($"PoET requires at least {MinimumNodes} nodes");
        }

        // Apply configuration
        if (configuration != null)
        {
            ApplyConfiguration(configuration);
        }

        // Initialize participating nodes (exclude Byzantine nodes from leader selection)
        ParticipatingNodes = nodeList
            .Where(n => n.IsActive && n.Status == NodeStatus.Online)
            .ToList();

        // Initialize tracking dictionaries
        _nodeWaitTimes.Clear();
        _leaderCounts.Clear();
        
        foreach (var node in ParticipatingNodes)
        {
            _leaderCounts[node.Id] = 0;
        }

        _logger.LogInformation("PoET initialized with {NodeCount} participating nodes", ParticipatingNodes.Count);
        await Task.CompletedTask;
    }

    public async Task<ConsensusResult> ExecuteRoundAsync(ConsensusRound round, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Executing PoET round {RoundNumber}", round.RoundNumber);

        try
        {
            round.Start();
            
            // Set the number of participating nodes
            round.ParticipatingNodes = ParticipatingNodes.Count(n => !n.IsByzantine && n.Status == NodeStatus.Online);

            // Step 1: Calculate wait times for all nodes
            var nodeWaitTimes = await CalculateNodeWaitTimes(round);
            
            // Step 2: Select leader based on shortest wait time (excluding Byzantine nodes)
            var leader = await SelectLeader(nodeWaitTimes, round);
            if (leader == null)
            {
                return new ConsensusResult
                {
                    Success = false,
                    ErrorMessage = "No valid leader could be selected",
                    Duration = DateTime.UtcNow - startTime,
                    ParticipatingNodes = ParticipatingNodes.Count,
                    LeaderId = null,
                    Events = new List<ConsensusEvent>
                    {
                        new ConsensusEvent
                        {
                            Timestamp = DateTime.UtcNow,
                            Type = EventType.ConsensusFailed,
                            NodeId = string.Empty,
                            Message = "No valid leader could be selected"
                        }
                    }
                };
            }

            // Step 3: Simulate the wait time
            var waitTime = nodeWaitTimes[leader.Id];
            await SimulateWaitTime(waitTime);

            // Step 4: Create consensus result
            round.LeaderId = leader.Id;
            round.ProposedValue = new Dictionary<string, object>
            {
                { "leader", leader.Id.ToString() },
                { "waitTime", waitTime },
                { "proof", GeneratePoetProof(leader.Id, waitTime) }
            };

            _leaderCounts[leader.Id]++;

            round.Complete(round.ProposedValue);

            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _roundTimes.Add(executionTime);

            _logger.LogInformation("PoET round {RoundNumber} completed successfully. Leader: {LeaderId}, Wait time: {WaitTime}ms", 
                round.RoundNumber, leader.Id, waitTime);

            return new ConsensusResult
            {
                Success = true,
                Duration = DateTime.UtcNow - startTime,
                ParticipatingNodes = ParticipatingNodes.Count,
                LeaderId = leader.Id.ToString(),
                Events = new List<ConsensusEvent>
                {
                    new ConsensusEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        Type = EventType.LeaderSelection,
                        NodeId = leader.Id.ToString(),
                        Message = $"Node {leader.Id} selected as leader with wait time {waitTime}ms"
                    },
                    new ConsensusEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        Type = EventType.ConsensusReached,
                        NodeId = leader.Id.ToString(),
                        Message = "PoET consensus reached successfully"
                    }
                },
                Metrics = new Dictionary<string, object>
                {
                    { "leaderId", leader.Id },
                    { "waitTime", waitTime },
                    { "executionTime", executionTime }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing PoET round {RoundNumber}", round.RoundNumber);
            round.Fail(ex.Message);
            
            return new ConsensusResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = DateTime.UtcNow - startTime,
                ParticipatingNodes = ParticipatingNodes.Count,
                LeaderId = null,
                Events = new List<ConsensusEvent>
                {
                    new ConsensusEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        Type = EventType.ConsensusFailed,
                        NodeId = string.Empty,
                        Message = $"PoET consensus failed: {ex.Message}"
                    }
                }
            };
        }
    }

    public bool CanNodeParticipate(Node node)
    {
        return node.IsActive && 
               node.Status == NodeStatus.Online && 
               !node.IsByzantine &&
               node.ConsensusAlgorithm == ConsensusAlgorithm.ProofOfElapsedTime;
    }

    public Dictionary<string, object> GetMetrics()
    {
        return new Dictionary<string, object>
        {
            { "totalRounds", _roundTimes.Count },
            { "averageWaitTime", _nodeWaitTimes.Values.Any() ? _nodeWaitTimes.Values.Average() : 0.0 },
            { "averageRoundTime", _roundTimes.Any() ? _roundTimes.Average() : 0.0 },
            { "leaderDistribution", new Dictionary<Guid, int>(_leaderCounts) },
            { "consensusEfficiency", CalculateConsensusEfficiency() }
        };
    }

    public async Task HandleNodeFaultAsync(Node node, FaultType faultType)
    {
        _logger.LogWarning("Handling node fault: {NodeId}, Fault: {FaultType}", node.Id, faultType);

        switch (faultType)
        {
            case FaultType.Byzantine:
                node.IsByzantine = true;
                break;
            case FaultType.Crash:
            case FaultType.NetworkPartition:
                node.Status = NodeStatus.Offline;
                break;
            case FaultType.SlowResponse:
                // Adjust node's effective wait time
                if (_nodeWaitTimes.ContainsKey(node.Id))
                {
                    _nodeWaitTimes[node.Id] *= 1.5; // Penalize slow nodes
                }
                break;
        }

        // Remove from participating nodes if necessary
        if (!CanNodeParticipate(node))
        {
            ParticipatingNodes.RemoveAll(n => n.Id == node.Id);
            _logger.LogInformation("Node {NodeId} removed from participating nodes due to fault", node.Id);
        }

        await Task.CompletedTask;
    }

    // Additional helper methods

    public async Task<ConsensusRound> PrepareRoundAsync(long roundNumber, Guid simulationRunId)
    {
        _logger.LogDebug("Preparing PoET round {RoundNumber}", roundNumber);

        var round = new ConsensusRound
        {
            Id = Guid.NewGuid(),
            RoundNumber = roundNumber,
            SimulationRunId = simulationRunId,
            ParticipatingNodes = ParticipatingNodes.Count,
            ConsensusThreshold = CalculateConsensusThreshold(ParticipatingNodes.Count),
            Status = ConsensusRoundStatus.Pending,
            StartedAt = DateTime.UtcNow,
            TimeoutDuration = TimeSpan.FromMilliseconds(_timeoutMs)
        };

        _logger.LogDebug("PoET round {RoundNumber} prepared with {ParticipatingNodes} nodes", 
            roundNumber, round.ParticipatingNodes);

        return await Task.FromResult(round);
    }

    public async Task<bool> ValidateBlockAsync(Block block)
    {
        _logger.LogDebug("Validating block {BlockNumber} for PoET", block.BlockNumber);

        try
        {
            // Basic block validation
            if (!block.ValidateBlock())
            {
                _logger.LogWarning("Block {BlockNumber} failed basic validation", block.BlockNumber);
                return false;
            }

            // PoET-specific validation
            if (block.Data == null || !block.Data.ContainsKey("waitTime") || !block.Data.ContainsKey("proof"))
            {
                _logger.LogWarning("Block {BlockNumber} missing PoET-specific data", block.BlockNumber);
                return false;
            }

            // Validate proposer is a participating node
            if (block.ProposerId.HasValue)
            {
                var proposer = ParticipatingNodes.FirstOrDefault(n => n.Id == block.ProposerId.Value);
                if (proposer == null)
                {
                    _logger.LogWarning("Block {BlockNumber} proposed by unknown node {ProposerId}", 
                        block.BlockNumber, block.ProposerId);
                    return false;
                }

                // Byzantine nodes cannot propose valid blocks
                if (proposer.IsByzantine)
                {
                    _logger.LogWarning("Block {BlockNumber} proposed by Byzantine node {ProposerId}", 
                        block.BlockNumber, block.ProposerId);
                    return false;
                }
            }

            // Validate PoET proof (simplified for simulation)
            var waitTime = Convert.ToDouble(block.Data["waitTime"]);
            var proof = block.Data["proof"].ToString();
            
            // For simulation, we accept any non-empty proof that contains the wait time
            // In production, this would be a more rigorous cryptographic proof validation
            if (string.IsNullOrEmpty(proof) || !proof.Contains(waitTime.ToString()) && !proof.Equals("valid_poet_proof", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Block {BlockNumber} has invalid PoET proof", block.BlockNumber);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating block {BlockNumber}", block.BlockNumber);
            return false;
        }
    }

    public async Task<Block> CreateGenesisBlockAsync(Guid simulationRunId)
    {
        _logger.LogInformation("Creating PoET genesis block for simulation {SimulationRunId}", simulationRunId);

        var genesisBlock = new Block
        {
            Id = Guid.NewGuid(),
            BlockNumber = 0,
            Hash = string.Empty,
            PreviousHash = null,
            SimulationRunId = simulationRunId,
            Timestamp = DateTime.UtcNow,
            ProposerId = null, // Genesis has no proposer
            Data = new Dictionary<string, object>
            {
                { "genesis", true },
                { "protocol", "PoET" },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            },
            Nonce = 0,
            Difficulty = 0,
            Size = 256,
            TransactionCount = 0
        };

        // Calculate genesis block hash
        genesisBlock.Hash = genesisBlock.CalculateHash();

        _logger.LogInformation("PoET genesis block created: {Hash}", genesisBlock.Hash);
        return await Task.FromResult(genesisBlock);
    }

    public int CalculateConsensusThreshold(int nodeCount)
    {
        // For PoET, we need a simple majority for consensus
        return (nodeCount / 2) + 1;
    }

    public bool SupportsNodeCount(int nodeCount)
    {
        return nodeCount >= MinimumNodes && nodeCount <= 1000;
    }

    public Dictionary<string, object> GetProtocolMetrics()
    {
        return GetMetrics();
    }

    public async Task CleanupAsync()
    {
        _logger.LogInformation("Cleaning up PoET consensus protocol");
        
        ParticipatingNodes.Clear();
        _nodeWaitTimes.Clear();
        _leaderCounts.Clear();
        _roundTimes.Clear();

        await Task.CompletedTask;
    }

    // Internal helper methods

    private void ApplyConfiguration(Dictionary<string, object> configuration)
    {
        if (configuration.TryGetValue("minWaitTimeMs", out var minWait))
        {
            _minWaitTimeMs = Convert.ToInt32(minWait);
        }

        if (configuration.TryGetValue("maxWaitTimeMs", out var maxWait))
        {
            _maxWaitTimeMs = Convert.ToInt32(maxWait);
        }

        if (configuration.TryGetValue("blockTime", out var blockTime))
        {
            _blockTimeMs = Convert.ToInt32(blockTime);
        }

        if (configuration.TryGetValue("timeoutMs", out var timeout))
        {
            _timeoutMs = Convert.ToInt32(timeout);
        }

        // Validate configuration
        if (_minWaitTimeMs >= _maxWaitTimeMs)
        {
            throw new ArgumentException("minWaitTimeMs must be less than maxWaitTimeMs");
        }

        if (_minWaitTimeMs < 100 || _maxWaitTimeMs > 60000)
        {
            throw new ArgumentException("Wait times must be between 100ms and 60000ms");
        }

        _logger.LogDebug("PoET configuration applied: MinWait={MinWait}ms, MaxWait={MaxWait}ms, BlockTime={BlockTime}ms", 
            _minWaitTimeMs, _maxWaitTimeMs, _blockTimeMs);
    }

    private async Task<Dictionary<Guid, double>> CalculateNodeWaitTimes(ConsensusRound round)
    {
        var waitTimes = new Dictionary<Guid, double>();

        foreach (var node in ParticipatingNodes.Where(n => !n.IsByzantine))
        {
            var waitTime = CalculateWaitTime();
            waitTimes[node.Id] = waitTime;
            _nodeWaitTimes[node.Id] = waitTime;

            _logger.LogDebug("Node {NodeId} assigned wait time: {WaitTime}ms", node.Id, waitTime);
        }

        return await Task.FromResult(waitTimes);
    }

    public double CalculateWaitTime(Dictionary<string, object>? configuration = null)
    {
        var minWait = _minWaitTimeMs;
        var maxWait = _maxWaitTimeMs;

        if (configuration != null)
        {
            if (configuration.TryGetValue("minWaitTimeMs", out var configMinWait))
                minWait = Convert.ToInt32(configMinWait);
            if (configuration.TryGetValue("maxWaitTimeMs", out var configMaxWait))
                maxWait = Convert.ToInt32(configMaxWait);
        }

        // Generate cryptographically secure random wait time
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[4];
        rng.GetBytes(randomBytes);
        var randomValue = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) / (double)int.MaxValue;

        return minWait + (randomValue * (maxWait - minWait));
    }

    private async Task<Node?> SelectLeader(Dictionary<Guid, double> waitTimes, ConsensusRound round)
    {
        if (!waitTimes.Any())
        {
            _logger.LogWarning("No nodes available for leader selection in round {RoundNumber}", round.RoundNumber);
            return null;
        }

        // Select node with shortest wait time
        var selectedNodeId = waitTimes.OrderBy(kvp => kvp.Value).First().Key;
        var leader = ParticipatingNodes.FirstOrDefault(n => n.Id == selectedNodeId);

        if (leader == null)
        {
            _logger.LogError("Selected leader node {NodeId} not found in participating nodes", selectedNodeId);
            return null;
        }

        _logger.LogDebug("Leader selected for round {RoundNumber}: Node {NodeId} with wait time {WaitTime}ms", 
            round.RoundNumber, leader.Id, waitTimes[selectedNodeId]);

        return await Task.FromResult(leader);
    }

    private async Task SimulateWaitTime(double waitTime)
    {
        // In a real implementation, nodes would actually wait
        // For simulation, we'll just add a small delay to represent the concept
        var simulatedDelay = Math.Min(waitTime / 100, 100); // Scale down for simulation
        await Task.Delay((int)simulatedDelay);
    }

    private string GeneratePoetProof(Guid nodeId, double waitTime)
    {
        // Simplified PoET proof generation for simulation
        var proofData = $"{nodeId}:{waitTime}:{DateTime.UtcNow:O}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(proofData));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private double CalculateConsensusEfficiency()
    {
        if (_roundTimes.Count == 0) return 0.0;

        // Calculate efficiency as ratio of successful rounds vs timeout
        var averageRoundTime = _roundTimes.Average();
        return Math.Max(0.0, Math.Min(1.0, 1.0 - (averageRoundTime / _timeoutMs)));
    }
}