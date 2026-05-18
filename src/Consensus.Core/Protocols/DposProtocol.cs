using Microsoft.Extensions.Logging;
using Consensus.Core.Interfaces;
using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Models;

namespace Consensus.Core.Protocols;

/// <summary>
/// Delegated Proof of Stake (DPoS) consensus protocol implementation
/// Features witness election, delegation mechanics, and round-robin block production
/// </summary>
public class DposProtocol : IConsensusProtocol
{
    private readonly ILogger<DposProtocol> _logger;
    private readonly Dictionary<Guid, decimal> _nodeStakes;
    private readonly Dictionary<Guid, decimal> _votePower;
    private readonly Dictionary<Guid, Dictionary<Guid, decimal>> _delegations; // delegator -> witness -> amount
    private readonly Dictionary<Guid, decimal> _witnessVotes; // witness -> total votes
    private readonly List<Node> _activeWitnesses;
    private readonly Dictionary<Guid, int> _witnessPerformance; // witness -> blocks produced
    private readonly Dictionary<Guid, int> _missedBlocks; // witness -> missed blocks
    private readonly List<double> _roundTimes;
    private Random _random;
    private int _currentWitnessIndex = 0;
    private long _currentRound = 0;

    public ConsensusAlgorithm Algorithm => ConsensusAlgorithm.DelegatedProofOfStake;
    public string Name => "DPoS";
    public int MinimumNodes => 4; // Need enough nodes for witness election
    public string Description => "Delegated Proof of Stake - Stakeholder-voted witnesses produce blocks in round-robin";
    public bool SupportsByzantineFaultTolerance => true;
    public List<Node> ParticipatingNodes { get; private set; } = new();

    // Configuration parameters
    private int _witnessCount = 5; // Number of active witnesses
    private decimal _minimumStake = 50m; // Minimum stake to participate
    private int _blockTimeMs = 3000; // Faster than PoS due to pre-selected witnesses
    private int _timeoutMs = 15000;
    private decimal _witnessReward = 15m; // Higher reward for witnesses
    private decimal _voterReward = 2m; // Reward for voting participation
    private int _maxMissedBlocks = 3; // Max blocks a witness can miss before removal
    private decimal _delegationFee = 0.1m; // 10% fee taken by witnesses from delegators

    public DposProtocol(ILogger<DposProtocol> logger)
    {
        _logger = logger;
        _nodeStakes = new Dictionary<Guid, decimal>();
        _votePower = new Dictionary<Guid, decimal>();
        _delegations = new Dictionary<Guid, Dictionary<Guid, decimal>>();
        _witnessVotes = new Dictionary<Guid, decimal>();
        _activeWitnesses = new List<Node>();
        _witnessPerformance = new Dictionary<Guid, int>();
        _missedBlocks = new Dictionary<Guid, int>();
        _roundTimes = new List<double>();
        _random = new Random();
    }

    public void SetRandom(Random rng) => _random = rng;

    public async Task InitializeAsync(IEnumerable<Node> nodes, Dictionary<string, object> configuration)
    {
        _logger.LogInformation("Initializing DPoS consensus protocol");

        // Validate nodes
        var nodeList = nodes.ToList();
        if (nodeList.Count < MinimumNodes)
        {
            throw new ArgumentException($"DPoS requires at least {MinimumNodes} nodes");
        }

        // Apply configuration
        if (configuration != null)
        {
            ApplyConfiguration(configuration);
        }

        // Initialize participating nodes (those with sufficient stake)
        ParticipatingNodes = nodeList
            .Where(n => n.IsActive && 
                       n.Status == NodeStatus.Online && 
                       n.StakeAmount >= _minimumStake)
            .ToList();

        if (ParticipatingNodes.Count < MinimumNodes)
        {
            throw new ArgumentException($"DPoS requires at least {MinimumNodes} nodes with minimum stake of {_minimumStake}");
        }

        // Initialize tracking dictionaries
        _nodeStakes.Clear();
        _votePower.Clear();
        _delegations.Clear();
        _witnessVotes.Clear();
        _witnessPerformance.Clear();
        _missedBlocks.Clear();
        
        foreach (var node in ParticipatingNodes)
        {
            _nodeStakes[node.Id] = node.StakeAmount;
            _votePower[node.Id] = node.StakeAmount; // Initial vote power equals stake
            _delegations[node.Id] = new Dictionary<Guid, decimal>();
            _witnessVotes[node.Id] = 0m;
            _witnessPerformance[node.Id] = 0;
            _missedBlocks[node.Id] = 0;
        }

        // Conduct initial witness election
        await ConductWitnessElection();

        _logger.LogInformation("DPoS initialized with {NodeCount} validators, {WitnessCount} witnesses elected", 
            ParticipatingNodes.Count, _activeWitnesses.Count);
        
        await Task.CompletedTask;
    }

