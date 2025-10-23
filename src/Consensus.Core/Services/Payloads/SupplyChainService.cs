using Consensus.Core.Enums;
using Consensus.Core.Models.Payloads;
using Microsoft.Extensions.Logging;

namespace Consensus.Core.Services.Payloads;

/// <summary>
/// Service for generating supply chain payload data
/// </summary>
public interface ISupplyChainService
{
    /// <summary>
    /// Generate a supply chain event payload
    /// </summary>
    Task<SupplyChainPayload> GenerateSupplyChainEventAsync(
        string? productId = null,
        SupplyChainEventType? eventType = null,
        string? actor = null);
    
    /// <summary>
    /// Get the next logical event in the supply chain for a product
    /// </summary>
    Task<SupplyChainEventType> GetNextEventTypeAsync(string productId, SupplyChainEventType currentEvent);
    
    /// <summary>
    /// Get supply chain analytics data
    /// </summary>
    Task<SupplyChainAnalytics> GetSupplyChainAnalyticsAsync(IEnumerable<SupplyChainPayload> payloads);
}

/// <summary>
/// Implementation of supply chain service
/// </summary>
public class SupplyChainService : ISupplyChainService
{
    private readonly ILogger<SupplyChainService> _logger;
    private readonly Random _random;
    
    private static readonly string[] Companies = {
        "Acme Manufacturing", "Global Logistics", "Premium Packaging", 
        "Swift Transport", "Quality Assurance Inc", "Retail Partners"
    };
    
    private static readonly string[] Locations = {
        "New York, USA", "Los Angeles, USA", "Chicago, USA", "Houston, USA",
        "London, UK", "Paris, France", "Berlin, Germany", "Tokyo, Japan",
        "Shanghai, China", "Mumbai, India", "São Paulo, Brazil", "Toronto, Canada"
    };
    
    private static readonly Dictionary<SupplyChainEventType, string[]> EventActors = new()
    {
        { SupplyChainEventType.Manufactured, new[] { "Acme Manufacturing", "TechCorp Industries", "Global Factory Ltd" } },
        { SupplyChainEventType.QualityChecked, new[] { "Quality Assurance Inc", "Inspection Services", "QC Experts" } },
        { SupplyChainEventType.Packaged, new[] { "Premium Packaging", "Pack-It Solutions", "Secure Pack Co" } },
        { SupplyChainEventType.Shipped, new[] { "Global Logistics", "Swift Transport", "Express Shipping" } },
        { SupplyChainEventType.InTransit, new[] { "Highway Transport", "Air Cargo Services", "Ocean Freight" } },
        { SupplyChainEventType.Delivered, new[] { "Last Mile Delivery", "Local Couriers", "Door-to-Door Express" } },
        { SupplyChainEventType.Received, new[] { "Retail Partners", "Distribution Center", "Warehouse Solutions" } },
        { SupplyChainEventType.Audited, new[] { "Audit Firm", "Compliance Check", "Third-Party Verification" } }
    };

    public SupplyChainService(ILogger<SupplyChainService> logger)
    {
        _logger = logger;
        _random = new Random();
    }

    public async Task<SupplyChainPayload> GenerateSupplyChainEventAsync(
        string? productId = null, 
        SupplyChainEventType? eventType = null, 
        string? actor = null)
    {
        productId ??= GenerateProductId();
        eventType ??= GetRandomEventType();
        actor ??= GetActorForEvent(eventType.Value);

        var payload = new SupplyChainPayload
        {
            ProductId = productId,
            BatchNumber = GenerateBatchNumber(),
            EventType = eventType.Value,
            Actor = actor,
            Location = Locations[_random.Next(Locations.Length)],
            Temperature = eventType == SupplyChainEventType.InTransit ? 
                _random.NextDouble() * 30 + 10 : null, // 10-40°C for transit
            Humidity = eventType == SupplyChainEventType.InTransit ? 
                _random.NextDouble() * 40 + 30 : null, // 30-70% for transit
            VerificationSignature = GenerateSignature(),
            EventData = GenerateEventData(eventType.Value),
            Metadata = $"Generated for simulation at {DateTime.UtcNow:O}"
        };

        _logger.LogDebug("Generated supply chain event: {EventType} for product {ProductId} by {Actor}", 
            eventType, productId, actor);

        return await Task.FromResult(payload);
    }

