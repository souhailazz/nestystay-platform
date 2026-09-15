using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWellnessOperationalWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "milestone_wellness_follow_up_task",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    priority = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    assignee_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_wellness_follow_up_task", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_wellness_officer_document",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    officer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    safe_file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    upload_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    storage_provider_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    scan_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    sha256_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    expires_on = table.Column<DateOnly>(type: "date", nullable: true),
                    review_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    review_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    upload_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_wellness_officer_document", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_wellness_payout_dispute",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    officer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    evidence_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: true),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    decision = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    decision_notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_wellness_payout_dispute", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_wellness_report_acknowledgement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acknowledged_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_wellness_report_acknowledgement", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_wellness_report_comment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_wellness_report_comment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_wellness_report_template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    definition_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_wellness_report_template", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_wellness_follow_up_task");

            migrationBuilder.DropTable(
                name: "milestone_wellness_officer_document");

            migrationBuilder.DropTable(
                name: "milestone_wellness_payout_dispute");

            migrationBuilder.DropTable(
                name: "milestone_wellness_report_acknowledgement");

            migrationBuilder.DropTable(
                name: "milestone_wellness_report_comment");

            migrationBuilder.DropTable(
                name: "milestone_wellness_report_template");
        }
    }
}
