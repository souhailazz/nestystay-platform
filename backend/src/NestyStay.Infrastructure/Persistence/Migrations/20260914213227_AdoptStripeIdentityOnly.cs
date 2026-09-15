using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdoptStripeIdentityOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "pricebook_entry",
                keyColumn: "id",
                keyValue: new Guid("4e2ec970-92a2-0943-f9d0-b275378e1450"));

            migrationBuilder.DeleteData(
                table: "provider_config",
                keyColumn: "id",
                keyValue: new Guid("bb92e267-1ae5-7f5f-5a68-1a769647db7f"));

            migrationBuilder.InsertData(
                table: "pricebook_entry",
                columns: new[] { "id", "active_from", "active_to", "amount", "applies_to", "cadence", "created_at", "created_by_user_id", "currency_or_unit", "is_configurable", "is_deleted", "key", "label", "updated_at", "updated_by_user_id" },
                values: new object[] { new Guid("02ee1343-7659-7c54-51cb-cac06c2d7df2"), null, null, 0.14m, "NestyStay", "Per check", new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "USD", true, false, "stripe-identity-vendor-cost", "stripe identity vendor cost", new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });

            migrationBuilder.InsertData(
                table: "provider_config",
                columns: new[] { "id", "created_at", "created_by_user_id", "encrypted_config_reference", "is_deleted", "is_primary", "kind", "provider_name", "updated_at", "updated_by_user_id" },
                values: new object[] { new Guid("0485c08d-334a-865d-7ca4-9685eded0d5e"), new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "vault://nestystay/ekyc/stripeidentity", false, true, "Ekyc", "StripeIdentity", new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "pricebook_entry",
                keyColumn: "id",
                keyValue: new Guid("02ee1343-7659-7c54-51cb-cac06c2d7df2"));

            migrationBuilder.DeleteData(
                table: "provider_config",
                keyColumn: "id",
                keyValue: new Guid("0485c08d-334a-865d-7ca4-9685eded0d5e"));

            migrationBuilder.InsertData(
                table: "pricebook_entry",
                columns: new[] { "id", "active_from", "active_to", "amount", "applies_to", "cadence", "created_at", "created_by_user_id", "currency_or_unit", "is_configurable", "is_deleted", "key", "label", "updated_at", "updated_by_user_id" },
                values: new object[] { new Guid("4e2ec970-92a2-0943-f9d0-b275378e1450"), null, null, 0.14m, "NestyStay", "Per check", new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "USD", true, false, "stripe-identity-vendor-cost", "stripe identity vendor cost", new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });

            migrationBuilder.InsertData(
                table: "provider_config",
                columns: new[] { "id", "created_at", "created_by_user_id", "encrypted_config_reference", "is_deleted", "is_primary", "kind", "provider_name", "updated_at", "updated_by_user_id" },
                values: new object[] { new Guid("bb92e267-1ae5-7f5f-5a68-1a769647db7f"), new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "vault://nestystay/ekyc/stripeidentity", false, true, "Ekyc", "StripeIdentity", new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });
        }
    }
}
