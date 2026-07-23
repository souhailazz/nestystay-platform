using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMilestoneUserProfilePhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "milestone_user_profile_photo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    safe_file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    upload_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    storage_provider_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    verified_content_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    uploaded_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    sha256_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    scan_status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    scan_provider_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    scan_checked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    thumbnail_object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    upload_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_user_profile_photo", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_user_profile_photo_object_key",
                table: "milestone_user_profile_photo",
                column: "object_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_user_profile_photo_user_id_status_is_current",
                table: "milestone_user_profile_photo",
                columns: new[] { "user_id", "status", "is_current" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_user_profile_photo");
        }
    }
}
