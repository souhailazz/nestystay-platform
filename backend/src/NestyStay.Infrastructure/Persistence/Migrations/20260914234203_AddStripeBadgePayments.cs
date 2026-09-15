using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeBadgePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "milestone_badge_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    level = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    badge_assignment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    renewal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    provider_payment_intent_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    request_snapshot_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    last_provider_event_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_badge_payment", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_badge_payment_idempotency_key",
                table: "milestone_badge_payment",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_badge_payment_provider_payment_intent_id",
                table: "milestone_badge_payment",
                column: "provider_payment_intent_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_badge_payment_subject_type_subject_id_level_status",
                table: "milestone_badge_payment",
                columns: new[] { "subject_type", "subject_id", "level", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_badge_payment");
        }
    }
}
