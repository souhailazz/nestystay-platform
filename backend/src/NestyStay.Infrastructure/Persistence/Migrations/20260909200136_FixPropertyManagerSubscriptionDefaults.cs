using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPropertyManagerSubscriptionDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rows created by the first additive migration pre-date these CLR defaults.
            // Normalize them before changing the database defaults so every existing
            // manager gets the same safe local/test lifecycle state.
            migrationBuilder.Sql("UPDATE milestone_property_manager SET auto_renew = TRUE WHERE auto_renew = FALSE");
            migrationBuilder.Sql("UPDATE milestone_property_manager SET billing_provider_status = 'LOCAL_TEST' WHERE billing_provider_status IS NULL OR billing_provider_status = ''");

            migrationBuilder.AlterColumn<string>(
                name: "billing_provider_status",
                table: "milestone_property_manager",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "LOCAL_TEST",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<bool>(
                name: "auto_renew",
                table: "milestone_property_manager",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "billing_provider_status",
                table: "milestone_property_manager",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldDefaultValue: "LOCAL_TEST");

            migrationBuilder.AlterColumn<bool>(
                name: "auto_renew",
                table: "milestone_property_manager",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);
        }
    }
}
