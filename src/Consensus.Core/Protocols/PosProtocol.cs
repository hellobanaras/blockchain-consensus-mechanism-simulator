using Microsoft.Extensions.Logging;
using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Consensus.Core.Protocols;

/// <summary>
/// Proof of Stake (PoS) consensus protocol implementation
/// PoS is a consensus mechanism where validators are chosen to create new blocks
/// based on their stake (token holdings) rather than computational power
/// </summary>
public class PosProtocol : IConsensusProtocol
{
    private readonly ILogger<PosProtocol> _logger;
    private readonly Dictionary<Guid, decimal> _nodeStakes;
    private readonly Dictionary<Guid, int> _validatorCounts;
    private readonly Dictionary<Guid, decimal> _slashingPenalties;
    private readonly Dictionary<Guid, DateTime> _lastValidationTime;
    private readonly List<double> _roundTimes;
    private Random _random;

    public ConsensusAlgorithm Algorithm => ConsensusAlgorithm.ProofOfStake;
    public string Name => "PoS";
    public int MinimumNodes => 3;
    public string Description => "Proof of Stake - Validator selection based on economic stake";
    public bool SupportsByzantineFaultTolerance => true;
    public List<Node> ParticipatingNodes { get; private set; } = new();

    // Configuration parameters
    private decimal _minimumStake = 100m;
    private decimal _slashingRate = 0.05m; // 5% penalty
    private int _blockTimeMs = 5000;
    private int _epochLength = 32; // blocks per epoch
    private int _timeoutMs = 30000;
    private decimal _rewardAmount = 10m;

    public PosProtocol(ILogger<PosProtocol> logger)
    {
        _logger = logger;
        _nodeStakes = new Dictionary<Guid, decimal>();
        _validatorCounts = new Dictionary<Guid, int>();
        _slashingPenalties = new Dictionary<Guid, decimal>();
        _lastValidationTime = new Dictionary<Guid, DateTime>();
        _roundTimes = new List<double>();
        _random = new Random();
    }

    public void SetRandom(Random rng) => _random = rng;

    public async Task InitializeAsync(IEnumerable<Node> nodes, Dictionary<string, object> configuration)
    {
        _logger.LogInformation("Initializing PoS consensus protocol");

        // Validate nodes
        var nodeList = nodes.ToList();
        if (nodeList.Count < MinimumNodes)
        {
            throw new ArgumentException($"PoS requires at least {MinimumNodes} nodes");
        }

        // Apply configuration
        if (configuration != null)
        {
            ApplyConfiguration(configuration);
        }

        // Initialize participating nodes (only those with sufficient stake)
        ParticipatingNodes = nodeList
            .Where(n => n.IsActive && 
                       n.Status == NodeStatus.Online && 
                       n.StakeAmount >= _minimumStake)
            .ToList();

        if (ParticipatingNodes.Count < MinimumNodes)
        {
            throw new ArgumentException($"PoS requires at least {MinimumNodes} nodes with minimum stake of {_minimumStake}");
        }

        // Initialize tracking dictionaries
        _nodeStakes.Clear();
        _validatorCounts.Clear();
        _slashingPenalties.Clear();
        _lastValidationTime.Clear();
        
        foreach (var node in ParticipatingNodes)
        {
            _nodeStakes[node.Id] = node.StakeAmount;
            _validatorCounts[node.Id] = 0;
            _slashingPenalties[node.Id] = 0m;
            _lastValidationTime[node.Id] = DateTime.UtcNow;
        }

        _logger.LogInformation("PoS initialized with {NodeCount} validators, total stake: {TotalStake}", 
            ParticipatingNodes.Count, _nodeStakes.Values.Sum());
        
        await Task.CompletedTask;
    }

