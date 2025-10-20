using Consensus.Core.Enums;

namespace Consensus.Core.Models.Payloads;

/// <summary>
/// Base class for all payload types
/// </summary>
public abstract class BasePayload
{
    public abstract PayloadMode PayloadMode { get; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Metadata { get; set; }
}

/// <summary>
/// Supply chain event types
/// </summary>
public enum SupplyChainEventType
{
    Manufactured,
    QualityChecked,
    Packaged,
    Shipped,
    InTransit,
    Delivered,
    Received,
    Audited
}

/// <summary>
/// Supply chain payload data for tracking product lifecycle
/// </summary>
public class SupplyChainPayload : BasePayload
{
    public override PayloadMode PayloadMode => PayloadMode.SupplyChain;
    
    /// <summary>
    /// Unique product identifier
    /// </summary>
    public string ProductId { get; set; } = string.Empty;
    
    /// <summary>
    /// Product batch number
    /// </summary>
    public string BatchNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of supply chain event
    /// </summary>
    public SupplyChainEventType EventType { get; set; }
    
    /// <summary>
    /// Organization or entity responsible for this event
    /// </summary>
    public string Actor { get; set; } = string.Empty;
    
    /// <summary>
    /// Geographic location of the event
    /// </summary>
    public string Location { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional event details
    /// </summary>
    public Dictionary<string, object>? EventData { get; set; }
    
    /// <summary>
    /// Temperature during transit/storage (if applicable)
    /// </summary>
    public double? Temperature { get; set; }
    
    /// <summary>
    /// Humidity during transit/storage (if applicable)
    /// </summary>
    public double? Humidity { get; set; }
    
    /// <summary>
    /// Digital signature or certificate for verification
    /// </summary>
    public string? VerificationSignature { get; set; }
}

/// <summary>
/// Federated learning model update types
/// </summary>
public enum FedLearningUpdateType
{
    ModelWeights,
    Gradient,
    ModelAggregation,
    ValidationMetrics,
    TrainingComplete
}

/// <summary>
/// Federated learning payload for model training coordination
/// </summary>
public class FederatedLearningPayload : BasePayload
{
    public override PayloadMode PayloadMode => PayloadMode.FederatedLearning;
    
    /// <summary>
    /// Unique model identifier
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Training round number
    /// </summary>
    public int Round { get; set; }
    
    /// <summary>
    /// Type of federated learning update
    /// </summary>
    public FedLearningUpdateType UpdateType { get; set; }
    
    /// <summary>
    /// Client/participant identifier
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    
    /// <summary>
    /// Serialized model weights or gradients (simplified as base64)
    /// </summary>
    public string? ModelData { get; set; }
    
    /// <summary>
    /// Model accuracy on validation set
    /// </summary>
    public double? Accuracy { get; set; }
    
    /// <summary>
    /// Training loss value
    /// </summary>
    public double? Loss { get; set; }
    
    /// <summary>
    /// Number of training samples used
    /// </summary>
    public int? SampleCount { get; set; }
    
    /// <summary>
    /// Training epochs completed
    /// </summary>
    public int? Epochs { get; set; }
    
    /// <summary>
    /// Learning rate used during training
    /// </summary>
    public double? LearningRate { get; set; }
    
    /// <summary>
    /// Additional training metadata
    /// </summary>
    public Dictionary<string, object>? TrainingMetadata { get; set; }
}