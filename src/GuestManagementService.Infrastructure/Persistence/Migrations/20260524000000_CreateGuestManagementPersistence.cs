using GuestManagementService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestManagementService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GuestManagementServiceDbContext))]
[Migration("20260524000000_CreateGuestManagementPersistence")]
public partial class CreateGuestManagementPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "event_references",
            columns: table => new
            {
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                event_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_event_references", x => x.event_id);
            });

        migrationBuilder.CreateTable(
            name: "guests",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                phone_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                normalized_phone_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                email_address = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                normalized_email_address = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_guests", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "inbox_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                handle_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_inbox_messages", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_guests_event_id",
            table: "guests",
            column: "event_id");

        migrationBuilder.CreateIndex(
            name: "ux_guests_event_id_normalized_email_address",
            table: "guests",
            columns: new[] { "event_id", "normalized_email_address" },
            unique: true,
            filter: "normalized_email_address IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ux_guests_event_id_normalized_phone_number",
            table: "guests",
            columns: new[] { "event_id", "normalized_phone_number" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "event_references");
        migrationBuilder.DropTable(name: "guests");
        migrationBuilder.DropTable(name: "inbox_messages");
    }
}
