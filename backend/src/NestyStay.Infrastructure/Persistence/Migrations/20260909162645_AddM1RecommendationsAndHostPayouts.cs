using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM1RecommendationsAndHostPayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "milestone_host_payout",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    platform_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    eligible_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    settlement_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_host_payout", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_host_payout_milestone_user_host_user_id",
                        column: x => x.host_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_traveler_preference",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    preferred_parish = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    maximum_nightly_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    preferred_badge_level = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    preferred_highlights_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_traveler_preference", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_traveler_preference_milestone_user_user_id",
                        column: x => x.user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_traveler_recommendation_interaction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_traveler_recommendation_interaction", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_traveler_recommendation_interaction_milestone_use~",
                        column: x => x.user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_host_payout_booking_id",
                table: "milestone_host_payout",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_host_payout_host_user_id_status_created_at",
                table: "milestone_host_payout",
                columns: new[] { "host_user_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_traveler_preference_user_id",
                table: "milestone_traveler_preference",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_traveler_recommendation_interaction_user_id_prop~1",
                table: "milestone_traveler_recommendation_interaction",
                columns: new[] { "user_id", "property_id", "action", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_traveler_recommendation_interaction_user_id_prope~",
                table: "milestone_traveler_recommendation_interaction",
                columns: new[] { "user_id", "property_id", "action" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_host_payout");

            migrationBuilder.DropTable(
                name: "milestone_traveler_preference");

            migrationBuilder.DropTable(
                name: "milestone_traveler_recommendation_interaction");
        }
    }
}
