using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyManagerP0Model : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "milestone_p0_account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    account_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_client_money = table.Column<bool>(type: "boolean", nullable: false),
                    is_pm_money = table.Column<bool>(type: "boolean", nullable: false),
                    is_third_party = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_account", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_account_milestone_manager_property_property_id",
                        column: x => x.property_id,
                        principalTable: "milestone_manager_property",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_account_milestone_user_manager_user_id",
                        column: x => x.manager_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_account_milestone_user_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_approval",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fee_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    threshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    evidence_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    decision_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_approval", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_approval_milestone_manager_property_property_id",
                        column: x => x.property_id,
                        principalTable: "milestone_manager_property",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_approval_milestone_user_manager_user_id",
                        column: x => x.manager_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_approval_milestone_user_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_journal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_number = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    source_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    accounting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    memo = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    reconciliation_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    total_debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reversal_of_journal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_journal", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_journal_milestone_user_manager_user_id",
                        column: x => x.manager_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_journal_milestone_user_posted_by_user_id",
                        column: x => x.posted_by_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_management_agreement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    supersedes_agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    terms_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    fee_rule_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    maintenance_approval_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    expense_approval_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    document_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    activated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    terminated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    termination_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_management_agreement", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_management_agreement_milestone_manager_documen~",
                        column: x => x.document_id,
                        principalTable: "milestone_manager_document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_management_agreement_milestone_manager_propert~",
                        column: x => x.property_id,
                        principalTable: "milestone_manager_property",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_management_agreement_milestone_user_manager_us~",
                        column: x => x.manager_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_management_agreement_milestone_user_owner_user~",
                        column: x => x.owner_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_management_fee_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    rule_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    calculation_basis = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fixed_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    minimum_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cleaning_markup = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    maintenance_markup = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_management_fee_rule", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_management_fee_rule_milestone_manager_property~",
                        column: x => x.property_id,
                        principalTable: "milestone_manager_property",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_management_fee_rule_milestone_user_manager_use~",
                        column: x => x.manager_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_management_fee_rule_milestone_user_owner_user_~",
                        column: x => x.owner_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_owner_lifecycle_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    from_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    to_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_owner_lifecycle_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_owner_lifecycle_event_milestone_user_actor_use~",
                        column: x => x.actor_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_owner_lifecycle_event_milestone_user_manager_u~",
                        column: x => x.manager_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_owner_lifecycle_event_milestone_user_owner_use~",
                        column: x => x.owner_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_owner_profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    contact_phone = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    billing_address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    preferred_currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    time_zone = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    operational_metadata_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    activated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_owner_profile", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_owner_profile_milestone_user_manager_user_id",
                        column: x => x.manager_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_owner_profile_milestone_user_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_payout_batch",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    period_from = table.Column<DateOnly>(type: "date", nullable: false),
                    period_to = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reserved_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    provider_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    statement_snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_payout_batch", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_payout_batch_milestone_user_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_payout_batch_milestone_user_manager_user_id",
                        column: x => x.manager_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_payout_batch_milestone_user_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_staff_membership",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    property_scope_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    owner_scope_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    can_manage_finance = table.Column<bool>(type: "boolean", nullable: false),
                    can_approve_payouts = table.Column<bool>(type: "boolean", nullable: false),
                    approval_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_staff_membership", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_staff_membership_milestone_user_manager_user_id",
                        column: x => x.manager_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_staff_membership_milestone_user_staff_user_id",
                        column: x => x.staff_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_statement_snapshot",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    period_from = table.Column<DateOnly>(type: "date", nullable: false),
                    period_to = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    opening_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    income = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    expenses = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    management_fees = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payouts = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    closing_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    snapshot_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    content_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    finalized_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    finalized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_statement_snapshot", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_statement_snapshot_milestone_manager_property_~",
                        column: x => x.property_id,
                        principalTable: "milestone_manager_property",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_statement_snapshot_milestone_user_manager_user~",
                        column: x => x.manager_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_statement_snapshot_milestone_user_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_approval_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    from_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    to_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    evidence_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_approval_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_approval_event_milestone_p0_approval_approval_~",
                        column: x => x.approval_id,
                        principalTable: "milestone_p0_approval",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_approval_event_milestone_user_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_journal_line",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_journal_line", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_journal_line_milestone_manager_property_proper~",
                        column: x => x.property_id,
                        principalTable: "milestone_manager_property",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_journal_line_milestone_p0_account_account_id",
                        column: x => x.account_id,
                        principalTable: "milestone_p0_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_journal_line_milestone_p0_journal_journal_id",
                        column: x => x.journal_id,
                        principalTable: "milestone_p0_journal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_journal_line_milestone_user_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_reconciliation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reconciled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_reconciliation", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_reconciliation_milestone_p0_journal_journal_id",
                        column: x => x.journal_id,
                        principalTable: "milestone_p0_journal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_reconciliation_milestone_user_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_reconciliation_milestone_user_manager_user_id",
                        column: x => x.manager_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_payout_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    from_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    to_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_payout_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_payout_event_milestone_p0_payout_batch_batch_id",
                        column: x => x.batch_id,
                        principalTable: "milestone_p0_payout_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_payout_event_milestone_user_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_payout_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    statement_snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_payout_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_payout_item_milestone_manager_property_propert~",
                        column: x => x.property_id,
                        principalTable: "milestone_manager_property",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_payout_item_milestone_p0_payout_batch_batch_id",
                        column: x => x.batch_id,
                        principalTable: "milestone_p0_payout_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_payout_item_milestone_user_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_p0_staff_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    from_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    to_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_p0_staff_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_p0_staff_event_milestone_p0_staff_membership_memb~",
                        column: x => x.membership_id,
                        principalTable: "milestone_p0_staff_membership",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_p0_staff_event_milestone_user_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_account_manager_user_id_code",
                table: "milestone_p0_account",
                columns: new[] { "manager_user_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_account_manager_user_id_owner_user_id_property~",
                table: "milestone_p0_account",
                columns: new[] { "manager_user_id", "owner_user_id", "property_id", "currency" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_account_owner_user_id",
                table: "milestone_p0_account",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_account_property_id",
                table: "milestone_p0_account",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_approval_manager_user_id_owner_user_id_status",
                table: "milestone_p0_approval",
                columns: new[] { "manager_user_id", "owner_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_approval_owner_user_id",
                table: "milestone_p0_approval",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_approval_property_id",
                table: "milestone_p0_approval",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_approval_event_actor_user_id",
                table: "milestone_p0_approval_event",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_approval_event_approval_id_created_at",
                table: "milestone_p0_approval_event",
                columns: new[] { "approval_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_journal_manager_user_id_accounting_date_curren~",
                table: "milestone_p0_journal",
                columns: new[] { "manager_user_id", "accounting_date", "currency" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_journal_manager_user_id_idempotency_key",
                table: "milestone_p0_journal",
                columns: new[] { "manager_user_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_journal_posted_by_user_id",
                table: "milestone_p0_journal",
                column: "posted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_journal_line_account_id",
                table: "milestone_p0_journal_line",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_journal_line_journal_id_account_id",
                table: "milestone_p0_journal_line",
                columns: new[] { "journal_id", "account_id" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_journal_line_owner_user_id",
                table: "milestone_p0_journal_line",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_journal_line_property_id",
                table: "milestone_p0_journal_line",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_management_agreement_document_id",
                table: "milestone_p0_management_agreement",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_management_agreement_manager_user_id_owner_us~1",
                table: "milestone_p0_management_agreement",
                columns: new[] { "manager_user_id", "owner_user_id", "property_id", "status", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_management_agreement_manager_user_id_owner_use~",
                table: "milestone_p0_management_agreement",
                columns: new[] { "manager_user_id", "owner_user_id", "property_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_management_agreement_owner_user_id",
                table: "milestone_p0_management_agreement",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_management_agreement_property_id",
                table: "milestone_p0_management_agreement",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_management_fee_rule_manager_user_id_owner_user~",
                table: "milestone_p0_management_fee_rule",
                columns: new[] { "manager_user_id", "owner_user_id", "property_id", "category", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_management_fee_rule_owner_user_id",
                table: "milestone_p0_management_fee_rule",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_management_fee_rule_property_id",
                table: "milestone_p0_management_fee_rule",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_owner_lifecycle_event_actor_user_id",
                table: "milestone_p0_owner_lifecycle_event",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_owner_lifecycle_event_manager_user_id_owner_us~",
                table: "milestone_p0_owner_lifecycle_event",
                columns: new[] { "manager_user_id", "owner_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_owner_lifecycle_event_owner_user_id",
                table: "milestone_p0_owner_lifecycle_event",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_owner_profile_manager_user_id_owner_user_id",
                table: "milestone_p0_owner_profile",
                columns: new[] { "manager_user_id", "owner_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_owner_profile_owner_user_id",
                table: "milestone_p0_owner_profile",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_payout_batch_approved_by_user_id",
                table: "milestone_p0_payout_batch",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_payout_batch_manager_user_id_idempotency_key",
                table: "milestone_p0_payout_batch",
                columns: new[] { "manager_user_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_payout_batch_manager_user_id_owner_user_id_cur~",
                table: "milestone_p0_payout_batch",
                columns: new[] { "manager_user_id", "owner_user_id", "currency", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_payout_batch_owner_user_id",
                table: "milestone_p0_payout_batch",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_payout_event_actor_user_id",
                table: "milestone_p0_payout_event",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_payout_event_batch_id_created_at",
                table: "milestone_p0_payout_event",
                columns: new[] { "batch_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_payout_item_batch_id",
                table: "milestone_p0_payout_item",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_payout_item_owner_user_id",
                table: "milestone_p0_payout_item",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_payout_item_property_id",
                table: "milestone_p0_payout_item",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_reconciliation_actor_user_id",
                table: "milestone_p0_reconciliation",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_reconciliation_journal_id",
                table: "milestone_p0_reconciliation",
                column: "journal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_reconciliation_manager_user_id",
                table: "milestone_p0_reconciliation",
                column: "manager_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_staff_event_actor_user_id",
                table: "milestone_p0_staff_event",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_staff_event_membership_id_created_at",
                table: "milestone_p0_staff_event",
                columns: new[] { "membership_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_staff_membership_manager_user_id_staff_user_id",
                table: "milestone_p0_staff_membership",
                columns: new[] { "manager_user_id", "staff_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_staff_membership_staff_user_id",
                table: "milestone_p0_staff_membership",
                column: "staff_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_statement_snapshot_manager_user_id_owner_user_~",
                table: "milestone_p0_statement_snapshot",
                columns: new[] { "manager_user_id", "owner_user_id", "property_id", "currency", "period_from", "period_to" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_statement_snapshot_owner_user_id",
                table: "milestone_p0_statement_snapshot",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_statement_snapshot_property_id",
                table: "milestone_p0_statement_snapshot",
                column: "property_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_p0_approval_event");

            migrationBuilder.DropTable(
                name: "milestone_p0_journal_line");

            migrationBuilder.DropTable(
                name: "milestone_p0_management_agreement");

            migrationBuilder.DropTable(
                name: "milestone_p0_management_fee_rule");

            migrationBuilder.DropTable(
                name: "milestone_p0_owner_lifecycle_event");

            migrationBuilder.DropTable(
                name: "milestone_p0_owner_profile");

            migrationBuilder.DropTable(
                name: "milestone_p0_payout_event");

            migrationBuilder.DropTable(
                name: "milestone_p0_payout_item");

            migrationBuilder.DropTable(
                name: "milestone_p0_reconciliation");

            migrationBuilder.DropTable(
                name: "milestone_p0_staff_event");

            migrationBuilder.DropTable(
                name: "milestone_p0_statement_snapshot");

            migrationBuilder.DropTable(
                name: "milestone_p0_approval");

            migrationBuilder.DropTable(
                name: "milestone_p0_account");

            migrationBuilder.DropTable(
                name: "milestone_p0_payout_batch");

            migrationBuilder.DropTable(
                name: "milestone_p0_journal");

            migrationBuilder.DropTable(
                name: "milestone_p0_staff_membership");
        }
    }
}
