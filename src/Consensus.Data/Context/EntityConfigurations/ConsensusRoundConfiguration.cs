using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Consensus.Core.Entities;
using System.Text.Json;

namespace Consensus.Data.Context.EntityConfigurations;

/// <summary>
/// Entity Framework configuration for ConsensusRound entity
/// </summary>
public class ConsensusRoundConfiguration : IEntityTypeConfiguration<ConsensusRound>
{
    public void Configure(EntityTypeBuilder<ConsensusRound> builder)
    {
        // Table configuration
        builder.ToTable("ConsensusRounds");
        
        // Primary key
        builder.HasKey(cr => cr.Id);
        
        // Properties
        builder.Property(cr => cr.RoundNumber)
            .IsRequired();
            
        builder.Property(cr => cr.Status)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(cr => cr.ParticipatingNodes)
            .IsRequired();
            
        builder.Property(cr => cr.VotesReceived)
            .HasDefaultValue(0);
            
        builder.Property(cr => cr.PositiveVotes)
            .HasDefaultValue(0);
            
        builder.Property(cr => cr.NegativeVotes)
            .HasDefaultValue(0);
            
        builder.Property(cr => cr.ConsensusThreshold)
            .IsRequired();
            
        builder.Property(cr => cr.ErrorMessage)
            .HasMaxLength(500);
            
        // JSON properties
        builder.Property(cr => cr.ProposedValue)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");
            
        builder.Property(cr => cr.AgreedValue)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");
            
        builder.Property(cr => cr.Data)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");
            
        // Timestamps
        builder.Property(cr => cr.StartedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        // Indexes
        builder.HasIndex(cr => cr.RoundNumber);
        builder.HasIndex(cr => cr.Status);
        builder.HasIndex(cr => cr.SimulationRunId);
        builder.HasIndex(cr => cr.LeaderId);
        builder.HasIndex(cr => new { cr.SimulationRunId, cr.RoundNumber })
            .IsUnique();
            
        // Relationships
        builder.HasOne(cr => cr.SimulationRun)
            .WithMany(sr => sr.ConsensusRounds)
            .HasForeignKey(cr => cr.SimulationRunId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(cr => cr.Leader)
            .WithMany()
            .HasForeignKey(cr => cr.LeaderId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasMany(cr => cr.Votes)
            .WithOne(v => v.ConsensusRound)
            .HasForeignKey(v => v.ConsensusRoundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}