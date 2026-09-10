using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationCancellationIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "milestone_pm_reservation_event",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_reservation_event_manager_user_id_idempotency_~",
                table: "milestone_pm_reservation_event",
                columns: new[] { "manager_user_id", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_milestone_pm_reservation_event_manager_user_id_idempotency_~",
                table: "milestone_pm_reservation_event");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "milestone_pm_reservation_event");
        }
    }
}
