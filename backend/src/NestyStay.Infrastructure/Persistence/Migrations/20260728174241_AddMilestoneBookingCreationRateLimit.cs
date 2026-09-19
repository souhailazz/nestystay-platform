using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMilestoneBookingCreationRateLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "milestone_booking_creation_rate_limit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    window_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    window_ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    request_count = table.Column<int>(type: "integer", nullable: false),
                    last_request_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_booking_creation_rate_limit", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_booking_creation_rate_limit_guest_user_id",
                table: "milestone_booking_creation_rate_limit",
                column: "guest_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_booking_creation_rate_limit");
        }
    }
}
