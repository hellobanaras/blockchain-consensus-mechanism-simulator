using Consensus.Core.Enums;
using Consensus.Web.Models.Api;
using Consensus.Web.Models.Forms;

namespace Consensus.Web.Services;

/// <summary>
/// Service for managing protocol configurations and presets
/// </summary>
public interface IProtocolConfigurationService
{
    /// <summary>
    /// Gets recommended configuration for a specific algorithm
    /// </summary>
    Task<SimulationFormModel> GetRecommendedConfigurationAsync(ConsensusAlgorithm algorithm);

    /// <summary>
    /// Validates a simulation configuration
    /// </summary>
    Task<ProtocolValidationResult> ValidateConfigurationAsync(SimulationFormModel model);

    /// <summary>
    /// Gets algorithm-specific parameter recommendations
    /// </summary>
    Task<Dictionary<string, object>> GetAlgorithmParametersAsync(ConsensusAlgorithm algorithm, int nodeCount);

    /// <summary>
    /// Saves a configuration as a preset
    /// </summary>
    Task<bool> SavePresetAsync(string name, SimulationFormModel model);

    /// <summary>
    /// Loads a saved preset
    /// </summary>
    Task<SimulationFormModel?> LoadPresetAsync(string name);

    /// <summary>
    /// Gets all available presets
    /// </summary>
    Task<List<string>> GetAvailablePresetsAsync();
}

/// <summary>
/// Validation result for simulation configuration
/// </summary>
public class ProtocolValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Implementation of protocol configuration service
/// </summary>
public class ProtocolConfigurationService : IProtocolConfigurationService
{
    private readonly ILogger<ProtocolConfigurationService> _logger;
    private readonly Dictionary<string, SimulationFormModel> _presets = new();

    public ProtocolConfigurationService(ILogger<ProtocolConfigurationService> logger)
    {
        _logger = logger;
        InitializeDefaultPresets();
    }

    public async Task<SimulationFormModel> GetRecommendedConfigurationAsync(ConsensusAlgorithm algorithm)
    {
        _logger.LogInformation("Getting recommended configuration for {Algorithm}", algorithm);

        var config = new SimulationFormModel
        {
            Algorithm = algorithm,
            Name = $"{GetAlgorithmDisplayName(algorithm)} Simulation {DateTime.Now:MM-dd HH:mm}"
        };

        // Set algorithm-specific recommendations
        switch (algorithm)
        {
            case ConsensusAlgorithm.ProofOfElapsedTime:
                config.NodeCount = 7;
                config.ByzantineNodeCount = 1;
                config.BlockTimeMs = 2000;
                config.NetworkLatencyMs = 100;
                config.DurationSeconds = 120;
                config.TransactionsPerBlock = 10;
                config.NetworkTopology = NetworkTopologyType.FullMesh;
                break;

            case ConsensusAlgorithm.ProofOfStake:
                config.NodeCount = 9;
                config.ByzantineNodeCount = 2;
                config.BlockTimeMs = 1000;
                config.NetworkLatencyMs = 50;
                config.DurationSeconds = 180;
                config.TransactionsPerBlock = 15;
                config.NetworkTopology = NetworkTopologyType.FullMesh;
                break;

            case ConsensusAlgorithm.ProofOfWork:
                config.NodeCount = 5;
                config.ByzantineNodeCount = 1;
                config.BlockTimeMs = 10000;
                config.NetworkLatencyMs = 200;
                config.DurationSeconds = 300;
                config.TransactionsPerBlock = 20;
                config.NetworkTopology = NetworkTopologyType.Random;
                break;

            case ConsensusAlgorithm.PracticalByzantineFaultTolerance:
                config.NodeCount = 7; // 3f + 1 = 7 (f = 2)
                config.ByzantineNodeCount = 2;
                config.BlockTimeMs = 500;
                config.NetworkLatencyMs = 50;
                config.DurationSeconds = 90;
                config.TransactionsPerBlock = 8;
                config.NetworkTopology = NetworkTopologyType.FullMesh;
                break;

            case ConsensusAlgorithm.Raft:
                config.NodeCount = 5; // Odd number preferred
                config.ByzantineNodeCount = 0; // Raft doesn't handle Byzantine failures
                config.BlockTimeMs = 1000;
                config.NetworkLatencyMs = 100;
                config.DurationSeconds = 120;
                config.TransactionsPerBlock = 12;
                config.NetworkTopology = NetworkTopologyType.FullMesh;
                break;

            case ConsensusAlgorithm.Tendermint:
                config.NodeCount = 7;
                config.ByzantineNodeCount = 2;
                config.BlockTimeMs = 1000;
                config.NetworkLatencyMs = 50;
                config.DurationSeconds = 150;
                config.TransactionsPerBlock = 25;
                config.NetworkTopology = NetworkTopologyType.FullMesh;
                break;

            default:
                config.NodeCount = 7;
                config.ByzantineNodeCount = 1;
                config.BlockTimeMs = 2000;
                config.NetworkLatencyMs = 100;
                config.DurationSeconds = 120;
                config.TransactionsPerBlock = 10;
                config.NetworkTopology = NetworkTopologyType.FullMesh;
                break;
        }

        return await Task.FromResult(config);
    }

