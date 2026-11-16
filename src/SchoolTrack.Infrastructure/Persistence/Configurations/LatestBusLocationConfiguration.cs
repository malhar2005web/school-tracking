using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class LatestBusLocationConfiguration : IEntityTypeConfiguration<LatestBusLocation>
{
    public void Configure(EntityTypeBuilder<LatestBusLocation> builder)
    {
        builder.ToTable("latest_bus_locations");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(l => l.BusId).HasColumnName("bus_id").IsRequired();
        builder.Property(l => l.TripId).HasColumnName("trip_id");
        builder.Property(l => l.Latitude).HasColumnName("latitude").HasPrecision(10, 7).IsRequired();
        builder.Property(l => l.Longitude).HasColumnName("longitude").HasPrecision(10, 7).IsRequired();
        builder.Property(l => l.SpeedKmh).HasColumnName("speed_kmh").HasPrecision(5, 2).IsRequired();
        builder.Property(l => l.Heading).HasColumnName("heading").HasPrecision(5, 2).IsRequired();
        builder.Property(l => l.Altitude).HasColumnName("altitude").HasPrecision(7, 2);
        builder.Property(l => l.AccuracyMeters).HasColumnName("accuracy_meters").HasPrecision(5, 2);
        builder.Property(l => l.IgnitionOn).HasColumnName("ignition_on").IsRequired().HasDefaultValue(true);
        builder.Property(l => l.IsOnline).HasColumnName("is_online").IsRequired().HasDefaultValue(true);
        builder.Property(l => l.RecordedAt).HasColumnName("recorded_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(l => l.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(l => l.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(l => l.Bus)
            .WithMany()
            .HasForeignKey(l => l.BusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Trip)
            .WithMany()
            .HasForeignKey(l => l.TripId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => l.BusId).IsUnique();
        builder.HasIndex(l => new { l.SchoolId, l.IsOnline });
    }
}

