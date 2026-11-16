using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("drivers");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(d => d.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(d => d.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(d => d.LicenseNumber).HasColumnName("license_number").HasMaxLength(50).IsRequired();
        builder.Property(d => d.LicenseExpiry).HasColumnName("license_expiry").HasColumnType("timestamptz");
        builder.Property(d => d.Phone).HasColumnName("phone").HasMaxLength(50).IsRequired();
        builder.Property(d => d.AvailabilityStatus).HasColumnName("availability_status").HasMaxLength(50).HasDefaultValue("Available").IsRequired();
        builder.Property(d => d.YearsExperience).HasColumnName("years_experience").HasDefaultValue(0).IsRequired();
        builder.Property(d => d.Rating).HasColumnName("rating").HasDefaultValue(5.0).IsRequired();

        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(d => new { d.SchoolId, d.LicenseNumber })
            .IsUnique()
            .HasDatabaseName("ix_drivers_school_license_number");

        builder.HasIndex(d => new { d.SchoolId, d.UserId })
            .IsUnique()
            .HasDatabaseName("ix_drivers_school_user_id");

        builder.HasOne(d => d.User)
            .WithOne(u => u.DriverProfile)
            .HasForeignKey<Driver>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

