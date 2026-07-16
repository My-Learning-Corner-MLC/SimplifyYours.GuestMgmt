using GuestManagementService.Domain.EventReferences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuestManagementService.Infrastructure.Persistence.Configurations;

internal sealed class EventReferenceConfiguration : IEntityTypeConfiguration<EventReference>
{
    public void Configure(EntityTypeBuilder<EventReference> builder)
    {
        builder.ToTable("event_references");

        builder.HasKey(reference => reference.EventId)
            .HasName("pk_event_references");

        builder.Property(reference => reference.EventId)
            .HasColumnName("event_id")
            .ValueGeneratedNever();

        builder.Property(reference => reference.EventName)
            .HasColumnName("event_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(reference => reference.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(reference => reference.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(reference => reference.LastSyncedAt)
            .HasColumnName("last_synced_at")
            .IsRequired();

        builder.Property(reference => reference.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(50)
            .IsRequired();
    }
}
