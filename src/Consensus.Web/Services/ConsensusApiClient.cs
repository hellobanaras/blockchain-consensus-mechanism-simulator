using System.Net.Http.Json;
using System.Text.Json;
using Consensus.Core.Entities;
using Consensus.Core.Models;

namespace Consensus.Web.Services;

/// <summary>
/// Read-only client into the Consensus.Api host. The Api is the canonical
/// source of truth for displayed data; the Web project never reaches into the
/// database directly for list/detail/metrics views.
/// </summary>
public interface IConsensusApiClient
{
    Task<IReadOnlyList<SimulationRun>> GetSimulationsAsync(CancellationToken ct = default);
    Task<SimulationRun?> GetSimulationAsync(Guid simulationId, CancellationToken ct = default);
    Task<SimulationMetrics?> GetSimulationMetricsAsync(Guid simulationId, CancellationToken ct = default);
}

public sealed class ConsensusApiClient : IConsensusApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ConsensusApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ConsensusApiClient(HttpClient http, ILogger<ConsensusApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SimulationRun>> GetSimulationsAsync(CancellationToken ct = default)
    {
        try
        {
            // SimulationsController in Api is mapped at api/v1/Simulations (see its
            // [Route] attribute). Default page size is 10; ask for a larger window
            // so the demo dashboard sees all recent runs at once.
            using var resp = await _http.GetAsync("api/v1/Simulations?page=1&pageSize=100", ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Api GET /api/Simulations failed: {Status}", resp.StatusCode);
                return Array.Empty<SimulationRun>();
            }
            var sims = await resp.Content.ReadFromJsonAsync<List<SimulationRun>>(JsonOptions, ct);
            return sims ?? new List<SimulationRun>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Api GET /api/Simulations failed");
            return Array.Empty<SimulationRun>();
        }
    }

    public async Task<SimulationRun?> GetSimulationAsync(Guid simulationId, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.GetAsync($"api/v1/Simulations/{simulationId}", ct);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Api GET /api/Simulations/{Id} failed: {Status}", simulationId, resp.StatusCode);
                return null;
            }
            return await resp.Content.ReadFromJsonAsync<SimulationRun>(JsonOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Api GET /api/Simulations/{Id} failed", simulationId);
            return null;
        }
    }

    public async Task<SimulationMetrics?> GetSimulationMetricsAsync(Guid simulationId, CancellationToken ct = default)
    {
        // SimulationDashboard's `live` ISimulationService returns null for any
        // completed sim (its in-memory runtime has been disposed). This fallback
        // calls the Api host's DbBackedSimulationService implementation, which
        // builds SimulationMetrics from persisted blocks + rounds.
        try
        {
            using var resp = await _http.GetAsync($"api/v1/Simulations/{simulationId}/metrics", ct);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Api GET /api/Simulations/{Id}/metrics failed: {Status}", simulationId, resp.StatusCode);
                return null;
            }
            return await resp.Content.ReadFromJsonAsync<SimulationMetrics>(JsonOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Api GET /api/Simulations/{Id}/metrics failed", simulationId);
            return null;
        }
    }
}
