using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddP0OwnerBillingMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "billing_metadata_json",
                table: "milestone_p0_owner_profile",
                type: "jsonb",
                maxLength: 20000,
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "payment_provider_customer_reference",
                table: "milestone_p0_owner_profile",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "billing_metadata_json",
                table: "milestone_p0_owner_profile");

            migrationBuilder.DropColumn(
                name: "payment_provider_customer_reference",
                table: "milestone_p0_owner_profile");
        }
    }
}
