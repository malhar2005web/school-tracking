using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class BusConfiguration : IEntityTypeConfiguration<Bus>
{
    public void Configure(EntityTypeBuilder<Bus> builder)
    {
        builder.ToTable("buses");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(b => b.SchoolId).HasColumnName("school_id").IsRequired();

        builder.Property(b => b.BusNumber).HasColumnName("bus_number").HasMaxLength(50).IsRequired();
        builder.Property(b => b.RegistrationNumber).HasColumnName("registration_number").HasMaxLength(50).IsRequired();
        builder.Property(b => b.Model).HasColumnName("model").HasMaxLength(100);
        builder.Property(b => b.Capacity).HasColumnName("capacity").HasDefaultValue(30).IsRequired();
        builder.Property(b => b.MaintenanceStatus).HasColumnName("maintenance_status").HasMaxLength(50).HasDefaultValue("Active").IsRequired();
        builder.Property(b => b.InsuranceExpiry).HasColumnName("insurance_expiry").HasColumnType("timestamptz");
        builder.Property(b => b.FitnessExpiry).HasColumnName("fitness_expiry").HasColumnType("timestamptz");
        builder.Property(b => b.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        builder.Property(b => b.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(b => new { b.SchoolId, b.RegistrationNumber })
            .IsUnique()
            .HasDatabaseName("ix_buses_school_registration_number");
    }
}

