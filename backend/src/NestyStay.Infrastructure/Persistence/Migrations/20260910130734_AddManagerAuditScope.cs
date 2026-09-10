using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerAuditScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "manager_user_id",
                table: "milestone_audit_event",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_audit_event_manager_user_id_created_at",
                table: "milestone_audit_event",
                columns: new[] { "manager_user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_milestone_audit_event_manager_user_id_created_at",
                table: "milestone_audit_event");

            migrationBuilder.DropColumn(
                name: "manager_user_id",
                table: "milestone_audit_event");
        }
    }
}
