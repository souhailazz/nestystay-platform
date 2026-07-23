using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMilestoneTotpEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pending_two_factor_enrollment_id",
                table: "milestone_user",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "pending_two_factor_expires_at",
                table: "milestone_user",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "pending_two_factor_secret",
                table: "milestone_user",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pending_two_factor_enrollment_id",
                table: "milestone_user");

            migrationBuilder.DropColumn(
                name: "pending_two_factor_expires_at",
                table: "milestone_user");

            migrationBuilder.DropColumn(
                name: "pending_two_factor_secret",
                table: "milestone_user");
        }
    }
}
