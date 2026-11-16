using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class BoardingEventConfiguration : IEntityTypeConfiguration<BoardingEvent>
{
    public void Configure(EntityTypeBuilder<BoardingEvent> builder)
    {
        builder.ToTable("boarding_events");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id");
        builder.Property(b => b.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(b => b.TripId).HasColumnName("trip_id").IsRequired();
        builder.Property(b => b.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(b => b.RfidCardId).HasColumnName("rfid_card_id").IsRequired();
        builder.Property(b => b.RouteStopId).HasColumnName("route_stop_id");
        builder.Property(b => b.BusId).HasColumnName("bus_id").IsRequired();
        builder.Property(b => b.RfidDeviceId).HasColumnName("rfid_device_id").IsRequired();

        builder.Property(b => b.ScanType).HasColumnName("scan_type").HasMaxLength(50).IsRequired();
        builder.Property(b => b.Latitude).HasColumnName("latitude").HasPrecision(10, 7);
        builder.Property(b => b.Longitude).HasColumnName("longitude").HasPrecision(10, 7);
        builder.Property(b => b.ScannedAt).HasColumnName("scanned_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(b => b.RecordedAt).HasColumnName("recorded_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(b => b.IsManualOverride).HasColumnName("is_manual_override").IsRequired().HasDefaultValue(false);
        builder.Property(b => b.Notes).HasColumnName("notes").HasMaxLength(500);

        builder.Property(b => b.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(b => b.Trip)
            .WithMany()
            .HasForeignKey(b => b.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Student)
            .WithMany()
            .HasForeignKey(b => b.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.RfidCard)
            .WithMany()
            .HasForeignKey(b => b.RfidCardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.RouteStop)
            .WithMany()
            .HasForeignKey(b => b.RouteStopId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.Bus)
            .WithMany()
            .HasForeignKey(b => b.BusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.RfidDevice)
            .WithMany()
            .HasForeignKey(b => b.RfidDeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.TripId, b.StudentId, b.ScannedAt });
        builder.HasIndex(b => new { b.SchoolId, b.ScannedAt });
        builder.HasIndex(b => new { b.StudentId, b.ScannedAt });
    }
}

