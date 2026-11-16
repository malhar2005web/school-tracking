using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class UnknownCardTapConfiguration : IEntityTypeConfiguration<UnknownCardTap>
{
    public void Configure(EntityTypeBuilder<UnknownCardTap> builder)
    {
        builder.ToTable("unknown_card_taps");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(u => u.BusId).HasColumnName("bus_id");
        builder.Property(u => u.TripId).HasColumnName("trip_id");
        builder.Property(u => u.RfidDeviceId).HasColumnName("rfid_device_id");

        builder.Property(u => u.DeviceCode).HasColumnName("device_code").HasMaxLength(100).IsRequired();
        builder.Property(u => u.CardUid).HasColumnName("card_uid").HasMaxLength(100).IsRequired();
        builder.Property(u => u.Reason).HasColumnName("reason").HasMaxLength(100).IsRequired();
        builder.Property(u => u.Latitude).HasColumnName("latitude").HasPrecision(10, 7);
        builder.Property(u => u.Longitude).HasColumnName("longitude").HasPrecision(10, 7);
        builder.Property(u => u.ScannedAt).HasColumnName("scanned_at").HasColumnType("timestamptz").IsRequired();

        builder.Property(u => u.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(u => u.Bus)
            .WithMany()
            .HasForeignKey(u => u.BusId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.Trip)
            .WithMany()
            .HasForeignKey(u => u.TripId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.RfidDevice)
            .WithMany()
            .HasForeignKey(u => u.RfidDeviceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(u => new { u.SchoolId, u.ScannedAt });
        builder.HasIndex(u => u.CardUid);
    }
}

