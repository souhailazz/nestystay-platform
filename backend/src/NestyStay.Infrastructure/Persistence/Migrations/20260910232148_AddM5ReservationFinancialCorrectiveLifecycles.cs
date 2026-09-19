using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM5ReservationFinancialCorrectiveLifecycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "correction_count",
                table: "milestone_work_order",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "currency",
                table: "milestone_work_order",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "final_amount",
                table: "milestone_work_order",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "financial_journal_id",
                table: "milestone_work_order",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "financial_reversal_journal_id",
                table: "milestone_work_order",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "manager_responsibility",
                table: "milestone_work_order",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "other_amount",
                table: "milestone_work_order",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "owner_approval_id",
                table: "milestone_work_order",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "owner_responsibility",
                table: "milestone_work_order",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "posting_status",
                table: "milestone_work_order",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "replacement_financial_journal_id",
                table: "milestone_work_order",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "row_version",
                table: "milestone_work_order",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "selected_quote_id",
                table: "milestone_work_order",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_inspection_id",
                table: "milestone_work_order",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "milestone_work_order",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vendor_responsibility",
                table: "milestone_work_order",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "payload_json",
                table: "milestone_pm_reservation_event",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "related_booking_id",
                table: "milestone_pm_reservation_event",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "milestone_pm_maintenance_event",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payload_json",
                table: "milestone_pm_maintenance_event",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "correction_count",
                table: "milestone_pm_maintenance_case",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "financial_reversal_journal_id",
                table: "milestone_pm_maintenance_case",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "financial_status",
                table: "milestone_pm_maintenance_case",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "replacement_financial_journal_id",
                table: "milestone_pm_maintenance_case",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reinspection_of_action_id",
                table: "milestone_pm_inspection_record",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "template_id",
                table: "milestone_pm_inspection_record",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "template_name",
                table: "milestone_pm_inspection_record",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "template_version",
                table: "milestone_pm_inspection_record",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "milestone_p0_approval_event",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "milestone_p0_approval",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_idempotency_key",
                table: "milestone_p0_approval",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_id",
                table: "milestone_p0_approval",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_type",
                table: "milestone_p0_approval",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "milestone_pm_checklist_template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    workflow_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    items_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    supersedes_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_pm_checklist_template", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_pm_corrective_action",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checklist_item_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    severity = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    blocks_readiness = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    retest_inspection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution_notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    last_idempotency_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_pm_corrective_action", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_pm_corrective_action_milestone_manager_property_p~",
                        column: x => x.property_id,
                        principalTable: "milestone_manager_property",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_pm_corrective_action_milestone_pm_inspection_reco~",
                        column: x => x.inspection_id,
                        principalTable: "milestone_pm_inspection_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_pm_corrective_action_milestone_pm_inspection_rec~1",
                        column: x => x.retest_inspection_id,
                        principalTable: "milestone_pm_inspection_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_pm_corrective_action_milestone_work_order_work_or~",
                        column: x => x.work_order_id,
                        principalTable: "milestone_work_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_pm_cost_line",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    responsibility = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    receipt_attachment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_pm_cost_line", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_pm_cost_line_milestone_manager_maintenance_attach~",
                        column: x => x.receipt_attachment_id,
                        principalTable: "milestone_manager_maintenance_attachment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_pm_cost_line_milestone_pm_maintenance_case_mainte~",
                        column: x => x.maintenance_id,
                        principalTable: "milestone_pm_maintenance_case",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_pm_cost_line_milestone_work_order_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "milestone_work_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_pm_work_order_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    from_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    to_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    details = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_pm_work_order_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_pm_work_order_event_milestone_work_order_work_ord~",
                        column: x => x.work_order_id,
                        principalTable: "milestone_work_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_pm_work_order_quote",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    scope = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    evidence_attachment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_pm_work_order_quote", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_pm_work_order_quote_milestone_manager_maintenance~",
                        column: x => x.evidence_attachment_id,
                        principalTable: "milestone_manager_maintenance_attachment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_pm_work_order_quote_milestone_manager_vendor_vend~",
                        column: x => x.vendor_id,
                        principalTable: "milestone_manager_vendor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_pm_work_order_quote_milestone_work_order_work_ord~",
                        column: x => x.work_order_id,
                        principalTable: "milestone_work_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_pm_property_checklist_assignment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    effective_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_pm_property_checklist_assignment", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_pm_property_checklist_assignment_milestone_manage~",
                        column: x => x.property_id,
                        principalTable: "milestone_manager_property",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_pm_property_checklist_assignment_milestone_pm_che~",
                        column: x => x.template_id,
                        principalTable: "milestone_pm_checklist_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_work_order_financial_journal_id",
                table: "milestone_work_order",
                column: "financial_journal_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_work_order_financial_reversal_journal_id",
                table: "milestone_work_order",
                column: "financial_reversal_journal_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_work_order_owner_approval_id",
                table: "milestone_work_order",
                column: "owner_approval_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_work_order_replacement_financial_journal_id",
                table: "milestone_work_order",
                column: "replacement_financial_journal_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_work_order_selected_quote_id",
                table: "milestone_work_order",
                column: "selected_quote_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_work_order_source_inspection_id",
                table: "milestone_work_order",
                column: "source_inspection_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_reservation_event_related_booking_id",
                table: "milestone_pm_reservation_event",
                column: "related_booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_maintenance_event_manager_user_id_idempotency_~",
                table: "milestone_pm_maintenance_event",
                columns: new[] { "manager_user_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_maintenance_case_financial_journal_id",
                table: "milestone_pm_maintenance_case",
                column: "financial_journal_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_maintenance_case_financial_reversal_journal_id",
                table: "milestone_pm_maintenance_case",
                column: "financial_reversal_journal_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_maintenance_case_replacement_financial_journal~",
                table: "milestone_pm_maintenance_case",
                column: "replacement_financial_journal_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_inspection_record_reinspection_of_action_id",
                table: "milestone_pm_inspection_record",
                column: "reinspection_of_action_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_inspection_record_template_id",
                table: "milestone_pm_inspection_record",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_approval_event_approval_id_idempotency_key",
                table: "milestone_p0_approval_event",
                columns: new[] { "approval_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_approval_manager_user_id_request_idempotency_k~",
                table: "milestone_p0_approval",
                columns: new[] { "manager_user_id", "request_idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_checklist_template_manager_user_id_workflow_ty~",
                table: "milestone_pm_checklist_template",
                columns: new[] { "manager_user_id", "workflow_type", "name", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_corrective_action_inspection_id_checklist_item~",
                table: "milestone_pm_corrective_action",
                columns: new[] { "inspection_id", "checklist_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_corrective_action_manager_user_id_property_id_~",
                table: "milestone_pm_corrective_action",
                columns: new[] { "manager_user_id", "property_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_corrective_action_property_id",
                table: "milestone_pm_corrective_action",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_corrective_action_retest_inspection_id",
                table: "milestone_pm_corrective_action",
                column: "retest_inspection_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_corrective_action_work_order_id",
                table: "milestone_pm_corrective_action",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_cost_line_maintenance_id_created_at",
                table: "milestone_pm_cost_line",
                columns: new[] { "maintenance_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_cost_line_manager_user_id_idempotency_key",
                table: "milestone_pm_cost_line",
                columns: new[] { "manager_user_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_cost_line_receipt_attachment_id",
                table: "milestone_pm_cost_line",
                column: "receipt_attachment_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_cost_line_work_order_id_created_at",
                table: "milestone_pm_cost_line",
                columns: new[] { "work_order_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_property_checklist_assignment_manager_user_id_~",
                table: "milestone_pm_property_checklist_assignment",
                columns: new[] { "manager_user_id", "property_id", "workflow_type", "ended_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_property_checklist_assignment_property_id",
                table: "milestone_pm_property_checklist_assignment",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_property_checklist_assignment_template_id",
                table: "milestone_pm_property_checklist_assignment",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_work_order_event_manager_user_id_idempotency_k~",
                table: "milestone_pm_work_order_event",
                columns: new[] { "manager_user_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_work_order_event_work_order_id_created_at",
                table: "milestone_pm_work_order_event",
                columns: new[] { "work_order_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_work_order_quote_evidence_attachment_id",
                table: "milestone_pm_work_order_quote",
                column: "evidence_attachment_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_work_order_quote_manager_user_id_idempotency_k~",
                table: "milestone_pm_work_order_quote",
                columns: new[] { "manager_user_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_work_order_quote_vendor_id",
                table: "milestone_pm_work_order_quote",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_work_order_quote_work_order_id_created_at",
                table: "milestone_pm_work_order_quote",
                columns: new[] { "work_order_id", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_pm_inspection_record_milestone_pm_checklist_templ~",
                table: "milestone_pm_inspection_record",
                column: "template_id",
                principalTable: "milestone_pm_checklist_template",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_pm_inspection_record_milestone_pm_corrective_acti~",
                table: "milestone_pm_inspection_record",
                column: "reinspection_of_action_id",
                principalTable: "milestone_pm_corrective_action",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_pm_maintenance_case_milestone_p0_journal_financia~",
                table: "milestone_pm_maintenance_case",
                column: "financial_journal_id",
                principalTable: "milestone_p0_journal",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_pm_maintenance_case_milestone_p0_journal_financi~1",
                table: "milestone_pm_maintenance_case",
                column: "financial_reversal_journal_id",
                principalTable: "milestone_p0_journal",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_pm_maintenance_case_milestone_p0_journal_replacem~",
                table: "milestone_pm_maintenance_case",
                column: "replacement_financial_journal_id",
                principalTable: "milestone_p0_journal",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_work_order_milestone_p0_approval_owner_approval_id",
                table: "milestone_work_order",
                column: "owner_approval_id",
                principalTable: "milestone_p0_approval",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_work_order_milestone_p0_journal_financial_journal~",
                table: "milestone_work_order",
                column: "financial_journal_id",
                principalTable: "milestone_p0_journal",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_work_order_milestone_p0_journal_financial_reversa~",
                table: "milestone_work_order",
                column: "financial_reversal_journal_id",
                principalTable: "milestone_p0_journal",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_work_order_milestone_p0_journal_replacement_finan~",
                table: "milestone_work_order",
                column: "replacement_financial_journal_id",
                principalTable: "milestone_p0_journal",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_work_order_milestone_pm_inspection_record_source_~",
                table: "milestone_work_order",
                column: "source_inspection_id",
                principalTable: "milestone_pm_inspection_record",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_work_order_milestone_pm_work_order_quote_selected~",
                table: "milestone_work_order",
                column: "selected_quote_id",
                principalTable: "milestone_pm_work_order_quote",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_milestone_pm_inspection_record_milestone_pm_checklist_templ~",
                table: "milestone_pm_inspection_record");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_pm_inspection_record_milestone_pm_corrective_acti~",
                table: "milestone_pm_inspection_record");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_pm_maintenance_case_milestone_p0_journal_financia~",
                table: "milestone_pm_maintenance_case");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_pm_maintenance_case_milestone_p0_journal_financi~1",
                table: "milestone_pm_maintenance_case");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_pm_maintenance_case_milestone_p0_journal_replacem~",
                table: "milestone_pm_maintenance_case");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_work_order_milestone_p0_approval_owner_approval_id",
                table: "milestone_work_order");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_work_order_milestone_p0_journal_financial_journal~",
                table: "milestone_work_order");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_work_order_milestone_p0_journal_financial_reversa~",
                table: "milestone_work_order");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_work_order_milestone_p0_journal_replacement_finan~",
                table: "milestone_work_order");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_work_order_milestone_pm_inspection_record_source_~",
                table: "milestone_work_order");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_work_order_milestone_pm_work_order_quote_selected~",
                table: "milestone_work_order");

            migrationBuilder.DropTable(
                name: "milestone_pm_corrective_action");

            migrationBuilder.DropTable(
                name: "milestone_pm_cost_line");

            migrationBuilder.DropTable(
                name: "milestone_pm_property_checklist_assignment");

            migrationBuilder.DropTable(
                name: "milestone_pm_work_order_event");

            migrationBuilder.DropTable(
                name: "milestone_pm_work_order_quote");

            migrationBuilder.DropTable(
                name: "milestone_pm_checklist_template");

            migrationBuilder.DropIndex(
                name: "IX_milestone_work_order_financial_journal_id",
                table: "milestone_work_order");

            migrationBuilder.DropIndex(
                name: "IX_milestone_work_order_financial_reversal_journal_id",
                table: "milestone_work_order");

            migrationBuilder.DropIndex(
                name: "IX_milestone_work_order_owner_approval_id",
                table: "milestone_work_order");

            migrationBuilder.DropIndex(
                name: "IX_milestone_work_order_replacement_financial_journal_id",
                table: "milestone_work_order");

            migrationBuilder.DropIndex(
                name: "IX_milestone_work_order_selected_quote_id",
                table: "milestone_work_order");

            migrationBuilder.DropIndex(
                name: "IX_milestone_work_order_source_inspection_id",
                table: "milestone_work_order");

            migrationBuilder.DropIndex(
                name: "IX_milestone_pm_reservation_event_related_booking_id",
                table: "milestone_pm_reservation_event");

            migrationBuilder.DropIndex(
                name: "IX_milestone_pm_maintenance_event_manager_user_id_idempotency_~",
                table: "milestone_pm_maintenance_event");

            migrationBuilder.DropIndex(
                name: "IX_milestone_pm_maintenance_case_financial_journal_id",
                table: "milestone_pm_maintenance_case");

            migrationBuilder.DropIndex(
                name: "IX_milestone_pm_maintenance_case_financial_reversal_journal_id",
                table: "milestone_pm_maintenance_case");

            migrationBuilder.DropIndex(
                name: "IX_milestone_pm_maintenance_case_replacement_financial_journal~",
                table: "milestone_pm_maintenance_case");

            migrationBuilder.DropIndex(
                name: "IX_milestone_pm_inspection_record_reinspection_of_action_id",
                table: "milestone_pm_inspection_record");

            migrationBuilder.DropIndex(
                name: "IX_milestone_pm_inspection_record_template_id",
                table: "milestone_pm_inspection_record");

            migrationBuilder.DropIndex(
                name: "IX_milestone_p0_approval_event_approval_id_idempotency_key",
                table: "milestone_p0_approval_event");

            migrationBuilder.DropIndex(
                name: "IX_milestone_p0_approval_manager_user_id_request_idempotency_k~",
                table: "milestone_p0_approval");

            migrationBuilder.DropColumn(
                name: "correction_count",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "currency",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "final_amount",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "financial_journal_id",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "financial_reversal_journal_id",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "manager_responsibility",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "other_amount",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "owner_approval_id",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "owner_responsibility",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "posting_status",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "replacement_financial_journal_id",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "selected_quote_id",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "source_inspection_id",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "vendor_responsibility",
                table: "milestone_work_order");

            migrationBuilder.DropColumn(
                name: "payload_json",
                table: "milestone_pm_reservation_event");

            migrationBuilder.DropColumn(
                name: "related_booking_id",
                table: "milestone_pm_reservation_event");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "milestone_pm_maintenance_event");

            migrationBuilder.DropColumn(
                name: "payload_json",
                table: "milestone_pm_maintenance_event");

            migrationBuilder.DropColumn(
                name: "correction_count",
                table: "milestone_pm_maintenance_case");

            migrationBuilder.DropColumn(
                name: "financial_reversal_journal_id",
                table: "milestone_pm_maintenance_case");

            migrationBuilder.DropColumn(
                name: "financial_status",
                table: "milestone_pm_maintenance_case");

            migrationBuilder.DropColumn(
                name: "replacement_financial_journal_id",
                table: "milestone_pm_maintenance_case");

            migrationBuilder.DropColumn(
                name: "reinspection_of_action_id",
                table: "milestone_pm_inspection_record");

            migrationBuilder.DropColumn(
                name: "template_id",
                table: "milestone_pm_inspection_record");

            migrationBuilder.DropColumn(
                name: "template_name",
                table: "milestone_pm_inspection_record");

            migrationBuilder.DropColumn(
                name: "template_version",
                table: "milestone_pm_inspection_record");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "milestone_p0_approval_event");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "milestone_p0_approval");

            migrationBuilder.DropColumn(
                name: "request_idempotency_key",
                table: "milestone_p0_approval");

            migrationBuilder.DropColumn(
                name: "source_id",
                table: "milestone_p0_approval");

            migrationBuilder.DropColumn(
                name: "source_type",
                table: "milestone_p0_approval");
        }
    }
}
