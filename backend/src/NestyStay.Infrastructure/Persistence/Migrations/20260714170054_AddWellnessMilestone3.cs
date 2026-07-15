using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWellnessMilestone3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "milestone_wellness_officer",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    badge_number = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    parish = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    coverage_area = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_active_off_duty = table.Column<bool>(type: "boolean", nullable: false),
                    is_retired = table.Column<bool>(type: "boolean", nullable: false),
                    verification_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    onboarding_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    availability_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    verification_metadata_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    admin_review_metadata_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    free_badges_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    notification_events_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_wellness_officer", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_wellness_payout",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    officer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    platform_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    officer_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    eligible_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    provider_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ledger_notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_wellness_payout", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_wellness_report",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    officer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    report_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    photos_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    location_metadata_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_wellness_report", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_wellness_visit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    officer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    officer_badge_number = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    parish = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    area = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    visit_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    platform_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    officer_payout_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    visit_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    report_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payment_provider = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payment_authorization_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payment_client_secret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payment_capture_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    timeline_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    notification_events_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_wellness_visit", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_wellness_officer_badge_number",
                table: "milestone_wellness_officer",
                column: "badge_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_wellness_officer_parish_verification_status_avail~",
                table: "milestone_wellness_officer",
                columns: new[] { "parish", "verification_status", "availability_status" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_wellness_payout_status",
                table: "milestone_wellness_payout",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_wellness_payout_visit_id",
                table: "milestone_wellness_payout",
                column: "visit_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_wellness_report_visit_id",
                table: "milestone_wellness_report",
                column: "visit_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_wellness_visit_officer_id_scheduled_at",
                table: "milestone_wellness_visit",
                columns: new[] { "officer_id", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_wellness_visit_payment_status",
                table: "milestone_wellness_visit",
                column: "payment_status");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_wellness_visit_property_id_scheduled_at",
                table: "milestone_wellness_visit",
                columns: new[] { "property_id", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_wellness_visit_visit_status",
                table: "milestone_wellness_visit",
                column: "visit_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_wellness_officer");

            migrationBuilder.DropTable(
                name: "milestone_wellness_payout");

            migrationBuilder.DropTable(
                name: "milestone_wellness_report");

            migrationBuilder.DropTable(
                name: "milestone_wellness_visit");
        }
    }
}
