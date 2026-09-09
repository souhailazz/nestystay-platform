using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectoryProviderWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "accessibility_info",
                table: "milestone_directory_provider",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "holiday_closures_json",
                table: "milestone_directory_provider",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "promotions_json",
                table: "milestone_directory_provider",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "weekly_hours_json",
                table: "milestone_directory_provider",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "milestone_directory_quote",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    preferred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    budget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    response_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    responded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_directory_quote", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_directory_quote_milestone_directory_provider_prov~",
                        column: x => x.provider_id,
                        principalTable: "milestone_directory_provider",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_directory_quote_milestone_user_requester_user_id",
                        column: x => x.requester_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_directory_review",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    provider_response = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    responded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_directory_review", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_directory_review_milestone_directory_provider_pro~",
                        column: x => x.provider_id,
                        principalTable: "milestone_directory_provider",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_directory_review_milestone_user_reviewer_user_id",
                        column: x => x.reviewer_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_directory_quote_provider_id_status_created_at",
                table: "milestone_directory_quote",
                columns: new[] { "provider_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_directory_quote_requester_user_id_created_at",
                table: "milestone_directory_quote",
                columns: new[] { "requester_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_directory_review_provider_id_reviewer_user_id",
                table: "milestone_directory_review",
                columns: new[] { "provider_id", "reviewer_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_directory_review_provider_id_status_created_at",
                table: "milestone_directory_review",
                columns: new[] { "provider_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_directory_review_reviewer_user_id",
                table: "milestone_directory_review",
                column: "reviewer_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_directory_provider_milestone_user_owner_user_id",
                table: "milestone_directory_provider",
                column: "owner_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_milestone_directory_provider_milestone_user_owner_user_id",
                table: "milestone_directory_provider");

            migrationBuilder.DropTable(
                name: "milestone_directory_quote");

            migrationBuilder.DropTable(
                name: "milestone_directory_review");

            migrationBuilder.DropColumn(
                name: "accessibility_info",
                table: "milestone_directory_provider");

            migrationBuilder.DropColumn(
                name: "holiday_closures_json",
                table: "milestone_directory_provider");

            migrationBuilder.DropColumn(
                name: "promotions_json",
                table: "milestone_directory_provider");

            migrationBuilder.DropColumn(
                name: "weekly_hours_json",
                table: "milestone_directory_provider");
        }
    }
}
