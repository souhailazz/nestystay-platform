using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddP0StatementIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "milestone_p0_statement_snapshot",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_p0_statement_snapshot_manager_user_id_idempotency~",
                table: "milestone_p0_statement_snapshot",
                columns: new[] { "manager_user_id", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_milestone_p0_statement_snapshot_manager_user_id_idempotency~",
                table: "milestone_p0_statement_snapshot");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "milestone_p0_statement_snapshot");
        }
    }
}