    public async Task<ProtocolValidationResult> ValidateConfigurationAsync(SimulationFormModel model)
    {
        var result = new ProtocolValidationResult { IsValid = true };

        try
        {
            // Basic validation
            if (model.Algorithm == null)
            {
                result.Errors.Add("Consensus algorithm must be selected");
                result.IsValid = false;
                return result;
            }

            // Node count validation
            if (model.NodeCount < 3)
            {
                result.Errors.Add("Minimum 3 nodes required for any consensus algorithm");
                result.IsValid = false;
            }

            // Algorithm-specific validation
            await ValidateAlgorithmSpecific(model, result);

            // Performance warnings
            if (model.NodeCount > 20)
            {
                result.Warnings.Add("Large node counts may impact simulation performance");
            }

            if (model.DurationSeconds > 600)
            {
                result.Warnings.Add("Long simulations may consume significant resources");
            }

            // Topology validation
            ValidateNetworkTopology(model, result);

            // Generate recommendations
            GenerateRecommendations(model, result);

            _logger.LogInformation("Validation completed for {Algorithm} with {NodeCount} nodes. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                model.Algorithm, model.NodeCount, result.IsValid, result.Errors.Count, result.Warnings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during configuration validation");
            result.Errors.Add("Validation failed due to an internal error");
            result.IsValid = false;
        }

        return result;
    }

    public async Task<Dictionary<string, object>> GetAlgorithmParametersAsync(ConsensusAlgorithm algorithm, int nodeCount)
    {
        var parameters = new Dictionary<string, object>();

        switch (algorithm)
        {
            case ConsensusAlgorithm.ProofOfElapsedTime:
                parameters["MinWaitTimeMs"] = 1000;
                parameters["MaxWaitTimeMs"] = Math.Min(5000, nodeCount * 500);
                parameters["AttestationRequired"] = true;
                break;

            case ConsensusAlgorithm.ProofOfStake:
                parameters["MinStake"] = 100;
                parameters["StakeDistribution"] = "equal";
                parameters["SlashingEnabled"] = false;
                parameters["DelegationAllowed"] = false;
                break;

            case ConsensusAlgorithm.ProofOfWork:
                parameters["Difficulty"] = Math.Min(6, Math.Max(2, nodeCount / 2));
                parameters["HashRateVariation"] = 20;
                parameters["MiningReward"] = 50;
                break;

            case ConsensusAlgorithm.PracticalByzantineFaultTolerance:
                parameters["ViewChangeTimeoutMs"] = Math.Max(3000, nodeCount * 500);
                parameters["MessageAuthentication"] = true;
                parameters["CheckpointInterval"] = 10;
                break;

            case ConsensusAlgorithm.Raft:
                parameters["ElectionTimeoutMs"] = 5000;
                parameters["HeartbeatIntervalMs"] = 1000;
                parameters["LogCompactionThreshold"] = 100;
                break;

            case ConsensusAlgorithm.Tendermint:
                parameters["TimeoutPropose"] = 3000;
                parameters["TimeoutPrevote"] = 1000;
                parameters["TimeoutPrecommit"] = 1000;
                parameters["SkipEmptyBlocks"] = true;
                break;
        }

        return await Task.FromResult(parameters);
    }

