using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Consensus.Core.Protocols;
using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Interfaces;

namespace Consensus.Core.Tests.Protocols;

public class PosProtocolTests
{
    private readonly Mock<ILogger<PosProtocol>> _mockLogger;
    private readonly PosProtocol _posProtocol;

    public PosProtocolTests()
    {
        _mockLogger = new Mock<ILogger<PosProtocol>>();
        _posProtocol = new PosProtocol(_mockLogger.Object);
    }

    [Fact]
    public void Protocol_Properties_ShouldHaveCorrectValues()
    {
        Assert.Equal(ConsensusAlgorithm.ProofOfStake, _posProtocol.Algorithm);
        Assert.Equal("PoS", _posProtocol.Name);
        Assert.True(_posProtocol.SupportsByzantineFaultTolerance);
        Assert.Equal(3, _posProtocol.MinimumNodes);
        Assert.Equal("Proof of Stake - Validator selection based on economic stake", _posProtocol.Description);
    }

    [Fact]
    public async Task InitializeAsync_WithValidNodes_ShouldSucceed()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        var configuration = new Dictionary<string, object>
        {
            { "minimumStake", 50m },
            { "slashingRate", 0.1m },
            { "blockTimeMs", 3000 }
        };

        // Act
        await _posProtocol.InitializeAsync(nodes, configuration);

