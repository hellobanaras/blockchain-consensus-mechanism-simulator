using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Consensus.Core.Entities;

namespace Consensus.Data.Context.EntityConfigurations;

/// <summary>
/// Entity Framework configuration for Node entity
/// </summary>
public class NodeConfiguration : IEntityTypeConfiguration<Node>
{
    public void Configure(EntityTypeBuilder<Node> builder)
    {
        // Table configuration
        builder.ToTable("Nodes");
        
        // Primary key
        builder.HasKey(n => n.Id);
        
        // Properties
        builder.Property(n => n.Name)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(n => n.Status)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(n => n.ConsensusAlgorithm)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(n => n.ConnectionInfo)
            .HasMaxLength(500);
            
        builder.Property(n => n.IsActive)
            .HasDefaultValue(true);
            
        builder.Property(n => n.StakeAmount)
            .HasPrecision(18, 8);
            
        builder.Property(n => n.ComputationalPower)
            .HasDefaultValue(0);
            
        builder.Property(n => n.ReputationScore)
            .HasPrecision(18, 8)
            .HasDefaultValue(100m);
            
        builder.Property(n => n.NetworkLatency)
            .HasDefaultValue(100);
            
        builder.Property(n => n.IsByzantine)
            .HasDefaultValue(false);
            
        // Ignore the Configuration property for now to resolve the navigation issue
        builder.Ignore(n => n.Configuration);
            
        // Timestamps
        builder.Property(n => n.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.Property(n => n.LastSeen)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.Property(n => n.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        // Indexes
        builder.HasIndex(n => n.Name);
        builder.HasIndex(n => n.SimulationRunId);
        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => new { n.SimulationRunId, n.Name })
            .IsUnique();
            
        // Relationships
        builder.HasOne(n => n.SimulationRun)
            .WithMany(sr => sr.Nodes)
            .HasForeignKey(n => n.SimulationRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}