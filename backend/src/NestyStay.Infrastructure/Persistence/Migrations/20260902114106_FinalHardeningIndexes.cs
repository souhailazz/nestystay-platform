using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinalHardeningIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_milestone_wellness_visit_host_user_id_scheduled_at",
                table: "milestone_wellness_visit",
                columns: new[] { "host_user_id", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_payment_invoice_id",
                table: "milestone_manager_payment",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_invoice_owner_user_id_due_date",
                table: "milestone_manager_invoice",
                columns: new[] { "owner_user_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_conversation_participant_user_id",
                table: "milestone_conversation_participant",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_booking_guest_user_id",
                table: "milestone_booking",
                column: "guest_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_booking_host_user_id",
                table: "milestone_booking",
                column: "host_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_milestone_wellness_visit_host_user_id_scheduled_at",
                table: "milestone_wellness_visit");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_payment_invoice_id",
                table: "milestone_manager_payment");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_invoice_owner_user_id_due_date",
                table: "milestone_manager_invoice");

            migrationBuilder.DropIndex(
                name: "IX_milestone_conversation_participant_user_id",
                table: "milestone_conversation_participant");

            migrationBuilder.DropIndex(
                name: "IX_milestone_booking_guest_user_id",
                table: "milestone_booking");

            migrationBuilder.DropIndex(
                name: "IX_milestone_booking_host_user_id",
                table: "milestone_booking");
        }
    }
}
