using System.ComponentModel.DataAnnotations;
using Consensus.Core.Enums;
using Consensus.Api.Models;

namespace Consensus.Web.Models.Forms;

/// <summary>
/// Mutable form model for creating simulation requests
/// </summary>
public class SimulationFormModel
{
    /// <summary>
    /// Name of the simulation run
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Consensus algorithm to use for the simulation
    /// </summary>
    [Required]
    public ConsensusAlgorithm? Algorithm { get; set; }

    /// <summary>
    /// Number of nodes to create in the network
    /// </summary>
    [Required]
    [Range(3, 100, ErrorMessage = "Node count must be between 3 and 100")]
    public int NodeCount { get; set; } = 7;

    /// <summary>
    /// Number of Byzantine (faulty) nodes in the network
    /// </summary>
    [Range(0, 33, ErrorMessage = "Byzantine node count must be between 0 and 33% of total nodes")]
    public int ByzantineNodeCount { get; set; } = 0;

    /// <summary>
    /// Duration of the simulation in seconds
    /// </summary>
    [Range(10, 3600, ErrorMessage = "Duration must be between 10 seconds and 1 hour")]
    public int DurationSeconds { get; set; } = 300; // Default 5 minutes

    /// <summary>
    /// Network topology type for the simulation
    /// </summary>
    public NetworkTopologyType NetworkTopology { get; set; } = NetworkTopologyType.FullMesh;

    /// <summary>
    /// Target block time in milliseconds
    /// </summary>
    [Range(100, 30000, ErrorMessage = "Block time must be between 100ms and 30 seconds")]
    public int BlockTimeMs { get; set; } = 2000;

    /// <summary>
    /// Number of transactions per block
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Transactions per block must be between 1 and 1000")]
    public int TransactionsPerBlock { get; set; } = 10;

    /// <summary>
    /// Network latency in milliseconds
    /// </summary>
    [Range(0, 2000, ErrorMessage = "Network latency must be between 0 and 2000ms")]
    public int NetworkLatencyMs { get; set; } = 100;

    /// <summary>
    /// Whether to start the simulation immediately after creation
    /// </summary>
    public bool AutoStart { get; set; } = true;

    /// <summary>
    /// Algorithm-specific configuration
    /// </summary>
    public Dictionary<string, object> AlgorithmConfiguration { get; set; } = new();

    /// <summary>
    /// Converts the form model to an API request model
    /// </summary>
    /// <returns>StartSimulationRequest for API call</returns>
    public StartSimulationRequest ToApiRequest()
    {
        if (!Algorithm.HasValue)
            throw new InvalidOperationException("Algorithm must be selected");

        return new StartSimulationRequest
        {
            Name = Name,
            Algorithm = Algorithm.Value,
            NodeCount = NodeCount,
            ByzantineNodeCount = ByzantineNodeCount,
            DurationSeconds = DurationSeconds,
            NetworkTopology = NetworkTopology,
            BlockTimeMs = BlockTimeMs,
            TransactionsPerBlock = TransactionsPerBlock,
            NetworkLatencyMs = NetworkLatencyMs,
            AutoStart = AutoStart,
            AlgorithmConfiguration = AlgorithmConfiguration
        };
    }

    /// <summary>
    /// Validates the request based on the selected algorithm
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> ValidateRequest()
    {
        var errors = new List<string>();

        if (!Algorithm.HasValue)
        {
            errors.Add("Consensus algorithm must be selected");
            return errors;
        }

        // Byzantine fault tolerance validation
        var maxByzantine = GetMaxByzantineNodes();
        if (ByzantineNodeCount > maxByzantine)
        {
            errors.Add($"Byzantine node count ({ByzantineNodeCount}) exceeds maximum allowed ({maxByzantine}) for {Algorithm} with {NodeCount} nodes");
        }

        // Algorithm-specific validations
        switch (Algorithm.Value)
        {
            case ConsensusAlgorithm.PracticalByzantineFaultTolerance:
                if (NodeCount < 4)
                {
                    errors.Add("pBFT requires at least 4 nodes");
                }
                var pbftMaxByzantine = (NodeCount - 1) / 3;
                if (ByzantineNodeCount > pbftMaxByzantine)
                {
                    errors.Add($"pBFT can tolerate at most {pbftMaxByzantine} Byzantine nodes with {NodeCount} total nodes");
                }
                break;

            case ConsensusAlgorithm.ProofOfElapsedTime:
                if (NodeCount < 3)
                {
                    errors.Add("PoET requires at least 3 nodes");
                }
                break;
        }

        // Network topology validations
        if (NetworkTopology == NetworkTopologyType.Star && NodeCount < 3)
        {
            errors.Add("Star topology requires at least 3 nodes");
        }

        return errors;
    }

    /// <summary>
    /// Gets the maximum number of Byzantine nodes for the current configuration
    /// </summary>
    public int GetMaxByzantineNodes()
    {
        if (!Algorithm.HasValue) return 0;

        return Algorithm.Value switch
        {
            ConsensusAlgorithm.PracticalByzantineFaultTolerance => Math.Max(0, (NodeCount - 1) / 3),
            _ => Math.Max(0, NodeCount / 2 - 1)
        };
    }

    /// <summary>
    /// Gets the Byzantine fault tolerance description
    /// </summary>
    public string GetByzantineTolerance()
    {
        var maxByzantine = GetMaxByzantineNodes();
        return $"f ≤ {maxByzantine}";
    }
}