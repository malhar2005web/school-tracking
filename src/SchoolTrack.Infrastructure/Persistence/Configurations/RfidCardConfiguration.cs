using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Infrastructure.Persistence.Configurations;

public sealed class RfidCardConfiguration : IEntityTypeConfiguration<RfidCard>
{
    public void Configure(EntityTypeBuilder<RfidCard> builder)
    {
        builder.ToTable("rfid_cards");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(c => c.StudentId).HasColumnName("student_id");

        builder.Property(c => c.CardUid).HasColumnName("card_uid").HasMaxLength(50).IsRequired();
        builder.Property(c => c.CardType).HasColumnName("card_type").HasMaxLength(50).HasDefaultValue("Permanent").IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("Active").IsRequired();
        builder.Property(c => c.IssuedAt).HasColumnName("issued_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(c => c.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(c => new { c.SchoolId, c.CardUid })
            .IsUnique()
            .HasDatabaseName("ix_rfid_cards_school_card_uid");

        builder.HasOne(c => c.Student)
            .WithOne(s => s.AssignedRfidCard)
            .HasForeignKey<RfidCard>(c => c.StudentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