        // Assert
        Assert.Equal(4, _posProtocol.ParticipatingNodes.Count);
        var metrics = _posProtocol.GetMetrics();
        Assert.Equal(50m, metrics["minimumStake"]);
    }

    [Fact]
    public async Task InitializeAsync_WithInsufficientNodes_ShouldThrowException()
    {
        // Arrange
        var nodes = CreateTestNodes(2);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _posProtocol.InitializeAsync(nodes, new Dictionary<string, object>()));
    }

    [Fact]
    public async Task InitializeAsync_WithInsufficientStake_ShouldThrowException()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        // Set stakes too low
        foreach (var node in nodes)
        {
            node.StakeAmount = 50m;
        }

        var configuration = new Dictionary<string, object>
        {
            { "minimumStake", 200m } // Higher than any node's stake
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _posProtocol.InitializeAsync(nodes, configuration));
    }

    [Fact]
    public async Task InitializeAsync_WithInvalidSlashingRate_ShouldThrowException()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        var configuration = new Dictionary<string, object>
        {
            { "slashingRate", 1.5m } // Invalid > 1.0
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _posProtocol.InitializeAsync(nodes, configuration));
    }

    [Fact]
    public async Task ExecuteRoundAsync_WithValidSetup_ShouldSelectValidator()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _posProtocol.InitializeAsync(nodes, new Dictionary<string, object> 
        { 
            { "minimumStake", 50m },
            { "blockTimeMs", 1000 } // Faster for testing
        });

        var round = await _posProtocol.PrepareRoundAsync(1, Guid.NewGuid());

        // Act
        var result = await _posProtocol.ExecuteRoundAsync(round, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.LeaderId);
        Assert.True(result.ParticipatingNodes > 0);
        Assert.Contains(result.Events, e => e.Type == EventType.LeaderSelection);
        Assert.Contains(result.Events, e => e.Type == EventType.ConsensusReached);
    }

    [Fact]
    public async Task ExecuteRoundAsync_ShouldDistributeRewardsToValidator()
    {
        // Arrange
        var nodes = CreateTestNodes(3);
        await _posProtocol.InitializeAsync(nodes, new Dictionary<string, object> 
        { 
            { "minimumStake", 50m },
            { "rewardAmount", 20m }
        });

        var initialMetrics = _posProtocol.GetMetrics();
        var initialStakes = (Dictionary<Guid, decimal>)initialMetrics["nodeStakes"];
        var initialTotalStake = (decimal)initialMetrics["totalStake"];

        var round = await _posProtocol.PrepareRoundAsync(1, Guid.NewGuid());

        // Act
        var result = await _posProtocol.ExecuteRoundAsync(round, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        
        var finalMetrics = _posProtocol.GetMetrics();
        var finalTotalStake = (decimal)finalMetrics["totalStake"];
        
        // Total stake should have increased due to rewards
        Assert.True(finalTotalStake > initialTotalStake);
    }

    [Fact]
    public void CanNodeParticipate_WithValidStakedNode_ShouldReturnTrue()
    {
        // Arrange
        var node = new Node
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            Status = NodeStatus.Online,
            IsByzantine = false,
            ConsensusAlgorithm = ConsensusAlgorithm.ProofOfStake,
            StakeAmount = 150m
        };

        // Act
        var canParticipate = _posProtocol.CanNodeParticipate(node);

        // Assert
        Assert.True(canParticipate);
    }

    [Fact]
    public void CanNodeParticipate_WithInsufficientStake_ShouldReturnFalse()
    {
        // Arrange
        var node = new Node
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            Status = NodeStatus.Online,
            IsByzantine = false,
            ConsensusAlgorithm = ConsensusAlgorithm.ProofOfStake,
            StakeAmount = 50m // Below default minimum of 100
        };

        // Act
        var canParticipate = _posProtocol.CanNodeParticipate(node);

        // Assert
        Assert.False(canParticipate);
    }

    [Fact]
    public void CanNodeParticipate_WithByzantineNode_ShouldReturnFalse()
    {
        // Arrange
        var node = new Node
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            Status = NodeStatus.Online,
            IsByzantine = true,
            ConsensusAlgorithm = ConsensusAlgorithm.ProofOfStake,
            StakeAmount = 150m
        };

        // Act
        var canParticipate = _posProtocol.CanNodeParticipate(node);

        // Assert
        Assert.False(canParticipate);
    }

    [Fact]
    public void CanNodeParticipate_WithOfflineNode_ShouldReturnFalse()
    {
        // Arrange
        var node = new Node
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            Status = NodeStatus.Offline,
            IsByzantine = false,
            ConsensusAlgorithm = ConsensusAlgorithm.ProofOfStake,
            StakeAmount = 150m
        };

        // Act
        var canParticipate = _posProtocol.CanNodeParticipate(node);

        // Assert
        Assert.False(canParticipate);
    }

    [Fact]
    public async Task HandleNodeFaultAsync_WithByzantineFault_ShouldApplySlashing()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _posProtocol.InitializeAsync(nodes, new Dictionary<string, object>());
        var faultyNode = nodes.First();
        
        var initialMetrics = _posProtocol.GetMetrics();
        var initialStakes = (Dictionary<Guid, decimal>)initialMetrics["nodeStakes"];
        var initialStake = initialStakes[faultyNode.Id];

        // Act
        await _posProtocol.HandleNodeFaultAsync(faultyNode, FaultType.Byzantine);

        // Assert
        Assert.True(faultyNode.IsByzantine);
        
        var finalMetrics = _posProtocol.GetMetrics();
        var finalStakes = (Dictionary<Guid, decimal>)finalMetrics["nodeStakes"];
        var penalties = (Dictionary<Guid, decimal>)finalMetrics["slashingPenalties"];
        
        // Stake should be reduced due to slashing
        Assert.True(finalStakes[faultyNode.Id] < initialStake);
        Assert.True(penalties[faultyNode.Id] > 0);
        Assert.DoesNotContain(faultyNode, _posProtocol.ParticipatingNodes);
    }

    [Fact]
    public async Task HandleNodeFaultAsync_WithCrashFault_ShouldMarkNodeOffline()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _posProtocol.InitializeAsync(nodes, new Dictionary<string, object>());
        var faultyNode = nodes.First();

        // Act
        await _posProtocol.HandleNodeFaultAsync(faultyNode, FaultType.Crash);

        // Assert
        Assert.Equal(NodeStatus.Offline, faultyNode.Status);
        Assert.DoesNotContain(faultyNode, _posProtocol.ParticipatingNodes);
    }

    [Fact]
    public async Task HandleNodeFaultAsync_WithSlowResponse_ShouldApplyMinorPenalty()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _posProtocol.InitializeAsync(nodes, new Dictionary<string, object>());
        var slowNode = nodes.First();
        
        var initialMetrics = _posProtocol.GetMetrics();
        var initialStakes = (Dictionary<Guid, decimal>)initialMetrics["nodeStakes"];
        var initialStake = initialStakes[slowNode.Id];

        // Act
        await _posProtocol.HandleNodeFaultAsync(slowNode, FaultType.SlowResponse);

        // Assert
        var finalMetrics = _posProtocol.GetMetrics();
        var finalStakes = (Dictionary<Guid, decimal>)finalMetrics["nodeStakes"];
        var penalties = (Dictionary<Guid, decimal>)finalMetrics["slashingPenalties"];
        
        // Should have minor penalty
        Assert.True(finalStakes[slowNode.Id] < initialStake);
        Assert.True(penalties[slowNode.Id] > 0);
        // Should still be able to participate if stake is above minimum
        Assert.True(finalStakes[slowNode.Id] >= 100m);
    }

    [Fact]
    public async Task ValidateBlockAsync_WithValidPosBlock_ShouldReturnTrue()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _posProtocol.InitializeAsync(nodes, new Dictionary<string, object>());

        var block = new Block
        {
            Id = Guid.NewGuid(),
            BlockNumber = 1,
            PreviousHash = "previousHash",
            SimulationRunId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            ProposerId = nodes.First().Id,
            Data = new Dictionary<string, object>
            {
                { "validator", nodes.First().Id.ToString() },
                { "stake", 200m }
            },
            Nonce = 0,
            Difficulty = 0,
            Size = 256,
            TransactionCount = 0
        };

        // Calculate and set correct hash
        block.Hash = block.CalculateHash();

        // Act
        var isValid = await _posProtocol.ValidateBlockAsync(block);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateBlockAsync_WithByzantineProposer_ShouldReturnFalse()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _posProtocol.InitializeAsync(nodes, new Dictionary<string, object>());
        
        var byzantineNode = nodes.First();
        byzantineNode.IsByzantine = true;

        var block = new Block
        {
            Id = Guid.NewGuid(),
            BlockNumber = 1,
            Hash = "validHash",
            PreviousHash = "previousHash",
            SimulationRunId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            ProposerId = byzantineNode.Id,
            Data = new Dictionary<string, object>
            {
                { "validator", byzantineNode.Id.ToString() },
                { "stake", 200m }
            },
            Nonce = 0,
            Difficulty = 0,
            Size = 256,
            TransactionCount = 0
        };

        // Act
        var isValid = await _posProtocol.ValidateBlockAsync(block);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateBlockAsync_WithMissingPosData_ShouldReturnFalse()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _posProtocol.InitializeAsync(nodes, new Dictionary<string, object>());

        var block = new Block
        {
            Id = Guid.NewGuid(),
            BlockNumber = 1,
            Hash = "validHash",
            PreviousHash = "previousHash",
            SimulationRunId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            ProposerId = nodes.First().Id,
            Data = new Dictionary<string, object>
            {
                // Missing "validator" and "stake" keys
                { "other", "data" }
            },
            Nonce = 0,
            Difficulty = 0,
            Size = 256,
            TransactionCount = 0
        };

        // Act
        var isValid = await _posProtocol.ValidateBlockAsync(block);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task CreateGenesisBlockAsync_ShouldCreateValidGenesisBlock()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _posProtocol.InitializeAsync(nodes, new Dictionary<string, object>());
        var simulationRunId = Guid.NewGuid();

        // Act
        var genesisBlock = await _posProtocol.CreateGenesisBlockAsync(simulationRunId);

        // Assert
        Assert.NotNull(genesisBlock);
        Assert.Equal(0, genesisBlock.BlockNumber);
        Assert.Equal(simulationRunId, genesisBlock.SimulationRunId);
        Assert.Null(genesisBlock.ProposerId);
        Assert.True(genesisBlock.Data.ContainsKey("genesis"));
        Assert.True((bool)genesisBlock.Data["genesis"]);
        Assert.Equal("PoS", genesisBlock.Data["protocol"]);
        Assert.True(genesisBlock.Data.ContainsKey("totalStake"));
        Assert.True(genesisBlock.Data.ContainsKey("validatorCount"));
    }

    [Fact]
    public void CalculateConsensusThreshold_ShouldReturnTwoThirdsMajority()
    {
        // Act & Assert
        Assert.Equal(2, _posProtocol.CalculateConsensusThreshold(3)); // 2/3 of 3 = 2
        Assert.Equal(3, _posProtocol.CalculateConsensusThreshold(4)); // 2/3 of 4 = 2.67 → 3
        Assert.Equal(4, _posProtocol.CalculateConsensusThreshold(5)); // 2/3 of 5 = 3.33 → 4
        Assert.Equal(7, _posProtocol.CalculateConsensusThreshold(10)); // 2/3 of 10 = 6.67 → 7
    }

    [Fact]
    public void SupportsNodeCount_WithValidCounts_ShouldReturnTrue()
    {
        // Act & Assert
        Assert.True(_posProtocol.SupportsNodeCount(3));
        Assert.True(_posProtocol.SupportsNodeCount(100));
        Assert.True(_posProtocol.SupportsNodeCount(1000));
    }

    [Fact]
    public void SupportsNodeCount_WithInvalidCounts_ShouldReturnFalse()
    {
        // Act & Assert
        Assert.False(_posProtocol.SupportsNodeCount(2));
        Assert.False(_posProtocol.SupportsNodeCount(1001));
    }

    [Fact]
    public void GetMetrics_ShouldReturnPosSpecificMetrics()
    {
        // Act
        var metrics = _posProtocol.GetMetrics();

        // Assert
        Assert.Contains("totalRounds", metrics.Keys);
        Assert.Contains("averageRoundTime", metrics.Keys);
        Assert.Contains("totalStake", metrics.Keys);
        Assert.Contains("averageStake", metrics.Keys);
        Assert.Contains("minimumStake", metrics.Keys);
        Assert.Contains("validatorCounts", metrics.Keys);
        Assert.Contains("nodeStakes", metrics.Keys);
        Assert.Contains("slashingPenalties", metrics.Keys);
        Assert.Contains("stakeDistribution", metrics.Keys);
    }

    [Fact]
    public async Task CleanupAsync_ShouldClearAllData()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _posProtocol.InitializeAsync(nodes, new Dictionary<string, object>());

        // Act
        await _posProtocol.CleanupAsync();

        // Assert
        Assert.Empty(_posProtocol.ParticipatingNodes);
        var metrics = _posProtocol.GetMetrics();
        Assert.Equal(0, metrics["totalRounds"]);
        Assert.Equal(0m, metrics["totalStake"]);
    }

    [Fact]
    public async Task StakeBasedSelection_ShouldFavorHigherStakeNodes()
    {
        // Arrange
        var nodes = CreateTestNodes(3);
        // Set different stakes
        nodes[0].StakeAmount = 500m; // High stake
        nodes[1].StakeAmount = 200m; // Medium stake
        nodes[2].StakeAmount = 100m; // Low stake

        await _posProtocol.InitializeAsync(nodes, new Dictionary<string, object>());

        // Run multiple rounds and track selections
        var selections = new Dictionary<Guid, int>();
        for (int i = 0; i < 20; i++)
        {
            var round = await _posProtocol.PrepareRoundAsync(i + 1, Guid.NewGuid());
            var result = await _posProtocol.ExecuteRoundAsync(round, CancellationToken.None);
            
            if (result.Success && Guid.TryParse(result.LeaderId, out var validatorId))
            {
                selections[validatorId] = selections.GetValueOrDefault(validatorId, 0) + 1;
            }
        }

        // Assert that higher stake node was selected more often
        // Note: This is probabilistic, but with enough rounds, the high-stake node should be selected more
        Assert.True(selections.Count > 0, "At least one validator should have been selected");
        
        if (selections.ContainsKey(nodes[0].Id) && selections.ContainsKey(nodes[2].Id))
        {
            // High stake node should be selected more often than low stake node
            Assert.True(selections[nodes[0].Id] >= selections[nodes[2].Id]);
        }
    }

    private List<Node> CreateTestNodes(int count)
    {
        var nodes = new List<Node>();
        for (int i = 0; i < count; i++)
        {
            nodes.Add(new Node
            {
                Id = Guid.NewGuid(),
                Name = $"Validator{i + 1}",
                IsActive = true,
                Status = NodeStatus.Online,
                IsByzantine = false,
                ConsensusAlgorithm = ConsensusAlgorithm.ProofOfStake,
                StakeAmount = 150m + (i * 50m), // Varying stakes: 150, 200, 250, 300... (well above minimum)
                NetworkLatency = 50 + (i * 10),
                SimulationRunId = Guid.NewGuid()
            });
        }
        return nodes;
    }
}