using Consensus.Core.Enums;
using System.Text.Json.Serialization;

namespace Consensus.Core.Models;

/// <summary>
/// Comprehensive simulation metrics tracking overall performance
/// </summary>
public class DetailedSimulationMetrics
{
    public Guid SimulationId { get; set; }
    public ConsensusAlgorithm ConsensusAlgorithm { get; set; }
    public int NodeCount { get; set; }
    public int TargetRounds { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public SimulationStatus Status { get; set; }
    
    // Block and Transaction Metrics
    public int TotalBlocks { get; set; }
    public int TotalTransactions { get; set; }
    public int SuccessfulRounds { get; set; }
    public int FailedRounds { get; set; }
    
    // Performance Metrics
    public TimeSpan AverageBlockTime { get; set; }
    public double ThroughputTps { get; set; }
    public double ConsensusEfficiency { get; set; }
    public TimeSpan NetworkLatency { get; set; }
    
    // Network Metrics
    public int ForkCount { get; set; }
    public int OrphanBlocks { get; set; }
    
    [JsonIgnore]
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.UtcNow - StartTime;
    
    [JsonIgnore]
    public double SuccessRate => SuccessfulRounds + FailedRounds > 0 ? 
        (double)SuccessfulRounds / (SuccessfulRounds + FailedRounds) * 100 : 0;
}

/// <summary>
/// Metrics for individual consensus rounds
/// </summary>
public class RoundMetrics
{
    public int RoundNumber { get; set; }
    public TimeSpan Duration { get; set; }
    public Guid ProposerNodeId { get; set; }
    public ConsensusAlgorithm ProposerAlgorithm { get; set; }
    
    // Block Metrics
    public int BlocksProposed { get; set; }
    public int BlocksAccepted { get; set; }
    public int BlocksRejected { get; set; }
    public int TransactionsProcessed { get; set; }
    
    // Consensus Metrics
    public bool ConsensusReached { get; set; }
    public int ParticipatingNodes { get; set; }
    public int VotesReceived { get; set; }
    public int NetworkMessages { get; set; }
    public TimeSpan AverageLatency { get; set; }
    
    // Success Tracking
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Performance metrics for individual nodes
/// </summary>
public class NodeMetrics
{
    public Guid NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    
    // Activity Metrics
    public int BlocksProposed { get; set; }
    public int BlocksAccepted { get; set; }
    public int VotesCast { get; set; }
    public int MessagesReceived { get; set; }
    public int MessagesSent { get; set; }
    
    // Performance Metrics
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan NetworkLatency { get; set; }
    public TimeSpan Uptime { get; set; }
    
    // Status Tracking
    public NodeStatus Status { get; set; }
    public DateTime LastActivity { get; set; }
    public double ConsensusParticipation { get; set; }
    
    // Algorithm-specific Metrics
    public decimal StakeAmount { get; set; }
    public decimal ReputationScore { get; set; }
    public DateTime Timestamp { get; set; }
    
