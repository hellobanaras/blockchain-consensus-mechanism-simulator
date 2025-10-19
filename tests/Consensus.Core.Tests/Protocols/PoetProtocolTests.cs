using Xunit;
using Consensus.Core.Protocols;
using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Consensus.Core.Tests.Protocols;

/// <summary>
/// Unit tests for the Proof of Elapsed Time (PoET) consensus protocol implementation
/// </summary>
public class PoetProtocolTests
{
    private readonly Mock<ILogger<PoetProtocol>> _mockLogger;
    private readonly PoetProtocol _poetProtocol;

    public PoetProtocolTests()
    {
        _mockLogger = new Mock<ILogger<PoetProtocol>>();
        _poetProtocol = new PoetProtocol(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act & Assert
        Assert.Equal(ConsensusAlgorithm.ProofOfElapsedTime, _poetProtocol.Algorithm);
        Assert.Equal("PoET", _poetProtocol.Name);
        Assert.True(_poetProtocol.SupportsByzantineFaultTolerance);
    }

    [Fact]
    public async Task InitializeAsync_WithValidNodes_ShouldSetupNodesCorrectly()
    {
        // Arrange
        var nodes = CreateTestNodes(5);
        var configuration = new Dictionary<string, object>
        {
            { "minWaitTimeMs", 1000 },
            { "maxWaitTimeMs", 5000 },
            { "blockTime", 2000 }
        };

        // Act
        await _poetProtocol.InitializeAsync(nodes, configuration);

        // Assert
        Assert.Equal(5, _poetProtocol.ParticipatingNodes.Count);
        Assert.All(_poetProtocol.ParticipatingNodes, node => 
            Assert.Equal(NodeStatus.Online, node.Status));
    }

    [Fact]
    public async Task InitializeAsync_WithInvalidConfiguration_ShouldThrowException()
    {
        // Arrange
        var nodes = CreateTestNodes(3);
        var invalidConfiguration = new Dictionary<string, object>
        {
            { "minWaitTimeMs", 5000 }, // Min > Max
            { "maxWaitTimeMs", 1000 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _poetProtocol.InitializeAsync(nodes, invalidConfiguration));
    }

    [Fact]
    public async Task PrepareRoundAsync_ShouldCreateValidConsensusRound()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _poetProtocol.InitializeAsync(nodes, GetDefaultConfiguration());
        var simulationRun = CreateTestSimulationRun();

        // Act
        var round = await _poetProtocol.PrepareRoundAsync(1, simulationRun.Id);

        // Assert
        Assert.NotNull(round);
        Assert.Equal(1, round.RoundNumber);
        Assert.Equal(simulationRun.Id, round.SimulationRunId);
        Assert.Equal(ConsensusRoundStatus.Pending, round.Status);
        Assert.Equal(4, round.ParticipatingNodes);
        Assert.True(round.ConsensusThreshold > 0);
    }

    [Fact]
    public async Task ExecuteRoundAsync_ShouldSelectLeaderBasedOnWaitTime()
    {
        // Arrange
        var nodes = CreateTestNodes(3);
        await _poetProtocol.InitializeAsync(nodes, GetDefaultConfiguration());
        var round = await _poetProtocol.PrepareRoundAsync(1, Guid.NewGuid());

        // Act
        var result = await _poetProtocol.ExecuteRoundAsync(round);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.LeaderId);
        Assert.Contains(nodes, n => n.Id.ToString() == result.LeaderId);
        Assert.NotEmpty(result.Events);
        
        // Should have events for leader selection and consensus reached
        Assert.Contains(result.Events, e => e.Type == EventType.LeaderSelection);
        Assert.Contains(result.Events, e => e.Type == EventType.ConsensusReached);
    }

    [Fact]
    public async Task ExecuteRoundAsync_WithByzantineNode_ShouldHandleCorrectly()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        nodes[0].IsByzantine = true; // Make first node Byzantine
        
        await _poetProtocol.InitializeAsync(nodes, GetDefaultConfiguration());
        var round = await _poetProtocol.PrepareRoundAsync(1, Guid.NewGuid());

