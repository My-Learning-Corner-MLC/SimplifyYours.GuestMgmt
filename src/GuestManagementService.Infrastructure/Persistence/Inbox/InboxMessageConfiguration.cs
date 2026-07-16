using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuestManagementService.Infrastructure.Persistence.Inbox;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(message => message.Id)
            .HasName("pk_inbox_messages");

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(message => message.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(message => message.ReceivedAt)
            .HasColumnName("received_at")
            .IsRequired();

        builder.Property(message => message.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired(false);

        builder.Property(message => message.HandleAttempts)
            .HasColumnName("handle_attempts")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(message => message.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(message => message.Error)
            .HasColumnName("error")
            .HasMaxLength(2_000)
            .IsRequired(false);
    }
}
