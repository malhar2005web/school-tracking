using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class ClassConfiguration : IEntityTypeConfiguration<Class>
{
    public void Configure(EntityTypeBuilder<Class> builder)
    {
        builder.ToTable("classes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.SchoolId).HasColumnName("school_id").IsRequired();

        builder.Property(c => c.Grade).HasColumnName("grade").HasMaxLength(50).IsRequired();
        builder.Property(c => c.Division).HasColumnName("division").HasMaxLength(50).IsRequired();
        builder.Property(c => c.AcademicYear).HasColumnName("academic_year").HasMaxLength(20).IsRequired();

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(c => new { c.SchoolId, c.Grade, c.Division, c.AcademicYear })
            .IsUnique()
            .HasDatabaseName("ix_classes_school_grade_division_year");
    }
}

