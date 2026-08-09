using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventReferenceDisplayFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "event_date",
                table: "event_references",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_description",
                table: "event_references",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "event_end_time",
                table: "event_references",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "event_start_time",
                table: "event_references",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "time_zone_id",
                table: "event_references",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "venue_address",
                table: "event_references",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "venue_name",
                table: "event_references",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "venue_notes",
                table: "event_references",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "event_date",
                table: "event_references");

            migrationBuilder.DropColumn(
                name: "event_description",
                table: "event_references");

            migrationBuilder.DropColumn(
                name: "event_end_time",
                table: "event_references");

            migrationBuilder.DropColumn(
                name: "event_start_time",
                table: "event_references");

            migrationBuilder.DropColumn(
                name: "time_zone_id",
                table: "event_references");

            migrationBuilder.DropColumn(
                name: "venue_address",
                table: "event_references");

            migrationBuilder.DropColumn(
                name: "venue_name",
                table: "event_references");

            migrationBuilder.DropColumn(
                name: "venue_notes",
                table: "event_references");
        }
    }
}
