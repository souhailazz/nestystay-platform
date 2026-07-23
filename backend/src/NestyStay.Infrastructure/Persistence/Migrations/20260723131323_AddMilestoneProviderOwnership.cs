using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMilestoneProviderOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "owner_user_id",
                table: "milestone_directory_provider",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_directory_provider_owner_user_id",
                table: "milestone_directory_provider",
                column: "owner_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_milestone_directory_provider_owner_user_id",
                table: "milestone_directory_provider");

            migrationBuilder.DropColumn(
                name: "owner_user_id",
                table: "milestone_directory_provider");
        }
    }
}
