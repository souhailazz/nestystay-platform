using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyManagerSubscriptionLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "auto_renew",
                table: "milestone_property_manager",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "billing_provider_status",
                table: "milestone_property_manager",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "milestone_property_manager",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "pending_subscription_effective_at",
                table: "milestone_property_manager",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_subscription_tier",
                table: "milestone_property_manager",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "auto_renew",
                table: "milestone_property_manager");

            migrationBuilder.DropColumn(
                name: "billing_provider_status",
                table: "milestone_property_manager");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "milestone_property_manager");

            migrationBuilder.DropColumn(
                name: "pending_subscription_effective_at",
                table: "milestone_property_manager");

            migrationBuilder.DropColumn(
                name: "pending_subscription_tier",
                table: "milestone_property_manager");
        }
    }
}
