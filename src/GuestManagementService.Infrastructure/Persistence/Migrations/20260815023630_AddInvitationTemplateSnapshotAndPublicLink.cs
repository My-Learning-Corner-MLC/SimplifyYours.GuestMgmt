using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationTemplateSnapshotAndPublicLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // template_id changes from a string slug (slice 1's test value, e.g. "marigold") to a
            // real catalog UUID. There is no production data yet, so the only rows that could exist
            // are local/test rows carrying the placeholder slug. Postgres has no implicit cast from
            // character varying to uuid — a plain ALTER COLUMN ... TYPE uuid fails outright on any
            // row that is not already a valid UUID string, placeholder or otherwise.
            //
            // Decision: any row whose template_id does not parse as a UUID is nulled out here,
            // rather than dropped or migrated to a real catalog id (there is no mapping from
            // "marigold" to a real template — the whole point of this migration is that the
            // placeholder never had one). A nulled template_id also leaves html_content/etc. null,
            // which the application layer already treats as "not configured" (see B3 in the
            // implementation plan) — never a broken document, never a 500. The organiser simply
            // needs to re-choose a template from the real gallery.
            // The column must allow NULL before any row can be nulled out below — it is still the
            // original NOT NULL varchar column at this point.
            migrationBuilder.Sql(
                """
                ALTER TABLE event_invitation_settings
                ALTER COLUMN template_id DROP NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE event_invitation_settings
                SET template_id = NULL
                WHERE template_id IS NOT NULL
                  AND template_id !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE event_invitation_settings
                ALTER COLUMN template_id TYPE uuid USING template_id::uuid;
                """);

            // The column is already uuid-typed after the raw SQL cast above; this call exists so
            // EF's model snapshot matches, and it also drops the old NOT NULL constraint (a legacy
            // row that got nulled out above must be storable).
            migrationBuilder.AlterColumn<Guid>(
                name: "template_id",
                table: "event_invitation_settings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "css_content",
                table: "event_invitation_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "html_content",
                table: "event_invitation_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "js_content",
                table: "event_invitation_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "preview_expires_at",
                table: "event_invitation_settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preview_token",
                table: "event_invitation_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "public_event_token",
                table: "event_invitation_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "public_link_enabled",
                table: "event_invitation_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "template_version",
                table: "event_invitation_settings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_invitation_settings_preview_token",
                table: "event_invitation_settings",
                column: "preview_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_invitation_settings_public_event_token",
                table: "event_invitation_settings",
                column: "public_event_token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_event_invitation_settings_preview_token",
                table: "event_invitation_settings");

            migrationBuilder.DropIndex(
                name: "ix_event_invitation_settings_public_event_token",
                table: "event_invitation_settings");

            migrationBuilder.DropColumn(
                name: "css_content",
                table: "event_invitation_settings");

            migrationBuilder.DropColumn(
                name: "html_content",
                table: "event_invitation_settings");

            migrationBuilder.DropColumn(
                name: "js_content",
                table: "event_invitation_settings");

            migrationBuilder.DropColumn(
                name: "preview_expires_at",
                table: "event_invitation_settings");

            migrationBuilder.DropColumn(
                name: "preview_token",
                table: "event_invitation_settings");

            migrationBuilder.DropColumn(
                name: "public_event_token",
                table: "event_invitation_settings");

            migrationBuilder.DropColumn(
                name: "public_link_enabled",
                table: "event_invitation_settings");

            migrationBuilder.DropColumn(
                name: "template_version",
                table: "event_invitation_settings");

            // Cast back to text explicitly (Postgres has no implicit uuid -> varchar cast either).
            // Left nullable, unlike the original NOT NULL column: rows nulled out by this
            // migration's Up() have no string value to restore, and forcing NOT NULL back on here
            // would fail outright if any such row exists. Rolling back is a schema-compatibility
            // safety valve, not a promise to reconstruct data the Up() migration discarded.
            migrationBuilder.Sql(
                """
                ALTER TABLE event_invitation_settings
                ALTER COLUMN template_id TYPE character varying(100) USING template_id::text;
                """);
        }
    }
}
