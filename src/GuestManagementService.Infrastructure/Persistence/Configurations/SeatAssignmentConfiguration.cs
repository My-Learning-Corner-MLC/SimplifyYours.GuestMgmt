using GuestManagementService.Domain.Guests;
using GuestManagementService.Domain.Seating;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuestManagementService.Infrastructure.Persistence.Configurations;

internal sealed class SeatAssignmentConfiguration : IEntityTypeConfiguration<SeatAssignment>
{
    public void Configure(EntityTypeBuilder<SeatAssignment> builder)
    {
        builder.ToTable("seat_assignments");

        builder.HasKey(assignment => assignment.Id)
            .HasName("pk_seat_assignments");

        builder.Property(assignment => assignment.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(assignment => assignment.SeatingTableId)
            .HasColumnName("seating_table_id")
            .IsRequired();

        builder.Property(assignment => assignment.GuestId)
            .HasColumnName("guest_id")
            .IsRequired();

        builder.Property(assignment => assignment.SeatIndex)
            .HasColumnName("seat_index")
            .IsRequired();

        builder.Property(assignment => assignment.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // SeatingLayoutId is not exposed on the domain entity (assignments are only ever
        // reached through the SeatingLayout aggregate root), so it's persisted as a shadow
        // FK. Declared here; SeatingLayoutConfiguration wires the collection navigation to it.
        builder.Property<Guid>("SeatingLayoutId")
            .HasColumnName("seating_layout_id")
            .IsRequired();

        // One occupant per seat.
        builder.HasIndex(assignment => new { assignment.SeatingTableId, assignment.SeatIndex })
            .IsUnique()
            .HasDatabaseName("ux_seat_assignments_table_id_seat_index");

        // One seat per guest within a layout.
        builder.HasIndex("SeatingLayoutId", nameof(SeatAssignment.GuestId))
            .IsUnique()
            .HasDatabaseName("ux_seat_assignments_layout_id_guest_id");

        builder.HasOne<Guest>()
            .WithMany()
            .HasForeignKey(assignment => assignment.GuestId)
            .HasConstraintName("fk_seat_assignments_guest_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<SeatingTable>()
            .WithMany()
            .HasForeignKey(assignment => assignment.SeatingTableId)
            .HasConstraintName("fk_seat_assignments_seating_table_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
