using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Consensus.Core.Entities;
using System.Text.Json;

namespace Consensus.Data.Context.EntityConfigurations;

/// <summary>
/// Entity Framework configuration for remaining entities
/// </summary>
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.Hash)
            .IsRequired()
            .HasMaxLength(64);
            
        builder.Property(t => t.FromAddress)
            .HasMaxLength(42);
            
        builder.Property(t => t.ToAddress)
            .HasMaxLength(42);
            
        builder.Property(t => t.Amount)
            .HasPrecision(18, 8);
            
        builder.Property(t => t.Fee)
            .HasPrecision(18, 8);
            
        builder.Property(t => t.Signature)
            .HasMaxLength(1000);
            
        builder.Property(t => t.Data)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");
            
        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.HasIndex(t => t.Hash).IsUnique();
        builder.HasIndex(t => t.FromAddress);
        builder.HasIndex(t => t.ToAddress);
        builder.HasIndex(t => t.SimulationRunId);
        builder.HasIndex(t => t.BlockId);
    }
}

public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.ToTable("Votes");
        builder.HasKey(v => v.Id);
        
        builder.Property(v => v.VoteType)
            .IsRequired()
            .HasConversion<string>();
        
        builder.Property(v => v.Value)
            .IsRequired();
            
        builder.Property(v => v.ValueHash)
            .HasMaxLength(64);
            
        builder.Property(v => v.Signature)
            .HasMaxLength(128);
            
        builder.Property(v => v.Weight)
            .HasPrecision(18, 8)
            .HasDefaultValue(1m);
            
        builder.Property(v => v.Data)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");
            
        builder.Property(v => v.CastedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.Property(v => v.ReceivedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.HasIndex(v => v.NodeId);
        builder.HasIndex(v => v.ConsensusRoundId);
        builder.HasIndex(v => new { v.NodeId, v.ConsensusRoundId }).IsUnique();
        
        // Relationships
        builder.HasOne(v => v.Node)
            .WithMany()
            .HasForeignKey(v => v.NodeId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(v => v.ConsensusRound)
            .WithMany(cr => cr.Votes)
            .HasForeignKey(v => v.ConsensusRoundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EventLogConfiguration : IEntityTypeConfiguration<EventLog>
{
    public void Configure(EntityTypeBuilder<EventLog> builder)
    {
        builder.ToTable("EventLogs");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.EventType)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(e => e.Level)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(e => e.Message)
            .IsRequired()
            .HasMaxLength(1000);
            
        builder.Property(e => e.Source)
            .HasMaxLength(100);
            
        builder.Property(e => e.Data)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");
            
        builder.Property(e => e.Timestamp)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => e.Level);
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => e.SimulationRunId);
    }
}

public class NetworkTopologyConfiguration : IEntityTypeConfiguration<NetworkTopology>
{
    public void Configure(EntityTypeBuilder<NetworkTopology> builder)
    {
        builder.ToTable("NetworkTopologies");
        builder.HasKey(nt => nt.Id);
        
        builder.Property(nt => nt.Name)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(nt => nt.Description)
            .HasMaxLength(500);
            
        builder.Property(nt => nt.TopologyType)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(nt => nt.NodeCount)
            .HasDefaultValue(0);
            
        builder.Property(nt => nt.AverageConnections)
            .HasPrecision(10, 2);
            
        builder.Property(nt => nt.MaxLatencyMs)
            .HasDefaultValue(1000);
            
        builder.Property(nt => nt.MinLatencyMs)
            .HasDefaultValue(10);
            
        builder.Property(nt => nt.PartitionProbability)
            .HasPrecision(5, 4);
            
        builder.Property(nt => nt.MessageLossProbability)
            .HasPrecision(5, 4);
            
        builder.Property(nt => nt.BandwidthLimitBps)
            .HasDefaultValue(0);
            
        builder.Property(nt => nt.Configuration)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");
            
        // Ignore complex properties that can't be easily mapped
        builder.Ignore(nt => nt.AdjacencyMatrix);
        builder.Ignore(nt => nt.Partitions);
        
        builder.Property(nt => nt.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.Property(nt => nt.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        builder.HasIndex(nt => nt.TopologyType);
        builder.HasIndex(nt => nt.SimulationRunId);
        
        // Relationships
        builder.HasOne(nt => nt.SimulationRun)
            .WithMany(sr => sr.NetworkTopologies)
            .HasForeignKey(nt => nt.SimulationRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}