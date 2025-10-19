using Consensus.Data;
using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Consensus.Web.Services;

/// <summary>
/// Service for database initialization and seeding
/// </summary>
public class DatabaseInitializationService
{
    private readonly ConsensusDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(
        ConsensusDbContext context,
        IConfiguration configuration,
        ILogger<DatabaseInitializationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Initialize database with migrations and optional seeding
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Check if we should auto-migrate in this environment
            var autoMigrate = _configuration.GetValue<bool>("ConsensusSimulator:AutoMigrateDatabase", false);
            
            if (autoMigrate)
            {
                _logger.LogInformation("Auto-migrating database...");
                await _context.Database.MigrateAsync();
                _logger.LogInformation("Database migration completed successfully");
            }
            else
            {
                // Just ensure the database can be connected to
                var canConnect = await _context.Database.CanConnectAsync();
                _logger.LogInformation("Database connection test: {CanConnect}", canConnect ? "Success" : "Failed");
            }
            
            // Seed test data if enabled
            var shouldSeed = _configuration.GetValue<bool>("ConsensusSimulator:SeedTestData", false);
            if (shouldSeed && autoMigrate)
            {
                await SeedTestDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database");
            // Don't throw - let the application continue even if DB init fails
        }
    }

    /// <summary>
    /// Seed database with test data for development
    /// </summary>
    private async Task SeedTestDataAsync()
    {
        _logger.LogInformation("Seeding test data...");
        
        // Check if we already have data
        if (await _context.SimulationRuns.AnyAsync())
        {
            _logger.LogInformation("Test data already exists, skipping seeding");
            return;
        }
        
        // Create a sample simulation run
        var simulationRun = new SimulationRun
        {
            Id = Guid.NewGuid(),
            Name = "Development Test Simulation",
            Description = "Test simulation for development purposes",
            ConsensusAlgorithm = ConsensusAlgorithm.ProofOfStake,
            Status = SimulationStatus.Ready,
            Configuration = new Dictionary<string, object>
            {
                { "nodeCount", 5 },
                { "maxRounds", 10 },
                { "networkLatency", 100 }
            },
            CreatedAt = DateTime.UtcNow
        };
        
        _context.SimulationRuns.Add(simulationRun);
        
        // Create sample nodes for the simulation
        for (int i = 1; i <= 5; i++)
        {
            var node = new Node
            {
                Id = Guid.NewGuid(),
                Name = $"TestNode{i:D2}",
                Status = NodeStatus.Offline,
                ConsensusAlgorithm = ConsensusAlgorithm.ProofOfStake,
                StakeAmount = 1000 + (i * 100),
                SimulationRunId = simulationRun.Id,
                ConnectionInfo = $"127.0.0.1:900{i}",
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Nodes.Add(node);
        }
        
        await _context.SaveChangesAsync();
        _logger.LogInformation("Test data seeding completed");
    }

    /// <summary>
    /// Check if database is available and properly configured
    /// </summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            return await _context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }
}