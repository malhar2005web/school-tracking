using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class TripStopConfiguration : IEntityTypeConfiguration<TripStop>
{
    public void Configure(EntityTypeBuilder<TripStop> builder)
    {
        builder.ToTable("trip_stops");

        builder.HasKey(ts => ts.Id);
        builder.Property(ts => ts.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(ts => ts.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(ts => ts.TripId).HasColumnName("trip_id").IsRequired();
        builder.Property(ts => ts.RouteStopId).HasColumnName("route_stop_id").IsRequired();

        builder.Property(ts => ts.SequenceOrder).HasColumnName("sequence_order").IsRequired();
        builder.Property(ts => ts.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("Pending").IsRequired();
        builder.Property(ts => ts.EstimatedArrivalAt).HasColumnName("estimated_arrival_at").HasColumnType("timestamptz");
        builder.Property(ts => ts.ActualArrivalAt).HasColumnName("actual_arrival_at").HasColumnType("timestamptz");
        builder.Property(ts => ts.ActualDepartureAt).HasColumnName("actual_departure_at").HasColumnType("timestamptz");

        builder.Property(ts => ts.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(ts => ts.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(ts => new { ts.TripId, ts.SequenceOrder })
            .IsUnique()
            .HasDatabaseName("ix_trip_stops_trip_sequence");

        builder.HasOne(ts => ts.Trip)
            .WithMany(t => t.Stops)
            .HasForeignKey(ts => ts.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ts => ts.RouteStop)
            .WithMany(rs => rs.TripStops)
            .HasForeignKey(ts => ts.RouteStopId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

