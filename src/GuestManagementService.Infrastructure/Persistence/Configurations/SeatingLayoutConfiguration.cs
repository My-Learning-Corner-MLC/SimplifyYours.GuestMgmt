using GuestManagementService.Domain.Seating;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuestManagementService.Infrastructure.Persistence.Configurations;

internal sealed class SeatingLayoutConfiguration : IEntityTypeConfiguration<SeatingLayout>
{
    public void Configure(EntityTypeBuilder<SeatingLayout> builder)
    {
        builder.ToTable("seating_layouts");

        builder.HasKey(layout => layout.Id)
            .HasName("pk_seating_layouts");

        builder.Property(layout => layout.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(layout => layout.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(layout => layout.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(layout => layout.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(layout => layout.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // One layout per event within a tenant.
        builder.HasIndex(layout => new { layout.TenantId, layout.EventId })
            .IsUnique()
            .HasDatabaseName("ux_seating_layouts_tenant_id_event_id");

        builder.HasIndex(layout => layout.EventId)
            .HasDatabaseName("ix_seating_layouts_event_id");

        builder.HasMany(layout => layout.Tables)
            .WithOne()
            .HasForeignKey(table => table.SeatingLayoutId)
            .HasConstraintName("fk_seating_tables_seating_layout_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(SeatingLayout.Tables))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(layout => layout.Assignments)
            .WithOne()
            .HasForeignKey("SeatingLayoutId")
            .HasConstraintName("fk_seat_assignments_seating_layout_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(SeatingLayout.Assignments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
