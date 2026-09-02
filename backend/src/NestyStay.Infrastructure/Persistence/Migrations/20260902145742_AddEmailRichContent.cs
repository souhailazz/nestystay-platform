using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailRichContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "html_body",
                table: "notification_queue_item",
                type: "character varying(100000)",
                maxLength: 100000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reply_to_email",
                table: "notification_queue_item",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reply_to_name",
                table: "notification_queue_item",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "text_body",
                table: "notification_queue_item",
                type: "character varying(100000)",
                maxLength: 100000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "html_body",
                table: "notification_queue_item");

            migrationBuilder.DropColumn(
                name: "reply_to_email",
                table: "notification_queue_item");

            migrationBuilder.DropColumn(
                name: "reply_to_name",
                table: "notification_queue_item");

            migrationBuilder.DropColumn(
                name: "text_body",
                table: "notification_queue_item");
        }
    }
}
