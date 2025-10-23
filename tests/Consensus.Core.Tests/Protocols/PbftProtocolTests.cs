using Xunit;
using Microsoft.Extensions.Logging;
using Consensus.Core.Protocols;
using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Consensus.Core.Tests.Protocols;

/// <summary>
/// Unit tests for PBFT (Practical Byzantine Fault Tolerance) consensus protocol
/// </summary>
public class PbftProtocolTests
{
    private readonly ILogger<PbftProtocol> _logger;
    private readonly PbftProtocol _protocol;

    public PbftProtocolTests()
    {
        _logger = new NullLogger<PbftProtocol>();
        _protocol = new PbftProtocol(_logger);
    }

    #region Basic Protocol Properties Tests

    [Fact]
    public void Protocol_Properties_ShouldBeCorrect()
    {
        // Act & Assert
        Assert.Equal("PBFT", _protocol.Name);
        Assert.Equal(ConsensusAlgorithm.PracticalByzantineFaultTolerance, _protocol.Algorithm);
        Assert.Equal(4, _protocol.MinimumNodes);
        Assert.True(_protocol.SupportsByzantineFaultTolerance);
    }

    [Fact]
    public void Description_ShouldContainCorrectInformation()
    {
        // Act
        var description = _protocol.Description;

        // Assert
        Assert.Contains("Byzantine Fault Tolerance", description);
        Assert.Contains("Three-phase", description);
    }

    #endregion

    #region Initialization Tests

    [Fact]
    public async Task InitializeAsync_WithValidNodes_ShouldSucceed()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        var config = new Dictionary<string, object>();

        // Act
        await _protocol.InitializeAsync(nodes, config);

