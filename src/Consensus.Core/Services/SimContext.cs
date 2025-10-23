using Consensus.Core.Entities;

namespace Consensus.Core.Services;

/// <summary>
/// Core simulation context that manages state and coordination for consensus protocol simulations.
/// Provides common functionality for all consensus protocol simulations.
/// This is an in-memory state manager - persistence is handled separately.
/// </summary>
public class SimContext
{
    /// <summary>
    /// Current simulation run being executed
    /// </summary>
    public SimulationRun SimulationRun { get; }

    /// <summary>
    /// Unique identifier for the simulation
    /// </summary>
    public Guid SimulationId => SimulationRun.Id;

    /// <summary>
    /// List of all participating nodes
    /// </summary>
    public List<Node> Nodes { get; }

    /// <summary>
    /// Current blockchain state (in-memory copy for performance)
    /// </summary>
    public List<Block> Blockchain { get; }

    /// <summary>
    /// Global simulation parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; }

    /// <summary>
    /// Random number generator (seeded for reproducibility)
    /// </summary>
    public Random Random { get; }

    /// <summary>
    /// In-memory event log for fast access during simulation
    /// </summary>
    public List<EventLog> EventLog { get; }

    /// <summary>
    /// Current active consensus round
    /// </summary>
    public ConsensusRound? CurrentRound { get; set; }

    /// <summary>
    /// Current blockchain height
    /// </summary>
    public int CurrentHeight => Blockchain.Count;

    /// <summary>
    /// Genesis block of the chain
    /// </summary>
    public Block? GenesisBlock => Blockchain.FirstOrDefault();

    /// <summary>
    /// Latest block in the chain
    /// </summary>
    public Block? LatestBlock => Blockchain.LastOrDefault();

    /// <summary>
    /// Total number of rounds completed
    /// </summary>
    public int CompletedRounds => EventLog.Count(e => e.EventType == "round_end");

    /// <summary>
    /// Initialize simulation context with a simulation run
    /// </summary>
    /// <param name="simulationRun">The simulation run to execute</param>
    /// <param name="nodes">List of nodes participating</param>
    /// <param name="parameters">Global simulation parameters</param>
    /// <param name="seed">Random seed for reproducible simulations</param>
    public SimContext(SimulationRun simulationRun, List<Node> nodes, Dictionary<string, object> parameters, int? seed = null)
    {
        SimulationRun = simulationRun ?? throw new ArgumentNullException(nameof(simulationRun));
        Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        Parameters = parameters ?? new Dictionary<string, object>();
        Blockchain = new List<Block>();
        EventLog = new List<EventLog>();
        
        // Initialize random number generator
        var randomSeed = seed ?? DateTime.UtcNow.Millisecond;
        Random = new Random(randomSeed);
        
        // Log context initialization
        LogEvent("simulation_init", $"Initialized simulation context with {nodes.Count} nodes", new Dictionary<string, object> { { "seed", randomSeed } });
    }

