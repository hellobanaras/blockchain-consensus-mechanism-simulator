namespace Consensus.Core.Enums;

/// <summary>
/// Available payload modes for blockchain simulations
/// </summary>
public enum PayloadMode
{
    /// <summary>
    /// No specific payload data
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Supply chain tracking payload with product events
    /// </summary>
    SupplyChain = 1,
    
    /// <summary>
    /// Federated learning payload with model updates
    /// </summary>
    FederatedLearning = 2
}