        // Assert
        Assert.Equal(4, _protocol.ParticipatingNodes.Count);
    }

    [Fact]
    public async Task InitializeAsync_WithInsufficientNodes_ShouldThrowException()
    {
        // Arrange
        var nodes = CreateTestNodes(3); // Less than minimum required
        var config = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _protocol.InitializeAsync(nodes, config));
    }

    [Fact]
    public async Task InitializeAsync_WithConfiguration_ShouldApplySettings()
    {
        // Arrange
        var nodes = CreateTestNodes(7);
        var config = new Dictionary<string, object>
        {
            ["viewChangeTimeoutMs"] = 10000,
            ["messageTimeoutMs"] = 5000,
            ["enableMessageAuthentication"] = false,
            ["blockTimeMs"] = 1500,
            ["leaderReward"] = 25m,
            ["participationReward"] = 8m
        };

        // Act
        await _protocol.InitializeAsync(nodes, config);

        // Assert
        var metrics = _protocol.GetMetrics();
        Assert.Equal(10000, metrics["viewChangeTimeoutMs"]);
    }

    [Fact]
    public async Task InitializeAsync_WithSevenNodes_ShouldTolerateTwoByzantineNodes()
    {
        // Arrange - 7 nodes should allow f=2 (3*2+1=7)
        var nodes = CreateTestNodes(7);
        var config = new Dictionary<string, object>();

        // Act
        await _protocol.InitializeAsync(nodes, config);

        // Assert
        var metrics = _protocol.GetMetrics();
        Assert.Equal(2, metrics["maxFaultyNodes"]);
        Assert.Equal("2/7", metrics["byzantineFaultTolerance"]);
    }

    #endregion

    #region Consensus Round Execution Tests

    [Fact]
    public async Task ExecuteRoundAsync_WithValidRound_ShouldSucceed()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _protocol.InitializeAsync(nodes, new Dictionary<string, object>());
        
        var round = CreateTestConsensusRound(1);

        // Act
        var result = await _protocol.ExecuteRoundAsync(round);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.ProducedBlock);
        Assert.True(result.Duration.TotalMilliseconds > 0);
        Assert.Equal(4, result.ParticipatingNodes);
        Assert.NotNull(result.LeaderId);
    }

    [Fact]
    public async Task ExecuteRoundAsync_ShouldExecuteThreePhases()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _protocol.InitializeAsync(nodes, new Dictionary<string, object>());
        
        var round = CreateTestConsensusRound(1);

        // Act
        var result = await _protocol.ExecuteRoundAsync(round);

        // Assert
        Assert.True(result.Success);
        
        // Check that events include all three phases
        var events = result.Events;
        Assert.Contains(events, e => e.Type == EventType.LeaderSelection);
        Assert.Contains(events, e => e.Type == EventType.ProposalCreated);
        Assert.Contains(events, e => e.Type == EventType.VoteCast && e.Data.ContainsKey("voteType"));
        Assert.Contains(events, e => e.Type == EventType.ConsensusReached);
    }

    [Fact]
    public async Task ExecuteRoundAsync_ShouldTrackMetrics()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _protocol.InitializeAsync(nodes, new Dictionary<string, object>());
        
        var round = CreateTestConsensusRound(1);

        // Act
        var result = await _protocol.ExecuteRoundAsync(round);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("totalPhases", result.Metrics.Keys);
        Assert.Contains("totalMessages", result.Metrics.Keys);
        Assert.Contains("averagePhaseTimeMs", result.Metrics.Keys);
        Assert.Contains("messagesPerSecond", result.Metrics.Keys);
        Assert.Contains("consensusLatencyMs", result.Metrics.Keys);
        
        Assert.Equal(3, result.Metrics["totalPhases"]); // pre-prepare, prepare, commit
    }

    [Fact]
    public async Task ExecuteRoundAsync_WithCancellation_ShouldReturnFailure()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _protocol.InitializeAsync(nodes, new Dictionary<string, object>());
        
        var round = CreateTestConsensusRound(1);
        var cts = new CancellationTokenSource();

        // Act
        cts.Cancel(); // Cancel immediately
        var result = await _protocol.ExecuteRoundAsync(round, cts.Token);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cancelled", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Node Participation Tests

    [Fact]
    public void CanNodeParticipate_WithActiveOnlineNode_ShouldReturnTrue()
    {
        // Arrange
        var node = new Node
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            Status = NodeStatus.Online
        };

        // Act
        var canParticipate = _protocol.CanNodeParticipate(node);

        // Assert
        Assert.True(canParticipate);
    }

    [Fact]
    public void CanNodeParticipate_WithInactiveNode_ShouldReturnFalse()
    {
        // Arrange
        var node = new Node
        {
            Id = Guid.NewGuid(),
            IsActive = false,
            Status = NodeStatus.Online
        };

        // Act
        var canParticipate = _protocol.CanNodeParticipate(node);

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
            Status = NodeStatus.Offline
        };

        // Act
        var canParticipate = _protocol.CanNodeParticipate(node);

        // Assert
        Assert.False(canParticipate);
    }

    #endregion

    #region Fault Handling Tests

    [Fact]
    public async Task HandleNodeFaultAsync_WithByzantineFault_ShouldUpdateStatus()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _protocol.InitializeAsync(nodes, new Dictionary<string, object>());
        
        var faultyNode = nodes.First();

        // Act
        await _protocol.HandleNodeFaultAsync(faultyNode, FaultType.Byzantine);

        // Assert
        var metrics = _protocol.GetMetrics();
        Assert.Equal(1, metrics["faultyNodeCount"]);
        Assert.Equal("Maintained", metrics["safetyGuarantee"]); // Still within tolerance
    }

    [Fact]
    public async Task HandleNodeFaultAsync_WithPrimaryNodeFault_ShouldTriggerViewChange()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _protocol.InitializeAsync(nodes, new Dictionary<string, object>());
        
        var initialMetrics = _protocol.GetMetrics();
        var initialView = (int)initialMetrics["currentView"];
        var initialPrimary = initialMetrics["primaryNodeId"].ToString();
        
        // Find the primary node
        var primaryNode = nodes.First(n => n.Id.ToString() == initialPrimary);

        // Act
        await _protocol.HandleNodeFaultAsync(primaryNode, FaultType.Crash);

        // Assert
        var updatedMetrics = _protocol.GetMetrics();
        var newView = (int)updatedMetrics["currentView"];
        var newPrimary = updatedMetrics["primaryNodeId"].ToString();
        
        Assert.True(newView > initialView); // View should have changed
        Assert.NotEqual(initialPrimary, newPrimary); // Primary should have changed
    }

    [Fact]
    public async Task HandleNodeFaultAsync_WithTooManyFaults_ShouldViolateSafety()
    {
        // Arrange
        var nodes = CreateTestNodes(4); // f=1, so 2 faults should violate safety
        await _protocol.InitializeAsync(nodes, new Dictionary<string, object>());

        // Act - Make 2 nodes faulty (exceeds f=1)
        await _protocol.HandleNodeFaultAsync(nodes[0], FaultType.Byzantine);
        await _protocol.HandleNodeFaultAsync(nodes[1], FaultType.Crash);

        // Assert
        var metrics = _protocol.GetMetrics();
        Assert.Equal(2, metrics["faultyNodeCount"]);
        Assert.Equal("Violated", metrics["safetyGuarantee"]);
    }

    #endregion

    #region Metrics Tests

    [Fact]
    public async Task GetMetrics_ShouldReturnCompleteInformation()
    {
        // Arrange
        var nodes = CreateTestNodes(7);
        await _protocol.InitializeAsync(nodes, new Dictionary<string, object>());

        // Act
        var metrics = _protocol.GetMetrics();

        // Assert
        Assert.Equal("PBFT", metrics["algorithm"]);
        Assert.Equal(0, metrics["currentView"]); // Initial view
        Assert.Equal(0, metrics["sequenceNumber"]); // Initial sequence
        Assert.Contains("primaryNodeId", metrics.Keys);
        Assert.Equal(0, metrics["faultyNodeCount"]); // No faults initially
        Assert.Equal(2, metrics["maxFaultyNodes"]); // f=2 for n=7
        Assert.Equal(7, metrics["totalNodes"]);
        Assert.Equal(0, metrics["totalMessages"]); // No messages sent yet
        Assert.Equal("2/7", metrics["byzantineFaultTolerance"]);
        Assert.Equal("Maintained", metrics["safetyGuarantee"]);
    }

    [Fact]
    public async Task GetMetrics_AfterRound_ShouldUpdateCounters()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _protocol.InitializeAsync(nodes, new Dictionary<string, object>());
        
        var round = CreateTestConsensusRound(1);

        // Act
        await _protocol.ExecuteRoundAsync(round);
        var metrics = _protocol.GetMetrics();

        // Assert
        Assert.Equal(1, metrics["sequenceNumber"]); // Should be incremented
        Assert.True((int)metrics["totalMessages"] > 0); // Should have messages
    }

    #endregion

    #region Byzantine Fault Tolerance Formula Tests

    [Theory]
    [InlineData(4, 1)] // n=4, f=1
    [InlineData(5, 1)] // n=5, f=1
    [InlineData(6, 1)] // n=6, f=1
    [InlineData(7, 2)] // n=7, f=2
    [InlineData(10, 3)] // n=10, f=3
    public async Task ByzantineFaultToleranceFormula_ShouldBeCorrect(int nodeCount, int expectedF)
    {
        // Arrange
        var nodes = CreateTestNodes(nodeCount);

        // Act
        await _protocol.InitializeAsync(nodes, new Dictionary<string, object>());
        var metrics = _protocol.GetMetrics();

        // Assert
        Assert.Equal(expectedF, metrics["maxFaultyNodes"]);
        Assert.Equal($"{expectedF}/{nodeCount}", metrics["byzantineFaultTolerance"]);
    }

    [Theory]
    [InlineData(1)] // n=1, violates 3f+1
    [InlineData(2)] // n=2, violates 3f+1
    [InlineData(3)] // n=3, violates 3f+1
    public async Task InitializeAsync_WithInvalidNodeCount_ShouldThrow(int nodeCount)
    {
        // Arrange
        var nodes = CreateTestNodes(nodeCount);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _protocol.InitializeAsync(nodes, new Dictionary<string, object>()));
    }

    #endregion

    #region Multiple Round Tests

    [Fact]
    public async Task ExecuteMultipleRounds_ShouldMaintainConsistency()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _protocol.InitializeAsync(nodes, new Dictionary<string, object>());

        // Act - Execute multiple rounds
        var results = new List<Interfaces.ConsensusResult>();
        for (int i = 1; i <= 3; i++)
        {
            var round = CreateTestConsensusRound(i);
            var result = await _protocol.ExecuteRoundAsync(round);
            results.Add(result);
        }

        // Assert
        Assert.All(results, r => Assert.True(r.Success));
        
        // Sequence numbers should increment
        var metrics = _protocol.GetMetrics();
        Assert.Equal(3, metrics["sequenceNumber"]);
        
        // All rounds should have same primary (no view changes)
        var primaryNodes = results.Select(r => r.LeaderId).Distinct();
        Assert.Single(primaryNodes);
    }

    [Fact]
    public async Task ExecuteRounds_WithFaultyPrimary_ShouldTriggerViewChange()
    {
        // Arrange
        var nodes = CreateTestNodes(4);
        await _protocol.InitializeAsync(nodes, new Dictionary<string, object>());
        
        // Execute first round to establish baseline
        var round1 = CreateTestConsensusRound(1);
        var result1 = await _protocol.ExecuteRoundAsync(round1);
        var primaryId1 = result1.LeaderId;

        // Make primary faulty
        var primaryNode = nodes.First(n => n.Id.ToString() == primaryId1);
        await _protocol.HandleNodeFaultAsync(primaryNode, FaultType.Crash);

        // Act - Execute second round with new primary
        var round2 = CreateTestConsensusRound(2);
        var result2 = await _protocol.ExecuteRoundAsync(round2);

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.NotEqual(primaryId1, result2.LeaderId); // Primary should have changed
        
        var metrics = _protocol.GetMetrics();
        Assert.True((int)metrics["currentView"] > 0); // View should have changed
    }

    #endregion

    #region Performance and Stress Tests

    [Fact]
    public async Task ExecuteRound_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        var nodes = CreateTestNodes(7);
        await _protocol.InitializeAsync(nodes, new Dictionary<string, object>());
        
        var round = CreateTestConsensusRound(1);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _protocol.ExecuteRoundAsync(round);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete within 5 seconds
        Assert.True(result.Duration.TotalMilliseconds > 0);
    }

    [Fact]
    public async Task LargeNodeSet_ShouldHandleCorrectly()
    {
        // Arrange - Test with larger node set
        var nodes = CreateTestNodes(10); // f=3
        await _protocol.InitializeAsync(nodes, new Dictionary<string, object>());
        
        var round = CreateTestConsensusRound(1);

        // Act
        var result = await _protocol.ExecuteRoundAsync(round);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(10, result.ParticipatingNodes);
        
        var metrics = _protocol.GetMetrics();
        Assert.Equal(3, metrics["maxFaultyNodes"]);
    }

    #endregion

    #region Helper Methods

    private List<Node> CreateTestNodes(int count)
    {
        var nodes = new List<Node>();
        for (int i = 0; i < count; i++)
        {
            nodes.Add(new Node
            {
                Id = Guid.NewGuid(),
                Name = $"Node-{i}",
                IsActive = true,
                Status = NodeStatus.Online,
                StakeAmount = 100m,
                CreatedAt = DateTime.UtcNow
            });
        }
        return nodes;
    }

    private ConsensusRound CreateTestConsensusRound(long roundNumber)
    {
        return new ConsensusRound
        {
            Id = Guid.NewGuid(),
            RoundNumber = roundNumber,
            Algorithm = ConsensusAlgorithm.PracticalByzantineFaultTolerance,
            Status = ConsensusRoundStatus.Pending,
            ParticipatingNodes = 4,
            ConsensusThreshold = 3, // 2f+1 for f=1
            StartedAt = DateTime.UtcNow,
            SimulationRunId = Guid.NewGuid()
        };
    }

    #endregion
}