    /// <summary>
    /// Start a new consensus round
    /// </summary>
    /// <param name="leaderId">Node serving as round leader (if applicable)</param>
    /// <param name="proposedValue">Value being proposed for consensus</param>
    /// <param name="data">Additional round-specific data</param>
    public ConsensusRound StartNewRound(Guid? leaderId = null, string? proposedValue = null, Dictionary<string, object>? data = null)
    {
        // End previous round if still active
        if (CurrentRound is not null && CurrentRound.CompletedAt == null)
        {
            CurrentRound.CompletedAt = DateTime.UtcNow;
            LogEvent("round_end", $"Previous round {CurrentRound.RoundNumber} terminated", new Dictionary<string, object> { { "roundId", CurrentRound.Id } });
        }

        // Create new round
        var roundNumber = CompletedRounds + 1;
        CurrentRound = new ConsensusRound
        {
            Id = Guid.NewGuid(),
            SimulationRunId = SimulationId,
            RoundNumber = roundNumber,
            LeaderId = leaderId,
            ProposedValue = proposedValue is not null ? new Dictionary<string, object> { { "value", proposedValue } } : null,
            Data = data,
            Status = Consensus.Core.Enums.ConsensusRoundStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        LogEvent("round_start", $"Started round {roundNumber}", new Dictionary<string, object> { 
            { "roundId", CurrentRound.Id }, 
            { "leaderId", leaderId ?? Guid.Empty },
            { "participatingNodes", Nodes.Count(n => n.Status == Consensus.Core.Enums.NodeStatus.Online) }
        });

        return CurrentRound;
    }

    /// <summary>
    /// Add a new block to the blockchain
    /// </summary>
    /// <param name="proposerId">Node that proposed the block</param>
    /// <param name="data">Block data</param>
    public Block AddBlock(Guid proposerId, Dictionary<string, object>? data = null)
    {
        var blockNumber = CurrentHeight;
        var prevHash = LatestBlock?.Hash;
        
        // Create block data for hashing
        var blockDataStr = $"{blockNumber}|{prevHash}|{proposerId}|{DateTime.UtcNow:O}|{System.Text.Json.JsonSerializer.Serialize(data)}";
        var hash = ComputeHash(blockDataStr);

        var block = new Block
        {
            Id = Guid.NewGuid(),
            BlockNumber = blockNumber,
            Hash = hash,
            PreviousHash = blockNumber == 0 ? null : prevHash,
            ProposerId = proposerId,
            Data = data,
            Timestamp = DateTime.UtcNow,
            SimulationRunId = SimulationRun.Id,
            CreatedAt = DateTime.UtcNow
        };

        Blockchain.Add(block);
        
        LogEvent("block_created", $"Added block at number {blockNumber}", new Dictionary<string, object> { 
            { "blockId", block.Id }, 
            { "blockNumber", blockNumber }, 
            { "hash", hash[..16] + "..." },
            { "proposer", proposerId }
        });

        return block;
    }

    /// <summary>
    /// Get nodes that are eligible for participation in consensus
    /// </summary>
    /// <param name="role">Optional role filter</param>
    public List<Node> GetEligibleNodes(string? role = null)
    {
        return Nodes
            .Where(n => n.Status == Consensus.Core.Enums.NodeStatus.Online && n.IsActive)
            .Where(n => role == null || n.ConsensusAlgorithm.ToString().Contains(role, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Select a random node from eligible nodes
    /// </summary>
    public Node? SelectRandomNode(string? role = null)
    {
        var eligible = GetEligibleNodes(role);
        return eligible.Count == 0 ? null : eligible[Random.Next(eligible.Count)];
    }

    /// <summary>
    /// Log an event to the simulation event log
    /// </summary>
    /// <param name="eventType">Type of event</param>
    /// <param name="message">Human-readable message</param>
    /// <param name="data">Additional structured data</param>
    /// <param name="level">Log level (default: Info)</param>
    /// <param name="nodeId">Associated node (if any)</param>
    public void LogEvent(string eventType, string message, Dictionary<string, object>? data = null, string level = "Info", Guid? nodeId = null)
    {
        var eventLog = new EventLog
        {
            Id = Guid.NewGuid(),
            SimulationRunId = SimulationId,
            ConsensusRoundId = CurrentRound?.Id,
            NodeId = nodeId,
            EventType = eventType,
            Level = level,
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        };

        EventLog.Add(eventLog);
    }

    /// <summary>
    /// Get current in-memory state for persistence
    /// </summary>
    public (List<EventLog> Events, List<Block> Blocks, ConsensusRound? CurrentRound) GetPersistableState()
    {
        return (EventLog.ToList(), Blockchain.ToList(), CurrentRound);
    }

    /// <summary>
    /// Get nodes by consensus algorithm
    /// </summary>
    public List<Node> GetNodesByAlgorithm(Consensus.Core.Enums.ConsensusAlgorithm algorithm)
    {
        return Nodes.Where(n => n.ConsensusAlgorithm == algorithm).ToList();
    }

    /// <summary>
    /// Calculate network partition based on node connectivity
    /// </summary>
    public List<List<Node>> CalculateNetworkPartitions()
    {
        // Simplified partition detection - group connected nodes
        var visited = new HashSet<Guid>();
        var partitions = new List<List<Node>>();

        foreach (var node in Nodes)
        {
            if (!visited.Contains(node.Id))
            {
                var partition = new List<Node> { node };
                visited.Add(node.Id);
                
                // In this simulation, we'll consider nodes in the same status as connected
                var connectedNodes = Nodes
                    .Where(n => n.Status == node.Status && !visited.Contains(n.Id))
                    .ToList();
                
                partition.AddRange(connectedNodes);
                foreach (var connected in connectedNodes)
                {
                    visited.Add(connected.Id);
                }
                
                partitions.Add(partition);
            }
        }

        return partitions;
    }

    /// <summary>
    /// Calculate simulation statistics and metrics
    /// </summary>
    public Dictionary<string, object> CalculateStatistics()
    {
        var completedRounds = EventLog.Count(e => e.EventType == "round_end");
        var successfulRounds = EventLog.Count(e => e.EventType == "round_end" && !e.Message.Contains("failed"));
        
        var roundEvents = EventLog.Where(e => e.EventType == "round_start" || e.EventType == "round_end").ToList();
        var avgRoundTime = 0.0;
        
        if (roundEvents.Count >= 2)
        {
            var roundTimes = new List<double>();
            for (int i = 0; i < roundEvents.Count - 1; i += 2)
            {
                if (i + 1 < roundEvents.Count)
                {
                    var startEvent = roundEvents[i];
                    var endEvent = roundEvents[i + 1];
                    if (startEvent.EventType == "round_start" && endEvent.EventType == "round_end")
                    {
                        roundTimes.Add((endEvent.Timestamp - startEvent.Timestamp).TotalMilliseconds);
                    }
                }
            }
            avgRoundTime = roundTimes.Count > 0 ? roundTimes.Average() : 0.0;
        }

        return new Dictionary<string, object>
        {
            { "total_rounds", completedRounds },
            { "successful_rounds", successfulRounds },
            { "success_rate", completedRounds > 0 ? (double)successfulRounds / completedRounds : 0.0 },
            { "total_blocks", Blockchain.Count },
            { "blockchain_height", CurrentHeight },
            { "active_nodes", Nodes.Count(n => n.Status == Consensus.Core.Enums.NodeStatus.Online) },
            { "total_nodes", Nodes.Count },
            { "avg_round_time_ms", avgRoundTime },
            { "simulation_duration_sec", (DateTime.UtcNow - SimulationRun.CreatedAt).TotalSeconds },
            { "events_logged", EventLog.Count }
        };
    }

    /// <summary>
    /// Simple hash computation for blocks and other data
    /// </summary>
    private string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Mark simulation as completed and prepare for cleanup
    /// </summary>
    public void CompleteSimulation()
    {
        // Mark simulation as completed if not already
        if (SimulationRun.CompletedAt == null)
        {
            SimulationRun.CompletedAt = DateTime.UtcNow;
            SimulationRun.Status = Consensus.Core.Enums.SimulationStatus.Completed;
            LogEvent("simulation_complete", "Simulation completed successfully", CalculateStatistics());
        }
    }
}