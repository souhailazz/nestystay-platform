using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMilestoneBookingRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payment_refund_reference",
                table: "milestone_booking",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refund_reason",
                table: "milestone_booking",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "refunded_amount",
                table: "milestone_booking",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "refunded_at",
                table: "milestone_booking",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payment_refund_reference",
                table: "milestone_booking");

            migrationBuilder.DropColumn(
                name: "refund_reason",
                table: "milestone_booking");

            migrationBuilder.DropColumn(
                name: "refunded_amount",
                table: "milestone_booking");

            migrationBuilder.DropColumn(
                name: "refunded_at",
                table: "milestone_booking");
        }
    }
}
