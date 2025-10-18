using Microsoft.EntityFrameworkCore;
using Consensus.Core.Entities;

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

        // Configure entity relationships and constraints
        ConfigureNode(modelBuilder);
        ConfigureBlock(modelBuilder);
        ConfigureTransaction(modelBuilder);
        ConfigureConsensusRound(modelBuilder);
        ConfigureVote(modelBuilder);
        ConfigureSimulationRun(modelBuilder);
        ConfigureNetworkTopology(modelBuilder);
        ConfigureEventLog(modelBuilder);

        // Configure indexes for performance
        ConfigureIndexes(modelBuilder);

        // Set default values and constraints
        ConfigureDefaults(modelBuilder);
    }

    private void ConfigureNode(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Node>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
            
            entity.Property(e => e.ConsensusAlgorithm)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);
            
            entity.Property(e => e.ConnectionInfo)
                .HasMaxLength(500);
                
            entity.Property(e => e.Configuration)
                .HasColumnType("jsonb");
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");
            
            entity.Property(e => e.LastSeen)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Indexes
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.SimulationRunId, e.Status });
        });
    }

    private void ConfigureBlock(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Block>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.BlockNumber)
                .IsRequired();
            
            entity.Property(e => e.Hash)
                .IsRequired()
                .HasMaxLength(64);
            
            entity.Property(e => e.PreviousHash)
                .HasMaxLength(64);
            
            entity.Property(e => e.MerkleRoot)
                .HasMaxLength(64);
            
            entity.Property(e => e.Data)
                .HasColumnType("jsonb");
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Relationships
            entity.HasOne<Node>()
                .WithMany()
                .HasForeignKey(e => e.ProposerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<SimulationRun>()
                .WithMany()
                .HasForeignKey(e => e.SimulationRunId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.Hash).IsUnique();
            entity.HasIndex(e => new { e.SimulationRunId, e.BlockNumber });
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    private void ConfigureTransaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Hash)
                .IsRequired()
                .HasMaxLength(64);
            
            entity.Property(e => e.FromAddress)
                .HasMaxLength(42);
            
            entity.Property(e => e.ToAddress)
                .HasMaxLength(42);
            
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,8)");
            
            entity.Property(e => e.Fee)
                .HasColumnType("decimal(18,8)");
            
            entity.Property(e => e.Data)
                .HasColumnType("jsonb");
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Relationships
            entity.HasOne<Block>()
                .WithMany()
                .HasForeignKey(e => e.BlockId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<SimulationRun>()
                .WithMany()
                .HasForeignKey(e => e.SimulationRunId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.Hash).IsUnique();
            entity.HasIndex(e => new { e.SimulationRunId, e.Status });
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    private void ConfigureConsensusRound(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConsensusRound>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.RoundNumber)
                .IsRequired();
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
            
            entity.Property(e => e.ProposedValue)
                .HasColumnType("jsonb");
                
            entity.Property(e => e.AgreedValue)
                .HasColumnType("jsonb");
                
            entity.Property(e => e.Data)
                .HasColumnType("jsonb");
            
            entity.Property(e => e.StartedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Relationships
            entity.HasOne<Node>()
                .WithMany()
                .HasForeignKey(e => e.LeaderId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<SimulationRun>()
                .WithMany()
                .HasForeignKey(e => e.SimulationRunId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => new { e.SimulationRunId, e.RoundNumber }).IsUnique();
            entity.HasIndex(e => e.Status);
        });
    }

    private void ConfigureVote(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.VoteType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
            
            entity.Property(e => e.Value)
                .IsRequired();
            
            entity.Property(e => e.Signature)
                .HasMaxLength(128);
                
            entity.Property(e => e.Data)
                .HasColumnType("jsonb");
            
            entity.Property(e => e.CastedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Relationships
            entity.HasOne<Node>()
                .WithMany()
                .HasForeignKey(e => e.NodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ConsensusRound>()
                .WithMany()
                .HasForeignKey(e => e.ConsensusRoundId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => new { e.ConsensusRoundId, e.NodeId }).IsUnique();
            entity.HasIndex(e => e.CastedAt);
        });
    }

    private void ConfigureSimulationRun(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SimulationRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Description)
                .HasMaxLength(500);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
            
            entity.Property(e => e.ConsensusAlgorithm)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);
            
            entity.Property(e => e.Configuration)
                .HasColumnType("jsonb");
            
            entity.Property(e => e.Results)
                .HasColumnType("jsonb");
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Indexes
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ConsensusAlgorithm);
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    private void ConfigureNetworkTopology(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NetworkTopology>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Description)
                .HasMaxLength(500);
            
            entity.Property(e => e.TopologyType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);
            
            entity.Property(e => e.Configuration)
                .HasColumnType("jsonb");
                
            // Ignore the complex nested List property and use JSON serialization instead
            entity.Ignore(e => e.Partitions);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Relationships
            entity.HasOne<SimulationRun>()
                .WithMany()
                .HasForeignKey(e => e.SimulationRunId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.TopologyType);
            entity.HasIndex(e => e.SimulationRunId);
        });
    }

    private void ConfigureEventLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Level)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Info");
            
            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.Source)
                .HasMaxLength(100);
            
            entity.Property(e => e.CorrelationId)
                .HasMaxLength(50);
            
            entity.Property(e => e.Data)
                .HasColumnType("jsonb");
            
            entity.Property(e => e.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Relationships
            entity.HasOne<SimulationRun>()
                .WithMany()
                .HasForeignKey(e => e.SimulationRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Node>()
                .WithMany()
                .HasForeignKey(e => e.NodeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<ConsensusRound>()
                .WithMany()
                .HasForeignKey(e => e.ConsensusRoundId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<Block>()
                .WithMany()
                .HasForeignKey(e => e.BlockId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(e => new { e.SimulationRunId, e.Timestamp });
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.CorrelationId);
        });
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Additional composite indexes for complex queries
        // These are in addition to the indexes configured per entity
        
        // Performance indexes for dashboard queries
        modelBuilder.Entity<Node>()
            .HasIndex(e => new { e.SimulationRunId, e.ConsensusAlgorithm, e.Status });
        
        modelBuilder.Entity<Block>()
            .HasIndex(e => new { e.SimulationRunId, e.ProposerId, e.CreatedAt });
        
        modelBuilder.Entity<Transaction>()
            .HasIndex(e => new { e.SimulationRunId, e.BlockId, e.Status });
        
        modelBuilder.Entity<Vote>()
            .HasIndex(e => new { e.ConsensusRoundId, e.VoteType, e.CastedAt });
    }

    private void ConfigureDefaults(ModelBuilder modelBuilder)
    {
        // Configure default values and database-level constraints
        
        modelBuilder.Entity<Node>()
            .Property(e => e.IsActive)
            .HasDefaultValue(true);
        
        modelBuilder.Entity<Block>()
            .Property(e => e.IsValid)
            .HasDefaultValue(true);
        
        modelBuilder.Entity<Transaction>()
            .Property(e => e.Nonce)
            .HasDefaultValue(0);
        
        // Add check constraints for data integrity using modern API
        modelBuilder.Entity<Block>()
            .ToTable(t => t.HasCheckConstraint("CK_Block_BlockNumber", "\"BlockNumber\" >= 0"));
        
        modelBuilder.Entity<Transaction>()
            .ToTable(t => 
            {
                t.HasCheckConstraint("CK_Transaction_Amount", "\"Amount\" >= 0");
                t.HasCheckConstraint("CK_Transaction_Fee", "\"Fee\" >= 0");
            });
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