    public async Task<bool> SavePresetAsync(string name, SimulationFormModel model)
    {
        try
        {
            _presets[name] = new SimulationFormModel
            {
                Name = model.Name,
                Algorithm = model.Algorithm,
                NodeCount = model.NodeCount,
                ByzantineNodeCount = model.ByzantineNodeCount,
                DurationSeconds = model.DurationSeconds,
                NetworkTopology = model.NetworkTopology,
                BlockTimeMs = model.BlockTimeMs,
                TransactionsPerBlock = model.TransactionsPerBlock,
                NetworkLatencyMs = model.NetworkLatencyMs,
                AutoStart = model.AutoStart,
                AlgorithmConfiguration = new Dictionary<string, object>(model.AlgorithmConfiguration)
            };

            _logger.LogInformation("Saved preset '{PresetName}' for algorithm {Algorithm}", name, model.Algorithm);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save preset '{PresetName}'", name);
            return false;
        }
    }

    public async Task<SimulationFormModel?> LoadPresetAsync(string name)
    {
        if (_presets.TryGetValue(name, out var preset))
        {
            _logger.LogInformation("Loaded preset '{PresetName}'", name);
            return await Task.FromResult(preset);
        }

        _logger.LogWarning("Preset '{PresetName}' not found", name);
        return null;
    }

    public async Task<List<string>> GetAvailablePresetsAsync()
    {
        return await Task.FromResult(_presets.Keys.ToList());
    }

    private async Task ValidateAlgorithmSpecific(SimulationFormModel model, ProtocolValidationResult result)
    {
        switch (model.Algorithm!.Value)
        {
            case ConsensusAlgorithm.PracticalByzantineFaultTolerance:
                if (model.NodeCount < 4)
                {
                    result.Errors.Add("pBFT requires at least 4 nodes (3f + 1 where f ≥ 1)");
                    result.IsValid = false;
                }
                
                var maxByzantinePbft = (model.NodeCount - 1) / 3;
                if (model.ByzantineNodeCount > maxByzantinePbft)
                {
                    result.Errors.Add($"pBFT can tolerate at most {maxByzantinePbft} Byzantine nodes with {model.NodeCount} total nodes");
                    result.IsValid = false;
                }
                break;

            case ConsensusAlgorithm.ProofOfElapsedTime:
                if (model.NodeCount < 3)
                {
                    result.Errors.Add("PoET requires at least 3 nodes for meaningful consensus");
                    result.IsValid = false;
                }
                break;

            case ConsensusAlgorithm.Raft:
                if (model.ByzantineNodeCount > 0)
                {
                    result.Warnings.Add("Raft is not Byzantine fault tolerant - Byzantine nodes will be treated as crashed nodes");
                }
                if (model.NodeCount % 2 == 0)
                {
                    result.Warnings.Add("Raft works best with odd number of nodes to avoid split votes");
                }
                break;

            case ConsensusAlgorithm.ProofOfWork:
                if (model.BlockTimeMs < 5000)
                {
                    result.Warnings.Add("Very short block times may not allow sufficient mining competition");
                }
                break;

            case ConsensusAlgorithm.ProofOfStake:
                if (model.NodeCount < 5)
                {
                    result.Warnings.Add("PoS works best with at least 5 validators for stake distribution");
                }
                break;
        }

        await Task.CompletedTask;
    }

    private static void ValidateNetworkTopology(SimulationFormModel model, ProtocolValidationResult result)
    {
        switch (model.NetworkTopology)
        {
            case NetworkTopologyType.Star:
                if (model.NodeCount < 3)
                {
                    result.Errors.Add("Star topology requires at least 3 nodes (1 hub + 2 spokes)");
                    result.IsValid = false;
                }
                break;

            case NetworkTopologyType.Ring:
                if (model.NodeCount < 3)
                {
                    result.Errors.Add("Ring topology requires at least 3 nodes");
                    result.IsValid = false;
                }
                break;

            case NetworkTopologyType.Grid:
                var gridSize = (int)Math.Sqrt(model.NodeCount);
                if (gridSize * gridSize != model.NodeCount)
                {
                    result.Warnings.Add($"Grid topology works best with perfect square node counts (nearest: {gridSize * gridSize})");
                }
                break;
        }
    }

