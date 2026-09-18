using GuestManagementService.Domain.Seating;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuestManagementService.Infrastructure.Persistence.Configurations;

internal sealed class SeatingTableConfiguration : IEntityTypeConfiguration<SeatingTable>
{
    public void Configure(EntityTypeBuilder<SeatingTable> builder)
    {
        builder.ToTable("seating_tables");

        builder.HasKey(table => table.Id)
            .HasName("pk_seating_tables");

        builder.Property(table => table.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(table => table.SeatingLayoutId)
            .HasColumnName("seating_layout_id")
            .IsRequired();

        builder.Property(table => table.Name)
            .HasColumnName("name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(table => table.Shape)
            .HasColumnName("shape")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(table => table.SeatCount)
            .HasColumnName("seat_count")
            .IsRequired();

        builder.Property(table => table.IsFull)
            .HasColumnName("is_full")
            .IsRequired();

        builder.Property(table => table.PositionX)
            .HasColumnName("position_x");

        builder.Property(table => table.PositionY)
            .HasColumnName("position_y");

        builder.Property(table => table.Rotation)
            .HasColumnName("rotation")
            .IsRequired();

        builder.Property(table => table.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(table => table.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(table => table.SeatingLayoutId)
            .HasDatabaseName("ix_seating_tables_seating_layout_id");
    }
}
