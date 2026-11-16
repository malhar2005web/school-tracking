using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class ParentStudentConfiguration : IEntityTypeConfiguration<ParentStudent>
{
    public void Configure(EntityTypeBuilder<ParentStudent> builder)
    {
        builder.ToTable("parent_students");

        builder.HasKey(ps => ps.Id);
        builder.Property(ps => ps.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(ps => ps.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(ps => ps.ParentId).HasColumnName("parent_id").IsRequired();
        builder.Property(ps => ps.StudentId).HasColumnName("student_id").IsRequired();

        builder.Property(ps => ps.Relationship).HasColumnName("relationship").HasMaxLength(50).IsRequired();
        builder.Property(ps => ps.IsPrimaryGuardian).HasColumnName("is_primary_guardian").HasDefaultValue(true).IsRequired();
        builder.Property(ps => ps.CanAuthorizePickup).HasColumnName("can_authorize_pickup").HasDefaultValue(true).IsRequired();

        builder.Property(ps => ps.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(ps => ps.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(ps => new { ps.ParentId, ps.StudentId })
            .IsUnique()
            .HasDatabaseName("ix_parent_students_parent_student");

        builder.HasOne(ps => ps.Parent)
            .WithMany(p => p.StudentLinks)
            .HasForeignKey(ps => ps.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Student)
            .WithMany(s => s.ParentLinks)
            .HasForeignKey(ps => ps.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

