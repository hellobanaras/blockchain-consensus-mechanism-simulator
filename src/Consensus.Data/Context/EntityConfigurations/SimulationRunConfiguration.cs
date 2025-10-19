using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Consensus.Core.Entities;
using System.Text.Json;

namespace Consensus.Data.Context.EntityConfigurations;

/// <summary>
/// Entity Framework configuration for SimulationRun entity
/// </summary>
public class SimulationRunConfiguration : IEntityTypeConfiguration<SimulationRun>
{
    public void Configure(EntityTypeBuilder<SimulationRun> builder)
    {
        // Table configuration
        builder.ToTable("SimulationRuns");
        
        // Primary key
        builder.HasKey(sr => sr.Id);
        
        // Properties
        builder.Property(sr => sr.Name)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(sr => sr.Description)
            .HasMaxLength(500);
            
        builder.Property(sr => sr.Status)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(sr => sr.ConsensusAlgorithm)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(sr => sr.NodeCount)
            .IsRequired();
            
        builder.Property(sr => sr.ByzantineNodeCount)
            .IsRequired();
            
        // JSON columns for complex data
        builder.Property(sr => sr.Configuration)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");
            
        builder.Property(sr => sr.Results)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");
            
        // Timestamps
        builder.Property(sr => sr.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.Property(sr => sr.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        // Indexes
        builder.HasIndex(sr => sr.Name);
        builder.HasIndex(sr => sr.Status);
        builder.HasIndex(sr => sr.ConsensusAlgorithm);
        builder.HasIndex(sr => sr.CreatedAt);
        
        // Relationships
        builder.HasMany(sr => sr.Nodes)
            .WithOne(n => n.SimulationRun)
            .HasForeignKey(n => n.SimulationRunId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(sr => sr.Blocks)
            .WithOne(b => b.SimulationRun)
            .HasForeignKey(b => b.SimulationRunId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(sr => sr.Transactions)
            .WithOne(t => t.SimulationRun)
            .HasForeignKey(t => t.SimulationRunId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(sr => sr.ConsensusRounds)
            .WithOne(cr => cr.SimulationRun)
            .HasForeignKey(cr => cr.SimulationRunId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(sr => sr.NetworkTopologies)
            .WithOne(nt => nt.SimulationRun)
            .HasForeignKey(nt => nt.SimulationRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}