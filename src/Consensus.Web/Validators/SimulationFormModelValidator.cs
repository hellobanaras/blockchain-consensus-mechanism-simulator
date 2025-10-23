using FluentValidation;
using Consensus.Web.Models.Forms;
using Consensus.Core.Enums;

namespace Consensus.Web.Validators;

/// <summary>
/// Validator for SimulationFormModel ensuring proper form input validation
/// </summary>
public class SimulationFormModelValidator : AbstractValidator<SimulationFormModel>
{
    public SimulationFormModelValidator()
    {
        // Basic validation rules
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Simulation name is required")
            .Length(1, 100).WithMessage("Simulation name must be between 1 and 100 characters")
            .Matches("^[a-zA-Z0-9\\s\\-_\\.]+$").WithMessage("Simulation name can only contain letters, numbers, spaces, hyphens, underscores, and periods");

        RuleFor(x => x.Algorithm)
            .NotNull().WithMessage("Please select a consensus algorithm");

        RuleFor(x => x.NodeCount)
            .InclusiveBetween(3, 100).WithMessage("Node count must be between 3 and 100");

        RuleFor(x => x.ByzantineNodeCount)
            .GreaterThanOrEqualTo(0).WithMessage("Byzantine node count cannot be negative")
            .Must((model, byzantineNodes) => ValidateByzantineNodeCount(model, byzantineNodes))
            .WithMessage(model => GetByzantineValidationMessage(model));

        RuleFor(x => x.DurationSeconds)
            .InclusiveBetween(10, 86400).WithMessage("Duration must be between 10 seconds and 24 hours (86,400 seconds)");

        RuleFor(x => x.NetworkTopology)
            .IsInEnum().WithMessage("Please select a valid network topology");

        RuleFor(x => x.TransactionsPerBlock)
            .InclusiveBetween(1, 1000).WithMessage("Transactions per block must be between 1 and 1,000");

        RuleFor(x => x.NetworkLatencyMs)
            .InclusiveBetween(0, 5000).WithMessage("Network latency must be between 0 and 5,000ms");

        RuleFor(x => x.BlockTimeMs)
            .InclusiveBetween(100, 30000).WithMessage("Block time must be between 100ms and 30 seconds");

        // Algorithm-specific validation
        When(x => x.Algorithm == ConsensusAlgorithm.ProofOfElapsedTime, () =>
        {
            RuleFor(x => x.AlgorithmConfiguration)
                .Must(config => ValidatePoETConfiguration(config))
                .WithMessage("PoET: Minimum wait time must be positive and less than maximum wait time");
        });

        When(x => x.Algorithm == ConsensusAlgorithm.ProofOfStake, () =>
        {
            RuleFor(x => x.AlgorithmConfiguration)
                .Must(config => ValidatePoSConfiguration(config))
                .WithMessage("PoS: Invalid stake configuration parameters");
        });

        When(x => x.Algorithm == ConsensusAlgorithm.ProofOfWork, () =>
        {
            RuleFor(x => x.AlgorithmConfiguration)
                .Must(config => ValidatePoWConfiguration(config))
                .WithMessage("PoW: Invalid difficulty or hash rate configuration");
        });

        When(x => x.Algorithm == ConsensusAlgorithm.PracticalByzantineFaultTolerance, () =>
        {
            RuleFor(x => x.AlgorithmConfiguration)
                .Must(config => ValidatePBFTConfiguration(config))
                .WithMessage("PBFT: Invalid timeout or authentication configuration");
        });

        // Logical validation rules
        RuleFor(x => x)
            .Must(model => ValidateNodeTopologyCompatibility(model))
            .WithMessage("Selected network topology may not work well with the specified node count");

        RuleFor(x => x)
            .Must(model => ValidatePerformanceConfiguration(model))
            .WithMessage("Configuration may result in poor performance or very long simulation times");
    }

    /// <summary>
    /// Validates Byzantine node count based on algorithm requirements
    /// </summary>
    private static bool ValidateByzantineNodeCount(SimulationFormModel model, int byzantineCount)
    {
        if (!model.Algorithm.HasValue) return true; // Skip validation if no algorithm selected

        return model.Algorithm.Value switch
        {
            ConsensusAlgorithm.PracticalByzantineFaultTolerance => 
                byzantineCount < (model.NodeCount / 3.0), // PBFT requires < 1/3 Byzantine nodes
            ConsensusAlgorithm.Raft => 
                byzantineCount == 0, // Raft doesn't handle Byzantine faults
            _ => byzantineCount < model.NodeCount // General rule: Byzantine nodes must be less than total
        };
    }

