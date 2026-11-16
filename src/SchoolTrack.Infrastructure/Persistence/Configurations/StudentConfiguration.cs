using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(s => s.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(s => s.ClassId).HasColumnName("class_id").IsRequired();

        builder.Property(s => s.AdmissionNumber).HasColumnName("admission_number").HasMaxLength(50).IsRequired();
        builder.Property(s => s.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(s => s.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        builder.Property(s => s.PhotoUrl).HasColumnName("photo_url").HasMaxLength(500);
        builder.Property(s => s.Gender).HasColumnName("gender").HasMaxLength(20);
        builder.Property(s => s.DateOfBirth).HasColumnName("date_of_birth").HasColumnType("timestamptz");
        builder.Property(s => s.EmergencyContactPhone).HasColumnName("emergency_contact_phone").HasMaxLength(50);
        builder.Property(s => s.SeatNumber).HasColumnName("seat_number").HasMaxLength(20);
        builder.Property(s => s.AssignedRouteId).HasColumnName("assigned_route_id");
        builder.Property(s => s.AssignedStopId).HasColumnName("assigned_stop_id");
        builder.Property(s => s.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(s => new { s.SchoolId, s.AdmissionNumber })
            .IsUnique()
            .HasDatabaseName("ix_students_school_admission_number");

        builder.HasOne(s => s.Class)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

