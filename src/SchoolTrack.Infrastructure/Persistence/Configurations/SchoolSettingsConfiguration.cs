using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity mapping for school operational settings.
/// </summary>
public sealed class SchoolSettingsConfiguration : IEntityTypeConfiguration<SchoolSettings>
{
    public void Configure(EntityTypeBuilder<SchoolSettings> builder)
    {
        builder.ToTable("school_settings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.SchoolId)
            .HasColumnName("school_id")
            .IsRequired();

        builder.HasIndex(s => s.SchoolId)
            .IsUnique()
            .HasDatabaseName("ix_school_settings_school_id");

        builder.Property(s => s.DuplicateScanWindowSeconds)
            .HasColumnName("duplicate_scan_window_seconds")
            .HasDefaultValue(60)
            .IsRequired();

        builder.Property(s => s.DeviceOfflineThresholdSeconds)
            .HasColumnName("device_offline_threshold_seconds")
            .HasDefaultValue(180)
            .IsRequired();

        builder.Property(s => s.GpsRetentionDays)
            .HasColumnName("gps_retention_days")
            .HasDefaultValue(90)
            .IsRequired();

        builder.Property(s => s.EnableSmsFallback)
            .HasColumnName("enable_sms_fallback")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.HasOne(s => s.School)
            .WithOne(school => school.Settings)
            .HasForeignKey<SchoolSettings>(s => s.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

