using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePreviewTokenFromInvitationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_event_invitation_settings_preview_token",
                table: "event_invitation_settings");

            migrationBuilder.DropColumn(
                name: "preview_expires_at",
                table: "event_invitation_settings");

            migrationBuilder.DropColumn(
                name: "preview_token",
                table: "event_invitation_settings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "ix_event_invitation_settings_preview_token",
                table: "event_invitation_settings",
                column: "preview_token",
                unique: true);
        }
    }
}
