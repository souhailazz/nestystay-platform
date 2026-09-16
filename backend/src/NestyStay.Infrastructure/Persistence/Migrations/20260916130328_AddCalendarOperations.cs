using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations;

public partial class AddCalendarOperations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "milestone_calendar_export_token",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                property_id = table.Column<Guid>(type: "uuid", nullable: false),
                host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                token_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_milestone_calendar_export_token", x => x.id));

        migrationBuilder.CreateTable(
            name: "milestone_calendar_manual_block",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                property_id = table.Column<Guid>(type: "uuid", nullable: false),
                host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                ends_on = table.Column<DateOnly>(type: "date", nullable: false),
                reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_milestone_calendar_manual_block", x => x.id));

        migrationBuilder.CreateIndex(
            name: "IX_milestone_calendar_export_token_property_id",
            table: "milestone_calendar_export_token",
            column: "property_id",
            unique: true,
            filter: "revoked_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_milestone_calendar_export_token_token_hash",
            table: "milestone_calendar_export_token",
            column: "token_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_milestone_calendar_manual_block_property_id_host_user_id_starts_on_ends_on",
            table: "milestone_calendar_manual_block",
            columns: new[] { "property_id", "host_user_id", "starts_on", "ends_on" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "milestone_calendar_export_token");
        migrationBuilder.DropTable(name: "milestone_calendar_manual_block");
    }
}
