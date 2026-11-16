using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(t => t.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(t => t.RouteId).HasColumnName("route_id").IsRequired();
        builder.Property(t => t.BusId).HasColumnName("bus_id").IsRequired();
        builder.Property(t => t.DriverId).HasColumnName("driver_id").IsRequired();

        builder.Property(t => t.TripDate).HasColumnName("trip_date").HasColumnType("date").IsRequired();
        builder.Property(t => t.Shift).HasColumnName("shift").HasMaxLength(50).IsRequired();
        builder.Property(t => t.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("Scheduled").IsRequired();
        builder.Property(t => t.ScheduledStartAt).HasColumnName("scheduled_start_at").HasColumnType("timestamptz");
        builder.Property(t => t.ActualStartAt).HasColumnName("actual_start_at").HasColumnType("timestamptz");
        builder.Property(t => t.ActualEndAt).HasColumnName("actual_end_at").HasColumnType("timestamptz");
        builder.Property(t => t.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(t => new { t.SchoolId, t.TripDate, t.Shift })
            .HasDatabaseName("ix_trips_school_date_shift");

        builder.HasIndex(t => new { t.BusId, t.TripDate, t.Shift })
            .HasDatabaseName("ix_trips_bus_date_shift");

        builder.HasIndex(t => new { t.DriverId, t.TripDate, t.Shift })
            .HasDatabaseName("ix_trips_driver_date_shift");

        builder.HasOne(t => t.Route)
            .WithMany(r => r.Trips)
            .HasForeignKey(t => t.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Bus)
            .WithMany()
            .HasForeignKey(t => t.BusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Driver)
            .WithMany()
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