    public async Task<ConsensusResult> ExecuteRoundAsync(ConsensusRound round, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Executing DPoS round {RoundNumber}", round.RoundNumber);
        _currentRound = round.RoundNumber;

        try
        {
            round.Start();
            
            // Set the number of participating nodes
            round.ParticipatingNodes = ParticipatingNodes.Count(n => !n.IsByzantine && n.Status == NodeStatus.Online);

            // Step 1: Get current witness (round-robin)
            var currentWitness = GetCurrentWitness();
            
            if (currentWitness == null)
            {
                return new ConsensusResult
                {
                    Success = false,
                    ErrorMessage = "No active witness available for block production"
                };
            }

            _logger.LogDebug("Current witness for round {RoundNumber}: {WitnessId}", round.RoundNumber, currentWitness.Id);

            // Step 2: Witness produces block
            var blockProduced = await ProduceBlock(currentWitness, round, cancellationToken);
            
            if (!blockProduced)
            {
                // Witness missed their slot
                await HandleMissedBlock(currentWitness);
                
                return new ConsensusResult
                {
                    Success = false,
                    ErrorMessage = $"Witness {currentWitness.Id} missed their block production slot"
                };
            }

            // Step 3: Other witnesses validate the block
            var validationResults = await ValidateWithWitnesses(currentWitness, round, cancellationToken);
            
            var validations = validationResults.Count(v => v);
            var validatingWitnesses = _activeWitnesses.Count - 1; // Exclude the producer
            var requiredValidations = (validatingWitnesses * 2 / 3) + 1; // 2/3+ majority of validating witnesses

            if (validations < requiredValidations)
            {
                return new ConsensusResult
                {
                    Success = false,
                    ErrorMessage = $"Insufficient witness validations: {validations}/{requiredValidations}"
                };
            }

            // Step 4: Distribute rewards
            await DistributeRewards(currentWitness, validationResults);

            // Step 5: Move to next witness
            _currentWitnessIndex = (_currentWitnessIndex + 1) % _activeWitnesses.Count;

            // Step 6: Periodic witness re-election (every 21 rounds)
            if (round.RoundNumber % 21 == 0)
            {
                await ConductWitnessElection();
            }

            var endTime = DateTime.UtcNow;
            var roundTime = (endTime - startTime).TotalMilliseconds;
            _roundTimes.Add(roundTime);

            round.Complete();

            return new ConsensusResult
            {
                Success = true,
                Duration = TimeSpan.FromMilliseconds(roundTime),
                LeaderId = currentWitness.Id.ToString(),
                ParticipatingNodes = round.ParticipatingNodes
            };
        }
        catch (OperationCanceledException)
        {
            return new ConsensusResult
            {
                Success = false,
                ErrorMessage = "DPoS consensus round was cancelled"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during DPoS consensus round {RoundNumber}", round.RoundNumber);
            return new ConsensusResult
            {
                Success = false,
                ErrorMessage = $"DPoS consensus failed: {ex.Message}"
            };
        }
    }

    public async Task CleanupAsync()
    {
        _logger.LogInformation("Cleaning up DPoS protocol");

        ParticipatingNodes.Clear();
        _nodeStakes.Clear();
        _votePower.Clear();
        _delegations.Clear();
        _witnessVotes.Clear();
        _activeWitnesses.Clear();
        _witnessPerformance.Clear();
        _missedBlocks.Clear();
        _roundTimes.Clear();
        _currentWitnessIndex = 0;
        _currentRound = 0;

        await Task.CompletedTask;
    }

    public bool CanNodeParticipate(Node node)
    {
        if (!node.IsActive || 
            node.Status != NodeStatus.Online || 
            node.IsByzantine ||
            node.ConsensusAlgorithm != ConsensusAlgorithm.DelegatedProofOfStake)
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
        var totalVotes = _witnessVotes.Values.Sum();
        var votingParticipation = _nodeStakes.Count > 0 ? 
            (decimal)_delegations.Count(d => d.Value.Any()) / _nodeStakes.Count : 0m;

        return new Dictionary<string, object>
        {
            { "totalRounds", _roundTimes.Count },
            { "averageRoundTime", _roundTimes.Any() ? _roundTimes.Average() : 0.0 },
            { "totalStake", totalStake },
            { "averageStake", averageStake },
            { "nodeStakes", new Dictionary<Guid, decimal>(_nodeStakes) },
            { "activeWitnesses", _activeWitnesses.Count },
            { "witnessCount", _witnessCount },
            { "currentWitness", _activeWitnesses.Any() ? _activeWitnesses[_currentWitnessIndex].Id : Guid.Empty },
            { "witnessPerformance", new Dictionary<Guid, int>(_witnessPerformance) },
            { "missedBlocks", new Dictionary<Guid, int>(_missedBlocks) },
            { "totalVotes", totalVotes },
            { "witnessVotes", new Dictionary<Guid, decimal>(_witnessVotes) },
            { "votingParticipation", votingParticipation },
            { "delegations", _delegations.ToDictionary(d => d.Key, d => new Dictionary<Guid, decimal>(d.Value)) },
            { "blockTimeMs", _blockTimeMs }
        };
    }

    public async Task HandleNodeFaultAsync(Node node, FaultType faultType)
    {
        _logger.LogWarning("Handling node fault: {NodeId}, Fault: {FaultType}", node.Id, faultType);

        switch (faultType)
        {
            case FaultType.Byzantine:
                node.IsByzantine = true;
                // Remove from witnesses immediately
                if (_activeWitnesses.Any(w => w.Id == node.Id))
                {
                    _activeWitnesses.RemoveAll(w => w.Id == node.Id);
                    await ConductWitnessElection(); // Emergency re-election
                }
                break;
                
            case FaultType.Crash:
            case FaultType.NetworkPartition:
                node.Status = NodeStatus.Offline;
                // Remove from witnesses if they're offline
                if (_activeWitnesses.Any(w => w.Id == node.Id))
                {
                    _activeWitnesses.RemoveAll(w => w.Id == node.Id);
                    await ConductWitnessElection();
                }
                break;
                
            case FaultType.SlowResponse:
                // Track as missed block for witnesses
                if (_activeWitnesses.Any(w => w.Id == node.Id))
                {
                    _missedBlocks[node.Id] = _missedBlocks.GetValueOrDefault(node.Id, 0) + 1;
                    
                    // Remove witness if they've missed too many blocks
                    if (_missedBlocks[node.Id] >= _maxMissedBlocks)
                    {
                        _activeWitnesses.RemoveAll(w => w.Id == node.Id);
                        await ConductWitnessElection();
                    }
                }
                break;
                
            case FaultType.InvalidMessage:
                // Reduce vote power for invalid messages
                if (_votePower.ContainsKey(node.Id))
                {
                    _votePower[node.Id] = Math.Max(0, _votePower[node.Id] * 0.9m);
                }
                break;
        }

        // Remove from participating nodes if necessary
        if (!CanNodeParticipate(node))
        {
            ParticipatingNodes.RemoveAll(n => n.Id == node.Id);
            _logger.LogInformation("Node {NodeId} removed from DPoS participants due to fault", node.Id);
        }

        await Task.CompletedTask;
    }

    public async Task<ConsensusRound> PrepareRoundAsync(long roundNumber, Guid simulationRunId)
    {
        _logger.LogDebug("Preparing DPoS round {RoundNumber}", roundNumber);

        var round = new ConsensusRound
        {
            RoundNumber = roundNumber,
            Algorithm = Algorithm,
            SimulationRunId = simulationRunId,
            StartedAt = DateTime.UtcNow,
            TimeoutDuration = TimeSpan.FromMilliseconds(_blockTimeMs),
            ParticipatingNodes = ParticipatingNodes.Count(n => !n.IsByzantine && n.Status == NodeStatus.Online)
        };

        await Task.CompletedTask;
        return round;
    }

    public async Task<bool> ValidateBlockAsync(Block block)
    {
        _logger.LogDebug("Validating block {BlockNumber} for DPoS", block.BlockNumber);

        try
        {
            // Basic block validation
            if (!block.ValidateBlock())
            {
                _logger.LogWarning("Block {BlockNumber} failed basic validation", block.BlockNumber);
                return false;
            }

            // DPoS-specific validation
            if (block.Data == null || !block.Data.ContainsKey("witness") || !block.Data.ContainsKey("round"))
            {
                _logger.LogWarning("Block {BlockNumber} missing DPoS-specific data", block.BlockNumber);
                return false;
            }

            // Validate proposer is an active witness
            if (block.ProposerId.HasValue)
            {
                var proposer = _activeWitnesses.FirstOrDefault(w => w.Id == block.ProposerId.Value);
                if (proposer == null)
                {
                    _logger.LogWarning("Block {BlockNumber} proposed by non-witness {ProposerId}", 
                        block.BlockNumber, block.ProposerId);
                    return false;
                }

                // Byzantine witnesses cannot propose valid blocks
                if (proposer.IsByzantine)
                {
                    _logger.LogWarning("Block {BlockNumber} proposed by Byzantine witness {ProposerId}", 
                        block.BlockNumber, block.ProposerId);
                    return false;
                }

                // Validate witness was supposed to produce this block (round-robin check)
                var expectedWitnessIndex = (int)(block.BlockNumber % _activeWitnesses.Count);
                var expectedWitness = _activeWitnesses[expectedWitnessIndex];
                
                if (proposer.Id != expectedWitness.Id)
                {
                    _logger.LogWarning("Block {BlockNumber} proposed by wrong witness. Expected: {ExpectedId}, Actual: {ActualId}", 
                        block.BlockNumber, expectedWitness.Id, proposer.Id);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating DPoS block {BlockNumber}", block.BlockNumber);
            return false;
        }
    }

    #region Private Methods

    private async Task ConductWitnessElection()
    {
        _logger.LogInformation("Conducting witness election for {WitnessCount} positions", _witnessCount);

        // Clear current witnesses
        _activeWitnesses.Clear();

        // Reset witness votes
        foreach (var nodeId in _witnessVotes.Keys.ToList())
        {
            _witnessVotes[nodeId] = 0m;
        }

        // Process delegations and calculate witness votes
        foreach (var delegator in _delegations.Keys)
        {
            var delegatorVotePower = _votePower.GetValueOrDefault(delegator, 0m);
            var totalDelegated = _delegations[delegator].Values.Sum();
            
            // Distribute vote power based on delegations
            foreach (var delegation in _delegations[delegator])
            {
                var witnessId = delegation.Key;
                var delegatedAmount = delegation.Value;
                var votePortion = totalDelegated > 0 ? delegatedAmount / totalDelegated : 0m;
                var voteAmount = delegatorVotePower * votePortion;
                
                _witnessVotes[witnessId] = _witnessVotes.GetValueOrDefault(witnessId, 0m) + voteAmount;
            }
        }

        // Select top witnesses by vote count, then by stake, then by ID for deterministic ordering
        var eligibleCandidates = ParticipatingNodes
            .Where(n => !n.IsByzantine && n.Status == NodeStatus.Online)
            .OrderByDescending(n => _witnessVotes.GetValueOrDefault(n.Id, 0m))
            .ThenByDescending(n => _nodeStakes.GetValueOrDefault(n.Id, 0m))
            .ThenBy(n => n.Id) // Deterministic tiebreaker
            .Take(_witnessCount)
            .ToList();

        _activeWitnesses.AddRange(eligibleCandidates);

        // Reset witness index
        _currentWitnessIndex = 0;

        _logger.LogInformation("Elected {Count} witnesses: {WitnessIds}", 
            _activeWitnesses.Count, 
            string.Join(", ", _activeWitnesses.Select(w => w.Id.ToString()[..8])));

        await Task.CompletedTask;
    }

    private Node? GetCurrentWitness()
    {
        if (!_activeWitnesses.Any())
        {
            return null;
        }

        return _activeWitnesses[_currentWitnessIndex];
    }

    private async Task<bool> ProduceBlock(Node witness, ConsensusRound round, CancellationToken cancellationToken)
    {
        try
        {
            // Simulate block production time
            var productionDelay = _blockTimeMs / 2; // Half the block time for production
            await Task.Delay(productionDelay, cancellationToken);

            // Check if witness is still online and not Byzantine
            if (witness.IsByzantine || witness.Status != NodeStatus.Online)
            {
                return false;
            }

            // Track successful block production
            _witnessPerformance[witness.Id] = _witnessPerformance.GetValueOrDefault(witness.Id, 0) + 1;
            
            _logger.LogDebug("Witness {WitnessId} produced block for round {RoundNumber}", 
                witness.Id, round.RoundNumber);

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during block production by witness {WitnessId}", witness.Id);
            return false;
        }
    }

    private async Task HandleMissedBlock(Node witness)
    {
        _missedBlocks[witness.Id] = _missedBlocks.GetValueOrDefault(witness.Id, 0) + 1;
        
        _logger.LogWarning("Witness {WitnessId} missed block. Total missed: {MissedCount}", 
            witness.Id, _missedBlocks[witness.Id]);

        // Remove witness if they've missed too many blocks
        if (_missedBlocks[witness.Id] >= _maxMissedBlocks)
        {
            _activeWitnesses.RemoveAll(w => w.Id == witness.Id);
            _logger.LogWarning("Witness {WitnessId} removed for missing {MissedCount} blocks", 
                witness.Id, _missedBlocks[witness.Id]);
            
            await ConductWitnessElection();
        }

        await Task.CompletedTask;
    }

    private async Task<List<bool>> ValidateWithWitnesses(Node producer, ConsensusRound round, CancellationToken cancellationToken)
    {
        var validationResults = new List<bool>();
        var validatingWitnesses = _activeWitnesses.Where(w => w.Id != producer.Id).ToList();

        var validationTasks = validatingWitnesses.Select(async witness =>
        {
            try
            {
                // Simulate validation time
                var validationDelay = _blockTimeMs / 4; // Quarter of block time for validation
                await Task.Delay(validationDelay, cancellationToken);

                // Byzantine witnesses may provide false validations
                if (witness.IsByzantine)
                {
                    return _random.NextDouble() < 0.3; // 30% chance of correct validation from Byzantine witness
                }

                // Honest witnesses validate correctly
                return witness.Status == NodeStatus.Online;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        });

        var results = await Task.WhenAll(validationTasks);
        validationResults.AddRange(results);

        _logger.LogDebug("Validation results for round {RoundNumber}: {Validations}/{Total}", 
            round.RoundNumber, results.Count(v => v), results.Length);

        return validationResults;
    }

    private async Task DistributeRewards(Node producer, List<bool> validationResults)
    {
        // Reward the block producer
        _nodeStakes[producer.Id] += _witnessReward;
        producer.StakeAmount = _nodeStakes[producer.Id];

        // Reward validators who participated
        var validatingWitnesses = _activeWitnesses.Where(w => w.Id != producer.Id).ToList();
        for (int i = 0; i < validationResults.Count && i < validatingWitnesses.Count; i++)
        {
            if (validationResults[i])
            {
                var validator = validatingWitnesses[i];
                var validatorReward = _witnessReward * 0.3m; // 30% of producer reward
                _nodeStakes[validator.Id] += validatorReward;
                validator.StakeAmount = _nodeStakes[validator.Id];
            }
        }

        // Reward voters (delegators)
        foreach (var delegatorId in _delegations.Keys)
        {
            var totalDelegated = _delegations[delegatorId].Values.Sum();
            if (totalDelegated > 0)
            {
                _nodeStakes[delegatorId] += _voterReward;
                var delegator = ParticipatingNodes.FirstOrDefault(n => n.Id == delegatorId);
                if (delegator != null)
                {
                    delegator.StakeAmount = _nodeStakes[delegatorId];
                }
            }
        }

        _logger.LogDebug("Distributed rewards: Producer {ProducerId} +{ProducerReward}, {ValidatorCount} validators", 
            producer.Id, _witnessReward, validationResults.Count(v => v));

        await Task.CompletedTask;
    }

    private void ApplyConfiguration(Dictionary<string, object> configuration)
    {
        if (configuration.TryGetValue("witnessCount", out var witnessCount))
        {
            _witnessCount = Convert.ToInt32(witnessCount);
        }

        if (configuration.TryGetValue("minimumStake", out var minStake))
        {
            _minimumStake = Convert.ToDecimal(minStake);
        }

        if (configuration.TryGetValue("blockTimeMs", out var blockTime))
        {
            _blockTimeMs = Convert.ToInt32(blockTime);
        }

        if (configuration.TryGetValue("witnessReward", out var reward))
        {
            _witnessReward = Convert.ToDecimal(reward);
        }

        if (configuration.TryGetValue("voterReward", out var voterReward))
        {
            _voterReward = Convert.ToDecimal(voterReward);
        }

        if (configuration.TryGetValue("timeoutMs", out var timeout))
        {
            _timeoutMs = Convert.ToInt32(timeout);
        }

        if (configuration.TryGetValue("maxMissedBlocks", out var maxMissed))
        {
            _maxMissedBlocks = Convert.ToInt32(maxMissed);
        }

        if (configuration.TryGetValue("delegationFee", out var fee))
        {
            _delegationFee = Convert.ToDecimal(fee);
        }

        // Validate configuration
        if (_witnessCount <= 0)
        {
            throw new ArgumentException("Witness count must be positive");
        }

        if (_minimumStake <= 0)
        {
            throw new ArgumentException("Minimum stake must be positive");
        }

        if (_blockTimeMs <= 0)
        {
            throw new ArgumentException("Block time must be positive");
        }

        if (_witnessReward < 0)
        {
            throw new ArgumentException("Witness reward cannot be negative");
        }
    }

    #endregion
}