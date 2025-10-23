using Consensus.Core.Enums;
using Consensus.Core.Models.Payloads;
using Microsoft.Extensions.Logging;

namespace Consensus.Core.Services.Payloads;

/// <summary>
/// Service for generating federated learning payload data
/// </summary>
public interface IFederatedLearningService
{
    /// <summary>
    /// Generate a federated learning update payload
    /// </summary>
    Task<FederatedLearningPayload> GenerateFLUpdateAsync(
        string? modelId = null,
        int? round = null,
        FedLearningUpdateType? updateType = null,
        string? clientId = null);
    
    /// <summary>
    /// Simulate model aggregation from multiple client updates
    /// </summary>
    Task<FederatedLearningPayload> AggregateModelUpdatesAsync(
        string modelId,
        int round,
        IEnumerable<FederatedLearningPayload> clientUpdates);
    
    /// <summary>
    /// Get federated learning analytics data
    /// </summary>
    Task<FederatedLearningAnalytics> GetFLAnalyticsAsync(IEnumerable<FederatedLearningPayload> payloads);
}

/// <summary>
/// Implementation of federated learning service
/// </summary>
public class FederatedLearningService : IFederatedLearningService
{
    private readonly ILogger<FederatedLearningService> _logger;
    private readonly Random _random;
    
    private static readonly string[] ModelTypes = {
        "ImageClassifier", "NLP-Transformer", "RecommendationSystem", 
        "FraudDetection", "SentimentAnalysis", "ObjectDetection"
    };
    
    private static readonly string[] ClientTypes = {
        "Hospital-", "Bank-", "University-", "Retail-", "Mobile-", "IoT-", "Edge-"
    };

    // Simulated federated learning parameters
    private readonly Dictionary<string, FLModelState> _modelStates = new();

    public FederatedLearningService(ILogger<FederatedLearningService> logger)
    {
        _logger = logger;
        _random = new Random();
    }

    public async Task<FederatedLearningPayload> GenerateFLUpdateAsync(
        string? modelId = null, 
        int? round = null, 
        FedLearningUpdateType? updateType = null, 
        string? clientId = null)
    {
        modelId ??= GenerateModelId();
        round ??= GetOrCreateModelState(modelId).CurrentRound;
        updateType ??= GetRandomUpdateType();
        clientId ??= GenerateClientId();

        var modelState = GetOrCreateModelState(modelId);
        
        var payload = new FederatedLearningPayload
        {
            ModelId = modelId,
            Round = round.Value,
            UpdateType = updateType.Value,
            ClientId = clientId,
            ModelData = GenerateModelData(updateType.Value),
            Accuracy = GenerateAccuracy(modelState, round.Value),
            Loss = GenerateLoss(modelState, round.Value),
            SampleCount = _random.Next(100, 10000),
            Epochs = _random.Next(1, 10),
            LearningRate = Math.Round(_random.NextDouble() * 0.1 + 0.001, 4), // 0.001 - 0.101
            TrainingMetadata = GenerateTrainingMetadata(clientId, updateType.Value),
            Metadata = $"Generated FL update for simulation at {DateTime.UtcNow:O}"
        };

        // Update model state
        UpdateModelState(modelState, payload);

        _logger.LogDebug("Generated FL update: {UpdateType} for model {ModelId} round {Round} from client {ClientId}", 
            updateType, modelId, round, clientId);

        return await Task.FromResult(payload);
    }

