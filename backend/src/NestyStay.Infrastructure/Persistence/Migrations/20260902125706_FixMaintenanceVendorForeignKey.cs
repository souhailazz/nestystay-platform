using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixMaintenanceVendorForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_maintenance_milestone_manager_property_ve~",
                table: "milestone_manager_maintenance");

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_maintenance_milestone_manager_vendor_vend~",
                table: "milestone_manager_maintenance",
                column: "vendor_id",
                principalTable: "milestone_manager_vendor",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_maintenance_milestone_manager_vendor_vend~",
                table: "milestone_manager_maintenance");

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_maintenance_milestone_manager_property_ve~",
                table: "milestone_manager_maintenance",
                column: "vendor_id",
                principalTable: "milestone_manager_property",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
