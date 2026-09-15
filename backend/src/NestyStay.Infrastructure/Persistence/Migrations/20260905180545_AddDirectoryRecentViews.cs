using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    public partial class AddDirectoryRecentViews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>("emergency_available", "milestone_directory_provider", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>("opening_hours", "milestone_directory_provider", type: "character varying(512)", maxLength: 512, nullable: true);
            migrationBuilder.AddColumn<decimal>("service_radius_km", "milestone_directory_provider", type: "numeric(18,2)", precision: 18, scale: 2, nullable: true);
            migrationBuilder.AddColumn<string>("services_json", "milestone_directory_provider", type: "jsonb", maxLength: 20000, nullable: false, defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "milestone_directory_recent_view",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    viewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_milestone_directory_recent_view", x => x.id));

            migrationBuilder.CreateIndex("IX_milestone_directory_recent_view_user_id_provider_id", "milestone_directory_recent_view", new[] { "user_id", "provider_id" }, unique: true);
            migrationBuilder.CreateIndex("IX_milestone_directory_recent_view_user_id_viewed_at", "milestone_directory_recent_view", new[] { "user_id", "viewed_at" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("milestone_directory_recent_view");
            migrationBuilder.DropColumn("emergency_available", "milestone_directory_provider");
            migrationBuilder.DropColumn("opening_hours", "milestone_directory_provider");
            migrationBuilder.DropColumn("service_radius_km", "milestone_directory_provider");
            migrationBuilder.DropColumn("services_json", "milestone_directory_provider");
        }
    }
}
