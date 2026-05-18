using Microsoft.Extensions.Logging;
using Consensus.Core.Interfaces;
using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Consensus.Core.Protocols;

/// <summary>
/// Practical Byzantine Fault Tolerance (PBFT) consensus protocol implementation
/// Supports up to f=(n-1)/3 faulty nodes with guaranteed safety and liveness
/// </summary>
public class PbftProtocol : IConsensusProtocol
{
    private readonly ILogger<PbftProtocol> _logger;
    private readonly Dictionary<Guid, Node> _nodes;
    private readonly Dictionary<string, PbftMessage> _messageLog;
    private readonly Dictionary<string, HashSet<Guid>> _prePrepareMsgs;
    private readonly Dictionary<string, HashSet<Guid>> _prepareMsgs;
    private readonly Dictionary<string, HashSet<Guid>> _commitMsgs;
    private readonly Dictionary<Guid, int> _nodeViewNumber;
    private readonly Dictionary<Guid, bool> _nodeStatus; // false = faulty
    private readonly Dictionary<Guid, DateTime> _lastMessageTime;
    private Random _random;

    public ConsensusAlgorithm Algorithm => ConsensusAlgorithm.PracticalByzantineFaultTolerance;
    public string Name => "PBFT";
    public int MinimumNodes => 4; // Need at least 4 nodes to tolerate 1 faulty node (3f+1)
    public string Description => "Practical Byzantine Fault Tolerance - Three-phase consensus with Byzantine fault tolerance";
    public bool SupportsByzantineFaultTolerance => true;
    public List<Node> ParticipatingNodes { get; private set; } = new();

    // PBFT State
    private int _currentView = 0;
    private Guid _primaryNodeId;
    private int _sequenceNumber = 0;
    private long _currentRound = 0;
    private readonly Dictionary<int, PbftRoundState> _roundStates;

    // Configuration parameters
    private int _viewChangeTimeoutMs = 5000; // Time to wait before view change
    private int _messageTimeoutMs = 3000; // Time to wait for messages
    private bool _enableMessageAuthentication = true; // Digital signatures
    private int _maxFaultyNodes = 1; // f in (3f+1) formula
    private int _blockTimeMs = 2000; // Target block time
    private decimal _leaderReward = 20m; // Primary node reward
    private decimal _participationReward = 5m; // Non-primary node reward

    public PbftProtocol(ILogger<PbftProtocol> logger)
    {
        _logger = logger;
        _nodes = new Dictionary<Guid, Node>();
        _messageLog = new Dictionary<string, PbftMessage>();
        _prePrepareMsgs = new Dictionary<string, HashSet<Guid>>();
        _prepareMsgs = new Dictionary<string, HashSet<Guid>>();
        _commitMsgs = new Dictionary<string, HashSet<Guid>>();
        _nodeViewNumber = new Dictionary<Guid, int>();
        _nodeStatus = new Dictionary<Guid, bool>();
        _lastMessageTime = new Dictionary<Guid, DateTime>();
        _roundStates = new Dictionary<int, PbftRoundState>();
        _random = new Random();
    }

    public void SetRandom(Random rng) => _random = rng;

    public async Task InitializeAsync(IEnumerable<Node> nodes, Dictionary<string, object> configuration)
    {
        _logger.LogInformation("Initializing PBFT consensus protocol");

        var nodeList = nodes.ToList();
        if (nodeList.Count < MinimumNodes)
        {
            throw new ArgumentException($"PBFT requires at least {MinimumNodes} nodes");
        }

        // Apply configuration
        if (configuration != null)
        {
            ApplyConfiguration(configuration);
        }

        // Validate PBFT formula: n >= 3f + 1
        var totalNodes = nodeList.Count(n => n.IsActive && n.Status == NodeStatus.Online);
        _maxFaultyNodes = (totalNodes - 1) / 3;
        
        if (totalNodes < (3 * _maxFaultyNodes + 1))
        {
            throw new ArgumentException($"PBFT requires n >= 3f + 1. With {totalNodes} nodes, can only tolerate {_maxFaultyNodes} faulty nodes");
        }

        // Initialize participating nodes
        ParticipatingNodes = nodeList
            .Where(n => n.IsActive && n.Status == NodeStatus.Online)
            .OrderBy(n => n.Id) // Deterministic ordering for primary selection
            .ToList();

        // Initialize node tracking
        _nodes.Clear();
        _nodeStatus.Clear();
        _nodeViewNumber.Clear();
        _lastMessageTime.Clear();
        
        foreach (var node in ParticipatingNodes)
        {
            _nodes[node.Id] = node;
            _nodeStatus[node.Id] = true; // All nodes start as non-faulty
            _nodeViewNumber[node.Id] = 0;
            _lastMessageTime[node.Id] = DateTime.UtcNow;
        }

        // Select primary node (deterministic based on view number)
        SelectPrimaryNode();

        _logger.LogInformation($"PBFT initialized with {ParticipatingNodes.Count} nodes, can tolerate {_maxFaultyNodes} faulty nodes");
        _logger.LogInformation($"Primary node: {_primaryNodeId}");

        await Task.CompletedTask;
    }

