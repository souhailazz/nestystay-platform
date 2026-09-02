using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignSelfHostedProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "provider_config",
                keyColumn: "id",
                keyValue: new Guid("52c17d79-74b3-c176-6bb8-bc6887d3fadb"));

            migrationBuilder.InsertData(
                table: "provider_config",
                columns: new[] { "id", "created_at", "created_by_user_id", "encrypted_config_reference", "is_deleted", "is_primary", "kind", "provider_name", "updated_at", "updated_by_user_id" },
                values: new object[] { new Guid("04da9ac7-7bf7-7dc3-4bff-0d43fee71f12"), new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "vault://nestystay/notification/brevotransactional", false, true, "Notification", "BrevoTransactional", new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "provider_config",
                keyColumn: "id",
                keyValue: new Guid("04da9ac7-7bf7-7dc3-4bff-0d43fee71f12"));

            migrationBuilder.InsertData(
                table: "provider_config",
                columns: new[] { "id", "created_at", "created_by_user_id", "encrypted_config_reference", "is_deleted", "is_primary", "kind", "provider_name", "updated_at", "updated_by_user_id" },
                values: new object[] { new Guid("52c17d79-74b3-c176-6bb8-bc6887d3fadb"), new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "vault://nestystay/notification/awssestwiliofirebase", false, true, "Notification", "AwsSesTwilioFirebase", new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });
        }
    }
}
