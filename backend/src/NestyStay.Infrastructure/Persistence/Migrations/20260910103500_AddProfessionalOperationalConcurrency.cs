using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessionalOperationalConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "milestone_pm_owner_block",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "milestone_pm_owner_block",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "row_version",
                table: "milestone_pm_inspection_record",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "row_version",
                table: "milestone_pm_incident",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "row_version",
                table: "milestone_pm_asset",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "milestone_pm_owner_block");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "milestone_pm_owner_block");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "milestone_pm_inspection_record");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "milestone_pm_incident");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "milestone_pm_asset");
        }
    }
}
