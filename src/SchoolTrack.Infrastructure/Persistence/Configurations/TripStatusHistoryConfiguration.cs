using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class TripStatusHistoryConfiguration : IEntityTypeConfiguration<TripStatusHistory>
{
    public void Configure(EntityTypeBuilder<TripStatusHistory> builder)
    {
        builder.ToTable("trip_status_history");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(h => h.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(h => h.TripId).HasColumnName("trip_id").IsRequired();

        builder.Property(h => h.FromStatus).HasColumnName("from_status").HasMaxLength(50).IsRequired();
        builder.Property(h => h.ToStatus).HasColumnName("to_status").HasMaxLength(50).IsRequired();
        builder.Property(h => h.ChangedByUserId).HasColumnName("changed_by_user_id").IsRequired();
        builder.Property(h => h.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(h => h.ChangedAt).HasColumnName("changed_at").HasColumnType("timestamptz").IsRequired();

        builder.Property(h => h.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(h => h.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(h => new { h.TripId, h.ChangedAt })
            .HasDatabaseName("ix_trip_status_history_trip_changed");

        builder.HasOne(h => h.Trip)
            .WithMany(t => t.StatusHistory)
            .HasForeignKey(h => h.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