    private static void GenerateRecommendations(SimulationFormModel model, ProtocolValidationResult result)
    {
        // Performance recommendations
        if (model.NodeCount > 10 && model.NetworkTopology == NetworkTopologyType.FullMesh)
        {
            result.Recommendations.Add("Consider using Random or Small World topology for better performance with large networks");
        }

        // Algorithm-specific recommendations
        switch (model.Algorithm!.Value)
        {
            case ConsensusAlgorithm.ProofOfWork:
                if (model.NetworkLatencyMs < 100)
                {
                    result.Recommendations.Add("Consider increasing network latency to better simulate real-world mining conditions");
                }
                break;

            case ConsensusAlgorithm.PracticalByzantineFaultTolerance:
                if (model.NetworkLatencyMs > 200)
                {
                    result.Recommendations.Add("pBFT performs best with low network latency");
                }
                break;

            case ConsensusAlgorithm.ProofOfElapsedTime:
                if (model.NodeCount > 15)
                {
                    result.Recommendations.Add("PoET scales well but consider monitoring performance with large node counts");
                }
                break;
        }

        // Duration recommendations
        var estimatedRounds = (model.DurationSeconds * 1000) / model.BlockTimeMs;
        if (estimatedRounds < 10)
        {
            result.Recommendations.Add("Consider increasing duration or decreasing block time for more meaningful results");
        }
    }

    private void InitializeDefaultPresets()
    {
        // Add some default presets
        _presets["PoET Quick Test"] = new SimulationFormModel
        {
            Name = "PoET Quick Test",
            Algorithm = ConsensusAlgorithm.ProofOfElapsedTime,
            NodeCount = 5,
            ByzantineNodeCount = 1,
            DurationSeconds = 60,
            BlockTimeMs = 2000,
            NetworkLatencyMs = 100,
            TransactionsPerBlock = 5,
            NetworkTopology = NetworkTopologyType.FullMesh,
            AutoStart = true
        };

        _presets["pBFT Stress Test"] = new SimulationFormModel
        {
            Name = "pBFT Stress Test",
            Algorithm = ConsensusAlgorithm.PracticalByzantineFaultTolerance,
            NodeCount = 10,
            ByzantineNodeCount = 3,
            DurationSeconds = 300,
            BlockTimeMs = 500,
            NetworkLatencyMs = 50,
            TransactionsPerBlock = 20,
            NetworkTopology = NetworkTopologyType.FullMesh,
            AutoStart = false
        };

        _presets["PoW Mining Simulation"] = new SimulationFormModel
        {
            Name = "PoW Mining Simulation",
            Algorithm = ConsensusAlgorithm.ProofOfWork,
            NodeCount = 7,
            ByzantineNodeCount = 1,
            DurationSeconds = 600,
            BlockTimeMs = 15000,
            NetworkLatencyMs = 250,
            TransactionsPerBlock = 30,
            NetworkTopology = NetworkTopologyType.Random,
            AutoStart = false
        };
    }

    private static string GetAlgorithmDisplayName(ConsensusAlgorithm algorithm)
    {
        return algorithm switch
        {
            ConsensusAlgorithm.ProofOfElapsedTime => "Proof of Elapsed Time (PoET)",
            ConsensusAlgorithm.ProofOfStake => "Proof of Stake (PoS)",
            ConsensusAlgorithm.ProofOfWork => "Proof of Work (PoW)",
            ConsensusAlgorithm.PracticalByzantineFaultTolerance => "Practical Byzantine Fault Tolerance (pBFT)",
            ConsensusAlgorithm.Raft => "Raft Consensus",
            ConsensusAlgorithm.Tendermint => "Tendermint",
            ConsensusAlgorithm.Algorand => "Algorand",
            _ => algorithm.ToString()
        };
    }
}