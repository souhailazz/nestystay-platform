using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInsuraGuestPolicyLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "insurance_claim",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    incident_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    evidence_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_insurance_claim", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "insurance_policy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    plan_code = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    market = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    monthly_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    property_damage_coverage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    accidental_medical_coverage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    last_idempotency_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    effective_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    renews_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_provider_event_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_insurance_policy", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_insurance_claim_property_id_status",
                table: "insurance_claim",
                columns: new[] { "property_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_insurance_policy_last_idempotency_key",
                table: "insurance_policy",
                column: "last_idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_insurance_policy_property_id_status",
                table: "insurance_policy",
                columns: new[] { "property_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_insurance_policy_provider_reference",
                table: "insurance_policy",
                column: "provider_reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "insurance_claim");

            migrationBuilder.DropTable(
                name: "insurance_policy");
        }
    }
}
