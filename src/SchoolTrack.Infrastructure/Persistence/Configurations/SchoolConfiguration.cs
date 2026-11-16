using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity mapping for the <see cref="School"/> aggregate.
/// </summary>
public sealed class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.ToTable("schools");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(s => s.Code)
            .IsUnique()
            .HasDatabaseName("ix_schools_code");

        builder.Property(s => s.TimeZoneId)
            .HasColumnName("time_zone_id")
            .HasMaxLength(100)
            .HasDefaultValue("UTC")
            .IsRequired();

        builder.Property(s => s.ContactEmail)
            .HasColumnName("contact_email")
            .HasMaxLength(256);

        builder.Property(s => s.ContactPhone)
            .HasColumnName("contact_phone")
            .HasMaxLength(50);

        builder.Property(s => s.Address)
            .HasColumnName("address")
            .HasMaxLength(500);

        builder.Property(s => s.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");
    }
}

