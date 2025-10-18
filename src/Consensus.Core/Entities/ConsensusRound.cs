using System.ComponentModel.DataAnnotations;
using Consensus.Core.Enums;

namespace Consensus.Core.Entities;

/// <summary>
/// Represents a round of consensus algorithm execution
/// </summary>
public class ConsensusRound
{
    /// <summary>
    /// Unique identifier for the consensus round
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Sequential round number within the simulation
    /// </summary>
    [Required]
    public long RoundNumber { get; set; }

    /// <summary>
    /// Current status of the consensus round
    /// </summary>
    [Required]
    public ConsensusRoundStatus Status { get; set; } = ConsensusRoundStatus.Pending;

    /// <summary>
    /// Node acting as leader/proposer for this round (if applicable)
    /// </summary>
    public Guid? LeaderId { get; set; }

    /// <summary>
    /// Value or block being proposed in this round
    /// </summary>
    public Dictionary<string, object>? ProposedValue { get; set; }

    /// <summary>
    /// Final agreed upon value (if consensus reached)
    /// </summary>
    public Dictionary<string, object>? AgreedValue { get; set; }

    /// <summary>
    /// Number of participating nodes in this round
    /// </summary>
    public int ParticipatingNodes { get; set; }

    /// <summary>
    /// Number of votes received
    /// </summary>
    public int VotesReceived { get; set; }

    /// <summary>
    /// Number of positive votes
    /// </summary>
    public int PositiveVotes { get; set; }

    /// <summary>
    /// Number of negative votes
    /// </summary>
    public int NegativeVotes { get; set; }

    /// <summary>
    /// Threshold required for consensus (e.g., 2/3 majority)
    /// </summary>
    public int ConsensusThreshold { get; set; }

    /// <summary>
    /// Time when the round started
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Time when the round completed (success or failure)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Maximum duration allowed for this round
    /// </summary>
    public TimeSpan? TimeoutDuration { get; set; }

    /// <summary>
    /// Additional round-specific data
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }

    /// <summary>
    /// Error message if the round failed
    /// </summary>
    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Simulation run this round belongs to
    /// </summary>
    [Required]
    public Guid SimulationRunId { get; set; }

    // Navigation properties
    public virtual Node? Leader { get; set; }
    public virtual SimulationRun? SimulationRun { get; set; }
    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();

    /// <summary>
    /// Checks if the round has reached consensus
    /// </summary>
    public bool HasReachedConsensus()
    {
        return Status == ConsensusRoundStatus.Completed && 
               PositiveVotes >= ConsensusThreshold;
    }

    /// <summary>
    /// Checks if the round has timed out
    /// </summary>
    public bool HasTimedOut()
    {
        if (!TimeoutDuration.HasValue) return false;
        
        return DateTime.UtcNow - StartedAt > TimeoutDuration.Value;
    }

    /// <summary>
    /// Gets the current duration of the round
    /// </summary>
    public TimeSpan GetDuration()
    {
        var endTime = CompletedAt ?? DateTime.UtcNow;
        return endTime - StartedAt;
    }

    /// <summary>
    /// Calculates the consensus percentage
    /// </summary>
    public double GetConsensusPercentage()
    {
        if (ParticipatingNodes == 0) return 0;
        return (double)PositiveVotes / ParticipatingNodes * 100;
    }

    /// <summary>
    /// Starts the consensus round
    /// </summary>
    public void Start()
    {
        Status = ConsensusRoundStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Completes the consensus round successfully
    /// </summary>
    public void Complete(Dictionary<string, object>? agreedValue = null)
    {
        Status = ConsensusRoundStatus.Completed;
        AgreedValue = agreedValue ?? ProposedValue;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Fails the consensus round
    /// </summary>
    public void Fail(string? errorMessage = null)
    {
        Status = ConsensusRoundStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Times out the consensus round
    /// </summary>
    public void Timeout()
    {
        Status = ConsensusRoundStatus.TimedOut;
        ErrorMessage = "Consensus round timed out";
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Aborts the consensus round
    /// </summary>
    public void Abort(string? reason = null)
    {
        Status = ConsensusRoundStatus.Aborted;
        ErrorMessage = reason ?? "Consensus round aborted";
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a vote to the round and updates counters
    /// </summary>
    public void AddVote(Vote vote)
    {
        VotesReceived++;
        
        if (vote.Value)
        {
            PositiveVotes++;
        }
        else
        {
            NegativeVotes++;
        }
    }

    /// <summary>
    /// Checks if enough votes have been received for decision
    /// </summary>
    public bool CanMakeDecision()
    {
        // Can decide if we have enough positive votes for consensus
        if (PositiveVotes >= ConsensusThreshold) return true;
        
        // Can decide if it's impossible to reach consensus
        var remainingVotes = ParticipatingNodes - VotesReceived;
        var maxPossiblePositive = PositiveVotes + remainingVotes;
        
        return maxPossiblePositive < ConsensusThreshold;
    }

    /// <summary>
    /// Determines if consensus should be reached based on current votes
    /// </summary>
    public bool ShouldReachConsensus()
    {
        return PositiveVotes >= ConsensusThreshold;
    }

    public override string ToString()
    {
        return $"ConsensusRound #{RoundNumber} - Status: {Status}, Votes: {PositiveVotes}/{ParticipatingNodes}, Leader: {LeaderId?.ToString()[..8]}...";
    }
}