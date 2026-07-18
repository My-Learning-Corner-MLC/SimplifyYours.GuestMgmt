using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestManagementService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "tags",
                table: "guests",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tags",
                table: "guests");
        }
    }
}
