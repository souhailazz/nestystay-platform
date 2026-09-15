using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddP0PayoutCreatedByLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_payout_batch_created_by_user_id",
                table: "milestone_p0_payout_batch",
                column: "created_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_p0_payout_batch_milestone_user_created_by_user_id",
                table: "milestone_p0_payout_batch",
                column: "created_by_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_milestone_p0_payout_batch_milestone_user_created_by_user_id",
                table: "milestone_p0_payout_batch");

            migrationBuilder.DropIndex(
                name: "IX_milestone_p0_payout_batch_created_by_user_id",
                table: "milestone_p0_payout_batch");
        }
    }
}
