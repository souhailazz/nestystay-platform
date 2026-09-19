using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPmsProfessionalCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "milestone_pm_professional_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    area = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    currency = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    search_text = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_pm_professional_record", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_pm_professional_record_milestone_manager_property~",
                        column: x => x.property_id,
                        principalTable: "milestone_manager_property",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_pm_professional_record_milestone_user_manager_use~",
                        column: x => x.manager_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_pm_professional_record_milestone_user_owner_user_~",
                        column: x => x.owner_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "milestone_pm_professional_record_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", maxLength: 20000, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_pm_professional_record_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_milestone_pm_professional_record_event_milestone_pm_profess~",
                        column: x => x.record_id,
                        principalTable: "milestone_pm_professional_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_pm_professional_record_event_milestone_user_actor~",
                        column: x => x.actor_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_milestone_pm_professional_record_event_milestone_user_manag~",
                        column: x => x.manager_user_id,
                        principalTable: "milestone_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_professional_record_manager_user_id_area_statu~",
                table: "milestone_pm_professional_record",
                columns: new[] { "manager_user_id", "area", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_professional_record_manager_user_id_idempotenc~",
                table: "milestone_pm_professional_record",
                columns: new[] { "manager_user_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_professional_record_manager_user_id_property_i~",
                table: "milestone_pm_professional_record",
                columns: new[] { "manager_user_id", "property_id", "owner_user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_professional_record_owner_user_id",
                table: "milestone_pm_professional_record",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_professional_record_property_id",
                table: "milestone_pm_professional_record",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_professional_record_event_actor_user_id",
                table: "milestone_pm_professional_record_event",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_professional_record_event_manager_user_id_crea~",
                table: "milestone_pm_professional_record_event",
                columns: new[] { "manager_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_pm_professional_record_event_record_id_created_at",
                table: "milestone_pm_professional_record_event",
                columns: new[] { "record_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_pm_professional_record_event");

            migrationBuilder.DropTable(
                name: "milestone_pm_professional_record");
        }
    }
}
