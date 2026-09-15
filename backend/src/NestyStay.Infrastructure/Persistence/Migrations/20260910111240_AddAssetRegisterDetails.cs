using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetRegisterDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "condition",
                table: "milestone_pm_asset",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "milestone_pm_asset",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "purchase_cost",
                table: "milestone_pm_asset",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "purchase_date",
                table: "milestone_pm_asset",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "serial_reference",
                table: "milestone_pm_asset",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "warranty_expiry",
                table: "milestone_pm_asset",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "condition",
                table: "milestone_pm_asset");

            migrationBuilder.DropColumn(
                name: "description",
                table: "milestone_pm_asset");

            migrationBuilder.DropColumn(
                name: "purchase_cost",
                table: "milestone_pm_asset");

            migrationBuilder.DropColumn(
                name: "purchase_date",
                table: "milestone_pm_asset");

            migrationBuilder.DropColumn(
                name: "serial_reference",
                table: "milestone_pm_asset");

            migrationBuilder.DropColumn(
                name: "warranty_expiry",
                table: "milestone_pm_asset");
        }
    }
}
