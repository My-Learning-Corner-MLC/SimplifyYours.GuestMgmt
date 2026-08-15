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

        // UUID, not the string slug slice 1 used as a placeholder. See the B1 migration for how
        // existing non-guid rows are handled.
        builder.Property(settings => settings.TemplateId)
            .HasColumnName("template_id");

        builder.Property(settings => settings.TemplateVersion)
            .HasColumnName("template_version");

        // Snapshotted once from template-management-service on save — see EventInvitationSettings'
        // remarks. Plain text: this exact content is served unauthenticated to anyone holding a
        // link, so it is published content, not a secret, and Postgres already TOASTs/compresses it.
        builder.Property(settings => settings.HtmlContent)
            .HasColumnName("html_content");

        builder.Property(settings => settings.CssContent)
            .HasColumnName("css_content");

        builder.Property(settings => settings.JsContent)
            .HasColumnName("js_content");

        builder.Property(settings => settings.PublicLinkEnabled)
            .HasColumnName("public_link_enabled")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(settings => settings.PublicEventToken)
            .HasColumnName("public_event_token");

        builder.HasIndex(settings => settings.PublicEventToken)
            .IsUnique()
            .HasDatabaseName("ix_event_invitation_settings_public_event_token");

        builder.Property(settings => settings.PreviewToken)
            .HasColumnName("preview_token");

        builder.HasIndex(settings => settings.PreviewToken)
            .IsUnique()
            .HasDatabaseName("ix_event_invitation_settings_preview_token");

        builder.Property(settings => settings.PreviewExpiresAt)
            .HasColumnName("preview_expires_at");

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
