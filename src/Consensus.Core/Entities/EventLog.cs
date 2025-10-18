using System.ComponentModel.DataAnnotations;
using Consensus.Core.Enums;

namespace Consensus.Core.Entities;

/// <summary>
/// Represents an event log entry for audit trail and simulation events
/// </summary>
public class EventLog
{
    /// <summary>
    /// Unique identifier for the event log entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Simulation run this event belongs to
    /// </summary>
    [Required]
    public Guid SimulationRunId { get; set; }

    /// <summary>
    /// Node that performed the action (if applicable)
    /// </summary>
    public Guid? NodeId { get; set; }

    /// <summary>
    /// Consensus round this event relates to (if applicable)
    /// </summary>
    public Guid? ConsensusRoundId { get; set; }

    /// <summary>
    /// Block this event relates to (if applicable)
    /// </summary>
    public Guid? BlockId { get; set; }

    /// <summary>
    /// Type of event (simulation_start, round_start, block_proposed, vote_cast, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Event level (Info, Warning, Error, Debug)
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Level { get; set; } = "Info";

    /// <summary>
    /// Human-readable event message
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional structured event data as JSON
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Source component that generated the event
    /// </summary>
    [StringLength(100)]
    public string? Source { get; set; }

    /// <summary>
    /// Correlation ID for tracking related events
    /// </summary>
    [StringLength(50)]
    public string? CorrelationId { get; set; }

    // Navigation properties
    public virtual SimulationRun? SimulationRun { get; set; }
    public virtual Node? Node { get; set; }
    public virtual ConsensusRound? ConsensusRound { get; set; }
    public virtual Block? Block { get; set; }

    /// <summary>
    /// Creates an info level event log entry
    /// </summary>
    public static EventLog Info(Guid simulationRunId, string eventType, string message, Dictionary<string, object>? data = null)
    {
        return new EventLog
        {
            Id = Guid.NewGuid(),
            SimulationRunId = simulationRunId,
            EventType = eventType,
            Level = "Info",
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a warning level event log entry
    /// </summary>
    public static EventLog Warning(Guid simulationRunId, string eventType, string message, Dictionary<string, object>? data = null)
    {
        return new EventLog
        {
            Id = Guid.NewGuid(),
            SimulationRunId = simulationRunId,
            EventType = eventType,
            Level = "Warning",
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an error level event log entry
    /// </summary>
    public static EventLog Error(Guid simulationRunId, string eventType, string message, Dictionary<string, object>? data = null)
    {
        return new EventLog
        {
            Id = Guid.NewGuid(),
            SimulationRunId = simulationRunId,
            EventType = eventType,
            Level = "Error",
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a debug level event log entry
    /// </summary>
    public static EventLog Debug(Guid simulationRunId, string eventType, string message, Dictionary<string, object>? data = null)
    {
        return new EventLog
        {
            Id = Guid.NewGuid(),
            SimulationRunId = simulationRunId,
            EventType = eventType,
            Level = "Debug",
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        };
    }

    public override string ToString()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Level}: {EventType} - {Message}";
    }
}