    public async Task<FederatedLearningPayload> AggregateModelUpdatesAsync(
        string modelId, 
        int round, 
        IEnumerable<FederatedLearningPayload> clientUpdates)
    {
        var updates = clientUpdates.ToList();
        var modelState = GetOrCreateModelState(modelId);

        // Simulate FedAvg aggregation
        var avgAccuracy = updates.Where(u => u.Accuracy.HasValue).Average(u => u.Accuracy!.Value);
        var avgLoss = updates.Where(u => u.Loss.HasValue).Average(u => u.Loss!.Value);
        var totalSamples = updates.Where(u => u.SampleCount.HasValue).Sum(u => u.SampleCount!.Value);

        var aggregatedPayload = new FederatedLearningPayload
        {
            ModelId = modelId,
            Round = round,
            UpdateType = FedLearningUpdateType.ModelAggregation,
            ClientId = "SERVER",
            ModelData = GenerateAggregatedModelData(updates),
            Accuracy = avgAccuracy,
            Loss = avgLoss,
            SampleCount = totalSamples,
            Epochs = 1,
            LearningRate = updates.Where(u => u.LearningRate.HasValue).Average(u => u.LearningRate!.Value),
            TrainingMetadata = new Dictionary<string, object>
            {
                ["client_count"] = updates.Count,
                ["aggregation_method"] = "FedAvg",
                ["total_samples"] = totalSamples,
                ["convergence_metric"] = CalculateConvergenceMetric(updates)
            },
            Metadata = $"Aggregated model update from {updates.Count} clients at {DateTime.UtcNow:O}"
        };

        // Update global model state
        modelState.GlobalAccuracy = avgAccuracy;
        modelState.GlobalLoss = avgLoss;
        modelState.CurrentRound = round + 1;
        modelState.ParticipatingClients = updates.Select(u => u.ClientId).ToHashSet();

        _logger.LogInformation("Aggregated FL model {ModelId} round {Round} from {ClientCount} clients. Accuracy: {Accuracy:F3}", 
            modelId, round, updates.Count, avgAccuracy);

        return await Task.FromResult(aggregatedPayload);
    }