        // Act
        var result = await _poetProtocol.ExecuteRoundAsync(round);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        
        // Byzantine node should not be selected as leader
        Assert.NotEqual(nodes[0].Id.ToString(), result.LeaderId);
        
        // Should have only honest nodes participating
        var honestNodeCount = nodes.Count(n => !n.IsByzantine);
        Assert.True(round.ParticipatingNodes <= honestNodeCount);
    }

    [Fact]
    public async Task ValidateBlockAsync_WithValidBlock_ShouldReturnTrue()
    {
        // Arrange
        var nodes = CreateTestNodes(3);
        await _poetProtocol.InitializeAsync(nodes, GetDefaultConfiguration());
        
        var block = new Block
        {
            Id = Guid.NewGuid(),
            BlockNumber = 1,
            PreviousHash = "genesis_hash",
            ProposerId = nodes[0].Id,
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, object>
            {
                { "waitTime", 2500 },
                { "proof", "valid_poet_proof" }
            }
        };
        
        // Calculate the correct hash for validation
        block.Hash = block.CalculateHash();

        // Act
        var isValid = await _poetProtocol.ValidateBlockAsync(block);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateBlockAsync_WithInvalidBlock_ShouldReturnFalse()
    {
        // Arrange
        var nodes = CreateTestNodes(3);
        await _poetProtocol.InitializeAsync(nodes, GetDefaultConfiguration());
        
        var block = new Block
        {
            Id = Guid.NewGuid(),
            BlockNumber = 1,
            Hash = "invalid_hash",
            PreviousHash = "genesis_hash",
            ProposerId = Guid.NewGuid(), // Unknown proposer
            Timestamp = DateTime.UtcNow,
            Data = null // Missing PoET-specific data
        };

        // Act
        var isValid = await _poetProtocol.ValidateBlockAsync(block);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task CreateGenesisBlockAsync_ShouldCreateValidGenesisBlock()
    {
        // Arrange
        var nodes = CreateTestNodes(3);
        await _poetProtocol.InitializeAsync(nodes, GetDefaultConfiguration());
        var simulationRunId = Guid.NewGuid();

        // Act
        var genesisBlock = await _poetProtocol.CreateGenesisBlockAsync(simulationRunId);

        // Assert
        Assert.NotNull(genesisBlock);
        Assert.Equal(0, genesisBlock.BlockNumber);
        Assert.Null(genesisBlock.PreviousHash);
        Assert.Equal(simulationRunId, genesisBlock.SimulationRunId);
        Assert.NotEmpty(genesisBlock.Hash);
        Assert.True(genesisBlock.ValidateBlock());
    }

    [Theory]
    [InlineData(3, 2)] // 3 nodes, consensus threshold should be 2
    [InlineData(4, 3)] // 4 nodes, consensus threshold should be 3
    [InlineData(5, 3)] // 5 nodes, consensus threshold should be 3
    [InlineData(6, 4)] // 6 nodes, consensus threshold should be 4
    public async Task CalculateConsensusThreshold_ShouldReturnCorrectValue(int nodeCount, int expectedThreshold)
    {
        // Arrange
        var nodes = CreateTestNodes(nodeCount);
        await _poetProtocol.InitializeAsync(nodes, GetDefaultConfiguration());

        // Act
        var threshold = _poetProtocol.CalculateConsensusThreshold(nodeCount);

        // Assert
        Assert.Equal(expectedThreshold, threshold);
    }

    [Fact]
    public void CalculateWaitTime_ShouldReturnValueInConfiguredRange()
    {
        // Arrange
        var minWaitTime = 1000;
        var maxWaitTime = 5000;
        var configuration = new Dictionary<string, object>
        {
            { "minWaitTimeMs", minWaitTime },
            { "maxWaitTimeMs", maxWaitTime }
        };

        // Act
        var waitTime1 = _poetProtocol.CalculateWaitTime(configuration);
        var waitTime2 = _poetProtocol.CalculateWaitTime(configuration);
        var waitTime3 = _poetProtocol.CalculateWaitTime(configuration);

        // Assert
        Assert.InRange(waitTime1, minWaitTime, maxWaitTime);
        Assert.InRange(waitTime2, minWaitTime, maxWaitTime);
        Assert.InRange(waitTime3, minWaitTime, maxWaitTime);

        // Wait times should vary (not always the same)
        var waitTimes = new[] { waitTime1, waitTime2, waitTime3 };
        Assert.True(waitTimes.Distinct().Count() > 1, "Wait times should vary");
    }

    [Fact]
    public async Task GetProtocolMetrics_ShouldReturnValidMetrics()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _poetProtocol.InitializeAsync(nodes, GetDefaultConfiguration());
        
        // Execute a few rounds to generate metrics
        for (int i = 1; i <= 3; i++)
        {
            var round = await _poetProtocol.PrepareRoundAsync(i, Guid.NewGuid());
            await _poetProtocol.ExecuteRoundAsync(round);
        }

        // Act
        var metrics = _poetProtocol.GetProtocolMetrics();

        // Assert
        Assert.NotNull(metrics);
        Assert.Contains("totalRounds", metrics.Keys);
        Assert.Contains("averageWaitTime", metrics.Keys);
        Assert.Contains("leaderDistribution", metrics.Keys);
        Assert.Contains("consensusEfficiency", metrics.Keys);

        Assert.Equal(3, metrics["totalRounds"]);
        Assert.True((double)metrics["averageWaitTime"] > 0);
    }

    [Fact]
    public void SupportsNodeCount_ShouldReturnTrueForValidCounts()
    {
        // Act & Assert
        Assert.True(_poetProtocol.SupportsNodeCount(3));
        Assert.True(_poetProtocol.SupportsNodeCount(10));
        Assert.True(_poetProtocol.SupportsNodeCount(100));
        
        Assert.False(_poetProtocol.SupportsNodeCount(1)); // Too few
        Assert.False(_poetProtocol.SupportsNodeCount(2)); // Too few
        Assert.False(_poetProtocol.SupportsNodeCount(1001)); // Too many
    }

    [Fact]
    public async Task Cleanup_ShouldResetProtocolState()
    {
        // Arrange
        var nodes = CreateTestNodes(3);
        await _poetProtocol.InitializeAsync(nodes, GetDefaultConfiguration());
        
        // Execute some rounds
        var round = await _poetProtocol.PrepareRoundAsync(1, Guid.NewGuid());
        await _poetProtocol.ExecuteRoundAsync(round);

        // Act
        await _poetProtocol.CleanupAsync();

        // Assert
        Assert.Empty(_poetProtocol.ParticipatingNodes);
        var metrics = _poetProtocol.GetProtocolMetrics();
        Assert.Equal(0, metrics["totalRounds"]);
    }

    // Helper methods
    private static List<Node> CreateTestNodes(int count)
    {
        var nodes = new List<Node>();
        for (int i = 0; i < count; i++)
        {
            nodes.Add(new Node
            {
                Id = Guid.NewGuid(),
                Name = $"Node_{i}",
                Status = NodeStatus.Online,
                ConsensusAlgorithm = ConsensusAlgorithm.ProofOfElapsedTime,
                IsActive = true,
                ComputationalPower = 100,
                ReputationScore = 100m,
                SimulationRunId = Guid.NewGuid()
            });
        }
        return nodes;
    }

    private static SimulationRun CreateTestSimulationRun()
    {
        return new SimulationRun
        {
            Id = Guid.NewGuid(),
            Name = "Test Simulation",
            ConsensusAlgorithm = ConsensusAlgorithm.ProofOfElapsedTime,
            NodeCount = 4,
            Status = Consensus.Core.Enums.SimulationStatus.Running
        };
    }

    private static Dictionary<string, object> GetDefaultConfiguration()
    {
        return new Dictionary<string, object>
        {
            { "minWaitTimeMs", 1000 },
            { "maxWaitTimeMs", 5000 },
            { "blockTime", 2000 },
            { "timeoutMs", 10000 }
        };
    }
}