    public async Task<SupplyChainEventType> GetNextEventTypeAsync(string productId, SupplyChainEventType currentEvent)
    {
        // Define the typical flow of supply chain events
        var nextEvent = currentEvent switch
        {
            SupplyChainEventType.Manufactured => SupplyChainEventType.QualityChecked,
            SupplyChainEventType.QualityChecked => SupplyChainEventType.Packaged,
            SupplyChainEventType.Packaged => SupplyChainEventType.Shipped,
            SupplyChainEventType.Shipped => SupplyChainEventType.InTransit,
            SupplyChainEventType.InTransit => SupplyChainEventType.Delivered,
            SupplyChainEventType.Delivered => SupplyChainEventType.Received,
            SupplyChainEventType.Received => SupplyChainEventType.Audited,
            SupplyChainEventType.Audited => SupplyChainEventType.Audited, // Terminal state
            _ => SupplyChainEventType.Manufactured
        };

        return await Task.FromResult(nextEvent);
    }

    public async Task<SupplyChainAnalytics> GetSupplyChainAnalyticsAsync(IEnumerable<SupplyChainPayload> payloads)
    {
        var analytics = new SupplyChainAnalytics();
        
        var payloadList = payloads.ToList();
        analytics.TotalEvents = payloadList.Count;
        analytics.UniqueProducts = payloadList.Select(p => p.ProductId).Distinct().Count();
        
        // Calculate event distribution
        analytics.EventDistribution = payloadList
            .GroupBy(p => p.EventType)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());
        
        // Calculate actor distribution
        analytics.ActorDistribution = payloadList
            .GroupBy(p => p.Actor)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Calculate location distribution
        analytics.LocationDistribution = payloadList
            .GroupBy(p => p.Location)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Calculate average transit temperature and humidity
        var transitEvents = payloadList.Where(p => p.EventType == SupplyChainEventType.InTransit).ToList();
        if (transitEvents.Any())
        {
            analytics.AverageTransitTemperature = transitEvents
                .Where(p => p.Temperature.HasValue)
                .Average(p => p.Temperature!.Value);
            
            analytics.AverageTransitHumidity = transitEvents
                .Where(p => p.Humidity.HasValue)
                .Average(p => p.Humidity!.Value);
        }

        return await Task.FromResult(analytics);
    }

    private string GenerateProductId()
    {
        var prefix = "PROD";
        var number = _random.Next(100000, 999999);
        return $"{prefix}-{number}";
    }

    private string GenerateBatchNumber()
    {
        var year = DateTime.UtcNow.Year;
        var batch = _random.Next(1000, 9999);
        return $"BATCH{year}{batch}";
    }

    private SupplyChainEventType GetRandomEventType()
    {
        var values = Enum.GetValues<SupplyChainEventType>();
        return values[_random.Next(values.Length)];
    }

    private string GetActorForEvent(SupplyChainEventType eventType)
    {
        if (EventActors.TryGetValue(eventType, out var actors))
        {
            return actors[_random.Next(actors.Length)];
        }
        return Companies[_random.Next(Companies.Length)];
    }

    private string GenerateSignature()
    {
        var bytes = new byte[32];
        _random.NextBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private Dictionary<string, object> GenerateEventData(SupplyChainEventType eventType)
    {
        return eventType switch
        {
            SupplyChainEventType.Manufactured => new Dictionary<string, object>
            {
                ["equipment_id"] = $"EQ-{_random.Next(1000, 9999)}",
                ["operator"] = $"Operator-{_random.Next(10, 99)}",
                ["quality_score"] = _random.NextDouble() * 40 + 60 // 60-100
            },
            SupplyChainEventType.QualityChecked => new Dictionary<string, object>
            {
                ["inspector"] = $"QC-{_random.Next(100, 999)}",
                ["test_results"] = _random.NextDouble() > 0.1 ? "PASS" : "FAIL",
                ["defect_rate"] = _random.NextDouble() * 5 // 0-5%
            },
            SupplyChainEventType.Shipped => new Dictionary<string, object>
            {
                ["carrier"] = GetActorForEvent(SupplyChainEventType.Shipped),
                ["tracking_number"] = $"TRK{_random.Next(100000000, 999999999)}",
                ["expected_delivery"] = DateTime.UtcNow.AddDays(_random.Next(1, 7)).ToString("O")
            },
            SupplyChainEventType.InTransit => new Dictionary<string, object>
            {
                ["vehicle_id"] = $"VEH-{_random.Next(1000, 9999)}",
                ["route"] = $"Route-{_random.Next(10, 99)}",
                ["progress_percentage"] = _random.Next(10, 90)
            },
            _ => new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["event_id"] = Guid.NewGuid().ToString()
            }
        };
    }
}

/// <summary>
/// Supply chain analytics data
/// </summary>
public class SupplyChainAnalytics
{
    public int TotalEvents { get; set; }
    public int UniqueProducts { get; set; }
    public Dictionary<string, int> EventDistribution { get; set; } = new();
    public Dictionary<string, int> ActorDistribution { get; set; } = new();
    public Dictionary<string, int> LocationDistribution { get; set; } = new();
    public double? AverageTransitTemperature { get; set; }
    public double? AverageTransitHumidity { get; set; }
}