using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using Xunit;
using Consensus.Data;
using Consensus.Core.Entities;
using Consensus.Core.Enums;
using System.Net;

namespace Consensus.Api.Tests;

/// <summary>
/// Integration tests for simulation-related API endpoints
/// </summary>
public class SimulationControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ConsensusDbContext _dbContext;

    public SimulationControllerTests(WebApplicationFactory<Program> factory)
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
    public async Task POST_Simulations_WithValidRequest_ShouldCreateSimulation()
    {
        // Arrange
        var request = new StartSimulationRequest
        {
            Name = "Test Simulation",
            Description = "Integration test simulation",
            ConsensusAlgorithm = ConsensusAlgorithm.ProofOfElapsedTime,
            NodeCount = 5,
            TargetBlockCount = 10,
            Configuration = new Dictionary<string, object>
            {
                { "minWaitTimeMs", 500 },
                { "maxWaitTimeMs", 2000 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/simulations", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SimulationResponse>();
        
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.ConsensusAlgorithm.ToString(), result.ConsensusAlgorithm);
        Assert.Equal(request.NodeCount, result.NodeCount);
        Assert.NotEqual(Guid.Empty, result.Id);

        // Verify in database
        var dbSimulation = await _dbContext.SimulationRuns.FindAsync(result.Id);
        Assert.NotNull(dbSimulation);
        Assert.Equal(request.Name, dbSimulation.Name);
    }

    [Fact]
    public async Task POST_Simulations_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange - Invalid request with 0 nodes
        var request = new StartSimulationRequest
        {
            Name = "",
            ConsensusAlgorithm = ConsensusAlgorithm.ProofOfElapsedTime,
            NodeCount = 0,
            TargetBlockCount = -1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/simulations", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_Simulations_ShouldReturnAllSimulations()
    {
        // Arrange - Create test simulations
        var simulation1 = new SimulationRun
        {
            Id = Guid.NewGuid(),
            Name = "Test Simulation 1",
            ConsensusAlgorithm = ConsensusAlgorithm.ProofOfElapsedTime,
            NodeCount = 3,
            Status = SimulationStatus.Completed
        };

        var simulation2 = new SimulationRun
        {
            Id = Guid.NewGuid(),
            Name = "Test Simulation 2",
            ConsensusAlgorithm = ConsensusAlgorithm.ProofOfWork,
            NodeCount = 5,
            Status = SimulationStatus.Running
        };

        _dbContext.SimulationRuns.AddRange(simulation1, simulation2);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/simulations");

        // Assert
        response.EnsureSuccessStatusCode();
        var simulations = await response.Content.ReadFromJsonAsync<List<SimulationResponse>>();
        
        Assert.NotNull(simulations);
        Assert.Equal(2, simulations.Count);
        Assert.Contains(simulations, s => s.Name == "Test Simulation 1");
        Assert.Contains(simulations, s => s.Name == "Test Simulation 2");
    }

    [Fact]
    public async Task GET_Simulation_ById_ShouldReturnSimulation()
    {
        // Arrange
        var simulationId = Guid.NewGuid();
        var simulation = new SimulationRun
        {
            Id = simulationId,
            Name = "Test Simulation",
            ConsensusAlgorithm = ConsensusAlgorithm.ProofOfElapsedTime,
            NodeCount = 4,
            Status = SimulationStatus.Running
        };

        _dbContext.SimulationRuns.Add(simulation);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/simulations/{simulationId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SimulationResponse>();
        
        Assert.NotNull(result);
        Assert.Equal(simulationId, result.Id);
        Assert.Equal("Test Simulation", result.Name);
    }

    [Fact]
    public async Task GET_Simulation_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/simulations/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_StopSimulation_ShouldStopRunningSimulation()
    {
        // Arrange
        var simulationId = Guid.NewGuid();
        var simulation = new SimulationRun
        {
            Id = simulationId,
            Name = "Running Simulation",
            ConsensusAlgorithm = ConsensusAlgorithm.ProofOfElapsedTime,
            NodeCount = 3,
            Status = SimulationStatus.Running
        };

        _dbContext.SimulationRuns.Add(simulation);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.PostAsync($"/api/simulations/{simulationId}/stop", null);

        // Assert
        response.EnsureSuccessStatusCode();
        
        // Verify simulation is stopped in database
        var stoppedSimulation = await _dbContext.SimulationRuns.FindAsync(simulationId);
        Assert.NotNull(stoppedSimulation);
        Assert.Equal(SimulationStatus.Stopped, stoppedSimulation.Status);
    }

    [Fact]
    public async Task GET_SimulationBlocks_ShouldReturnBlocksForSimulation()
    {
        // Arrange
        var simulationId = Guid.NewGuid();
        var simulation = new SimulationRun
        {
            Id = simulationId,
            Name = "Test Simulation",
            ConsensusAlgorithm = ConsensusAlgorithm.ProofOfElapsedTime,
            NodeCount = 3,
            Status = SimulationStatus.Completed
        };

        var block1 = new Block
        {
            Id = Guid.NewGuid(),
            BlockNumber = 0,
            Hash = "genesis_hash",
            SimulationRunId = simulationId,
            Timestamp = DateTime.UtcNow.AddMinutes(-10)
        };

        var block2 = new Block
        {
            Id = Guid.NewGuid(),
            BlockNumber = 1,
            Hash = "block_1_hash",
            PreviousHash = "genesis_hash",
            SimulationRunId = simulationId,
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        };

        _dbContext.SimulationRuns.Add(simulation);
        _dbContext.Blocks.AddRange(block1, block2);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/simulations/{simulationId}/blocks");

        // Assert
        response.EnsureSuccessStatusCode();
        var blocks = await response.Content.ReadFromJsonAsync<List<BlockSummary>>();
        
        Assert.NotNull(blocks);
        Assert.Equal(2, blocks.Count);
        Assert.Contains(blocks, b => b.BlockNumber == 0);
        Assert.Contains(blocks, b => b.BlockNumber == 1);
    }

    [Fact]
    public async Task GET_SimulationEvents_ShouldReturnEventsForSimulation()
    {
        // Arrange
        var simulationId = Guid.NewGuid();
        var simulation = new SimulationRun
        {
            Id = simulationId,
            Name = "Test Simulation",
            ConsensusAlgorithm = ConsensusAlgorithm.ProofOfElapsedTime,
            NodeCount = 3,
            Status = SimulationStatus.Running
        };

        var event1 = new EventLog
        {
            Id = Guid.NewGuid(),
            SimulationRunId = simulationId,
            EventType = EventType.SimulationStarted,
            Level = LogLevel.Information,
            Message = "Simulation started",
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        };

        var event2 = new EventLog
        {
            Id = Guid.NewGuid(),
            SimulationRunId = simulationId,
            EventType = EventType.BlockCreated,
            Level = LogLevel.Information,
            Message = "Block created",
            Timestamp = DateTime.UtcNow.AddMinutes(-2)
        };

        _dbContext.SimulationRuns.Add(simulation);
        _dbContext.EventLogs.AddRange(event1, event2);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/simulations/{simulationId}/events");

        // Assert
        response.EnsureSuccessStatusCode();
        var events = await response.Content.ReadFromJsonAsync<List<SimulationEvent>>();
        
        Assert.NotNull(events);
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.Message == "Simulation started");
        Assert.Contains(events, e => e.Message == "Block created");
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _client?.Dispose();
    }
}

// DTOs for API testing
public class StartSimulationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ConsensusAlgorithm ConsensusAlgorithm { get; set; }
    public int NodeCount { get; set; }
    public int? TargetBlockCount { get; set; }
    public int? DurationSeconds { get; set; }
    public Dictionary<string, object>? Configuration { get; set; }
}