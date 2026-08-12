using GuestManagementService.Domain.Invitations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuestManagementService.Infrastructure.Persistence.Configurations;

internal sealed class EventInvitationSettingsConfiguration : IEntityTypeConfiguration<EventInvitationSettings>
{
    public void Configure(EntityTypeBuilder<EventInvitationSettings> builder)
    {
        builder.ToTable("event_invitation_settings");

        builder.HasKey(settings => settings.EventId)
            .HasName("pk_event_invitation_settings");

        builder.Property(settings => settings.EventId)
            .HasColumnName("event_id")
            .ValueGeneratedNever();

        builder.Property(settings => settings.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(settings => settings.TemplateId)
            .HasColumnName("template_id")
            .HasMaxLength(100)
            .IsRequired();

        // jsonb, matching how guests.metadata is stored: the field set is template- and
        // event-type dependent, so columns would mean a migration per template.
        builder.Property(settings => settings.FieldValues)
            .HasColumnName("field_values")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(settings => settings.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(settings => settings.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