    /// <summary>
    /// Gets appropriate Byzantine validation message based on algorithm
    /// </summary>
    private static string GetByzantineValidationMessage(SimulationFormModel model)
    {
        if (!model.Algorithm.HasValue) return "Byzantine node count is invalid";

        return model.Algorithm.Value switch
        {
            ConsensusAlgorithm.PracticalByzantineFaultTolerance => 
                $"PBFT requires fewer than {model.NodeCount / 3} Byzantine nodes (less than 1/3 of total)",
            ConsensusAlgorithm.Raft => 
                "Raft does not support Byzantine fault tolerance - set Byzantine nodes to 0",
            _ => $"Byzantine nodes must be fewer than total nodes ({model.NodeCount})"
        };
    }

    /// <summary>
    /// Validates network topology compatibility with node count
    /// </summary>
    private static bool ValidateNodeTopologyCompatibility(SimulationFormModel model)
    {
        return model.NetworkTopology switch
        {
            NetworkTopologyType.Ring when model.NodeCount < 3 => false, // Ring needs at least 3 nodes
            NetworkTopologyType.Tree when model.NodeCount < 3 => false, // Tree needs at least 3 nodes
            NetworkTopologyType.Grid when model.NodeCount < 4 => false, // Grid works better with 4+ nodes
            _ => true
        };
    }

    /// <summary>
    /// Validates performance-related configuration
    /// </summary>
    private static bool ValidatePerformanceConfiguration(SimulationFormModel model)
    {
        // Warn about potentially slow configurations
        var hasHighLatency = model.NetworkLatencyMs > 1000;
        var hasManyTransactions = model.TransactionsPerBlock > 500;
        var hasManyNodes = model.NodeCount > 50;

        // Allow configuration but warn if it might be very slow
        if (hasHighLatency && hasManyNodes) return false;
        if (hasManyTransactions && hasManyNodes && model.BlockTimeMs < 1000) return false;

        return true;
    }

    /// <summary>
    /// Validates Proof of Elapsed Time configuration parameters
    /// </summary>
    private static bool ValidatePoETConfiguration(Dictionary<string, object> config)
    {
        if (config.TryGetValue("MinWaitTimeMs", out var minWaitObj) &&
            config.TryGetValue("MaxWaitTimeMs", out var maxWaitObj))
        {
            if (int.TryParse(minWaitObj?.ToString(), out var minWait) &&
                int.TryParse(maxWaitObj?.ToString(), out var maxWait))
            {
                return minWait > 0 && maxWait > minWait && maxWait <= 60000; // Max 1 minute
            }
        }
        return true; // Allow empty configuration (use defaults)
    }

    /// <summary>
    /// Validates Proof of Stake configuration parameters
    /// </summary>
    private static bool ValidatePoSConfiguration(Dictionary<string, object> config)
    {
        if (config.TryGetValue("MinStake", out var minStakeObj))
        {
            if (int.TryParse(minStakeObj?.ToString(), out var minStake))
            {
                return minStake > 0 && minStake <= 1000000; // Reasonable stake limits
            }
        }

        if (config.TryGetValue("StakeDistribution", out var distribution))
        {
            var validDistributions = new[] { "equal", "random", "weighted", "pareto" };
            return validDistributions.Contains(distribution?.ToString()?.ToLower());
        }

        return true; // Allow empty configuration
    }

    /// <summary>
    /// Validates Proof of Work configuration parameters
    /// </summary>
    private static bool ValidatePoWConfiguration(Dictionary<string, object> config)
    {
        if (config.TryGetValue("Difficulty", out var difficultyObj))
        {
            if (int.TryParse(difficultyObj?.ToString(), out var difficulty))
            {
                return difficulty >= 1 && difficulty <= 8; // Reasonable difficulty range
            }
        }

        if (config.TryGetValue("HashRateVariation", out var variationObj))
        {
            if (int.TryParse(variationObj?.ToString(), out var variation))
            {
                return variation >= 0 && variation <= 100; // Percentage variation
            }
        }

        return true; // Allow empty configuration
    }

    /// <summary>
    /// Validates PBFT configuration parameters
    /// </summary>
    private static bool ValidatePBFTConfiguration(Dictionary<string, object> config)
    {
        if (config.TryGetValue("ViewChangeTimeout", out var timeoutObj))
        {
            if (int.TryParse(timeoutObj?.ToString(), out var timeout))
            {
                return timeout >= 1000 && timeout <= 60000; // 1 second to 1 minute
            }
        }

        return true; // Allow empty configuration
    }
}