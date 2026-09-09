using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyManagerDocumentExports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "milestone_manager_document_export",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_ids_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    error = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_manager_document_export", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_manager_document_export_milestone_user_manager_us~",
                        column: x => x.manager_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_document_export_manager_user_id_status_cr~",
                table: "milestone_manager_document_export",
                columns: new[] { "manager_user_id", "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_manager_document_export");
        }
    }
}
