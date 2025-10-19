using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using Xunit;
using Consensus.Data;
using Consensus.Core.Entities;
using Consensus.Core.Enums;

namespace Consensus.E2E.Tests;

/// <summary>
/// End-to-end tests for the complete simulation flow including protocol selection and execution
/// </summary>
public class SimulationFlowTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ConsensusDbContext _dbContext;

    public SimulationFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace database with in-memory for testing
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ConsensusDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ConsensusDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ConsensusDbContext>();
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task StartSimulation_WithPoETProtocol_ShouldCreateSimulationAndRun()
    {
        // Arrange
        var simulationRequest = new
        {
            Name = "Test PoET Simulation",
            Description = "End-to-end test simulation",
            ConsensusAlgorithm = "ProofOfElapsedTime",
            NodeCount = 6,
            TargetBlockCount = 10,
            Configuration = new Dictionary<string, object>
            {
                { "minWaitTimeMs", 1000 },
                { "maxWaitTimeMs", 5000 },
                { "blockTime", 2000 }
            }
        };

        // Act - Start simulation
        var response = await _client.PostAsJsonAsync("/api/simulations", simulationRequest);

        // Assert - Simulation should be created successfully
        response.EnsureSuccessStatusCode();
        var createdSimulation = await response.Content.ReadFromJsonAsync<SimulationResponse>();
        
        Assert.NotNull(createdSimulation);
        Assert.Equal("Test PoET Simulation", createdSimulation.Name);
        Assert.Equal("ProofOfElapsedTime", createdSimulation.ConsensusAlgorithm);
        Assert.Equal(6, createdSimulation.NodeCount);
        Assert.Equal("Running", createdSimulation.Status);

        // Wait for simulation to progress
        await Task.Delay(3000);

        // Act - Get simulation status
        var statusResponse = await _client.GetAsync($"/api/simulations/{createdSimulation.Id}");
        statusResponse.EnsureSuccessStatusCode();
        var simulationStatus = await statusResponse.Content.ReadFromJsonAsync<SimulationResponse>();

        // Assert - Simulation should have progressed
        Assert.NotNull(simulationStatus);
        Assert.True(simulationStatus.Status == "Running" || simulationStatus.Status == "Completed");

        // Act - Get blocks created
        var blocksResponse = await _client.GetAsync($"/api/simulations/{createdSimulation.Id}/blocks");
        blocksResponse.EnsureSuccessStatusCode();
        var blocks = await blocksResponse.Content.ReadFromJsonAsync<List<BlockSummary>>();

        // Assert - Should have created at least genesis block
        Assert.NotNull(blocks);
        Assert.NotEmpty(blocks);
        Assert.Contains(blocks, b => b.BlockNumber == 0); // Genesis block
    }

    [Fact]
    public async Task StartSimulation_WithInvalidParameters_ShouldReturnValidationError()
    {
        // Arrange - Invalid simulation request (0 nodes)
        var simulationRequest = new
        {
            Name = "",
            ConsensusAlgorithm = "ProofOfElapsedTime",
            NodeCount = 0,
            TargetBlockCount = -1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/simulations", simulationRequest);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnProtocolSelectionInterface()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        // Should contain protocol selection elements
        Assert.Contains("Protocol Selection", content);
        Assert.Contains("ProofOfElapsedTime", content);
        Assert.Contains("Node Count", content);
        Assert.Contains("Start Simulation", content);
    }

    [Fact]
    public async Task SimulationWebSocket_ShouldReceiveRealTimeUpdates()
    {
        // This test would require SignalR test client setup
        // For now, we'll test the HTTP endpoints that SignalR would use
        
        // Arrange - Create a simulation first
        var simulationRequest = new
        {
            Name = "WebSocket Test Simulation",
            ConsensusAlgorithm = "ProofOfElapsedTime",
            NodeCount = 3,
            TargetBlockCount = 5
        };

        var response = await _client.PostAsJsonAsync("/api/simulations", simulationRequest);
        response.EnsureSuccessStatusCode();
        var simulation = await response.Content.ReadFromJsonAsync<SimulationResponse>();

        // Act - Get real-time updates endpoint
        var updatesResponse = await _client.GetAsync($"/api/simulations/{simulation!.Id}/events");
        
        // Assert
        updatesResponse.EnsureSuccessStatusCode();
        var events = await updatesResponse.Content.ReadFromJsonAsync<List<SimulationEvent>>();
        Assert.NotNull(events);
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _client?.Dispose();
    }
}

// DTOs for testing
public class SimulationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConsensusAlgorithm { get; set; } = string.Empty;
    public int NodeCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class BlockSummary
{
    public Guid Id { get; set; }
    public long BlockNumber { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string? PreviousHash { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? ProposerId { get; set; }
}

public class SimulationEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}