using System.Text.Json;
using Consensus.Core.Enums;
using Consensus.Core.Models.Payloads;
using Consensus.Core.Services.Payloads;
using Microsoft.Extensions.Logging;

namespace Consensus.Core.Services;

/// <summary>
/// Coordinator service for managing different payload modes
/// </summary>
public interface IPayloadService
{
    /// <summary>
    /// Generate payload data for a given mode
    /// </summary>
    Task<BasePayload> GeneratePayloadAsync(PayloadMode mode, Dictionary<string, object>? parameters = null);
    
    /// <summary>
    /// Serialize payload to JSON for storage in blockchain
    /// </summary>
    string SerializePayload(BasePayload payload);
    
    /// <summary>
    /// Deserialize payload from JSON
    /// </summary>
    BasePayload? DeserializePayload(string payloadJson, PayloadMode mode);
    
    /// <summary>
    /// Get available payload modes
    /// </summary>
    IEnumerable<PayloadMode> GetAvailablePayloadModes();
    
    /// <summary>
    /// Get payload mode analytics
    /// </summary>
    Task<object> GetPayloadAnalyticsAsync(PayloadMode mode, IEnumerable<BasePayload> payloads);
}

/// <summary>
/// Implementation of payload coordinator service
/// </summary>
public class PayloadService : IPayloadService
{
    private readonly ISupplyChainService _supplyChainService;
    private readonly IFederatedLearningService _federatedLearningService;
    private readonly ILogger<PayloadService> _logger;

    public PayloadService(
        ISupplyChainService supplyChainService,
        IFederatedLearningService federatedLearningService,
        ILogger<PayloadService> logger)
    {
        _supplyChainService = supplyChainService;
        _federatedLearningService = federatedLearningService;
        _logger = logger;
    }

    public async Task<BasePayload> GeneratePayloadAsync(PayloadMode mode, Dictionary<string, object>? parameters = null)
    {
        _logger.LogDebug("Generating payload for mode {PayloadMode} with parameters {Parameters}", mode, parameters);

        return mode switch
        {
            PayloadMode.SupplyChain => await GenerateSupplyChainPayload(parameters),
            PayloadMode.FederatedLearning => await GenerateFederatedLearningPayload(parameters),
            PayloadMode.None => new NonePayload(),
            _ => throw new ArgumentException($"Unsupported payload mode: {mode}")
        };
    }

    public string SerializePayload(BasePayload payload)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(payload, payload.GetType(), options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing payload of type {PayloadType}", payload.GetType().Name);
            throw;
        }
    }

    public BasePayload? DeserializePayload(string payloadJson, PayloadMode mode)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return null;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return mode switch
            {
                PayloadMode.SupplyChain => JsonSerializer.Deserialize<SupplyChainPayload>(payloadJson, options),
                PayloadMode.FederatedLearning => JsonSerializer.Deserialize<FederatedLearningPayload>(payloadJson, options),
                PayloadMode.None => JsonSerializer.Deserialize<NonePayload>(payloadJson, options),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing payload for mode {PayloadMode}: {PayloadJson}", mode, payloadJson);
            return null;
        }
    }

    public IEnumerable<PayloadMode> GetAvailablePayloadModes()
    {
        return Enum.GetValues<PayloadMode>();
    }

    public async Task<object> GetPayloadAnalyticsAsync(PayloadMode mode, IEnumerable<BasePayload> payloads)
    {
        return mode switch
        {
            PayloadMode.SupplyChain => await _supplyChainService.GetSupplyChainAnalyticsAsync(
                payloads.OfType<SupplyChainPayload>()),
            PayloadMode.FederatedLearning => await _federatedLearningService.GetFLAnalyticsAsync(
                payloads.OfType<FederatedLearningPayload>()),
            PayloadMode.None => new { Message = "No analytics available for None payload mode" },
            _ => new { Error = $"Unsupported payload mode: {mode}" }
        };
    }

    private async Task<SupplyChainPayload> GenerateSupplyChainPayload(Dictionary<string, object>? parameters)
    {
        string? productId = null;
        SupplyChainEventType? eventType = null;
        string? actor = null;

        if (parameters != null)
        {
            if (parameters.TryGetValue("productId", out var prodIdObj))
                productId = prodIdObj.ToString();
            
            if (parameters.TryGetValue("eventType", out var eventTypeObj) && 
                Enum.TryParse<SupplyChainEventType>(eventTypeObj.ToString(), out var parsedEventType))
                eventType = parsedEventType;
            
            if (parameters.TryGetValue("actor", out var actorObj))
                actor = actorObj.ToString();
        }

        return await _supplyChainService.GenerateSupplyChainEventAsync(productId, eventType, actor);
    }

    private async Task<FederatedLearningPayload> GenerateFederatedLearningPayload(Dictionary<string, object>? parameters)
    {
        string? modelId = null;
        int? round = null;
        FedLearningUpdateType? updateType = null;
        string? clientId = null;

        if (parameters != null)
        {
            if (parameters.TryGetValue("modelId", out var modelIdObj))
                modelId = modelIdObj.ToString();
            
            if (parameters.TryGetValue("round", out var roundObj) && int.TryParse(roundObj.ToString(), out var parsedRound))
                round = parsedRound;
            
            if (parameters.TryGetValue("updateType", out var updateTypeObj) && 
                Enum.TryParse<FedLearningUpdateType>(updateTypeObj.ToString(), out var parsedUpdateType))
                updateType = parsedUpdateType;
            
            if (parameters.TryGetValue("clientId", out var clientIdObj))
                clientId = clientIdObj.ToString();
        }

        return await _federatedLearningService.GenerateFLUpdateAsync(modelId, round, updateType, clientId);
    }
}

/// <summary>
/// Payload for none/default mode
/// </summary>
public class NonePayload : BasePayload
{
    public override PayloadMode PayloadMode => PayloadMode.None;
    
    public string Message { get; set; } = "No specific payload data";
}