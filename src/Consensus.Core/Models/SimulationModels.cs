using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Interfaces;
using Consensus.Core.Protocols;

namespace Consensus.Core.Models;

/// <summary>
/// Request model for creating a new simulation
/// </summary>
public record CreateSimulationRequest
{
    public string Name { get; init; } = string.Empty;
    public ConsensusAlgorithm Algorithm { get; init; }
    public int NodeCount { get; init; }
    public int ByzantineNodeCount { get; init; }
    public int DurationSeconds { get; init; }
    public NetworkTopologyType NetworkTopology { get; init; }
    public int BlockTimeMs { get; init; }
    public int TransactionsPerBlock { get; init; }
    public int NetworkLatencyMs { get; init; }
    public int? RandomSeed { get; init; }
    public Dictionary<string, object> AlgorithmConfiguration { get; init; } = new();

    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Name is required");

        if (NodeCount < 3)
            errors.Add("At least 3 nodes are required");

        if (ByzantineNodeCount > NodeCount / 3)
            errors.Add("Byzantine nodes cannot exceed 1/3 of total nodes");

        if (DurationSeconds < 10)
            errors.Add("Duration must be at least 10 seconds");

        return errors;
    }
}

/// <summary>
/// Event arguments for simulation status changes
/// </summary>
public class SimulationStatusChangedEventArgs : EventArgs
{
    public SimulationRun Simulation { get; }

    public SimulationStatusChangedEventArgs(SimulationRun simulation)
    {
        Simulation = simulation;
    }
}

/// <summary>
/// Event arguments for completed consensus rounds
/// </summary>
public class RoundCompletedEventArgs : EventArgs
{
    public SimulationRun Simulation { get; }
    public ConsensusRound Round { get; }
    public ConsensusResult Result { get; }

    public RoundCompletedEventArgs(SimulationRun simulation, ConsensusRound round, ConsensusResult result)
    {
        Simulation = simulation;
        Round = round;
        Result = result;
    }
}

/// <summary>
/// Simulation metrics data model
/// </summary>
public record SimulationMetrics
{
    public int TotalNodes { get; init; }
    public int ActiveNodes { get; init; }
    public int ConsensusRounds { get; init; }
    public double AverageBlockTime { get; init; }
    public int TotalTransactions { get; init; }
    public double NetworkLatency { get; init; }
    public double FaultTolerance { get; init; }
    public double ThroughputTps { get; init; }
}

/// <summary>
/// Runtime state for an active simulation
/// </summary>
internal class SimulationRuntime
{
    public SimulationRun Simulation { get; set; } = null!;
    public IConsensusProtocol Protocol { get; set; } = null!;
    public CancellationTokenSource CancellationTokenSource { get; set; } = null!;
}