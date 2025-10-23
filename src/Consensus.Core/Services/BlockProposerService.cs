using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Consensus.Core.Services;

/// <summary>
/// Interface for block proposer selection strategies
/// </summary>
public interface IBlockProposerService
{
    /// <summary>
    /// Selects the next block proposer based on consensus algorithm
    /// </summary>
    Task<ProposerSelectionResult> SelectProposerAsync(ProposerSelectionRequest request);
    
    /// <summary>
    /// Validates if a node is eligible to propose a block
    /// </summary>
    Task<bool> ValidateProposerEligibilityAsync(ProposerValidationRequest request);
    
    /// <summary>
    /// Gets proposer selection statistics for analysis
    /// </summary>
    Task<ProposerSelectionStats> GetSelectionStatsAsync(Guid simulationId);
}

/// <summary>
/// Block proposer selection service implementation
/// </summary>
public class BlockProposerService : IBlockProposerService
{
    private readonly ILogger<BlockProposerService> _logger;
    private readonly Random _random;

    public BlockProposerService(ILogger<BlockProposerService> logger)
    {
        _logger = logger;
        _random = new Random();
    }

    public async Task<ProposerSelectionResult> SelectProposerAsync(ProposerSelectionRequest request)
    {
        _logger.LogDebug("Selecting proposer for block {BlockNumber} using {Algorithm}", 
            request.BlockNumber, request.Algorithm);

        try
        {
            var eligibleNodes = request.Nodes
                .Where(n => n.Status == NodeStatus.Online && !n.IsByzantine)
                .ToList();

            if (!eligibleNodes.Any())
            {
                return new ProposerSelectionResult
                {
                    Success = false,
                    ErrorMessage = "No eligible nodes available for block proposal",
                    SelectedProposer = null
                };
            }

            var proposer = request.Algorithm switch
            {
                ConsensusAlgorithm.ProofOfWork => await SelectProofOfWorkProposerAsync(eligibleNodes, request),
                ConsensusAlgorithm.ProofOfStake => await SelectProofOfStakeProposerAsync(eligibleNodes, request),
                ConsensusAlgorithm.ProofOfElapsedTime => await SelectPoetProposerAsync(eligibleNodes, request),
                ConsensusAlgorithm.PracticalByzantineFaultTolerance => await SelectPbftProposerAsync(eligibleNodes, request),
                _ => await SelectRandomProposerAsync(eligibleNodes, request)
            };

            if (proposer != null)
            {
                _logger.LogInformation("Selected node {NodeId} as proposer for block {BlockNumber}", 
                    proposer.Id, request.BlockNumber);

                return new ProposerSelectionResult
                {
                    Success = true,
                    SelectedProposer = proposer,
                    SelectionReason = GetSelectionReason(request.Algorithm, proposer),
                    SelectionData = GetSelectionData(request.Algorithm, proposer, request)
                };
            }

            return new ProposerSelectionResult
            {
                Success = false,
                ErrorMessage = "Failed to select a valid proposer",
                SelectedProposer = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting proposer for block {BlockNumber}", request.BlockNumber);
            return new ProposerSelectionResult
            {
                Success = false,
                ErrorMessage = $"Proposer selection error: {ex.Message}",
                SelectedProposer = null
            };
        }
    }

    public async Task<bool> ValidateProposerEligibilityAsync(ProposerValidationRequest request)
    {
        _logger.LogDebug("Validating proposer eligibility for node {NodeId}", request.ProposerId);

        try
        {
            var proposer = request.Nodes.FirstOrDefault(n => n.Id == request.ProposerId);
            if (proposer == null)
            {
                _logger.LogWarning("Proposer node {NodeId} not found", request.ProposerId);
                return false;
            }

            // Basic eligibility checks
            if (proposer.Status != NodeStatus.Online)
            {
                _logger.LogWarning("Proposer node {NodeId} is not online", request.ProposerId);
                return false;
            }

            if (proposer.IsByzantine)
            {
                _logger.LogWarning("Byzantine node {NodeId} cannot be a valid proposer", request.ProposerId);
                return false;
            }

            // Algorithm-specific validation
            var algorithmSpecificValid = request.Algorithm switch
            {
                ConsensusAlgorithm.ProofOfWork => ValidatePoWProposerEligibility(proposer, request),
                ConsensusAlgorithm.ProofOfStake => ValidatePoSProposerEligibility(proposer, request),
                ConsensusAlgorithm.ProofOfElapsedTime => ValidatePoetProposerEligibility(proposer, request),
                ConsensusAlgorithm.PracticalByzantineFaultTolerance => ValidatePbftProposerEligibility(proposer, request),
                _ => true // Unknown algorithms default to basic validation
            };

            return algorithmSpecificValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating proposer eligibility for node {NodeId}", request.ProposerId);
            return false;
        }
    }

    public async Task<ProposerSelectionStats> GetSelectionStatsAsync(Guid simulationId)
    {
        _logger.LogDebug("Getting proposer selection stats for simulation {SimulationId}", simulationId);

        try
        {
            // In a real implementation, this would query the database for historical proposer data
            // For now, return mock statistics
            return new ProposerSelectionStats
            {
                SimulationId = simulationId,
                TotalBlocksProposed = 100,
                UniqueProposers = 5,
                ProposerDistribution = new Dictionary<Guid, ProposerDistributionInfo>(),
                AverageSelectionTime = TimeSpan.FromMilliseconds(50),
                SelectionSuccessRate = 0.98m,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting proposer selection stats for simulation {SimulationId}", simulationId);
            throw;
        }
    }

    // Algorithm-specific proposer selection methods
    private async Task<Node?> SelectProofOfWorkProposerAsync(List<Node> eligibleNodes, ProposerSelectionRequest request)
    {
        _logger.LogDebug("Selecting PoW proposer (mining simulation)");

        // In PoW, the "proposer" is the miner who successfully solves the puzzle
        // For simulation, we'll use computational power weighting
        var weightedNodes = eligibleNodes.Select(node => new
        {
            Node = node,
            // Simulate mining power based on node properties
            MiningPower = CalculateMiningPower(node),
            // Simulate mining time (exponential distribution)
            MiningTime = GenerateExponentialTime(CalculateMiningPower(node))
        }).ToList();

        // Select the node with the shortest mining time (first to solve puzzle)
        var winner = weightedNodes.OrderBy(n => n.MiningTime).First();
        
        _logger.LogDebug("PoW proposer selected: Node {NodeId} with mining time {MiningTime}ms", 
            winner.Node.Id, winner.MiningTime);

        return winner.Node;
    }

    private async Task<Node?> SelectProofOfStakeProposerAsync(List<Node> eligibleNodes, ProposerSelectionRequest request)
    {
        _logger.LogDebug("Selecting PoS proposer (stake-weighted)");

        // For PoS, selection probability is proportional to stake
        var totalStake = eligibleNodes.Sum(n => GetNodeStake(n));
        if (totalStake <= 0)
        {
            // If no stakes defined, use equal probability
            return eligibleNodes[_random.Next(eligibleNodes.Count)];
        }

        var randomValue = _random.NextDouble() * (double)totalStake;
        decimal cumulativeStake = 0;

        foreach (var node in eligibleNodes)
        {
            cumulativeStake += GetNodeStake(node);
            if (randomValue <= (double)cumulativeStake)
            {
                _logger.LogDebug("PoS proposer selected: Node {NodeId} with stake {Stake}", 
                    node.Id, GetNodeStake(node));
                return node;
            }
        }

        // Fallback to last node
        return eligibleNodes.Last();
    }

    private async Task<Node?> SelectPoetProposerAsync(List<Node> eligibleNodes, ProposerSelectionRequest request)
    {
        _logger.LogDebug("Selecting PoET proposer (shortest wait time)");

        // In PoET, each node gets a wait time, and the shortest wait time wins
        var nodeWaitTimes = eligibleNodes.Select(node => new
        {
            Node = node,
            WaitTime = GeneratePoetWaitTime(node, request)
        }).ToList();

        var winner = nodeWaitTimes.OrderBy(n => n.WaitTime).First();
        
        _logger.LogDebug("PoET proposer selected: Node {NodeId} with wait time {WaitTime}ms", 
            winner.Node.Id, winner.WaitTime);

        return winner.Node;
    }

    private async Task<Node?> SelectPbftProposerAsync(List<Node> eligibleNodes, ProposerSelectionRequest request)
    {
        _logger.LogDebug("Selecting PBFT proposer (round-robin or leader-based)");

        // In PBFT, leader selection can be round-robin or deterministic
        // For simulation, we'll use round-robin based on block number
        var leaderIndex = (int)(request.BlockNumber % eligibleNodes.Count);
        var leader = eligibleNodes[leaderIndex];

        _logger.LogDebug("PBFT proposer selected: Node {NodeId} (round-robin leader for block {BlockNumber})", 
            leader.Id, request.BlockNumber);

        return leader;
    }

    private async Task<Node?> SelectRandomProposerAsync(List<Node> eligibleNodes, ProposerSelectionRequest request)
    {
        _logger.LogDebug("Selecting random proposer");

        var proposer = eligibleNodes[_random.Next(eligibleNodes.Count)];
        
        _logger.LogDebug("Random proposer selected: Node {NodeId}", proposer.Id);
        return proposer;
    }

    // Algorithm-specific validation methods
    private bool ValidatePoWProposerEligibility(Node proposer, ProposerValidationRequest request)
    {
        // In PoW, any node can potentially mine a block
        // Validation would typically involve checking the proof of work
        return true; // Simplified for simulation
    }

    private bool ValidatePoSProposerEligibility(Node proposer, ProposerValidationRequest request)
    {
        // In PoS, proposer must have stake
        var stake = GetNodeStake(proposer);
        return stake > 0;
    }

    private bool ValidatePoetProposerEligibility(Node proposer, ProposerValidationRequest request)
    {
        // In PoET, proposer must have the shortest valid wait time
        // This would typically involve verifying the PoET proof
        return true; // Simplified for simulation
    }

    private bool ValidatePbftProposerEligibility(Node proposer, ProposerValidationRequest request)
    {
        // In PBFT, only the current leader can propose
        var eligibleNodes = request.Nodes.Where(n => n.Status == NodeStatus.Online && !n.IsByzantine).ToList();
        var expectedLeaderIndex = (int)(request.BlockNumber % eligibleNodes.Count);
        var expectedLeader = eligibleNodes[expectedLeaderIndex];
        
        return proposer.Id == expectedLeader.Id;
    }

    // Helper methods
    private decimal CalculateMiningPower(Node node)
    {
        // Simulate mining power based on node properties
        // In reality, this would be based on computational resources
        return 1.0m + (decimal)_random.NextDouble(); // Random mining power between 1.0 and 2.0
    }

    private decimal GetNodeStake(Node node)
    {
        // Get stake from node data or configuration
        if (node.Configuration?.ContainsKey("stake") == true)
        {
            if (decimal.TryParse(node.Configuration["stake"].ToString(), out var stake))
            {
                return stake;
            }
        }

        // Fallback to direct stake amount property
        return node.StakeAmount > 0 ? node.StakeAmount : 100m;
    }

    private double GenerateExponentialTime(decimal rate)
    {
        // Generate exponentially distributed time (simulating mining)
        var lambda = (double)rate / 1000.0; // Convert to appropriate scale
        var u = _random.NextDouble();
        return -Math.Log(1 - u) / lambda * 1000; // Return in milliseconds
    }

    private int GeneratePoetWaitTime(Node node, ProposerSelectionRequest request)
    {
        // Generate PoET wait time based on node and block
        // In reality, this would be cryptographically generated
        var seed = $"{node.Id}{request.BlockNumber}{request.PreviousBlockHash}";
        var hash = ComputeHash(seed);
        var waitTime = Math.Abs(BitConverter.ToInt32(hash, 0)) % 10000; // 0-10 seconds
        
        return waitTime;
    }

    private byte[] ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
    }

    private string GetSelectionReason(ConsensusAlgorithm algorithm, Node proposer)
    {
        return algorithm switch
        {
            ConsensusAlgorithm.ProofOfWork => "Shortest mining time (simulated)",
            ConsensusAlgorithm.ProofOfStake => $"Stake-weighted selection (stake: {GetNodeStake(proposer)})",
            ConsensusAlgorithm.ProofOfElapsedTime => "Shortest PoET wait time",
            ConsensusAlgorithm.PracticalByzantineFaultTolerance => "Round-robin leader selection",
            _ => "Random selection"
        };
    }

    private Dictionary<string, object> GetSelectionData(ConsensusAlgorithm algorithm, Node proposer, ProposerSelectionRequest request)
    {
        var data = new Dictionary<string, object>
        {
            ["algorithm"] = algorithm.ToString(),
            ["proposerId"] = proposer.Id,
            ["blockNumber"] = request.BlockNumber,
            ["selectionTime"] = DateTime.UtcNow
        };

        switch (algorithm)
        {
            case ConsensusAlgorithm.ProofOfWork:
                data["miningPower"] = CalculateMiningPower(proposer);
                break;
            case ConsensusAlgorithm.ProofOfStake:
                data["stake"] = GetNodeStake(proposer);
                break;
            case ConsensusAlgorithm.ProofOfElapsedTime:
                data["waitTime"] = GeneratePoetWaitTime(proposer, request);
                break;
            case ConsensusAlgorithm.PracticalByzantineFaultTolerance:
                var eligibleNodes = request.Nodes.Where(n => n.Status == NodeStatus.Online && !n.IsByzantine).ToList();
                data["leaderIndex"] = request.BlockNumber % eligibleNodes.Count;
                break;
        }

        return data;
    }
}

// Supporting models and records
public record ProposerSelectionRequest
{
    public required IEnumerable<Node> Nodes { get; init; }
    public required ConsensusAlgorithm Algorithm { get; init; }
    public required long BlockNumber { get; init; }
    public string? PreviousBlockHash { get; init; }
    public Guid SimulationId { get; init; }
    public Dictionary<string, object>? AdditionalData { get; init; }
}

public record ProposerValidationRequest
{
    public required Guid ProposerId { get; init; }
    public required IEnumerable<Node> Nodes { get; init; }
    public required ConsensusAlgorithm Algorithm { get; init; }
    public required long BlockNumber { get; init; }
    public string? PreviousBlockHash { get; init; }
    public Dictionary<string, object>? AdditionalData { get; init; }
}

public record ProposerSelectionResult
{
    public required bool Success { get; init; }
    public Node? SelectedProposer { get; init; }
    public string? ErrorMessage { get; init; }
    public string? SelectionReason { get; init; }
    public Dictionary<string, object>? SelectionData { get; init; }
    public TimeSpan SelectionDuration { get; init; }
}

public record ProposerSelectionStats
{
    public required Guid SimulationId { get; init; }
    public required int TotalBlocksProposed { get; init; }
    public required int UniqueProposers { get; init; }
    public required Dictionary<Guid, ProposerDistributionInfo> ProposerDistribution { get; init; }
    public required TimeSpan AverageSelectionTime { get; init; }
    public required decimal SelectionSuccessRate { get; init; }
    public required DateTime LastUpdated { get; init; }
}

public record ProposerDistributionInfo
{
    public required Guid NodeId { get; init; }
    public required int BlocksProposed { get; init; }
    public required decimal SelectionPercentage { get; init; }
    public required TimeSpan TotalSelectionTime { get; init; }
    public required TimeSpan AverageSelectionTime { get; init; }
    public DateTime? LastSelectedAt { get; init; }
}