using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddP0ReversalUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_journal_manager_user_id_reversal_of_journal_id",
                table: "milestone_p0_journal",
                columns: new[] { "manager_user_id", "reversal_of_journal_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_milestone_p0_journal_manager_user_id_reversal_of_journal_id",
                table: "milestone_p0_journal");
        }
    }
}
