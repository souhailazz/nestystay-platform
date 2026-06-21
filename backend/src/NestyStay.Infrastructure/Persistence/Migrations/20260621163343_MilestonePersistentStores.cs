using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MilestonePersistentStores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "milestone_badge_assignment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    badge_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    badge_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    level = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    subject_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    earned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    paid_through = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    amount_charged = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payment_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    unlocks_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_badge_assignment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_badge_definition",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    level = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    applies_to = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    pricebook_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    unlocks_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_badge_definition", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_badge_renewal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    badge_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reminder_due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payment_attempted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    payment_status = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    amount_due = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_badge_renewal", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_booking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    host_email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    guest_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    guest_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    check_in = table.Column<DateOnly>(type: "date", nullable: false),
                    check_out = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    verification_status = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    requires_guest_verification = table.Column<bool>(type: "boolean", nullable: false),
                    hold_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    nights = table.Column<int>(type: "integer", nullable: false),
                    nightly_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    stay_subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    guest_platform_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    property_title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ekyc_provider = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ekyc_transaction_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ekyc_transaction_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    payment_provider = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    payment_authorization_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    payment_client_secret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    payment_capture_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    price_breakdown_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    notifications_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    timeline_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_booking", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_campaign",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    campaign_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    override_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    applies_to = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    opens_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closes_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_campaign", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_campaign_enrollment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    subject_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrolled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_campaign_enrollment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_founding_benefit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    guest_flat_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    host_commission_percent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_lifetime_guest_fee = table.Column<bool>(type: "boolean", nullable: false),
                    is_transferable_with_property = table.Column<bool>(type: "boolean", nullable: false),
                    is_forfeited = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_founding_benefit", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_pricebook_entry",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    label = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    cadence = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    applies_to = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_configurable = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    active_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    active_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_pricebook_entry", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_property",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    host_email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    location = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    country = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    nightly_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    badge_level = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    guest_verification_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    insura_guest_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    cancellation_policy = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    highlights_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_property", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_two_factor_challenge",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenge_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_two_factor_challenge", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    display_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    phone = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    two_factor_secret = table.Column<byte[]>(type: "bytea", nullable: false),
                    roles_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_user", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_badge_assignment_subject_type_subject_id_level",
                table: "milestone_badge_assignment",
                columns: new[] { "subject_type", "subject_id", "level" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_badge_definition_level_applies_to",
                table: "milestone_badge_definition",
                columns: new[] { "level", "applies_to" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_badge_renewal_badge_assignment_id_reminder_due_at",
                table: "milestone_badge_renewal",
                columns: new[] { "badge_assignment_id", "reminder_due_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_booking_ekyc_transaction_id",
                table: "milestone_booking",
                column: "ekyc_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_booking_property_id_check_in_check_out",
                table: "milestone_booking",
                columns: new[] { "property_id", "check_in", "check_out" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_campaign_key",
                table: "milestone_campaign",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_campaign_enrollment_campaign_key_subject_type_sub~",
                table: "milestone_campaign_enrollment",
                columns: new[] { "campaign_key", "subject_type", "subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_founding_benefit_property_id",
                table: "milestone_founding_benefit",
                column: "property_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pricebook_entry_key",
                table: "milestone_pricebook_entry",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_property_host_user_id",
                table: "milestone_property",
                column: "host_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_two_factor_challenge_challenge_id",
                table: "milestone_two_factor_challenge",
                column: "challenge_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_user_normalized_email",
                table: "milestone_user",
                column: "normalized_email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_badge_assignment");

            migrationBuilder.DropTable(
                name: "milestone_badge_definition");

            migrationBuilder.DropTable(
                name: "milestone_badge_renewal");

            migrationBuilder.DropTable(
                name: "milestone_booking");

            migrationBuilder.DropTable(
                name: "milestone_campaign");

            migrationBuilder.DropTable(
                name: "milestone_campaign_enrollment");

            migrationBuilder.DropTable(
                name: "milestone_founding_benefit");

            migrationBuilder.DropTable(
                name: "milestone_pricebook_entry");

            migrationBuilder.DropTable(
                name: "milestone_property");

            migrationBuilder.DropTable(
                name: "milestone_two_factor_challenge");

            migrationBuilder.DropTable(
                name: "milestone_user");
        }
    }
}
