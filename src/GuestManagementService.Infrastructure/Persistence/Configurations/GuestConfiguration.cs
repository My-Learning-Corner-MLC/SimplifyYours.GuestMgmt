using GuestManagementService.Domain.Guests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuestManagementService.Infrastructure.Persistence.Configurations;

internal sealed class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> builder)
    {
        builder.ToTable("guests");

        builder.HasKey(guest => guest.Id);

        builder.Property(guest => guest.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(guest => guest.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(guest => guest.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(guest => guest.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(guest => guest.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(guest => guest.NormalizedPhoneNumber)
            .HasColumnName("normalized_phone_number")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(guest => guest.EmailAddress)
            .HasColumnName("email_address")
            .HasMaxLength(254);

        builder.Property(guest => guest.NormalizedEmailAddress)
            .HasColumnName("normalized_email_address")
            .HasMaxLength(254);

        builder.Property(guest => guest.Gender)
            .HasColumnName("gender")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(guest => guest.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(guest => guest.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(guest => guest.EventId)
            .HasDatabaseName("ix_guests_event_id");

        builder.HasIndex(guest => new { guest.EventId, guest.NormalizedPhoneNumber })
            .IsUnique()
            .HasDatabaseName("ux_guests_event_id_normalized_phone_number");

        builder.HasIndex(guest => new { guest.EventId, guest.NormalizedEmailAddress })
            .IsUnique()
            .HasFilter("normalized_email_address IS NOT NULL")
            .HasDatabaseName("ux_guests_event_id_normalized_email_address");
    }
}
