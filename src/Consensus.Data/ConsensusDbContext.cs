using Microsoft.EntityFrameworkCore;
using Consensus.Core.Entities;
using Consensus.Data.Context.EntityConfigurations;

namespace Consensus.Data;

/// <summary>
/// Entity Framework DbContext for the Consensus Mechanism Simulator
/// </summary>
public class ConsensusDbContext : DbContext
{
    public ConsensusDbContext(DbContextOptions<ConsensusDbContext> options) : base(options)
    {
    }

    // Entity Sets
    public DbSet<Node> Nodes { get; set; } = null!;
    public DbSet<Block> Blocks { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<ConsensusRound> ConsensusRounds { get; set; } = null!;
    public DbSet<Vote> Votes { get; set; } = null!;
    public DbSet<SimulationRun> SimulationRuns { get; set; } = null!;
    public DbSet<NetworkTopology> NetworkTopologies { get; set; } = null!;
    public DbSet<EventLog> EventLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new SimulationRunConfiguration());
        modelBuilder.ApplyConfiguration(new NodeConfiguration());
        modelBuilder.ApplyConfiguration(new BlockConfiguration());
        modelBuilder.ApplyConfiguration(new ConsensusRoundConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new VoteConfiguration());
        modelBuilder.ApplyConfiguration(new EventLogConfiguration());
        modelBuilder.ApplyConfiguration(new NetworkTopologyConfiguration());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // This will be overridden by dependency injection in production
            optionsBuilder.UseNpgsql("Host=localhost;Database=consensusdb;Username=consensus_user;Password=consensus_password");
        }

        // Enable sensitive data logging in development
        #if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
        #endif
    }
}