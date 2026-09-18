using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatingPartyReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "guest_id",
                table: "seat_assignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "party_owner_guest_id",
                table: "seat_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Backfill existing rows: every seat assignment created before this migration is a
            // guest's own seat, so party_owner_guest_id is just guest_id. Must run before the
            // foreign key below, since the column's default (all-zeros) wouldn't satisfy it.
            migrationBuilder.Sql("UPDATE seat_assignments SET party_owner_guest_id = guest_id;");

            migrationBuilder.CreateIndex(
                name: "IX_seat_assignments_party_owner_guest_id",
                table: "seat_assignments",
                column: "party_owner_guest_id");

            migrationBuilder.AddForeignKey(
                name: "fk_seat_assignments_party_owner_guest_id",
                table: "seat_assignments",
                column: "party_owner_guest_id",
                principalTable: "guests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_seat_assignments_party_owner_guest_id",
                table: "seat_assignments");

            migrationBuilder.DropIndex(
                name: "IX_seat_assignments_party_owner_guest_id",
                table: "seat_assignments");

            migrationBuilder.DropColumn(
                name: "party_owner_guest_id",
                table: "seat_assignments");

            // Rows reserved for a party's accompanying attendees (guest_id IS NULL) only
            // exist because of this migration's feature — restoring NOT NULL below would
            // reject them, so they're discarded along with the column that created them.
            migrationBuilder.Sql("DELETE FROM seat_assignments WHERE guest_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "guest_id",
                table: "seat_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
