using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
{
    public void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        builder.ToTable("route_stops");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(s => s.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(s => s.RouteId).HasColumnName("route_id").IsRequired();

        builder.Property(s => s.StopName).HasColumnName("stop_name").HasMaxLength(100).IsRequired();
        builder.Property(s => s.SequenceOrder).HasColumnName("sequence_order").IsRequired();
        builder.Property(s => s.Latitude).HasColumnName("latitude").HasPrecision(10, 7).IsRequired();
        builder.Property(s => s.Longitude).HasColumnName("longitude").HasPrecision(10, 7).IsRequired();
        builder.Property(s => s.ScheduledOffsetMinutes).HasColumnName("scheduled_offset_minutes").HasDefaultValue(0).IsRequired();
        builder.Property(s => s.GeofenceRadiusMeters).HasColumnName("geofence_radius_meters").HasDefaultValue(50).IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(s => new { s.RouteId, s.SequenceOrder })
            .IsUnique()
            .HasDatabaseName("ix_route_stops_route_sequence");

        builder.HasOne(s => s.Route)
            .WithMany(r => r.Stops)
            .HasForeignKey(s => s.RouteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

