using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyManagerOperationsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_gate_message_manager_user_id",
                table: "milestone_manager_gate_message");

            migrationBuilder.AddColumn<string>(
                name: "availability_json",
                table: "milestone_manager_vendor",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<int>(
                name: "completed_job_count",
                table: "milestone_manager_vendor",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_preferred",
                table: "milestone_manager_vendor",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_suspended",
                table: "milestone_manager_vendor",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "rate",
                table: "milestone_manager_vendor",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rating",
                table: "milestone_manager_vendor",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "service_areas_json",
                table: "milestone_manager_vendor",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<decimal>(
                name: "spend_total",
                table: "milestone_manager_vendor",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closed_at",
                table: "milestone_manager_proposal",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "result_proof_hash",
                table: "milestone_manager_proposal",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "attempt_number",
                table: "milestone_manager_payment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "reconciliation_reference",
                table: "milestone_manager_payment",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reconciliation_status",
                table: "milestone_manager_payment",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "PENDING");

            migrationBuilder.AddColumn<string>(
                name: "refund_reason",
                table: "milestone_manager_payment",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "refunded_amount",
                table: "milestone_manager_payment",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "refunded_at",
                table: "milestone_manager_payment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "saved_payment_method_reference",
                table: "milestone_manager_payment",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "acknowledgement_due_at",
                table: "milestone_manager_notice",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "audience_owner_ids_json",
                table: "milestone_manager_notice",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "audience_roles_json",
                table: "milestone_manager_notice",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "milestone_manager_notice",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closed_at",
                table: "milestone_manager_maintenance",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "sla_due_at",
                table: "milestone_manager_maintenance",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "milestone_manager_gate_message",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "current_version",
                table: "milestone_manager_document",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Existing messages predate client idempotency keys. Give each its own
            // stable namespace before installing the manager/key unique index.
            migrationBuilder.Sql("UPDATE milestone_manager_gate_message SET idempotency_key = 'legacy:' || id::text WHERE idempotency_key = '';");

            migrationBuilder.AddColumn<DateOnly>(
                name: "expires_on",
                table: "milestone_manager_document",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "milestone_cleaning_task",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_staff_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_cleaning_task", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_inspection",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    checklist_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    issues_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_inspection", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_management_agreement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    fee_rule_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    maintenance_approval_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    expense_approval_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    signed_document_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_management_agreement", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_management_fee_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rule_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fixed_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cleaning_markup = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    maintenance_markup = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_management_fee_rule", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_calendar_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    source_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_calendar_event", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_dashboard_preference",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kpi_order_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    visible_kpis_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    saved_filters_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    saved_views_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_dashboard_preference", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_document_access_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_document_access_event", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_document_version",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_document_version", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_gate_delivery_attempt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gate_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_gate_delivery_attempt", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_invitation_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_invitation_event", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_maintenance_activity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    details = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_maintenance_activity", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_maintenance_attachment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_maintenance_attachment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_meter_reading",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    utility_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    billing_period = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    previous_reading = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    current_reading = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    usage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    photo_object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    bill_object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    reading_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_anomaly = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_meter_reading", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_notice_acknowledgement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    notice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_notice_acknowledgement", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_notice_comment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    notice_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_milestone_manager_notice_comment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_owner_verification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    document_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_owner_verification", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_payment_attempt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    provider_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_payment_attempt", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_payment_method",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    brand = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    last4 = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    exp_month = table.Column<int>(type: "integer", nullable: false),
                    exp_year = table.Column<int>(type: "integer", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_payment_method", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_property_assignment_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    new_owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_property_assignment_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_proposal_attachment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_proposal_attachment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_proposal_discussion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_moderated = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_proposal_discussion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_staff",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    property_scope_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    owner_scope_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    can_manage_finance = table.Column<bool>(type: "boolean", nullable: false),
                    approval_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_staff", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_subscription_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    from_tier = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    to_tier = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    effective_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_subscription_event", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_utility_dispute",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    utility_charge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    evidence_object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    decision = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    adjustment_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_utility_dispute", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_utility_schedule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    utility_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    day_of_month = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_utility_schedule", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_manager_vendor_document",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    expires_on = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_vendor_document", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_owner_approval",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    decision_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_owner_approval", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_owner_payout",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_from = table.Column<DateOnly>(type: "date", nullable: false),
                    period_to = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_owner_payout", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_work_order",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    work_order_number = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    scope = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    quote_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    approved_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    labor_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    parts_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sla_due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_work_order", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_gate_message_manager_user_id_idempotency_~",
                table: "milestone_manager_gate_message",
                columns: new[] { "manager_user_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_cleaning_task_manager_user_id_property_id_due_at",
                table: "milestone_cleaning_task",
                columns: new[] { "manager_user_id", "property_id", "due_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_inspection_manager_user_id_property_id_created_at",
                table: "milestone_inspection",
                columns: new[] { "manager_user_id", "property_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_management_agreement_manager_user_id_owner_user_i~",
                table: "milestone_management_agreement",
                columns: new[] { "manager_user_id", "owner_user_id", "property_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_management_fee_rule_manager_user_id_property_id_e~",
                table: "milestone_management_fee_rule",
                columns: new[] { "manager_user_id", "property_id", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_calendar_event_manager_user_id_starts_at_~",
                table: "milestone_manager_calendar_event",
                columns: new[] { "manager_user_id", "starts_at", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_dashboard_preference_manager_user_id",
                table: "milestone_manager_dashboard_preference",
                column: "manager_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_document_access_event_document_id_created~",
                table: "milestone_manager_document_access_event",
                columns: new[] { "document_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_document_version_document_id_version",
                table: "milestone_manager_document_version",
                columns: new[] { "document_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_gate_delivery_attempt_gate_message_id_att~",
                table: "milestone_manager_gate_delivery_attempt",
                columns: new[] { "gate_message_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_invitation_event_manager_user_id_owner_us~",
                table: "milestone_manager_invitation_event",
                columns: new[] { "manager_user_id", "owner_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_maintenance_activity_maintenance_id_creat~",
                table: "milestone_manager_maintenance_activity",
                columns: new[] { "maintenance_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_maintenance_attachment_maintenance_id_cre~",
                table: "milestone_manager_maintenance_attachment",
                columns: new[] { "maintenance_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_meter_reading_manager_user_id_property_id~",
                table: "milestone_manager_meter_reading",
                columns: new[] { "manager_user_id", "property_id", "utility_type", "billing_period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_notice_acknowledgement_notice_id_owner_us~",
                table: "milestone_manager_notice_acknowledgement",
                columns: new[] { "notice_id", "owner_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_notice_comment_notice_id_created_at",
                table: "milestone_manager_notice_comment",
                columns: new[] { "notice_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_owner_verification_manager_user_id_owner_~",
                table: "milestone_manager_owner_verification",
                columns: new[] { "manager_user_id", "owner_user_id", "requirement" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_payment_attempt_payment_id_attempt_number",
                table: "milestone_manager_payment_attempt",
                columns: new[] { "payment_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_payment_method_manager_user_id_owner_user~",
                table: "milestone_manager_payment_method",
                columns: new[] { "manager_user_id", "owner_user_id", "provider_reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_property_assignment_history_manager_user_~",
                table: "milestone_manager_property_assignment_history",
                columns: new[] { "manager_user_id", "property_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_proposal_attachment_proposal_id",
                table: "milestone_manager_proposal_attachment",
                column: "proposal_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_proposal_discussion_proposal_id_created_at",
                table: "milestone_manager_proposal_discussion",
                columns: new[] { "proposal_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_staff_manager_user_id_staff_user_id",
                table: "milestone_manager_staff",
                columns: new[] { "manager_user_id", "staff_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_subscription_event_manager_user_id_effect~",
                table: "milestone_manager_subscription_event",
                columns: new[] { "manager_user_id", "effective_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_utility_dispute_manager_user_id_status",
                table: "milestone_manager_utility_dispute",
                columns: new[] { "manager_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_utility_schedule_manager_user_id_property~",
                table: "milestone_manager_utility_schedule",
                columns: new[] { "manager_user_id", "property_id", "utility_type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_owner_approval_manager_user_id_owner_user_id_stat~",
                table: "milestone_owner_approval",
                columns: new[] { "manager_user_id", "owner_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_owner_payout_manager_user_id_owner_user_id_period~",
                table: "milestone_owner_payout",
                columns: new[] { "manager_user_id", "owner_user_id", "period_from", "period_to" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_work_order_manager_user_id_work_order_number",
                table: "milestone_work_order",
                columns: new[] { "manager_user_id", "work_order_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_cleaning_task");

            migrationBuilder.DropTable(
                name: "milestone_inspection");

            migrationBuilder.DropTable(
                name: "milestone_management_agreement");

            migrationBuilder.DropTable(
                name: "milestone_management_fee_rule");

            migrationBuilder.DropTable(
                name: "milestone_manager_calendar_event");

            migrationBuilder.DropTable(
                name: "milestone_manager_dashboard_preference");

            migrationBuilder.DropTable(
                name: "milestone_manager_document_access_event");

            migrationBuilder.DropTable(
                name: "milestone_manager_document_version");

            migrationBuilder.DropTable(
                name: "milestone_manager_gate_delivery_attempt");

            migrationBuilder.DropTable(
                name: "milestone_manager_invitation_event");

            migrationBuilder.DropTable(
                name: "milestone_manager_maintenance_activity");

            migrationBuilder.DropTable(
                name: "milestone_manager_maintenance_attachment");

            migrationBuilder.DropTable(
                name: "milestone_manager_meter_reading");

            migrationBuilder.DropTable(
                name: "milestone_manager_notice_acknowledgement");

            migrationBuilder.DropTable(
                name: "milestone_manager_notice_comment");

            migrationBuilder.DropTable(
                name: "milestone_manager_owner_verification");

            migrationBuilder.DropTable(
                name: "milestone_manager_payment_attempt");

            migrationBuilder.DropTable(
                name: "milestone_manager_payment_method");

            migrationBuilder.DropTable(
                name: "milestone_manager_property_assignment_history");

            migrationBuilder.DropTable(
                name: "milestone_manager_proposal_attachment");

            migrationBuilder.DropTable(
                name: "milestone_manager_proposal_discussion");

            migrationBuilder.DropTable(
                name: "milestone_manager_staff");

            migrationBuilder.DropTable(
                name: "milestone_manager_subscription_event");

            migrationBuilder.DropTable(
                name: "milestone_manager_utility_dispute");

            migrationBuilder.DropTable(
                name: "milestone_manager_utility_schedule");

            migrationBuilder.DropTable(
                name: "milestone_manager_vendor_document");

            migrationBuilder.DropTable(
                name: "milestone_owner_approval");

            migrationBuilder.DropTable(
                name: "milestone_owner_payout");

            migrationBuilder.DropTable(
                name: "milestone_work_order");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_gate_message_manager_user_id_idempotency_~",
                table: "milestone_manager_gate_message");

            migrationBuilder.DropColumn(
                name: "availability_json",
                table: "milestone_manager_vendor");

            migrationBuilder.DropColumn(
                name: "completed_job_count",
                table: "milestone_manager_vendor");

            migrationBuilder.DropColumn(
                name: "is_preferred",
                table: "milestone_manager_vendor");

            migrationBuilder.DropColumn(
                name: "is_suspended",
                table: "milestone_manager_vendor");

            migrationBuilder.DropColumn(
                name: "rate",
                table: "milestone_manager_vendor");

            migrationBuilder.DropColumn(
                name: "rating",
                table: "milestone_manager_vendor");

            migrationBuilder.DropColumn(
                name: "service_areas_json",
                table: "milestone_manager_vendor");

            migrationBuilder.DropColumn(
                name: "spend_total",
                table: "milestone_manager_vendor");

            migrationBuilder.DropColumn(
                name: "closed_at",
                table: "milestone_manager_proposal");

            migrationBuilder.DropColumn(
                name: "result_proof_hash",
                table: "milestone_manager_proposal");

            migrationBuilder.DropColumn(
                name: "attempt_number",
                table: "milestone_manager_payment");

            migrationBuilder.DropColumn(
                name: "reconciliation_reference",
                table: "milestone_manager_payment");

            migrationBuilder.DropColumn(
                name: "reconciliation_status",
                table: "milestone_manager_payment");

            migrationBuilder.DropColumn(
                name: "refund_reason",
                table: "milestone_manager_payment");

            migrationBuilder.DropColumn(
                name: "refunded_amount",
                table: "milestone_manager_payment");

            migrationBuilder.DropColumn(
                name: "refunded_at",
                table: "milestone_manager_payment");

            migrationBuilder.DropColumn(
                name: "saved_payment_method_reference",
                table: "milestone_manager_payment");

            migrationBuilder.DropColumn(
                name: "acknowledgement_due_at",
                table: "milestone_manager_notice");

            migrationBuilder.DropColumn(
                name: "audience_owner_ids_json",
                table: "milestone_manager_notice");

            migrationBuilder.DropColumn(
                name: "audience_roles_json",
                table: "milestone_manager_notice");

            migrationBuilder.DropColumn(
                name: "category",
                table: "milestone_manager_notice");

            migrationBuilder.DropColumn(
                name: "closed_at",
                table: "milestone_manager_maintenance");

            migrationBuilder.DropColumn(
                name: "sla_due_at",
                table: "milestone_manager_maintenance");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "milestone_manager_gate_message");

            migrationBuilder.DropColumn(
                name: "current_version",
                table: "milestone_manager_document");

            migrationBuilder.DropColumn(
                name: "expires_on",
                table: "milestone_manager_document");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_gate_message_manager_user_id",
                table: "milestone_manager_gate_message",
                column: "manager_user_id");
        }
    }
}
