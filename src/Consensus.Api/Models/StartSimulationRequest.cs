using System.ComponentModel.DataAnnotations;
using Consensus.Core.Enums;

namespace Consensus.Api.Models;

/// <summary>
/// Request model for starting a new consensus simulation
/// </summary>
public record StartSimulationRequest
{
    /// <summary>
    /// Name of the simulation run
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Consensus algorithm to use for the simulation
    /// </summary>
    [Required]
    public ConsensusAlgorithm Algorithm { get; init; }

    /// <summary>
    /// Number of nodes to create in the network
    /// </summary>
    [Required]
    [Range(3, 100, ErrorMessage = "Node count must be between 3 and 100")]
    public int NodeCount { get; init; }

    /// <summary>
    /// Number of Byzantine (faulty) nodes in the network
    /// </summary>
    [Range(0, 33, ErrorMessage = "Byzantine node count must be between 0 and 33% of total nodes")]
    public int ByzantineNodeCount { get; init; } = 0;

    /// <summary>
    /// Duration of the simulation in seconds
    /// </summary>
    [Range(10, 3600, ErrorMessage = "Duration must be between 10 seconds and 1 hour")]
    public int DurationSeconds { get; init; } = 300; // Default 5 minutes

    /// <summary>
    /// Network topology type for the simulation
    /// </summary>
    public NetworkTopologyType NetworkTopology { get; init; } = NetworkTopologyType.FullMesh;

    /// <summary>
    /// Target block time in milliseconds (time between block creation)
    /// </summary>
    [Range(1000, 60000, ErrorMessage = "Block time must be between 1 and 60 seconds")]
    public int BlockTimeMs { get; init; } = 5000; // Default 5 seconds

    /// <summary>
    /// Number of transactions per block
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Transactions per block must be between 1 and 1000")]
    public int TransactionsPerBlock { get; init; } = 10;

    /// <summary>
    /// Network latency simulation in milliseconds
    /// </summary>
    [Range(0, 2000, ErrorMessage = "Network latency must be between 0 and 2000ms")]
    public int NetworkLatencyMs { get; init; } = 100; // Default 100ms

    /// <summary>
    /// Custom configuration parameters specific to the consensus algorithm
    /// </summary>
    public Dictionary<string, object> AlgorithmConfiguration { get; init; } = new();

    /// <summary>
    /// Whether to enable detailed logging for the simulation
    /// </summary>
    public bool EnableDetailedLogging { get; init; } = false;

    /// <summary>
    /// Whether to automatically start the simulation after creation
    /// </summary>
    public bool AutoStart { get; init; } = true;

    /// <summary>
    /// Validates the request and returns any validation errors
    /// </summary>
    public IEnumerable<string> ValidateRequest()
    {
        var errors = new List<string>();

        // Validate Byzantine node count doesn't exceed 1/3 of total nodes
        if (ByzantineNodeCount > NodeCount / 3)
        {
            errors.Add($"Byzantine node count ({ByzantineNodeCount}) cannot exceed 1/3 of total nodes ({NodeCount / 3})");
        }

        // Validate algorithm-specific requirements
        switch (Algorithm)
        {
            case ConsensusAlgorithm.ProofOfElapsedTime:
                if (NodeCount < 3)
                {
                    errors.Add("PoET requires at least 3 nodes");
                }
                break;

            case ConsensusAlgorithm.PracticalByzantineFaultTolerance:
                if (NodeCount < 4)
                {
                    errors.Add("PBFT requires at least 4 nodes (3f+1 where f=1)");
                }
                break;

            case ConsensusAlgorithm.Raft:
                if (NodeCount % 2 == 0)
                {
                    errors.Add("Raft works best with odd number of nodes to avoid split votes");
                }
                break;
        }

        // Validate network topology makes sense for node count
        if (NetworkTopology == NetworkTopologyType.Star && NodeCount < 3)
        {
            errors.Add("Star topology requires at least 3 nodes (1 hub + 2 spokes)");
        }

        return errors;
    }
}

/// <summary>
/// Response model for simulation start request
/// </summary>
public record StartSimulationResponse
{
    /// <summary>
    /// Unique identifier of the created simulation
    /// </summary>
    public Guid SimulationId { get; init; }

    /// <summary>
    /// Name of the simulation
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Current status of the simulation
    /// </summary>
    public SimulationStatus Status { get; init; }

    /// <summary>
    /// Timestamp when the simulation was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Estimated completion time based on duration
    /// </summary>
    public DateTime? EstimatedCompletionAt { get; init; }

    /// <summary>
    /// WebSocket endpoint URL for real-time updates
    /// </summary>
    public string WebSocketEndpoint { get; init; } = string.Empty;

    /// <summary>
    /// Any warnings or informational messages about the simulation setup
    /// </summary>
    public List<string> Warnings { get; init; } = new();

    /// <summary>
    /// Success indicator
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if simulation creation failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}