    public async Task<ConsensusResult> ExecuteRoundAsync(ConsensusRound round, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Executing PoS validation round {RoundNumber}", round.RoundNumber);

        try
        {
            round.Start();
            
            // Set the number of participating nodes
            round.ParticipatingNodes = ParticipatingNodes.Count(n => !n.IsByzantine && n.Status == NodeStatus.Online);

            // Step 1: Select validator based on stake
            var validator = await SelectValidator(round.RoundNumber);
            
            if (validator == null)
            {
                return new ConsensusResult
                {
                    Success = false,
                    ErrorMessage = "No eligible validator found",
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
                            Message = "No eligible validator found for PoS round"
                        }
                    }
                };
            }

            // Step 2: Simulate validation process
            await SimulateValidation(validator, round);

            // Step 3: Check for attestations from other validators
            var attestations = await CollectAttestations(validator, round, cancellationToken);
            
            // Step 4: Verify we have enough attestations for consensus
            var requiredAttestations = CalculateConsensusThreshold(ParticipatingNodes.Count) - 1; // -1 for the proposer
            
            if (attestations.Count < requiredAttestations)
            {
                return new ConsensusResult
                {
                    Success = false,
                    ErrorMessage = $"Insufficient attestations: {attestations.Count}/{requiredAttestations}",
                    Duration = DateTime.UtcNow - startTime,
                    ParticipatingNodes = ParticipatingNodes.Count,
                    LeaderId = validator.Id.ToString(),
                    Events = new List<ConsensusEvent>
                    {
                        new ConsensusEvent
                        {
                            Timestamp = DateTime.UtcNow,
                            Type = EventType.ConsensusFailed,
                            NodeId = validator.Id.ToString(),
                            Message = "Insufficient validator attestations"
                        }
                    }
                };
            }

            // Step 5: Create consensus result
            round.LeaderId = validator.Id;
            round.ProposedValue = new Dictionary<string, object>
            {
                { "validator", validator.Id.ToString() },
                { "stake", _nodeStakes[validator.Id] },
                { "attestations", attestations.Count },
                { "reward", _rewardAmount },
                { "blockTime", _blockTimeMs }
            };

            // Update validator metrics
            _validatorCounts[validator.Id]++;
            _lastValidationTime[validator.Id] = DateTime.UtcNow;
            
            // Distribute rewards and penalties
            await DistributeRewards(validator, attestations);

            round.Complete(round.ProposedValue);

            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _roundTimes.Add(executionTime);

            _logger.LogInformation("PoS round {RoundNumber} completed successfully. Validator: {ValidatorId}, Attestations: {AttestationCount}", 
                round.RoundNumber, validator.Id, attestations.Count);

