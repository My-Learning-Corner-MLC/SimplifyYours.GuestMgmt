using System;
using GuestManagementService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestManagementService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GuestManagementServiceDbContext))]
[Migration("20260609000000_AddGuestManagementTenantId")]
public partial class AddGuestManagementTenantId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "tenant_id",
            table: "event_references",
            type: "uuid",
            nullable: false);

        migrationBuilder.AddColumn<Guid>(
            name: "tenant_id",
            table: "guests",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "ix_guests_tenant_id_event_id",
            table: "guests",
            columns: new[] { "tenant_id", "event_id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_guests_tenant_id_event_id",
            table: "guests");

        migrationBuilder.DropColumn(
            name: "tenant_id",
            table: "guests");

        migrationBuilder.DropColumn(
            name: "tenant_id",
            table: "event_references");
    }
}
