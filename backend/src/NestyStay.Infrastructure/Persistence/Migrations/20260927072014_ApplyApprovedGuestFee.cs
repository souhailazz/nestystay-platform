using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ApplyApprovedGuestFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                value: 10m);

            migrationBuilder.UpdateData(
                table: "pricebook_entry",
                keyColumn: "id",
                keyValue: new Guid("8c5b02ff-6832-3fe3-d13d-434e74947d1c"),
                column: "amount",
                value: 10m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                keyValue: new Guid("8c5b02ff-6832-3fe3-d13d-434e74947d1c"),
                column: "amount",
                value: 9m);
        }
    }
}
