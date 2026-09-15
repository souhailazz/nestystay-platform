using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM5ReleaseOperationalControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cost_breakdown_json",
                table: "milestone_pm_maintenance_case",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<Guid>(
                name: "financial_journal_id",
                table: "milestone_pm_maintenance_case",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "financially_posted",
                table: "milestone_pm_maintenance_case",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "selected_quote_id",
                table: "milestone_pm_maintenance_case",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "template_name",
                table: "milestone_pm_cleaning_readiness",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "DEFAULT");

            migrationBuilder.AddColumn<int>(
                name: "template_version",
                table: "milestone_pm_cleaning_readiness",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "rental_listing_id",
                table: "milestone_manager_property",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "rental_listing_linked_at",
                table: "milestone_manager_property",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "milestone_pm_reservation_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    from_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    to_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_pm_reservation_event", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_property_rental_listing_id",
                table: "milestone_manager_property",
                column: "rental_listing_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_reservation_event_manager_user_id_booking_id_c~",
                table: "milestone_pm_reservation_event",
                columns: new[] { "manager_user_id", "booking_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_pm_reservation_event");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_property_rental_listing_id",
                table: "milestone_manager_property");

            migrationBuilder.DropColumn(
                name: "cost_breakdown_json",
                table: "milestone_pm_maintenance_case");

            migrationBuilder.DropColumn(
                name: "financial_journal_id",
                table: "milestone_pm_maintenance_case");

            migrationBuilder.DropColumn(
                name: "financially_posted",
                table: "milestone_pm_maintenance_case");

            migrationBuilder.DropColumn(
                name: "selected_quote_id",
                table: "milestone_pm_maintenance_case");

            migrationBuilder.DropColumn(
                name: "template_name",
                table: "milestone_pm_cleaning_readiness");

            migrationBuilder.DropColumn(
                name: "template_version",
                table: "milestone_pm_cleaning_readiness");

            migrationBuilder.DropColumn(
                name: "rental_listing_id",
                table: "milestone_manager_property");

            migrationBuilder.DropColumn(
                name: "rental_listing_linked_at",
                table: "milestone_manager_property");
        }
    }
}
