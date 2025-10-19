namespace Consensus.Core.Enums;

/// <summary>
/// Represents the operational status of a node in the network
/// </summary>
public enum NodeStatus
{
    /// <summary>
    /// Node is offline and not participating
    /// </summary>
    Offline = 0,

    /// <summary>
    /// Node is online and ready to participate
    /// </summary>
    Online = 1,

    /// <summary>
    /// Node is starting up and initializing
    /// </summary>
    Starting = 2,

    /// <summary>
    /// Node is shutting down gracefully
    /// </summary>
    Stopping = 3,

    /// <summary>
    /// Node has failed or crashed
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Node is temporarily disconnected but may recover
    /// </summary>
    Disconnected = 5,

    /// <summary>
    /// Node is synchronizing with the network
    /// </summary>
    Synchronizing = 6,

    /// <summary>
    /// Node is in maintenance mode
    /// </summary>
    Maintenance = 7
}

/// <summary>
/// Supported consensus algorithms in the simulator
/// </summary>
public enum ConsensusAlgorithm
{
    /// <summary>
    /// Proof of Work consensus algorithm
    /// </summary>
    ProofOfWork = 0,

    /// <summary>
    /// Proof of Stake consensus algorithm
    /// </summary>
    ProofOfStake = 1,

    /// <summary>
    /// Delegated Proof of Stake consensus algorithm
    /// </summary>
    DelegatedProofOfStake = 2,

    /// <summary>
    /// Practical Byzantine Fault Tolerance algorithm
    /// </summary>
    PracticalByzantineFaultTolerance = 3,

    /// <summary>
    /// Raft consensus algorithm
    /// </summary>
    Raft = 4,

    /// <summary>
    /// HoneyBadgerBFT consensus algorithm
    /// </summary>
    HoneyBadgerBFT = 5,

    /// <summary>
    /// Tendermint consensus algorithm
    /// </summary>
    Tendermint = 6,

    /// <summary>
    /// Algorand consensus algorithm
    /// </summary>
    Algorand = 7,

    /// <summary>
    /// Stellar Consensus Protocol
    /// </summary>
    StellarConsensusProtocol = 8,

    /// <summary>
    /// FedCoin consensus (simplified for testing)
    /// </summary>
    FedCoin = 9,

    /// <summary>
    /// Proof of Elapsed Time consensus algorithm
    /// </summary>
    ProofOfElapsedTime = 10
}

/// <summary>
/// Status of a consensus round
/// </summary>
public enum ConsensusRoundStatus
{
    /// <summary>
    /// Round has been initiated but not started
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Round is currently in progress
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Round completed successfully with consensus
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Round failed to reach consensus
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Round was aborted due to network issues
    /// </summary>
    Aborted = 4,

    /// <summary>
    /// Round timed out
    /// </summary>
    TimedOut = 5
}

/// <summary>
/// Simplified round status for UI components
/// </summary>
public enum RoundStatus
{
    /// <summary>
    /// Round has been initiated but not started
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Round is starting up
    /// </summary>
    Starting = 1,

    /// <summary>
    /// Round is currently in progress
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Round completed successfully with consensus
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Round failed to reach consensus
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Round was aborted due to network issues
    /// </summary>
    Aborted = 5,

    /// <summary>
    /// Round timed out
    /// </summary>
    TimedOut = 6
}

/// <summary>
/// Types of votes in consensus rounds
/// </summary>
public enum VoteType
{
    /// <summary>
    /// Vote to propose a value or block
    /// </summary>
    Propose = 0,

    /// <summary>
    /// Vote to approve a proposal
    /// </summary>
    Approve = 1,

    /// <summary>
    /// Vote to reject a proposal
    /// </summary>
    Reject = 2,

    /// <summary>
    /// Pre-vote in multi-phase consensus
    /// </summary>
    PreVote = 3,

    /// <summary>
    /// Pre-commit vote in multi-phase consensus
    /// </summary>
    PreCommit = 4,

    /// <summary>
    /// Final commit vote
    /// </summary>
    Commit = 5,

    /// <summary>
    /// Vote to abort the current round
    /// </summary>
    Abort = 6
}

/// <summary>
/// Status of a transaction
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction is pending validation
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Transaction has been validated and is in mempool
    /// </summary>
    Validated = 1,

    /// <summary>
    /// Transaction has been included in a block
    /// </summary>
    Confirmed = 2,

    /// <summary>
    /// Transaction was rejected due to validation failure
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Transaction failed during execution
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Transaction was canceled before confirmation
    /// </summary>
    Canceled = 5
}

/// <summary>
/// Status of a simulation run
/// </summary>
public enum SimulationStatus
{
    /// <summary>
    /// Simulation has been created but not initialized
    /// </summary>
    Created = -1,

    /// <summary>
    /// Simulation is being set up
    /// </summary>
    Initializing = 0,

    /// <summary>
    /// Simulation is starting up
    /// </summary>
    Starting = 1,

    /// <summary>
    /// Simulation is ready to start
    /// </summary>
    Ready = 2,

    /// <summary>
    /// Simulation is currently running
    /// </summary>
    Running = 3,

    /// <summary>
    /// Simulation is paused
    /// </summary>
    Paused = 4,

    /// <summary>
    /// Simulation completed successfully
    /// </summary>
    Completed = 5,

    /// <summary>
    /// Simulation was stopped by user
    /// </summary>
    Stopped = 6,

    /// <summary>
    /// Simulation failed due to error
    /// </summary>
    Failed = 7,

    /// <summary>
    /// Simulation is being cleaned up
    /// </summary>
    Cleanup = 8
}

/// <summary>
/// Network topology types for simulations
/// </summary>
public enum NetworkTopologyType
{
    /// <summary>
    /// Full mesh - every node connected to every other node
    /// </summary>
    FullMesh = 0,

    /// <summary>
    /// Ring topology - nodes connected in a circular fashion
    /// </summary>
    Ring = 1,

    /// <summary>
    /// Star topology - central hub with spokes to other nodes
    /// </summary>
    Star = 2,

    /// <summary>
    /// Tree topology - hierarchical structure
    /// </summary>
    Tree = 3,

    /// <summary>
    /// Random topology - random connections between nodes
    /// </summary>
    Random = 4,

    /// <summary>
    /// Small world network - high clustering with short paths
    /// </summary>
    SmallWorld = 5,

    /// <summary>
    /// Scale-free network - follows power law degree distribution
    /// </summary>
    ScaleFree = 6,

    /// <summary>
    /// Grid topology - nodes arranged in a 2D grid
    /// </summary>
    Grid = 7,

    /// <summary>
    /// Custom topology defined by configuration
    /// </summary>
    Custom = 8
}