using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "seat_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seating_table_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seat_index = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    seating_layout_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seat_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_seat_assignments_guest_id",
                        column: x => x.guest_id,
                        principalTable: "guests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_seat_assignments_seating_layout_id",
                        column: x => x.seating_layout_id,
                        principalTable: "seating_layouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_seat_assignments_seating_table_id",
                        column: x => x.seating_table_id,
                        principalTable: "seating_tables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_seat_assignments_guest_id",
                table: "seat_assignments",
                column: "guest_id");

            migrationBuilder.CreateIndex(
                name: "ux_seat_assignments_layout_id_guest_id",
                table: "seat_assignments",
                columns: new[] { "seating_layout_id", "guest_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_seat_assignments_table_id_seat_index",
                table: "seat_assignments",
                columns: new[] { "seating_table_id", "seat_index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seat_assignments");
        }
    }
}
