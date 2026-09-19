using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDeliveryOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "body",
                table: "notification_queue_item",
                type: "character varying(100000)",
                maxLength: 100000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AddColumn<int>(
                name: "attempt_count",
                table: "notification_queue_item",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "delivery_status",
                table: "notification_queue_item",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "PENDING");

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "notification_queue_item",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_error",
                table: "notification_queue_item",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_attempt_at",
                table: "notification_queue_item",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_message_id",
                table: "notification_queue_item",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_queue_item_channel_delivery_status_next_attemp~",
                table: "notification_queue_item",
                columns: new[] { "channel", "delivery_status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_queue_item_idempotency_key",
                table: "notification_queue_item",
                column: "idempotency_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_queue_item_channel_delivery_status_next_attemp~",
                table: "notification_queue_item");

            migrationBuilder.DropIndex(
                name: "IX_notification_queue_item_idempotency_key",
                table: "notification_queue_item");

            migrationBuilder.DropColumn(
                name: "attempt_count",
                table: "notification_queue_item");

            migrationBuilder.DropColumn(
                name: "delivery_status",
                table: "notification_queue_item");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "notification_queue_item");

            migrationBuilder.DropColumn(
                name: "last_error",
                table: "notification_queue_item");

            migrationBuilder.DropColumn(
                name: "next_attempt_at",
                table: "notification_queue_item");

            migrationBuilder.DropColumn(
                name: "provider_message_id",
                table: "notification_queue_item");

            migrationBuilder.AlterColumn<string>(
                name: "body",
                table: "notification_queue_item",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100000)",
                oldMaxLength: 100000);
        }
    }
}
