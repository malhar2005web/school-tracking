using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class ParentConfiguration : IEntityTypeConfiguration<Parent>
{
    public void Configure(EntityTypeBuilder<Parent> builder)
    {
        builder.ToTable("parents");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(p => p.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(p => p.PrimaryPhone).HasColumnName("primary_phone").HasMaxLength(50).IsRequired();
        builder.Property(p => p.AlternatePhone).HasColumnName("alternate_phone").HasMaxLength(50);
        builder.Property(p => p.EmergencyContactName).HasColumnName("emergency_contact_name").HasMaxLength(150);
        builder.Property(p => p.EmergencyContactPhone).HasColumnName("emergency_contact_phone").HasMaxLength(50);
        builder.Property(p => p.Address).HasColumnName("address").HasMaxLength(500);

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(p => new { p.SchoolId, p.UserId })
            .IsUnique()
            .HasDatabaseName("ix_parents_school_user_id");

        builder.HasOne(p => p.User)
            .WithOne(u => u.ParentProfile)
            .HasForeignKey<Parent>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

