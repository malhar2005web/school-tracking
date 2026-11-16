using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class RouteAssignmentConfiguration : IEntityTypeConfiguration<RouteAssignment>
{
    public void Configure(EntityTypeBuilder<RouteAssignment> builder)
    {
        builder.ToTable("route_assignments");

        builder.HasKey(ra => ra.Id);
        builder.Property(ra => ra.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(ra => ra.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(ra => ra.RouteId).HasColumnName("route_id").IsRequired();
        builder.Property(ra => ra.BusId).HasColumnName("bus_id").IsRequired();
        builder.Property(ra => ra.DriverId).HasColumnName("driver_id").IsRequired();

        builder.Property(ra => ra.Shift).HasColumnName("shift").HasMaxLength(50).IsRequired();
        builder.Property(ra => ra.EffectiveFrom).HasColumnName("effective_from").HasColumnType("timestamptz").IsRequired();
        builder.Property(ra => ra.EffectiveTo).HasColumnName("effective_to").HasColumnType("timestamptz");
        builder.Property(ra => ra.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        builder.Property(ra => ra.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(ra => ra.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(ra => ra.Route)
            .WithMany(r => r.Assignments)
            .HasForeignKey(ra => ra.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ra => ra.Bus)
            .WithMany()
            .HasForeignKey(ra => ra.BusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ra => ra.Driver)
            .WithMany()
            .HasForeignKey(ra => ra.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

