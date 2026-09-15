using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM1AuthSessionsAndPasskeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "milestone_passkey_challenge",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    challenge_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    challenge = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    purpose = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    options_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_passkey_challenge", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_passkey_challenge_milestone_user_user_id",
                        column: x => x.user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_passkey_credential",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_id_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    credential_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    public_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    sign_count = table.Column<long>(type: "bigint", nullable: false),
                    transports_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    label = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_passkey_credential", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_passkey_credential_milestone_user_user_id",
                        column: x => x.user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_user_session",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_id_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    device_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    browser = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    approximate_location = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ip_address_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    trusted_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_user_session", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_user_session_milestone_user_user_id",
                        column: x => x.user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_passkey_challenge_challenge_id",
                table: "milestone_passkey_challenge",
                column: "challenge_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_passkey_challenge_user_id",
                table: "milestone_passkey_challenge",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_passkey_credential_credential_id_hash",
                table: "milestone_passkey_credential",
                column: "credential_id_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_passkey_credential_user_id_revoked_at",
                table: "milestone_passkey_credential",
                columns: new[] { "user_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_user_session_user_id_last_used_at",
                table: "milestone_user_session",
                columns: new[] { "user_id", "last_used_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_user_session_user_id_token_id_hash",
                table: "milestone_user_session",
                columns: new[] { "user_id", "token_id_hash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_passkey_challenge");

            migrationBuilder.DropTable(
                name: "milestone_passkey_credential");

            migrationBuilder.DropTable(
                name: "milestone_user_session");
        }
    }
}
