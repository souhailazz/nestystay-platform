using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM1CalendarSyncMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_at",
                table: "milestone_calendar_feed",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_sync_at",
                table: "milestone_calendar_feed",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "milestone_calendar_sync_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    feed_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    block_count = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_calendar_sync_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_calendar_sync_event_milestone_calendar_feed_feed_~",
                        column: x => x.feed_id,
                        principalTable: "milestone_calendar_feed",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_calendar_sync_event_feed_id_started_at",
                table: "milestone_calendar_sync_event",
                columns: new[] { "feed_id", "started_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_calendar_sync_event");

            migrationBuilder.DropColumn(
                name: "last_modified_at",
                table: "milestone_calendar_feed");

            migrationBuilder.DropColumn(
                name: "next_sync_at",
                table: "milestone_calendar_feed");
        }
    }
}
