using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class BusAssignmentConfiguration : IEntityTypeConfiguration<BusAssignment>
{
    public void Configure(EntityTypeBuilder<BusAssignment> builder)
    {
        builder.ToTable("bus_assignments");

        builder.HasKey(ba => ba.Id);
        builder.Property(ba => ba.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(ba => ba.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(ba => ba.BusId).HasColumnName("bus_id").IsRequired();
        builder.Property(ba => ba.DriverId).HasColumnName("driver_id").IsRequired();

        builder.Property(ba => ba.AssignedFrom).HasColumnName("assigned_from").HasColumnType("timestamptz").IsRequired();
        builder.Property(ba => ba.AssignedTo).HasColumnName("assigned_to").HasColumnType("timestamptz");
        builder.Property(ba => ba.IsCurrent).HasColumnName("is_current").HasDefaultValue(true).IsRequired();

        builder.Property(ba => ba.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(ba => ba.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(ba => ba.Bus)
            .WithMany(b => b.Assignments)
            .HasForeignKey(ba => ba.BusId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ba => ba.Driver)
            .WithMany(d => d.BusAssignments)
            .HasForeignKey(ba => ba.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