    [JsonIgnore]
    public double BlockAcceptanceRate => BlocksProposed > 0 ? 
        (double)BlocksAccepted / BlocksProposed * 100 : 0;
}

/// <summary>
/// Consensus events for tracking important simulation events
/// </summary>
public class ConsensusEvent
{
    public string EventType { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
    public Guid? NodeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    public string Severity { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Comprehensive simulation summary with all key metrics and analysis
/// </summary>
public class SimulationSummary
{
    public Guid SimulationId { get; set; }
    public ConsensusAlgorithm ConsensusAlgorithm { get; set; }
    public TimeSpan Duration { get; set; }
    
    // Round Statistics
    public int TotalRounds { get; set; }
    public int SuccessfulRounds { get; set; }
    public int FailedRounds { get; set; }
    
    // Block Statistics
    public int TotalBlocks { get; set; }
    public int TotalTransactions { get; set; }
    public TimeSpan AverageBlockTime { get; set; }
    
    // Performance Metrics
    public double ThroughputTps { get; set; }
    public double ConsensusEfficiency { get; set; }
    public TimeSpan NetworkLatency { get; set; }
    
    // Network Statistics
    public int NodeCount { get; set; }
    public int ForkCount { get; set; }
    public int OrphanBlocks { get; set; }
    
    // Detailed Analysis
    public Dictionary<Guid, NodePerformanceMetrics> NodePerformance { get; set; } = new();
    public RoundStatistics RoundStatistics { get; set; } = new();
    public List<ConsensusEvent> ConsensusEvents { get; set; } = new();
    
    public DateTime GeneratedAt { get; set; }
    
    [JsonIgnore]
    public double SuccessRate => TotalRounds > 0 ? (double)SuccessfulRounds / TotalRounds * 100 : 0;
    
    [JsonIgnore]
    public double BlocksPerSecond => Duration.TotalSeconds > 0 ? TotalBlocks / Duration.TotalSeconds : 0;
    
    [JsonIgnore]
    public string PerformanceGrade => ConsensusEfficiency switch
    {
        >= 95 => "A+",
        >= 90 => "A",
        >= 85 => "B+",
        >= 80 => "B",
        >= 75 => "C+",
        >= 70 => "C",
        >= 60 => "D",
        _ => "F"
    };
}

/// <summary>
/// Node performance summary metrics
/// </summary>
public class NodePerformanceMetrics
{
    public Guid NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    
    // Aggregated Activity
    public int TotalBlocksProposed { get; set; }
    public int TotalBlocksAccepted { get; set; }
    public int TotalVotes { get; set; }
    
    // Performance Summary
    public TimeSpan AverageResponseTime { get; set; }
    public double UptimePercentage { get; set; }
    public double ConsensusParticipation { get; set; }
    
    // Final State
    public decimal FinalStake { get; set; }
    public decimal FinalReputation { get; set; }
    
    [JsonIgnore]
    public double BlockSuccessRate => TotalBlocksProposed > 0 ? 
        (double)TotalBlocksAccepted / TotalBlocksProposed * 100 : 0;
    
    [JsonIgnore]
    public string PerformanceRating => UptimePercentage switch
    {
        >= 99 => "Excellent",
        >= 95 => "Good",
        >= 90 => "Average",
        >= 80 => "Poor",
        _ => "Critical"
    };
}

/// <summary>
/// Statistical analysis of consensus rounds
/// </summary>
public class RoundStatistics
{
    public int TotalRounds { get; set; }
    public int SuccessfulRounds { get; set; }
    public int FailedRounds { get; set; }
    
    // Duration Analysis
    public TimeSpan AverageRoundDuration { get; set; }
    public TimeSpan MinRoundDuration { get; set; }
    public TimeSpan MaxRoundDuration { get; set; }
    
    // Activity Analysis
    public double AverageBlocksPerRound { get; set; }
    public double AverageTransactionsPerRound { get; set; }
    public double AverageParticipatingNodes { get; set; }
    
    [JsonIgnore]
    public double SuccessRate => TotalRounds > 0 ? (double)SuccessfulRounds / TotalRounds * 100 : 0;
    
    [JsonIgnore]
    public TimeSpan DurationVariance => MaxRoundDuration - MinRoundDuration;
}

/// <summary>
/// Exportable simulation results for external analysis
/// </summary>
public class ExportableSimulationResults
{
    public SimulationSummary Summary { get; set; } = new();
    public List<RoundMetrics> RoundData { get; set; } = new();
    public List<NodeMetrics> NodeData { get; set; } = new();
    public List<ConsensusEvent> EventLog { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string ExportFormat { get; set; } = "JSON";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public string ExportedBy { get; set; } = string.Empty;
}

/// <summary>
/// Real-time metrics update for live monitoring
/// </summary>
public class LiveMetricsUpdate
{
    public Guid SimulationId { get; set; }
    public int CurrentRound { get; set; }
    public int TotalBlocks { get; set; }
    public int TotalTransactions { get; set; }
    public double CurrentThroughput { get; set; }
    public double ConsensusEfficiency { get; set; }
    public int ActiveNodes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
}