            return new ConsensusResult
            {
                Success = true,
                Duration = DateTime.UtcNow - startTime,
                ParticipatingNodes = ParticipatingNodes.Count,
                LeaderId = validator.Id.ToString(),
                Events = new List<ConsensusEvent>
                {
                    new ConsensusEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        Type = EventType.LeaderSelection,
                        NodeId = validator.Id.ToString(),
                        Message = $"Validator {validator.Id} selected with stake {_nodeStakes[validator.Id]}"
                    },
                    new ConsensusEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        Type = EventType.ConsensusReached,
                        NodeId = validator.Id.ToString(),
                        Message = $"PoS consensus reached with {attestations.Count} attestations"
                    }
                },
                Metrics = new Dictionary<string, object>
                {
                    { "validatorId", validator.Id },
                    { "validatorStake", _nodeStakes[validator.Id] },
                    { "attestationCount", attestations.Count },
                    { "executionTime", executionTime },
                    { "reward", _rewardAmount }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing PoS round {RoundNumber}", round.RoundNumber);
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
                        Message = $"PoS consensus failed: {ex.Message}"
                    }
                }
            };
        }
    }

    public bool CanNodeParticipate(Node node)
    {
        if (!node.IsActive || 
            node.Status != NodeStatus.Online || 
            node.IsByzantine ||
            node.ConsensusAlgorithm != ConsensusAlgorithm.ProofOfStake)
        {
            return false;
        }

        // Check stake amount - first check internal tracking, then node property
        var nodeStake = _nodeStakes.ContainsKey(node.Id) ? _nodeStakes[node.Id] : node.StakeAmount;
        return nodeStake >= _minimumStake;
    }

    public Dictionary<string, object> GetMetrics()
    {
        var totalStake = _nodeStakes.Values.Sum();
        var averageStake = _nodeStakes.Count > 0 ? totalStake / _nodeStakes.Count : 0m;
        
        return new Dictionary<string, object>
        {
            { "totalRounds", _roundTimes.Count },
            { "averageRoundTime", _roundTimes.Any() ? _roundTimes.Average() : 0.0 },
            { "totalStake", totalStake },
            { "averageStake", averageStake },
            { "minimumStake", _minimumStake },
            { "validatorCounts", new Dictionary<Guid, int>(_validatorCounts) },
            { "nodeStakes", new Dictionary<Guid, decimal>(_nodeStakes) },
            { "slashingPenalties", new Dictionary<Guid, decimal>(_slashingPenalties) },
            { "stakeDistribution", CalculateStakeDistribution() }
        };
    }

    public async Task HandleNodeFaultAsync(Node node, FaultType faultType)
    {
        _logger.LogWarning("Handling node fault: {NodeId}, Fault: {FaultType}", node.Id, faultType);

        switch (faultType)
        {
            case FaultType.Byzantine:
                node.IsByzantine = true;
                // Apply slashing penalty for Byzantine behavior
                await ApplySlashing(node.Id, _slashingRate, "Byzantine behavior detected");
                break;
                
            case FaultType.Crash:
            case FaultType.NetworkPartition:
                node.Status = NodeStatus.Offline;
                break;
                
            case FaultType.SlowResponse:
                // Minor penalty for slow response
                await ApplySlashing(node.Id, _slashingRate * 0.1m, "Slow response penalty");
                break;
                
            case FaultType.InvalidMessage:
                // Penalty for invalid messages
                await ApplySlashing(node.Id, _slashingRate * 0.5m, "Invalid message penalty");
                break;
        }

        // Remove from participating nodes if necessary
        if (!CanNodeParticipate(node))
        {
            ParticipatingNodes.RemoveAll(n => n.Id == node.Id);
            _logger.LogInformation("Node {NodeId} removed from PoS validators due to fault", node.Id);
        }

        await Task.CompletedTask;
    }

    public async Task<ConsensusRound> PrepareRoundAsync(long roundNumber, Guid simulationRunId)
    {
        _logger.LogDebug("Preparing PoS round {RoundNumber}", roundNumber);

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

        _logger.LogDebug("PoS round {RoundNumber} prepared with {ParticipatingNodes} validators", 
            roundNumber, round.ParticipatingNodes);

        return await Task.FromResult(round);
    }

    public async Task<bool> ValidateBlockAsync(Block block)
    {
        _logger.LogDebug("Validating block {BlockNumber} for PoS", block.BlockNumber);

        try
        {
            // Basic block validation
            if (!block.ValidateBlock())
            {
                _logger.LogWarning("Block {BlockNumber} failed basic validation", block.BlockNumber);
                return false;
            }

            // PoS-specific validation
            if (block.Data == null || !block.Data.ContainsKey("validator") || !block.Data.ContainsKey("stake"))
            {
                _logger.LogWarning("Block {BlockNumber} missing PoS-specific data", block.BlockNumber);
                return false;
            }

            // Validate proposer is a participating validator
            if (block.ProposerId.HasValue)
            {
                var proposer = ParticipatingNodes.FirstOrDefault(n => n.Id == block.ProposerId.Value);
                if (proposer == null)
                {
                    _logger.LogWarning("Block {BlockNumber} proposed by unknown validator {ProposerId}", 
                        block.BlockNumber, block.ProposerId);
                    return false;
                }

                // Byzantine nodes cannot propose valid blocks
                if (proposer.IsByzantine)
                {
                    _logger.LogWarning("Block {BlockNumber} proposed by Byzantine validator {ProposerId}", 
                        block.BlockNumber, block.ProposerId);
                    return false;
                }

                // Validate proposer has sufficient stake
                if (!_nodeStakes.ContainsKey(proposer.Id) || _nodeStakes[proposer.Id] < _minimumStake)
                {
                    _logger.LogWarning("Block {BlockNumber} proposed by validator with insufficient stake", 
                        block.BlockNumber);
                    return false;
                }
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
        _logger.LogInformation("Creating PoS genesis block for simulation {SimulationRunId}", simulationRunId);

        var totalStake = _nodeStakes.Values.Sum();
        
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
                { "protocol", "PoS" },
                { "totalStake", totalStake },
                { "minimumStake", _minimumStake },
                { "validatorCount", ParticipatingNodes.Count },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            },
            Nonce = 0,
            Difficulty = 0,
            Size = 256,
            TransactionCount = 0
        };

        // Calculate genesis block hash
        genesisBlock.Hash = genesisBlock.CalculateHash();

        _logger.LogInformation("PoS genesis block created: {Hash}", genesisBlock.Hash);
        return await Task.FromResult(genesisBlock);
    }

    public int CalculateConsensusThreshold(int nodeCount)
    {
        // For PoS, we need 2/3 majority of validators
        return (int)Math.Ceiling(nodeCount * 2.0 / 3.0);
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
        _logger.LogInformation("Cleaning up PoS consensus protocol");
        
        ParticipatingNodes.Clear();
        _nodeStakes.Clear();
        _validatorCounts.Clear();
        _slashingPenalties.Clear();
        _lastValidationTime.Clear();
        _roundTimes.Clear();

        await Task.CompletedTask;
    }

    // Internal helper methods

    private void ApplyConfiguration(Dictionary<string, object> configuration)
    {
        if (configuration.TryGetValue("minimumStake", out var minStake))
        {
            _minimumStake = Convert.ToDecimal(minStake);
        }

        if (configuration.TryGetValue("slashingRate", out var slashing))
        {
            _slashingRate = Convert.ToDecimal(slashing);
        }

        if (configuration.TryGetValue("blockTimeMs", out var blockTime))
        {
            _blockTimeMs = Convert.ToInt32(blockTime);
        }

        if (configuration.TryGetValue("rewardAmount", out var reward))
        {
            _rewardAmount = Convert.ToDecimal(reward);
        }

        if (configuration.TryGetValue("timeoutMs", out var timeout))
        {
            _timeoutMs = Convert.ToInt32(timeout);
        }

        // Validate configuration
        if (_minimumStake <= 0)
        {
            throw new ArgumentException("Minimum stake must be positive");
        }

        if (_slashingRate < 0 || _slashingRate > 1)
        {
            throw new ArgumentException("Slashing rate must be between 0 and 1");
        }

        _logger.LogDebug("PoS configuration applied: MinStake={MinStake}, SlashingRate={SlashingRate}, BlockTime={BlockTime}ms", 
            _minimumStake, _slashingRate, _blockTimeMs);
    }

    private async Task<Node?> SelectValidator(long roundNumber)
    {
        var eligibleValidators = ParticipatingNodes
            .Where(n => !n.IsByzantine && n.Status == NodeStatus.Online && _nodeStakes[n.Id] >= _minimumStake)
            .ToList();

        if (!eligibleValidators.Any())
        {
            _logger.LogWarning("No eligible validators found for round {RoundNumber}", roundNumber);
            return null;
        }

        // Weighted random selection based on stake
        var totalStake = eligibleValidators.Sum(v => _nodeStakes[v.Id]);
        var randomValue = (decimal)_random.NextDouble() * totalStake;
        
        decimal currentWeight = 0;
        foreach (var validator in eligibleValidators)
        {
            currentWeight += _nodeStakes[validator.Id];
            if (randomValue <= currentWeight)
            {
                _logger.LogDebug("Selected validator {ValidatorId} with stake {Stake} for round {RoundNumber}", 
                    validator.Id, _nodeStakes[validator.Id], roundNumber);
                return validator;
            }
        }

        // Fallback to last validator if rounding errors occur
        return eligibleValidators.Last();
    }

    private async Task SimulateValidation(Node validator, ConsensusRound round)
    {
        // Simulate validation time based on block complexity
        var validationDelay = _blockTimeMs / 4; // Quarter of block time for validation
        await Task.Delay(validationDelay);
        
        _logger.LogDebug("Validator {ValidatorId} completed validation for round {RoundNumber}", 
            validator.Id, round.RoundNumber);
    }

    private async Task<List<Node>> CollectAttestations(Node validator, ConsensusRound round, CancellationToken cancellationToken)
    {
        var attesters = ParticipatingNodes
            .Where(n => n.Id != validator.Id && !n.IsByzantine && n.Status == NodeStatus.Online)
            .ToList();

        var attestations = new List<Node>();
        var attestationTasks = attesters.Select(async attester =>
        {
            try
            {
                // Simulate attestation delay based on network latency
                var delay = attester.NetworkLatency + _random.Next(0, 100);
                await Task.Delay(delay, cancellationToken);
                
                // Probability of attestation based on stake and network reliability
                var stakeProportion = _nodeStakes[attester.Id] / _nodeStakes.Values.Sum();
                var attestationProbability = 0.85 + ((double)stakeProportion * 0.1); // 85-95% chance
                
                if (_random.NextDouble() < attestationProbability)
                {
                    lock (attestations)
                    {
                        attestations.Add(attester);
                    }
                    _logger.LogDebug("Attester {AttesterId} provided attestation for round {RoundNumber}", 
                        attester.Id, round.RoundNumber);
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout occurred
            }
        });

        try
        {
            await Task.WhenAll(attestationTasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Attestation collection timed out for round {RoundNumber}", round.RoundNumber);
        }

        return attestations;
    }

    private async Task DistributeRewards(Node validator, List<Node> attesters)
    {
        // Reward the validator
        _nodeStakes[validator.Id] += _rewardAmount;
        validator.StakeAmount = _nodeStakes[validator.Id];
        
        // Reward attesters (smaller amount)
        var attesterReward = _rewardAmount * 0.1m; // 10% of validator reward
        foreach (var attester in attesters)
        {
            _nodeStakes[attester.Id] += attesterReward;
            attester.StakeAmount = _nodeStakes[attester.Id];
        }

        _logger.LogDebug("Distributed rewards: Validator {ValidatorId} +{ValidatorReward}, {AttesterCount} attesters +{AttesterReward} each", 
            validator.Id, _rewardAmount, attesters.Count, attesterReward);

        await Task.CompletedTask;
    }

    private async Task ApplySlashing(Guid nodeId, decimal rate, string reason)
    {
        if (!_nodeStakes.ContainsKey(nodeId)) return;

        var penalty = _nodeStakes[nodeId] * rate;
        _nodeStakes[nodeId] -= penalty;
        
        // Ensure the penalty dictionary has an entry for this node
        if (!_slashingPenalties.ContainsKey(nodeId))
        {
            _slashingPenalties[nodeId] = 0m;
        }
        _slashingPenalties[nodeId] += penalty;

        // Synchronize with node object
        var node = ParticipatingNodes.FirstOrDefault(n => n.Id == nodeId);
        if (node != null)
        {
            node.StakeAmount = _nodeStakes[nodeId];
        }

        _logger.LogWarning("Applied slashing penalty of {Penalty} to node {NodeId}: {Reason}", 
            penalty, nodeId, reason);

        await Task.CompletedTask;
    }

    private Dictionary<string, decimal> CalculateStakeDistribution()
    {
        var totalStake = _nodeStakes.Values.Sum();
        return _nodeStakes.ToDictionary(
            kvp => kvp.Key.ToString(), 
            kvp => totalStake > 0 ? kvp.Value / totalStake : 0m
        );
    }
}