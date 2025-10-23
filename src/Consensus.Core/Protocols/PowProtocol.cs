using Microsoft.Extensions.Logging;
using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Consensus.Core.Protocols;

/// <summary>
/// Proof of Work (PoW) consensus protocol implementation
/// PoW is a consensus mechanism where nodes compete to solve computationally intensive puzzles
/// to validate transactions and create new blocks
/// </summary>
public class PowProtocol : IConsensusProtocol
{
    private readonly ILogger<PowProtocol> _logger;
    private readonly Dictionary<Guid, int> _nodeHashrates;
    private readonly Dictionary<Guid, int> _leaderCounts;
    private readonly List<double> _roundTimes;
    private readonly Dictionary<Guid, long> _nodeNonces;

    public ConsensusAlgorithm Algorithm => ConsensusAlgorithm.ProofOfWork;
    public string Name => "PoW";
    public int MinimumNodes => 2;
    public string Description => "Proof of Work - Competitive mining through hash computation";
    public bool SupportsByzantineFaultTolerance => true;
    public List<Node> ParticipatingNodes { get; private set; } = new();

    // Configuration parameters
    private int _difficulty = 4;
    private int _maxHashAttemptsPerNode = 100000;
    private int _blockTimeTargetMs = 5000;
    private int _timeoutMs = 30000;

    public PowProtocol(ILogger<PowProtocol> logger)
    {
        _logger = logger;
        _nodeHashrates = new Dictionary<Guid, int>();
        _leaderCounts = new Dictionary<Guid, int>();
        _roundTimes = new List<double>();
        _nodeNonces = new Dictionary<Guid, long>();
    }

    public async Task InitializeAsync(IEnumerable<Node> nodes, Dictionary<string, object> configuration)
    {
        _logger.LogInformation("Initializing PoW consensus protocol");

        // Validate nodes
        var nodeList = nodes.ToList();
        if (nodeList.Count < MinimumNodes)
        {
            throw new ArgumentException($"PoW requires at least {MinimumNodes} nodes");
        }

        // Apply configuration
        if (configuration != null)
        {
            ApplyConfiguration(configuration);
        }

        // Initialize participating nodes
        ParticipatingNodes = nodeList
            .Where(n => n.IsActive && n.Status == NodeStatus.Online)
            .ToList();

        // Initialize tracking dictionaries
        _nodeHashrates.Clear();
        _leaderCounts.Clear();
        _nodeNonces.Clear();
        
        foreach (var node in ParticipatingNodes)
        {
            // Hashrate based on computational power (simulate varying mining capability)
            _nodeHashrates[node.Id] = (int)(node.ComputationalPower * 1000); // Convert to hashes per second
            _leaderCounts[node.Id] = 0;
            _nodeNonces[node.Id] = 0;
        }

        _logger.LogInformation("PoW initialized with {NodeCount} participating nodes, difficulty {Difficulty}", 
            ParticipatingNodes.Count, _difficulty);
        
        await Task.CompletedTask;
    }

    public async Task<ConsensusResult> ExecuteRoundAsync(ConsensusRound round, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Executing PoW mining round {RoundNumber} with difficulty {Difficulty}", 
            round.RoundNumber, _difficulty);

