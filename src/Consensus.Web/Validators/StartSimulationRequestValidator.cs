using FluentValidation;
using Consensus.Web.Models.Api;
using Consensus.Core.Enums;

namespace Consensus.Web.Validators;

/// <summary>
/// Validator for StartSimulationRequest ensuring proper simulation parameters
/// </summary>
public class StartSimulationRequestValidator : AbstractValidator<StartSimulationRequest>
{
    public StartSimulationRequestValidator()
    {
        // Basic validation rules
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Simulation name is required")
            .Length(1, 100).WithMessage("Simulation name must be between 1 and 100 characters")
            .Matches("^[a-zA-Z0-9\\s\\-_]+$").WithMessage("Simulation name can only contain letters, numbers, spaces, hyphens, and underscores");

        RuleFor(x => x.Algorithm)
            .IsInEnum().WithMessage("Valid consensus algorithm must be selected");

        RuleFor(x => x.NodeCount)
            .InclusiveBetween(3, 100).WithMessage("Node count must be between 3 and 100");

        RuleFor(x => x.ByzantineNodeCount)
            .GreaterThanOrEqualTo(0).WithMessage("Byzantine node count cannot be negative")
            .Must((request, byzantineNodes) => ValidateByzantineNodeCount(request, byzantineNodes))
            .WithMessage("Invalid Byzantine node count for the selected algorithm and node count");

        RuleFor(x => x.DurationSeconds)
            .InclusiveBetween(10, 86400).WithMessage("Duration must be between 10 seconds and 24 hours");

        RuleFor(x => x.NetworkTopology)
            .IsInEnum().WithMessage("Valid network topology must be selected");

        RuleFor(x => x.TransactionsPerBlock)
            .InclusiveBetween(1, 1000).WithMessage("Transactions per block must be between 1 and 1,000");

        RuleFor(x => x.NetworkLatencyMs)
            .InclusiveBetween(0, 5000).WithMessage("Network latency must be between 0 and 5,000ms");

        // Algorithm-specific validation
        When(x => x.Algorithm == ConsensusAlgorithm.ProofOfElapsedTime, () =>
        {
            RuleFor(x => x.AlgorithmConfiguration)
                .Must(config => ValidatePoETConfiguration(config))
                .WithMessage("Invalid PoET configuration parameters");
        });

        When(x => x.Algorithm == ConsensusAlgorithm.ProofOfStake, () =>
        {
            RuleFor(x => x.AlgorithmConfiguration)
                .Must(config => ValidatePoSConfiguration(config))
                .WithMessage("Invalid Proof of Stake configuration parameters");
        });

        When(x => x.Algorithm == ConsensusAlgorithm.ProofOfWork, () =>
        {
            RuleFor(x => x.AlgorithmConfiguration)
                .Must(config => ValidatePoWConfiguration(config))
                .WithMessage("Invalid Proof of Work configuration parameters");
        });

        When(x => x.Algorithm == ConsensusAlgorithm.PracticalByzantineFaultTolerance, () =>
        {
            RuleFor(x => x.AlgorithmConfiguration)
                .Must(config => ValidatePBFTConfiguration(config))
                .WithMessage("Invalid PBFT configuration parameters");
        });
    }

    /// <summary>
    /// Validates Byzantine node count based on algorithm requirements
    /// </summary>
    private static bool ValidateByzantineNodeCount(StartSimulationRequest request, int byzantineCount)
    {
        return request.Algorithm switch
        {
            ConsensusAlgorithm.PracticalByzantineFaultTolerance => 
                byzantineCount < (request.NodeCount / 3.0), // PBFT requires < 1/3 Byzantine nodes
            ConsensusAlgorithm.Raft => 
                byzantineCount == 0, // Raft doesn't handle Byzantine faults
            _ => byzantineCount < request.NodeCount // General rule: Byzantine nodes must be less than total
        };
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