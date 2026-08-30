using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M3M4WellnessDirectoriesAndQr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "property_id",
                table: "qr_scan_log",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "booking_id",
                table: "qr_access_code",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "guest_user_id",
                table: "qr_access_code",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_validated_at",
                table: "qr_access_code",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "property_id",
                table: "qr_access_code",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "qr_access_code",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "valid_from",
                table: "qr_access_code",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "validation_count",
                table: "qr_access_code",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_brick_and_mortar",
                table: "milestone_directory_provider",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "police_badge_number",
                table: "milestone_directory_provider",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "milestone_directory_provider",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "verification_status",
                table: "milestone_directory_provider",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "badge_definition",
                keyColumn: "id",
                keyValue: new Guid("6b747f5f-f571-4846-24fa-1e31902964ab"),
                column: "unlocks_json",
                value: "[\"Trusted badge\",\"Trades directory\",\"Search boost\",\"Referral program\"]");

            migrationBuilder.UpdateData(
                table: "badge_definition",
                keyColumn: "id",
                keyValue: new Guid("a7465928-a25f-ea05-0dca-d5da270ed2cb"),
                column: "unlocks_json",
                value: "[\"Listings\",\"Calendar\",\"Messaging\",\"QR code access\",\"97% payout\"]");

            migrationBuilder.UpdateData(
                table: "badge_definition",
                keyColumn: "id",
                keyValue: new Guid("b76cbb37-5c92-9613-47dd-6cc804ba7267"),
                column: "unlocks_json",
                value: "[\"Police directory\",\"Wellness visits\",\"In-person guest ID check\",\"Drive-by property patrol\",\"Wellness badge\",\"Police Verified filter\"]");

            migrationBuilder.CreateIndex(
                name: "IX_qr_scan_log_qr_access_code_id_scanned_at",
                table: "qr_scan_log",
                columns: new[] { "qr_access_code_id", "scanned_at" });

            migrationBuilder.CreateIndex(
                name: "IX_qr_access_code_booking_id_is_revoked_expires_at",
                table: "qr_access_code",
                columns: new[] { "booking_id", "is_revoked", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_qr_access_code_property_id_expires_at",
                table: "qr_access_code",
                columns: new[] { "property_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_directory_provider_status_verification_status_is_~",
                table: "milestone_directory_provider",
                columns: new[] { "status", "verification_status", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_qr_scan_log_qr_access_code_id_scanned_at",
                table: "qr_scan_log");

            migrationBuilder.DropIndex(
                name: "IX_qr_access_code_booking_id_is_revoked_expires_at",
                table: "qr_access_code");

            migrationBuilder.DropIndex(
                name: "IX_qr_access_code_property_id_expires_at",
                table: "qr_access_code");

            migrationBuilder.DropIndex(
                name: "IX_milestone_directory_provider_status_verification_status_is_~",
                table: "milestone_directory_provider");

            migrationBuilder.DropColumn(
                name: "property_id",
                table: "qr_scan_log");

            migrationBuilder.DropColumn(
                name: "booking_id",
                table: "qr_access_code");

            migrationBuilder.DropColumn(
                name: "guest_user_id",
                table: "qr_access_code");

            migrationBuilder.DropColumn(
                name: "last_validated_at",
                table: "qr_access_code");

            migrationBuilder.DropColumn(
                name: "property_id",
                table: "qr_access_code");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "qr_access_code");

            migrationBuilder.DropColumn(
                name: "valid_from",
                table: "qr_access_code");

            migrationBuilder.DropColumn(
                name: "validation_count",
                table: "qr_access_code");

            migrationBuilder.DropColumn(
                name: "is_brick_and_mortar",
                table: "milestone_directory_provider");

            migrationBuilder.DropColumn(
                name: "police_badge_number",
                table: "milestone_directory_provider");

            migrationBuilder.DropColumn(
                name: "status",
                table: "milestone_directory_provider");

            migrationBuilder.DropColumn(
                name: "verification_status",
                table: "milestone_directory_provider");

            migrationBuilder.UpdateData(
                table: "badge_definition",
                keyColumn: "id",
                keyValue: new Guid("6b747f5f-f571-4846-24fa-1e31902964ab"),
                column: "unlocks_json",
                value: "[\"Trades directory\",\"Search boost\",\"Referral program\"]");

            migrationBuilder.UpdateData(
                table: "badge_definition",
                keyColumn: "id",
                keyValue: new Guid("a7465928-a25f-ea05-0dca-d5da270ed2cb"),
                column: "unlocks_json",
                value: "[\"Listings\",\"Calendar\",\"Messaging\",\"QR\",\"Stripe\",\"InsuraGuest\",\"97% payout\"]");

            migrationBuilder.UpdateData(
                table: "badge_definition",
                keyColumn: "id",
                keyValue: new Guid("b76cbb37-5c92-9613-47dd-6cc804ba7267"),
                column: "unlocks_json",
                value: "[\"Police directory\",\"Wellness visits\",\"Wellness badge\",\"Security verified filter\"]");
        }
    }
}
