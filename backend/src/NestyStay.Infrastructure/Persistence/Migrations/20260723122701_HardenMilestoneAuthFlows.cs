using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenMilestoneAuthFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_milestone_recovery_code_user_id_code",
                table: "milestone_recovery_code");

            migrationBuilder.DropIndex(
                name: "IX_milestone_auth_flow_user_id_flow_type_status",
                table: "milestone_auth_flow");

            migrationBuilder.DropIndex(
                name: "IX_milestone_auth_flow_token",
                table: "milestone_auth_flow");

            migrationBuilder.DropColumn(
                name: "code",
                table: "milestone_recovery_code");

            migrationBuilder.DropColumn(
                name: "token",
                table: "milestone_auth_flow");

            migrationBuilder.DropColumn(
                name: "code",
                table: "milestone_auth_flow");

            migrationBuilder.AddColumn<string>(
                name: "secret_salt",
                table: "milestone_recovery_code",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code_hash",
                table: "milestone_recovery_code",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code_hash",
                table: "milestone_auth_flow",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "token_hash",
                table: "milestone_auth_flow",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "secret_salt",
                table: "milestone_auth_flow",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "delivery_channel",
                table: "milestone_auth_flow",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "destination_hash",
                table: "milestone_auth_flow",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "failed_attempts",
                table: "milestone_auth_flow",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "invalidated_at",
                table: "milestone_auth_flow",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_sent_at",
                table: "milestone_auth_flow",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_destination",
                table: "milestone_auth_flow",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "request_ip_hash",
                table: "milestone_auth_flow",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_recovery_code_user_id_code_hash",
                table: "milestone_recovery_code",
                columns: new[] { "user_id", "code_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_auth_flow_token_hash",
                table: "milestone_auth_flow",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_auth_flow_request_ip_hash_created_at",
                table: "milestone_auth_flow",
                columns: new[] { "request_ip_hash", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_auth_flow_user_id_flow_type_normalized_destinatio~",
                table: "milestone_auth_flow",
                columns: new[] { "user_id", "flow_type", "normalized_destination", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_milestone_recovery_code_user_id_code_hash",
                table: "milestone_recovery_code");

            migrationBuilder.DropIndex(
                name: "IX_milestone_auth_flow_request_ip_hash_created_at",
                table: "milestone_auth_flow");

            migrationBuilder.DropIndex(
                name: "IX_milestone_auth_flow_user_id_flow_type_normalized_destinatio~",
                table: "milestone_auth_flow");

            migrationBuilder.DropIndex(
                name: "IX_milestone_auth_flow_token_hash",
                table: "milestone_auth_flow");

            migrationBuilder.DropColumn(
                name: "code_hash",
                table: "milestone_recovery_code");

            migrationBuilder.DropColumn(
                name: "secret_salt",
                table: "milestone_recovery_code");

            migrationBuilder.DropColumn(
                name: "code_hash",
                table: "milestone_auth_flow");

            migrationBuilder.DropColumn(
                name: "token_hash",
                table: "milestone_auth_flow");

            migrationBuilder.DropColumn(
                name: "secret_salt",
                table: "milestone_auth_flow");

            migrationBuilder.DropColumn(
                name: "delivery_channel",
                table: "milestone_auth_flow");

            migrationBuilder.DropColumn(
                name: "destination_hash",
                table: "milestone_auth_flow");

            migrationBuilder.DropColumn(
                name: "failed_attempts",
                table: "milestone_auth_flow");

            migrationBuilder.DropColumn(
                name: "invalidated_at",
                table: "milestone_auth_flow");

            migrationBuilder.DropColumn(
                name: "last_sent_at",
                table: "milestone_auth_flow");

            migrationBuilder.DropColumn(
                name: "normalized_destination",
                table: "milestone_auth_flow");

            migrationBuilder.DropColumn(
                name: "request_ip_hash",
                table: "milestone_auth_flow");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "milestone_recovery_code",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "token",
                table: "milestone_auth_flow",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "milestone_auth_flow",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_recovery_code_user_id_code",
                table: "milestone_recovery_code",
                columns: new[] { "user_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_auth_flow_user_id_flow_type_status",
                table: "milestone_auth_flow",
                columns: new[] { "user_id", "flow_type", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_auth_flow_token",
                table: "milestone_auth_flow",
                column: "token",
                unique: true);
        }
    }
}
