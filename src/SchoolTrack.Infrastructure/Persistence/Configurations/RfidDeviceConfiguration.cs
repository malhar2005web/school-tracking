using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class RfidDeviceConfiguration : IEntityTypeConfiguration<RfidDevice>
{
    public void Configure(EntityTypeBuilder<RfidDevice> builder)
    {
        builder.ToTable("rfid_devices");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(r => r.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(r => r.BusId).HasColumnName("bus_id");

        builder.Property(r => r.DeviceCode).HasColumnName("device_code").HasMaxLength(100).IsRequired();
        builder.Property(r => r.ApiKey).HasColumnName("api_key").HasMaxLength(100);
        builder.Property(r => r.MacAddress).HasColumnName("mac_address").HasMaxLength(50);
        builder.Property(r => r.GateLocation).HasColumnName("gate_location").HasMaxLength(150);
        builder.Property(r => r.Model).HasColumnName("model").HasMaxLength(100);
        builder.Property(r => r.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
        builder.Property(r => r.Port).HasColumnName("port");
        builder.Property(r => r.ProtocolType).HasColumnName("protocol_type").HasMaxLength(50).HasDefaultValue("Simulator").IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("Offline").IsRequired();
        builder.Property(r => r.LastSeenAt).HasColumnName("last_seen_at").HasColumnType("timestamptz");
        builder.Property(r => r.LastScanAt).HasColumnName("last_scan_at").HasColumnType("timestamptz");

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(r => new { r.SchoolId, r.DeviceCode })
            .IsUnique()
            .HasDatabaseName("ix_rfid_devices_school_device_code");

        builder.HasOne(r => r.Bus)
            .WithOne(b => b.RfidDevice)
            .HasForeignKey<RfidDevice>(r => r.BusId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

