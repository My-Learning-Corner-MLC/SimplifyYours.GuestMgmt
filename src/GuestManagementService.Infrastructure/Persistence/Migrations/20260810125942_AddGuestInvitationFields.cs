using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestInvitationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "delivery_status",
                table: "guests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotSent");

            // Three steps rather than a single NOT NULL add. EF's default would insert an empty
            // string into every existing row and then build a UNIQUE index over them, which fails
            // outright on two or more rows — and on exactly one row would leave a guest holding an
            // empty, guessable invitation token.
            migrationBuilder.AddColumn<string>(
                name: "invitation_token",
                table: "guests",
                type: "character varying(43)",
                maxLength: 43,
                nullable: true);

            // gen_random_uuid() is built into Postgres 13+ (no pgcrypto needed) and yields 128 bits
            // of randomness — the same strength as the C# generator, in a different encoding.
            // Tokens are opaque, so the two formats coexist happily.
            migrationBuilder.Sql(
                """
                UPDATE guests
                SET invitation_token = replace(gen_random_uuid()::text, '-', '')
                WHERE invitation_token IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "invitation_token",
                table: "guests",
                type: "character varying(43)",
                maxLength: 43,
                nullable: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "responded_at",
                table: "guests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rsvp_status",
                table: "guests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NoResponse");

            migrationBuilder.CreateIndex(
                name: "ix_guests_invitation_token",
                table: "guests",
                column: "invitation_token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_guests_invitation_token",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "delivery_status",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "invitation_token",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "responded_at",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "rsvp_status",
                table: "guests");
        }
    }
}
