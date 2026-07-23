using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageAttachmentUploadVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "scan_checked_at",
                table: "milestone_message_attachment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scan_provider_name",
                table: "milestone_message_attachment",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scan_status",
                table: "milestone_message_attachment",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "PendingScan");

            migrationBuilder.AddColumn<string>(
                name: "sha256_hash",
                table: "milestone_message_attachment",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "storage_provider_name",
                table: "milestone_message_attachment",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "Cloudflare R2");

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_object_key",
                table: "milestone_message_attachment",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "uploaded_size_bytes",
                table: "milestone_message_attachment",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verified_content_type",
                table: "milestone_message_attachment",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE milestone_message_attachment
                SET storage_provider_name = 'Cloudflare R2',
                    scan_status = CASE
                        WHEN status IN ('Uploaded', 'Attached') THEN 'Clean'
                        ELSE 'PendingScan'
                    END,
                    uploaded_size_bytes = CASE
                        WHEN status IN ('Uploaded', 'Attached') THEN size_bytes
                        ELSE uploaded_size_bytes
                    END,
                    verified_content_type = CASE
                        WHEN status IN ('Uploaded', 'Attached') THEN content_type
                        ELSE verified_content_type
                    END,
                    scan_provider_name = CASE
                        WHEN status IN ('Uploaded', 'Attached') THEN 'Legacy migration backfill'
                        ELSE scan_provider_name
                    END,
                    scan_checked_at = CASE
                        WHEN status IN ('Uploaded', 'Attached') THEN updated_at
                        ELSE scan_checked_at
                    END
                WHERE storage_provider_name = '' OR scan_status = 'PendingScan';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "scan_checked_at",
                table: "milestone_message_attachment");

            migrationBuilder.DropColumn(
                name: "scan_provider_name",
                table: "milestone_message_attachment");

            migrationBuilder.DropColumn(
                name: "scan_status",
                table: "milestone_message_attachment");

            migrationBuilder.DropColumn(
                name: "sha256_hash",
                table: "milestone_message_attachment");

            migrationBuilder.DropColumn(
                name: "storage_provider_name",
                table: "milestone_message_attachment");

            migrationBuilder.DropColumn(
                name: "thumbnail_object_key",
                table: "milestone_message_attachment");

            migrationBuilder.DropColumn(
                name: "uploaded_size_bytes",
                table: "milestone_message_attachment");

            migrationBuilder.DropColumn(
                name: "verified_content_type",
                table: "milestone_message_attachment");
        }
    }
}
