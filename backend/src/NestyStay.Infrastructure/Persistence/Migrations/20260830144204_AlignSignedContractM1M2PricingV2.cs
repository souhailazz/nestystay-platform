using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignSignedContractM1M2PricingV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the persistent milestone pricebook aligned with the signed agreement.
            migrationBuilder.Sql("UPDATE milestone_pricebook_entry SET amount = 9 WHERE key IN ('guest-standard-fee-low', 'guest-standard-fee-mid', 'guest-standard-fee-short');");
            migrationBuilder.Sql("UPDATE milestone_pricebook_entry SET amount = 0, cadence = 'Included' WHERE key = 'verified-host-standard-annual';");
            migrationBuilder.Sql("UPDATE milestone_pricebook_entry SET amount = 49, cadence = 'One time (annual renewal capability)' WHERE key = 'trusted-host-standard-annual';");
            migrationBuilder.Sql("INSERT INTO milestone_pricebook_entry (id, key, label, amount, currency, cadence, applies_to, is_configurable, is_active, active_from, active_to, created_at, updated_at, created_by_user_id, updated_by_user_id, is_deleted) SELECT 'f11c7c1c-0e95-4a8e-9e5f-0e4f2f4d3f0a', 'wellness-subscription-pdf', 'Wellness badge subscription', 19, 'USD', 'Monthly', 'Hosts', TRUE, TRUE, NOW(), NULL, NOW(), NOW(), NULL, NULL, FALSE WHERE NOT EXISTS (SELECT 1 FROM milestone_pricebook_entry WHERE key = 'wellness-subscription-pdf');");
            migrationBuilder.Sql("UPDATE milestone_badge_definition SET pricebook_key = 'wellness-subscription-pdf', unlocks_json = '[\"Police directory\",\"Wellness visits\",\"In-person guest ID check\",\"Drive-by property patrol\",\"Wellness badge\",\"Police Verified filter\"]' WHERE key = 'host-wellness';");
            migrationBuilder.Sql("UPDATE milestone_badge_definition SET unlocks_json = '[\"Trusted badge\",\"Trades directory\",\"Search boost\",\"Referral program\"]' WHERE key = 'host-trusted';");
            migrationBuilder.Sql("UPDATE milestone_badge_definition SET unlocks_json = '[\"Listings\",\"Calendar\",\"Messaging\",\"QR code access\",\"97% payout\"]' WHERE key = 'host-free';");
            migrationBuilder.UpdateData(
                table: "pricebook_entry",
                keyColumn: "id",
                keyValue: new Guid("0cce0a0c-73a8-85dc-17bb-5b4b8ab355f9"),
                column: "amount",
                value: 9m);

            migrationBuilder.UpdateData(
                table: "pricebook_entry",
                keyColumn: "id",
                keyValue: new Guid("59d4c1d5-9e48-a929-56d5-96eb67d1feb0"),
                column: "amount",
                value: 9m);

            migrationBuilder.UpdateData(
                table: "pricebook_entry",
                keyColumn: "id",
                keyValue: new Guid("818cd436-df71-3b5e-6095-601ee9a6370d"),
                columns: new[] { "amount", "cadence" },
                values: new object[] { 0m, "Included" });

            migrationBuilder.UpdateData(
                table: "pricebook_entry",
                keyColumn: "id",
                keyValue: new Guid("8c5b02ff-6832-3fe3-d13d-434e74947d1c"),
                column: "amount",
                value: 9m);

            migrationBuilder.UpdateData(
                table: "pricebook_entry",
                keyColumn: "id",
                keyValue: new Guid("a633594e-c72f-9093-a9c4-a224ff6b5d1e"),
                columns: new[] { "amount", "cadence" },
                values: new object[] { 49m, "One time (annual renewal capability)" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE milestone_pricebook_entry SET amount = 8 WHERE key = 'guest-standard-fee-low';");
            migrationBuilder.Sql("UPDATE milestone_pricebook_entry SET amount = 10 WHERE key = 'guest-standard-fee-mid';");
            migrationBuilder.Sql("UPDATE milestone_pricebook_entry SET amount = 12 WHERE key = 'guest-standard-fee-short';");
            migrationBuilder.Sql("UPDATE milestone_pricebook_entry SET amount = 60, cadence = 'Annual' WHERE key = 'verified-host-standard-annual';");
            migrationBuilder.Sql("UPDATE milestone_pricebook_entry SET amount = 120, cadence = 'Annual' WHERE key = 'trusted-host-standard-annual';");
            migrationBuilder.Sql("DELETE FROM milestone_pricebook_entry WHERE key = 'wellness-subscription-pdf';");
            migrationBuilder.UpdateData(
                table: "pricebook_entry",
                keyColumn: "id",
                keyValue: new Guid("0cce0a0c-73a8-85dc-17bb-5b4b8ab355f9"),
                column: "amount",
                value: 10m);

            migrationBuilder.UpdateData(
                table: "pricebook_entry",
                keyColumn: "id",
                keyValue: new Guid("59d4c1d5-9e48-a929-56d5-96eb67d1feb0"),
                column: "amount",
                value: 8m);

            migrationBuilder.UpdateData(
                table: "pricebook_entry",
                keyColumn: "id",
                keyValue: new Guid("818cd436-df71-3b5e-6095-601ee9a6370d"),
                columns: new[] { "amount", "cadence" },
                values: new object[] { 60m, "Annual" });

            migrationBuilder.UpdateData(
                table: "pricebook_entry",
                keyColumn: "id",
                keyValue: new Guid("8c5b02ff-6832-3fe3-d13d-434e74947d1c"),
                column: "amount",
                value: 12m);

            migrationBuilder.UpdateData(
                table: "pricebook_entry",
                keyColumn: "id",
                keyValue: new Guid("a633594e-c72f-9093-a9c4-a224ff6b5d1e"),
                columns: new[] { "amount", "cadence" },
                values: new object[] { 120m, "Annual" });
        }
    }
}
