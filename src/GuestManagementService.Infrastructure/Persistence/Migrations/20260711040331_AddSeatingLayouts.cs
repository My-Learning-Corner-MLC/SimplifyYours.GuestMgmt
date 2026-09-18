using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatingLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "seating_layouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seating_layouts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "seating_tables",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seating_layout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    shape = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    seat_count = table.Column<int>(type: "integer", nullable: false),
                    is_full = table.Column<bool>(type: "boolean", nullable: false),
                    position_x = table.Column<double>(type: "double precision", nullable: true),
                    position_y = table.Column<double>(type: "double precision", nullable: true),
                    rotation = table.Column<double>(type: "double precision", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seating_tables", x => x.id);
                    table.ForeignKey(
                        name: "fk_seating_tables_seating_layout_id",
                        column: x => x.seating_layout_id,
                        principalTable: "seating_layouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_seating_layouts_event_id",
                table: "seating_layouts",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ux_seating_layouts_tenant_id_event_id",
                table: "seating_layouts",
                columns: new[] { "tenant_id", "event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_seating_tables_seating_layout_id",
                table: "seating_tables",
                column: "seating_layout_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seating_tables");

            migrationBuilder.DropTable(
                name: "seating_layouts");
        }
    }
}