    public async Task<FederatedLearningAnalytics> GetFLAnalyticsAsync(IEnumerable<FederatedLearningPayload> payloads)
    {
        var analytics = new FederatedLearningAnalytics();
        var payloadList = payloads.ToList();

        analytics.TotalUpdates = payloadList.Count;
        analytics.UniqueModels = payloadList.Select(p => p.ModelId).Distinct().Count();
        analytics.UniqueClients = payloadList.Select(p => p.ClientId).Distinct().Count();
        
        // Calculate update type distribution
        analytics.UpdateTypeDistribution = payloadList
            .GroupBy(p => p.UpdateType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Calculate model performance by rounds
        var modelGroups = payloadList.GroupBy(p => p.ModelId);
        analytics.ModelPerformance = new Dictionary<string, ModelPerformanceMetrics>();

        foreach (var modelGroup in modelGroups)
        {
            var modelPayloads = modelGroup.OrderBy(p => p.Round).ToList();
            var latestPayload = modelPayloads.LastOrDefault();
            
            analytics.ModelPerformance[modelGroup.Key] = new ModelPerformanceMetrics
            {
                MaxRounds = modelPayloads.Max(p => p.Round),
                FinalAccuracy = latestPayload?.Accuracy ?? 0,
                FinalLoss = latestPayload?.Loss ?? 0,
                TotalSamples = modelPayloads.Where(p => p.SampleCount.HasValue).Sum(p => p.SampleCount!.Value),
                ParticipatingClients = modelPayloads.Select(p => p.ClientId).Distinct().Count(),
                AccuracyTrend = CalculateAccuracyTrend(modelPayloads),
                ConvergenceRate = CalculateConvergenceRate(modelPayloads)
            };
        }

        // Calculate client participation
        analytics.ClientParticipation = payloadList
            .GroupBy(p => p.ClientId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Calculate average metrics
        var accuracyValues = payloadList.Where(p => p.Accuracy.HasValue).Select(p => p.Accuracy!.Value).ToList();
        var lossValues = payloadList.Where(p => p.Loss.HasValue).Select(p => p.Loss!.Value).ToList();

        if (accuracyValues.Any())
        {
            analytics.AverageAccuracy = accuracyValues.Average();
            analytics.BestAccuracy = accuracyValues.Max();
        }

        if (lossValues.Any())
        {
            analytics.AverageLoss = lossValues.Average();
            analytics.BestLoss = lossValues.Min();
        }

        return await Task.FromResult(analytics);
    }

    private FLModelState GetOrCreateModelState(string modelId)
    {
        if (!_modelStates.TryGetValue(modelId, out var state))
        {
            state = new FLModelState
            {
                ModelId = modelId,
                CurrentRound = 1,
                InitialAccuracy = _random.NextDouble() * 0.3 + 0.1, // 0.1 - 0.4
                TargetAccuracy = _random.NextDouble() * 0.2 + 0.8, // 0.8 - 1.0
                GlobalAccuracy = 0,
                GlobalLoss = double.MaxValue,
                ParticipatingClients = new HashSet<string>()
            };
            _modelStates[modelId] = state;
        }
        return state;
    }

    private void UpdateModelState(FLModelState state, FederatedLearningPayload payload)
    {
        if (payload.Accuracy.HasValue)
        {
            // Simulate gradual improvement
            state.GlobalAccuracy = Math.Max(state.GlobalAccuracy, payload.Accuracy.Value);
        }

        if (payload.Loss.HasValue)
        {
            state.GlobalLoss = Math.Min(state.GlobalLoss, payload.Loss.Value);
        }

        state.ParticipatingClients.Add(payload.ClientId);
    }

    private string GenerateModelId()
    {
        var modelType = ModelTypes[_random.Next(ModelTypes.Length)];
        var version = $"v{_random.Next(1, 5)}.{_random.Next(0, 10)}";
        return $"{modelType}-{version}";
    }

    private string GenerateClientId()
    {
        var clientType = ClientTypes[_random.Next(ClientTypes.Length)];
        var number = _random.Next(1000, 9999);
        return $"{clientType}{number}";
    }

    private FedLearningUpdateType GetRandomUpdateType()
    {
        var values = Enum.GetValues<FedLearningUpdateType>();
        // Weight towards model weights and gradients (more common)
        var weights = new[] { 0.4, 0.4, 0.1, 0.08, 0.02 }; // ModelWeights, Gradient, ModelAggregation, ValidationMetrics, TrainingComplete
        var randomValue = _random.NextDouble();
        var cumulativeWeight = 0.0;
        
        for (int i = 0; i < weights.Length && i < values.Length; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue <= cumulativeWeight)
                return values[i];
        }
        
        return values[0];
    }

    private string GenerateModelData(FedLearningUpdateType updateType)
    {
        // Simulate serialized model data (in real scenario this would be actual model weights/gradients)
        var dataSize = updateType switch
        {
            FedLearningUpdateType.ModelWeights => _random.Next(1000, 5000),
            FedLearningUpdateType.Gradient => _random.Next(500, 2000),
            FedLearningUpdateType.ModelAggregation => _random.Next(2000, 8000),
            _ => _random.Next(100, 500)
        };

        var data = new byte[dataSize];
        _random.NextBytes(data);
        return Convert.ToBase64String(data);
    }

    private string GenerateAggregatedModelData(IEnumerable<FederatedLearningPayload> updates)
    {
        // Simulate aggregated model data (larger than individual updates)
        var totalSize = updates.Sum(u => u.ModelData?.Length ?? 100);
        var aggregatedSize = Math.Max(totalSize / 2, 1000); // Compressed aggregation
        
        var data = new byte[aggregatedSize];
        _random.NextBytes(data);
        return Convert.ToBase64String(data);
    }

    private double GenerateAccuracy(FLModelState state, int round)
    {
        // Simulate improving accuracy over rounds with some randomness
        var progressRatio = Math.Min((double)round / 50, 1.0); // Assume convergence around round 50
        var targetAccuracy = state.TargetAccuracy;
        var initialAccuracy = state.InitialAccuracy;
        
        var baseAccuracy = initialAccuracy + (targetAccuracy - initialAccuracy) * progressRatio;
        var noise = (_random.NextDouble() - 0.5) * 0.05; // ±2.5% noise
        
        return Math.Max(0, Math.Min(1, baseAccuracy + noise));
    }

    private double GenerateLoss(FLModelState state, int round)
    {
        // Simulate decreasing loss over rounds
        var progressRatio = Math.Min((double)round / 50, 1.0);
        var initialLoss = 2.5;
        var targetLoss = 0.1;
        
        var baseLoss = initialLoss - (initialLoss - targetLoss) * progressRatio;
        var noise = (_random.NextDouble() - 0.5) * 0.2; // Some noise
        
        return Math.Max(0.01, baseLoss + noise);
    }

    private Dictionary<string, object> GenerateTrainingMetadata(string clientId, FedLearningUpdateType updateType)
    {
        return new Dictionary<string, object>
        {
            ["client_type"] = clientId.Split('-')[0],
            ["training_duration_seconds"] = _random.Next(30, 300),
            ["batch_size"] = _random.Next(16, 128),
            ["optimizer"] = new[] { "SGD", "Adam", "RMSprop" }[_random.Next(3)],
            ["device_type"] = new[] { "CPU", "GPU", "TPU" }[_random.Next(3)],
            ["memory_usage_mb"] = _random.Next(100, 2000),
            ["update_size_kb"] = _random.Next(50, 500),
            ["compression_enabled"] = _random.NextDouble() > 0.5
        };
    }

    private double CalculateConvergenceMetric(IEnumerable<FederatedLearningPayload> updates)
    {
        var accuracies = updates.Where(u => u.Accuracy.HasValue).Select(u => u.Accuracy!.Value).ToList();
        if (accuracies.Count < 2) return 0;
        
        // Calculate variance as a convergence metric (lower variance = better convergence)
        var mean = accuracies.Average();
        var variance = accuracies.Sum(acc => Math.Pow(acc - mean, 2)) / accuracies.Count;
        
        return Math.Round(1.0 / (1.0 + variance * 10), 4); // Normalize to 0-1 range
    }

    private List<double> CalculateAccuracyTrend(IEnumerable<FederatedLearningPayload> payloads)
    {
        return payloads
            .Where(p => p.Accuracy.HasValue)
            .OrderBy(p => p.Round)
            .Select(p => p.Accuracy!.Value)
            .ToList();
    }

    private double CalculateConvergenceRate(IEnumerable<FederatedLearningPayload> payloads)
    {
        var accuracyTrend = CalculateAccuracyTrend(payloads);
        if (accuracyTrend.Count < 2) return 0;
        
        // Calculate improvement rate
        var firstAccuracy = accuracyTrend.First();
        var lastAccuracy = accuracyTrend.Last();
        var rounds = accuracyTrend.Count;
        
        return rounds > 0 ? (lastAccuracy - firstAccuracy) / rounds : 0;
    }

    // Supporting classes
    private class FLModelState
    {
        public string ModelId { get; set; } = string.Empty;
        public int CurrentRound { get; set; }
        public double InitialAccuracy { get; set; }
        public double TargetAccuracy { get; set; }
        public double GlobalAccuracy { get; set; }
        public double GlobalLoss { get; set; }
        public HashSet<string> ParticipatingClients { get; set; } = new();
    }
}

/// <summary>
/// Federated learning analytics data
/// </summary>
public class FederatedLearningAnalytics
{
    public int TotalUpdates { get; set; }
    public int UniqueModels { get; set; }
    public int UniqueClients { get; set; }
    public Dictionary<string, int> UpdateTypeDistribution { get; set; } = new();
    public Dictionary<string, ModelPerformanceMetrics> ModelPerformance { get; set; } = new();
    public Dictionary<string, int> ClientParticipation { get; set; } = new();
    public double AverageAccuracy { get; set; }
    public double BestAccuracy { get; set; }
    public double AverageLoss { get; set; }
    public double BestLoss { get; set; }
}

/// <summary>
/// Model performance metrics
/// </summary>
public class ModelPerformanceMetrics
{
    public int MaxRounds { get; set; }
    public double FinalAccuracy { get; set; }
    public double FinalLoss { get; set; }
    public int TotalSamples { get; set; }
    public int ParticipatingClients { get; set; }
    public List<double> AccuracyTrend { get; set; } = new();
    public double ConvergenceRate { get; set; }
}