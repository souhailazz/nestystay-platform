using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    public partial class AddWellnessCoverageCoordinates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>("scheduled_time_zone", "milestone_wellness_visit", type: "character varying(512)", maxLength: 512, nullable: false, defaultValue: "America/Jamaica");
            migrationBuilder.AddColumn<decimal>("latitude", "milestone_wellness_officer", type: "numeric(18,2)", precision: 18, scale: 2, nullable: true);
            migrationBuilder.AddColumn<decimal>("longitude", "milestone_wellness_officer", type: "numeric(18,2)", precision: 18, scale: 2, nullable: true);
            migrationBuilder.AddColumn<decimal>("service_radius_km", "milestone_wellness_officer", type: "numeric(18,2)", precision: 18, scale: 2, nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("scheduled_time_zone", "milestone_wellness_visit");
            migrationBuilder.DropColumn("latitude", "milestone_wellness_officer");
            migrationBuilder.DropColumn("longitude", "milestone_wellness_officer");
            migrationBuilder.DropColumn("service_radius_km", "milestone_wellness_officer");
        }
    }
}