        try
        {
            round.Start();
            
            // Set the number of participating nodes
            round.ParticipatingNodes = ParticipatingNodes.Count(n => !n.IsByzantine && n.Status == NodeStatus.Online);

            // Step 1: Prepare mining target
            var targetHash = GenerateTarget(_difficulty);
            
            // Step 2: Simulate competitive mining
            var miningResult = await SimulateMining(round, targetHash, cancellationToken);
            
            if (miningResult == null)
            {
                return new ConsensusResult
                {
                    Success = false,
                    ErrorMessage = "No miner found a valid hash within timeout",
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
                            Message = "Mining timeout - no valid solution found"
                        }
                    }
                };
            }

            // Step 3: Validate the mining result
            var winner = miningResult.Value.miner;
            var winningNonce = miningResult.Value.nonce;
            var winningHash = miningResult.Value.hash;

            // Step 4: Create consensus result
            round.LeaderId = winner.Id;
            round.ProposedValue = new Dictionary<string, object>
            {
                { "miner", winner.Id.ToString() },
                { "nonce", winningNonce },
                { "hash", winningHash },
                { "difficulty", _difficulty },
                { "target", targetHash }
            };

            _leaderCounts[winner.Id]++;
            _nodeNonces[winner.Id] = winningNonce;

            round.Complete(round.ProposedValue);

            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _roundTimes.Add(executionTime);

            _logger.LogInformation("PoW round {RoundNumber} completed successfully. Miner: {MinerId}, Nonce: {Nonce}, Hash: {Hash}", 
                round.RoundNumber, winner.Id, winningNonce, winningHash);

            return new ConsensusResult
            {
                Success = true,
                Duration = DateTime.UtcNow - startTime,
                ParticipatingNodes = ParticipatingNodes.Count,
                LeaderId = winner.Id.ToString(),
                Events = new List<ConsensusEvent>
                {
                    new ConsensusEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        Type = EventType.LeaderSelection,
                        NodeId = winner.Id.ToString(),
                        Message = $"Miner {winner.Id} found valid hash {winningHash} with nonce {winningNonce}"
                    },
                    new ConsensusEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        Type = EventType.ConsensusReached,
                        NodeId = winner.Id.ToString(),
                        Message = "PoW consensus reached - valid block mined"
                    }
                },
                Metrics = new Dictionary<string, object>
                {
                    { "minerId", winner.Id },
                    { "nonce", winningNonce },
                    { "hash", winningHash },
                    { "difficulty", _difficulty },
                    { "executionTime", executionTime }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing PoW round {RoundNumber}", round.RoundNumber);
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
                        Message = $"PoW consensus failed: {ex.Message}"
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
               node.ConsensusAlgorithm == ConsensusAlgorithm.ProofOfWork &&
               node.ComputationalPower > 0;
    }

    public Dictionary<string, object> GetMetrics()
    {
        return new Dictionary<string, object>
        {
            { "totalRounds", _roundTimes.Count },
            { "averageRoundTime", _roundTimes.Any() ? _roundTimes.Average() : 0.0 },
            { "difficulty", _difficulty },
            { "leaderDistribution", new Dictionary<Guid, int>(_leaderCounts) },
            { "nodeHashrates", new Dictionary<Guid, int>(_nodeHashrates) },
            { "miningEfficiency", CalculateMiningEfficiency() }
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
                // Reduce mining capability for slow nodes
                if (_nodeHashrates.ContainsKey(node.Id))
                {
                    _nodeHashrates[node.Id] = (int)(_nodeHashrates[node.Id] * 0.5);
                }
                break;
        }

        // Remove from participating nodes if necessary
        if (!CanNodeParticipate(node))
        {
            ParticipatingNodes.RemoveAll(n => n.Id == node.Id);
            _logger.LogInformation("Node {NodeId} removed from PoW mining due to fault", node.Id);
        }

        await Task.CompletedTask;
    }

    public async Task<ConsensusRound> PrepareRoundAsync(long roundNumber, Guid simulationRunId)
    {
        _logger.LogDebug("Preparing PoW round {RoundNumber}", roundNumber);

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

        _logger.LogDebug("PoW round {RoundNumber} prepared with {ParticipatingNodes} miners", 
            roundNumber, round.ParticipatingNodes);

        return await Task.FromResult(round);
    }

    public async Task<bool> ValidateBlockAsync(Block block)
    {
        _logger.LogDebug("Validating block {BlockNumber} for PoW", block.BlockNumber);

        try
        {
            // Basic block validation
            if (!block.ValidateBlock())
            {
                _logger.LogWarning("Block {BlockNumber} failed basic validation", block.BlockNumber);
                return false;
            }

            // PoW-specific validation
            if (block.Data == null || !block.Data.ContainsKey("nonce") || !block.Data.ContainsKey("hash"))
            {
                _logger.LogWarning("Block {BlockNumber} missing PoW-specific data", block.BlockNumber);
                return false;
            }

            // Validate proposer is a participating node
            if (block.ProposerId.HasValue)
            {
                var proposer = ParticipatingNodes.FirstOrDefault(n => n.Id == block.ProposerId.Value);
                if (proposer == null)
                {
                    _logger.LogWarning("Block {BlockNumber} proposed by unknown miner {ProposerId}", 
                        block.BlockNumber, block.ProposerId);
                    return false;
                }

                // Byzantine nodes cannot propose valid blocks
                if (proposer.IsByzantine)
                {
                    _logger.LogWarning("Block {BlockNumber} proposed by Byzantine miner {ProposerId}", 
                        block.BlockNumber, block.ProposerId);
                    return false;
                }
            }

            // Validate PoW proof
            var nonce = Convert.ToInt64(block.Data["nonce"]);
            var hash = block.Data["hash"].ToString();
            
            // Recreate the hash and verify it meets difficulty target
            var blockData = $"{block.PreviousHash}:{block.Timestamp:O}:{nonce}";
            var expectedHash = ComputeHash(blockData);
            
            if (hash != expectedHash)
            {
                _logger.LogWarning("Block {BlockNumber} has invalid PoW hash", block.BlockNumber);
                return false;
            }

            // Verify hash meets difficulty requirement
            if (!IsValidHash(hash, _difficulty))
            {
                _logger.LogWarning("Block {BlockNumber} hash does not meet difficulty requirement", block.BlockNumber);
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
        _logger.LogInformation("Creating PoW genesis block for simulation {SimulationRunId}", simulationRunId);

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
                { "protocol", "PoW" },
                { "difficulty", _difficulty },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            },
            Nonce = 0,
            Difficulty = _difficulty,
            Size = 256,
            TransactionCount = 0
        };

        // Calculate genesis block hash
        genesisBlock.Hash = genesisBlock.CalculateHash();

        _logger.LogInformation("PoW genesis block created: {Hash}", genesisBlock.Hash);
        return await Task.FromResult(genesisBlock);
    }

    public int CalculateConsensusThreshold(int nodeCount)
    {
        // For PoW, only one miner needs to find a valid solution
        return 1;
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
        _logger.LogInformation("Cleaning up PoW consensus protocol");
        
        ParticipatingNodes.Clear();
        _nodeHashrates.Clear();
        _leaderCounts.Clear();
        _roundTimes.Clear();
        _nodeNonces.Clear();

        await Task.CompletedTask;
    }

    // Internal helper methods

    private void ApplyConfiguration(Dictionary<string, object> configuration)
    {
        if (configuration.TryGetValue("difficulty", out var difficulty))
        {
            _difficulty = Convert.ToInt32(difficulty);
        }

        if (configuration.TryGetValue("maxHashAttemptsPerNode", out var maxAttempts))
        {
            _maxHashAttemptsPerNode = Convert.ToInt32(maxAttempts);
        }

        if (configuration.TryGetValue("blockTimeTargetMs", out var blockTime))
        {
            _blockTimeTargetMs = Convert.ToInt32(blockTime);
        }

        if (configuration.TryGetValue("timeoutMs", out var timeout))
        {
            _timeoutMs = Convert.ToInt32(timeout);
        }

        // Validate configuration
        if (_difficulty < 1 || _difficulty > 8)
        {
            throw new ArgumentException("Difficulty must be between 1 and 8");
        }

        if (_maxHashAttemptsPerNode < 1000)
        {
            throw new ArgumentException("Max hash attempts must be at least 1000");
        }

        _logger.LogDebug("PoW configuration applied: Difficulty={Difficulty}, MaxAttempts={MaxAttempts}, BlockTime={BlockTime}ms", 
            _difficulty, _maxHashAttemptsPerNode, _blockTimeTargetMs);
    }

    private async Task<(Node miner, long nonce, string hash)?> SimulateMining(ConsensusRound round, string target, CancellationToken cancellationToken)
    {
        var eligibleMiners = ParticipatingNodes.Where(n => !n.IsByzantine && n.Status == NodeStatus.Online).ToList();
        
        if (!eligibleMiners.Any())
        {
            _logger.LogWarning("No eligible miners for round {RoundNumber}", round.RoundNumber);
            return null;
        }

        var tasks = eligibleMiners.Select(miner => MineForNode(miner, round, target, cancellationToken)).ToArray();
        
        try
        {
            var completedTask = await Task.WhenAny(tasks);
            var result = await completedTask;
            
            if (result.HasValue)
            {
                _logger.LogDebug("Mining completed by {MinerId} for round {RoundNumber}", 
                    result.Value.miner.Id, round.RoundNumber);
                return result;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Mining cancelled for round {RoundNumber}", round.RoundNumber);
        }

        return null;
    }

    private async Task<(Node miner, long nonce, string hash)?> MineForNode(Node miner, ConsensusRound round, string target, CancellationToken cancellationToken)
    {
        var hashrate = _nodeHashrates[miner.Id];
        var hashesSoFar = 0;
        var maxHashes = Math.Min(_maxHashAttemptsPerNode, hashrate * (_timeoutMs / 1000));
        
        // Simulate mining speed based on hashrate
        var delayBetweenHashes = Math.Max(1, 1000 / hashrate); // milliseconds per hash
        
        var nonce = _nodeNonces[miner.Id];
        
        while (hashesSoFar < maxHashes && !cancellationToken.IsCancellationRequested)
        {
            // Create block data for hashing
            var blockData = $"{round.RoundNumber}:{miner.Id}:{DateTime.UtcNow.Ticks}:{nonce}";
            var hash = ComputeHash(blockData);
            
            // Check if hash meets difficulty target
            if (IsValidHash(hash, _difficulty))
            {
                _logger.LogDebug("Valid hash found by miner {MinerId}: {Hash} with nonce {Nonce}", 
                    miner.Id, hash, nonce);
                return (miner, nonce, hash);
            }
            
            nonce++;
            hashesSoFar++;
            
            // Simulate mining delay
            if (delayBetweenHashes > 0 && hashesSoFar % 100 == 0) // Check delay every 100 hashes for performance
            {
                await Task.Delay(delayBetweenHashes * 100 / 100, cancellationToken);
            }
        }
        
        return null; // No valid hash found within limits
    }

    private string GenerateTarget(int difficulty)
    {
        // Create target string with required number of leading zeros
        return new string('0', difficulty) + new string('f', 64 - difficulty);
    }

    private string ComputeHash(string data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private bool IsValidHash(string hash, int difficulty)
    {
        // Check if hash has required number of leading zeros
        return hash.StartsWith(new string('0', difficulty));
    }

    private double CalculateMiningEfficiency()
    {
        if (_roundTimes.Count == 0) return 0.0;

        // Calculate efficiency as ratio of target time vs actual time
        var averageRoundTime = _roundTimes.Average();
        return Math.Max(0.0, Math.Min(1.0, _blockTimeTargetMs / averageRoundTime));
    }
}