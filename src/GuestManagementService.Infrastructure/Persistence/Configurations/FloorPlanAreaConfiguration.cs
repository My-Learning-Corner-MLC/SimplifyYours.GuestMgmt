using GuestManagementService.Domain.Seating;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuestManagementService.Infrastructure.Persistence.Configurations;

internal sealed class FloorPlanAreaConfiguration : IEntityTypeConfiguration<FloorPlanArea>
{
    public void Configure(EntityTypeBuilder<FloorPlanArea> builder)
    {
        builder.ToTable("floor_plan_areas");

        builder.HasKey(area => area.Id)
            .HasName("pk_floor_plan_areas");

        builder.Property(area => area.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(area => area.SeatingLayoutId)
            .HasColumnName("seating_layout_id")
            .IsRequired();

        builder.Property(area => area.Name)
            .HasColumnName("name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(area => area.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(area => area.Shape)
            .HasColumnName("shape")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(area => area.Width)
            .HasColumnName("width")
            .IsRequired();

        builder.Property(area => area.Height)
            .HasColumnName("height")
            .IsRequired();

        builder.Property(area => area.PositionX)
            .HasColumnName("position_x");

        builder.Property(area => area.PositionY)
            .HasColumnName("position_y");

        builder.Property(area => area.Rotation)
            .HasColumnName("rotation")
            .IsRequired();

        builder.Property(area => area.Color)
            .HasColumnName("color")
            .HasMaxLength(32);

        builder.Property(area => area.Capacity)
            .HasColumnName("capacity");

        builder.Property(area => area.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(area => area.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(area => area.SeatingLayoutId)
            .HasDatabaseName("ix_floor_plan_areas_seating_layout_id");
    }
}
