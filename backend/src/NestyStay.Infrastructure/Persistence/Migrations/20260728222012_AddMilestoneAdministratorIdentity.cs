using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMilestoneAdministratorIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "admin_permissions_json",
                table: "milestone_user",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "milestone_user",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_user_status",
                table: "milestone_user",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_milestone_user_status",
                table: "milestone_user");

            migrationBuilder.DropColumn(
                name: "admin_permissions_json",
                table: "milestone_user");

            migrationBuilder.DropColumn(
                name: "status",
                table: "milestone_user");
        }
    }
}
