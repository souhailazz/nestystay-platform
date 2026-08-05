using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderWebhookEventTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "event_id",
                table: "provider_event",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "payload_sha256",
                table: "provider_event",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "processed_at",
                table: "provider_event",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "processing_result",
                table: "provider_event",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "provider_event",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "Received");

            migrationBuilder.AddColumn<Guid>(
                name: "subject_id",
                table: "provider_event",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject_type",
                table: "provider_event",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE provider_event SET event_id = id::text WHERE event_id = '';");
            migrationBuilder.Sql("UPDATE provider_event SET status = 'Processed' WHERE status = '';");

            migrationBuilder.CreateIndex(
                name: "IX_provider_event_kind_provider_name_event_id",
                table: "provider_event",
                columns: new[] { "kind", "provider_name", "event_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_provider_event_kind_provider_name_event_id",
                table: "provider_event");

            migrationBuilder.DropColumn(
                name: "event_id",
                table: "provider_event");

            migrationBuilder.DropColumn(
                name: "payload_sha256",
                table: "provider_event");

            migrationBuilder.DropColumn(
                name: "processed_at",
                table: "provider_event");

            migrationBuilder.DropColumn(
                name: "processing_result",
                table: "provider_event");

            migrationBuilder.DropColumn(
                name: "status",
                table: "provider_event");

            migrationBuilder.DropColumn(
                name: "subject_id",
                table: "provider_event");

            migrationBuilder.DropColumn(
                name: "subject_type",
                table: "provider_event");
        }
    }
}
