using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFloorPlanAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "floor_plan_areas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seating_layout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    shape = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    width = table.Column<double>(type: "double precision", nullable: false),
                    height = table.Column<double>(type: "double precision", nullable: false),
                    position_x = table.Column<double>(type: "double precision", nullable: true),
                    position_y = table.Column<double>(type: "double precision", nullable: true),
                    rotation = table.Column<double>(type: "double precision", nullable: false),
                    color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_floor_plan_areas", x => x.id);
                    table.ForeignKey(
                        name: "fk_floor_plan_areas_seating_layout_id",
                        column: x => x.seating_layout_id,
                        principalTable: "seating_layouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_floor_plan_areas_seating_layout_id",
                table: "floor_plan_areas",
                column: "seating_layout_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "floor_plan_areas");
        }
    }
}
