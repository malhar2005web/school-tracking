using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class GpsDeviceConfiguration : IEntityTypeConfiguration<GpsDevice>
{
    public void Configure(EntityTypeBuilder<GpsDevice> builder)
    {
        builder.ToTable("gps_devices");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(g => g.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(g => g.BusId).HasColumnName("bus_id");

        builder.Property(g => g.DeviceCode).HasColumnName("device_code").HasMaxLength(100).IsRequired();
        builder.Property(g => g.Imei).HasColumnName("imei").HasMaxLength(50);
        builder.Property(g => g.ApiKey).HasColumnName("api_key").HasMaxLength(100);
        builder.Property(g => g.Model).HasColumnName("model").HasMaxLength(100);
        builder.Property(g => g.SimCardNumber).HasColumnName("sim_card_number").HasMaxLength(50);
        builder.Property(g => g.ProtocolType).HasColumnName("protocol_type").HasMaxLength(50).HasDefaultValue("Simulator").IsRequired();
        builder.Property(g => g.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("Offline").IsRequired();
        builder.Property(g => g.LastSeenAt).HasColumnName("last_seen_at").HasColumnType("timestamptz");
        builder.Property(g => g.LastLocationAt).HasColumnName("last_location_at").HasColumnType("timestamptz");

        builder.Property(g => g.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(g => g.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(g => new { g.SchoolId, g.DeviceCode })
            .IsUnique()
            .HasDatabaseName("ix_gps_devices_school_device_code");

        builder.HasOne(g => g.Bus)
            .WithOne(b => b.GpsDevice)
            .HasForeignKey<GpsDevice>(g => g.BusId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