    public async Task<ConsensusResult> ExecuteRoundAsync(ConsensusRound round, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var events = new List<Interfaces.ConsensusEvent>();
        var metrics = new Dictionary<string, object>();
        
        _currentRound = round.RoundNumber;
        _sequenceNumber++;

        _logger.LogInformation($"Starting PBFT round {_currentRound}, sequence {_sequenceNumber}, view {_currentView}");

        try
        {
            // Initialize round state
            var roundState = new PbftRoundState
            {
                SequenceNumber = _sequenceNumber,
                ViewNumber = _currentView,
                PrimaryNodeId = _primaryNodeId,
                StartTime = startTime
            };
            _roundStates[_sequenceNumber] = roundState;

            events.Add(new Interfaces.ConsensusEvent
            {
                Timestamp = DateTime.UtcNow,
                Type = EventType.LeaderSelection,
                NodeId = _primaryNodeId.ToString(),
                Message = $"Primary node selected for view {_currentView}"
            });

            // Phase 1: Pre-Prepare (Primary proposes a block)
            var prePrepareMsgId = await ExecutePrePreparePhase(round, events, cancellationToken);
            if (prePrepareMsgId == null)
            {
                return CreateFailureResult(startTime, events, "Pre-prepare phase failed", ParticipatingNodes.Count);
            }

            // Phase 2: Prepare (Backups validate and broadcast prepare messages)
            var prepareSuccess = await ExecutePreparePhase(prePrepareMsgId, events, cancellationToken);
            if (!prepareSuccess)
            {
                return CreateFailureResult(startTime, events, "Prepare phase failed", ParticipatingNodes.Count);
            }

            // Phase 3: Commit (All nodes commit after receiving enough prepare messages)
            var commitSuccess = await ExecuteCommitPhase(prePrepareMsgId, events, cancellationToken);
            if (!commitSuccess)
            {
                return CreateFailureResult(startTime, events, "Commit phase failed", ParticipatingNodes.Count);
            }

            // Create the final block
            var block = CreateBlock(round, roundState);
            
            // Update metrics
            var duration = DateTime.UtcNow - startTime;
            UpdateMetrics(metrics, duration, events);

            events.Add(new Interfaces.ConsensusEvent
            {
                Timestamp = DateTime.UtcNow,
                Type = EventType.ConsensusReached,
                NodeId = _primaryNodeId.ToString(),
                Message = $"PBFT consensus reached for sequence {_sequenceNumber}"
            });

            _logger.LogInformation($"PBFT round {_currentRound} completed successfully in {duration.TotalMilliseconds}ms");

            return new ConsensusResult
            {
                Success = true,
                Duration = duration,
                ParticipatingNodes = ParticipatingNodes.Count(n => _nodeStatus[n.Id]),
                Metrics = metrics,
                ProducedBlock = block,
                LeaderId = _primaryNodeId.ToString(),
                Events = events
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning($"PBFT round {_currentRound} was cancelled");
            return CreateFailureResult(startTime, events, "Round was cancelled", ParticipatingNodes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"PBFT round {_currentRound} failed with exception");
            return CreateFailureResult(startTime, events, $"Exception: {ex.Message}", ParticipatingNodes.Count);
        }
    }

    private async Task<string?> ExecutePrePreparePhase(ConsensusRound round, List<Interfaces.ConsensusEvent> events, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting pre-prepare phase");

        // Primary node creates pre-prepare message
        var blockData = CreateBlockData(round);
        var prePrepareMsgId = $"preprepare_{_currentView}_{_sequenceNumber}_{ComputeHash(blockData)}";
        
        var prePreparMsg = new PbftMessage
        {
            Type = PbftMessageType.PrePrepare,
            ViewNumber = _currentView,
            SequenceNumber = _sequenceNumber,
            BlockData = blockData,
            SenderId = _primaryNodeId,
            Timestamp = DateTime.UtcNow,
            MessageId = prePrepareMsgId
        };

        // Store the message
        _messageLog[prePrepareMsgId] = prePreparMsg;
        _prePrepareMsgs[prePrepareMsgId] = new HashSet<Guid> { _primaryNodeId };

        events.Add(new Interfaces.ConsensusEvent
        {
            Timestamp = DateTime.UtcNow,
            Type = EventType.ProposalCreated,
            NodeId = _primaryNodeId.ToString(),
            Message = $"Pre-prepare message sent for sequence {_sequenceNumber}",
            Data = new Dictionary<string, object> { ["messageId"] = prePrepareMsgId }
        });

        // Simulate message propagation delay
        await Task.Delay(_random.Next(50, 200), cancellationToken);

        return prePrepareMsgId;
    }

    private async Task<bool> ExecutePreparePhase(string prePrepareMsgId, List<Interfaces.ConsensusEvent> events, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting prepare phase");

        if (!_messageLog.TryGetValue(prePrepareMsgId, out var prePreparMsg))
        {
            return false;
        }

        _prepareMsgs[prePrepareMsgId] = new HashSet<Guid>();

        // Each backup node validates and sends prepare message
        foreach (var node in ParticipatingNodes.Where(n => n.Id != _primaryNodeId && _nodeStatus[n.Id]))
        {
            // Simulate message validation
            await Task.Delay(_random.Next(100, 300), cancellationToken);

            // Validate pre-prepare message (simplified)
            if (ValidatePrePrepareMessage(prePreparMsg, node))
            {
                _prepareMsgs[prePrepareMsgId].Add(node.Id);
                
                events.Add(new Interfaces.ConsensusEvent
                {
                    Timestamp = DateTime.UtcNow,
                    Type = EventType.VoteCast,
                    NodeId = node.Id.ToString(),
                    Message = $"Prepare message sent for sequence {_sequenceNumber}",
                    Data = new Dictionary<string, object> 
                    { 
                        ["messageId"] = prePrepareMsgId,
                        ["voteType"] = "prepare"
                    }
                });
            }
        }

        // Check if we have enough prepare messages (2f+1 including primary)
        var requiredPrepares = 2 * _maxFaultyNodes;
        var receivedPrepares = _prepareMsgs[prePrepareMsgId].Count;
        
        _logger.LogDebug($"Received {receivedPrepares} prepare messages, required: {requiredPrepares}");
        
        return receivedPrepares >= requiredPrepares;
    }

    private async Task<bool> ExecuteCommitPhase(string prePrepareMsgId, List<Interfaces.ConsensusEvent> events, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting commit phase");

        _commitMsgs[prePrepareMsgId] = new HashSet<Guid>();

        // All nodes (including primary) send commit messages after receiving enough prepares
        foreach (var node in ParticipatingNodes.Where(n => _nodeStatus[n.Id]))
        {
            // Simulate commit message processing
            await Task.Delay(_random.Next(50, 150), cancellationToken);

            _commitMsgs[prePrepareMsgId].Add(node.Id);
            
            events.Add(new Interfaces.ConsensusEvent
            {
                Timestamp = DateTime.UtcNow,
                Type = EventType.VoteCast,
                NodeId = node.Id.ToString(),
                Message = $"Commit message sent for sequence {_sequenceNumber}",
                Data = new Dictionary<string, object> 
                { 
                    ["messageId"] = prePrepareMsgId,
                    ["voteType"] = "commit"
                }
            });
        }

        // Check if we have enough commit messages (2f+1)
        var requiredCommits = 2 * _maxFaultyNodes + 1;
        var receivedCommits = _commitMsgs[prePrepareMsgId].Count;
        
        _logger.LogDebug($"Received {receivedCommits} commit messages, required: {requiredCommits}");
        
        return receivedCommits >= requiredCommits;
    }

    public bool CanNodeParticipate(Node node)
    {
        return node.IsActive && 
               node.Status == NodeStatus.Online && 
               _nodeStatus.GetValueOrDefault(node.Id, true);
    }

    public Dictionary<string, object> GetMetrics()
    {
        var faultyNodeCount = _nodeStatus.Values.Count(status => !status);
        var totalMessages = _messageLog.Count;
        var avgRoundTime = _roundStates.Values.Any() ? 
            _roundStates.Values.Average(r => (DateTime.UtcNow - r.StartTime).TotalMilliseconds) : 0;

        return new Dictionary<string, object>
        {
            ["algorithm"] = Name,
            ["currentView"] = _currentView,
            ["sequenceNumber"] = _sequenceNumber,
            ["primaryNodeId"] = _primaryNodeId.ToString(),
            ["faultyNodeCount"] = faultyNodeCount,
            ["maxFaultyNodes"] = _maxFaultyNodes,
            ["totalNodes"] = ParticipatingNodes.Count,
            ["totalMessages"] = totalMessages,
            ["viewChangeTimeoutMs"] = _viewChangeTimeoutMs,
            ["averageRoundTimeMs"] = avgRoundTime,
            ["byzantineFaultTolerance"] = $"{_maxFaultyNodes}/{ParticipatingNodes.Count}",
            ["safetyGuarantee"] = faultyNodeCount <= _maxFaultyNodes ? "Maintained" : "Violated"
        };
    }

    public async Task HandleNodeFaultAsync(Node node, FaultType faultType)
    {
        _logger.LogWarning($"Handling fault for node {node.Id}: {faultType}");

        _nodeStatus[node.Id] = false;
        _lastMessageTime[node.Id] = DateTime.UtcNow;

        var faultyCount = _nodeStatus.Values.Count(status => !status);
        
        if (faultyCount > _maxFaultyNodes)
        {
            _logger.LogError($"Too many faulty nodes ({faultyCount} > {_maxFaultyNodes}). Safety guarantee violated!");
        }

        // If primary node becomes faulty, initiate view change
        if (node.Id == _primaryNodeId)
        {
            _logger.LogInformation($"Primary node {node.Id} is faulty, initiating view change");
            await InitiateViewChange();
        }
    }

    private void ApplyConfiguration(Dictionary<string, object> configuration)
    {
        if (configuration.TryGetValue("viewChangeTimeoutMs", out var viewTimeout))
            _viewChangeTimeoutMs = Convert.ToInt32(viewTimeout);

        if (configuration.TryGetValue("messageTimeoutMs", out var msgTimeout))
            _messageTimeoutMs = Convert.ToInt32(msgTimeout);

        if (configuration.TryGetValue("enableMessageAuthentication", out var auth))
            _enableMessageAuthentication = Convert.ToBoolean(auth);

        if (configuration.TryGetValue("blockTimeMs", out var blockTime))
            _blockTimeMs = Convert.ToInt32(blockTime);

        if (configuration.TryGetValue("leaderReward", out var leaderReward))
            _leaderReward = Convert.ToDecimal(leaderReward);

        if (configuration.TryGetValue("participationReward", out var participationReward))
            _participationReward = Convert.ToDecimal(participationReward);

        _logger.LogInformation($"PBFT configured - ViewTimeout: {_viewChangeTimeoutMs}ms, MessageTimeout: {_messageTimeoutMs}ms");
    }

    private void SelectPrimaryNode()
    {
        // Primary node is selected based on view number: primary = view % n
        var activeNodes = ParticipatingNodes.Where(n => _nodeStatus[n.Id]).ToList();
        if (activeNodes.Any())
        {
            var primaryIndex = _currentView % activeNodes.Count;
            _primaryNodeId = activeNodes[primaryIndex].Id;
        }
    }

    private async Task InitiateViewChange()
    {
        _currentView++;
        SelectPrimaryNode();
        
        _logger.LogInformation($"View change completed. New view: {_currentView}, New primary: {_primaryNodeId}");
        
        // Clear previous round state for view change
        _messageLog.Clear();
        _prePrepareMsgs.Clear();
        _prepareMsgs.Clear();
        _commitMsgs.Clear();
        
        await Task.CompletedTask;
    }

    private bool ValidatePrePrepareMessage(PbftMessage message, Node validatingNode)
    {
        // Basic validation (simplified for simulation)
        return message.Type == PbftMessageType.PrePrepare &&
               message.ViewNumber == _currentView &&
               message.SenderId == _primaryNodeId &&
               !string.IsNullOrEmpty(message.BlockData);
    }

    private string CreateBlockData(ConsensusRound round)
    {
        var blockData = new
        {
            RoundNumber = round.RoundNumber,
            PreviousHash = "genesis", // Simplified for demo
            Timestamp = DateTime.UtcNow,
            PrimaryNode = _primaryNodeId,
            View = _currentView,
            Sequence = _sequenceNumber,
            Transactions = Array.Empty<object>() // Simplified for demo
        };

        return JsonSerializer.Serialize(blockData);
    }

    private Block CreateBlock(ConsensusRound round, PbftRoundState roundState)
    {
        var blockData = CreateBlockData(round);
        
        return new Block
        {
            Id = Guid.NewGuid(),
            BlockNumber = round.RoundNumber,
            Hash = ComputeHash(blockData),
            PreviousHash = "genesis", // Simplified for demo
            Timestamp = DateTime.UtcNow,
            ProposerId = _primaryNodeId,
            Data = new Dictionary<string, object> { ["blockData"] = blockData },
            IsValid = true,
            TransactionCount = 0, // Simplified for demo
            Size = Encoding.UTF8.GetByteCount(blockData),
            SimulationRunId = round.SimulationRunId
        };
    }

    private string ComputeHash(string data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private void UpdateMetrics(Dictionary<string, object> metrics, TimeSpan duration, List<Interfaces.ConsensusEvent> events)
    {
        var messageCount = events.Count(e => e.Type == EventType.VoteCast);
        var phaseCount = 3; // pre-prepare, prepare, commit
        
        metrics["totalPhases"] = phaseCount;
        metrics["totalMessages"] = messageCount;
        metrics["averagePhaseTimeMs"] = duration.TotalMilliseconds / phaseCount;
        metrics["messagesPerSecond"] = messageCount / Math.Max(duration.TotalSeconds, 0.1);
        metrics["consensusLatencyMs"] = duration.TotalMilliseconds;
        metrics["throughputTps"] = 1.0 / Math.Max(duration.TotalSeconds, 0.1);
        metrics["viewNumber"] = _currentView;
        metrics["sequenceNumber"] = _sequenceNumber;
    }

    private ConsensusResult CreateFailureResult(DateTime startTime, List<Interfaces.ConsensusEvent> events, string errorMessage, int participatingNodes)
    {
        return new ConsensusResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Duration = DateTime.UtcNow - startTime,
            ParticipatingNodes = participatingNodes,
            Metrics = GetMetrics(),
            Events = events
        };
    }
}

/// <summary>
/// PBFT message types for the three-phase protocol
/// </summary>
public enum PbftMessageType
{
    PrePrepare,
    Prepare,
    Commit,
    ViewChange,
    NewView
}

/// <summary>
/// PBFT protocol message
/// </summary>
public class PbftMessage
{
    public PbftMessageType Type { get; set; }
    public int ViewNumber { get; set; }
    public int SequenceNumber { get; set; }
    public string BlockData { get; set; } = string.Empty;
    public Guid SenderId { get; set; }
    public DateTime Timestamp { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string? Signature { get; set; } // For message authentication
}

/// <summary>
/// State tracking for a PBFT consensus round
/// </summary>
public class PbftRoundState
{
    public int SequenceNumber { get; set; }
    public int ViewNumber { get; set; }
    public Guid PrimaryNodeId { get; set; }
    public DateTime StartTime { get; set; }
    public bool PrePrepareReceived { get; set; }
    public bool PreparedState { get; set; }
    public bool CommittedState { get; set; }
}