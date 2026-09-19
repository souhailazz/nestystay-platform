using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignSelfHostedStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "provider_config",
                keyColumn: "id",
                keyValue: new Guid("2051c60e-60c1-29f7-0be7-c9f83a1797a2"));

            migrationBuilder.InsertData(
                table: "provider_config",
                columns: new[] { "id", "created_at", "created_by_user_id", "encrypted_config_reference", "is_deleted", "is_primary", "kind", "provider_name", "updated_at", "updated_by_user_id" },
                values: new object[] { new Guid("78f9eed4-7f80-0ef8-32b9-404b60a9463e"), new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "vault://nestystay/storage/localpersistentobjectstorage", false, true, "Storage", "LocalPersistentObjectStorage", new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "provider_config",
                keyColumn: "id",
                keyValue: new Guid("78f9eed4-7f80-0ef8-32b9-404b60a9463e"));

            migrationBuilder.InsertData(
                table: "provider_config",
                columns: new[] { "id", "created_at", "created_by_user_id", "encrypted_config_reference", "is_deleted", "is_primary", "kind", "provider_name", "updated_at", "updated_by_user_id" },
                values: new object[] { new Guid("2051c60e-60c1-29f7-0be7-c9f83a1797a2"), new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "vault://nestystay/storage/cloudflarer2", false, true, "Storage", "CloudflareR2", new DateTimeOffset(new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });
        }
    }
}
