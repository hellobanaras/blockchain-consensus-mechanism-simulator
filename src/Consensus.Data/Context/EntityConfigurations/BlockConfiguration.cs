using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Consensus.Core.Entities;
using System.Text.Json;

namespace Consensus.Data.Context.EntityConfigurations;

/// <summary>
/// Entity Framework configuration for Block entity
/// </summary>
public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        // Table configuration
        builder.ToTable("Blocks");
        
        // Primary key
        builder.HasKey(b => b.Id);
        
        // Properties
        builder.Property(b => b.BlockNumber)
            .IsRequired();
            
        builder.Property(b => b.Hash)
            .IsRequired()
            .HasMaxLength(64);
            
        builder.Property(b => b.PreviousHash)
            .HasMaxLength(64);
            
        builder.Property(b => b.MerkleRoot)
            .HasMaxLength(64);
            
        builder.Property(b => b.Nonce)
            .HasDefaultValue(0);
            
        builder.Property(b => b.Difficulty)
            .HasDefaultValue(0);
            
        builder.Property(b => b.Size)
            .HasDefaultValue(0);
            
        builder.Property(b => b.TransactionCount)
            .HasDefaultValue(0);
            
        builder.Property(b => b.IsValid)
            .HasDefaultValue(true);
            
        // JSON properties
        builder.Property(b => b.Data)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");
            
        // Timestamps
        builder.Property(b => b.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.Property(b => b.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        // Indexes
        builder.HasIndex(b => b.BlockNumber);
        builder.HasIndex(b => b.Hash)
            .IsUnique();
        builder.HasIndex(b => b.PreviousHash);
        builder.HasIndex(b => b.SimulationRunId);
        builder.HasIndex(b => b.ProposerId);
        builder.HasIndex(b => new { b.SimulationRunId, b.BlockNumber })
            .IsUnique();
            
        // Relationships
        builder.HasOne(b => b.SimulationRun)
            .WithMany(sr => sr.Blocks)
            .HasForeignKey(b => b.SimulationRunId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(b => b.Proposer)
            .WithMany()
            .HasForeignKey(b => b.ProposerId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasMany(b => b.Transactions)
            .WithOne(t => t.Block)
            .HasForeignKey(t => t.BlockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}