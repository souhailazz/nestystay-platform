using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelerPaymentMethodProviderReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "provider_name",
                table: "milestone_traveler_payment_method",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "Stripe");

            migrationBuilder.AddColumn<string>(
                name: "provider_payment_method_reference",
                table: "milestone_traveler_payment_method",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "setup_intent_reference",
                table: "milestone_traveler_payment_method",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE milestone_traveler_payment_method
                SET provider_name = 'Stripe',
                    provider_payment_method_reference = 'legacy_' || replace(id::text, '-', ''),
                    setup_intent_reference = 'legacy_' || replace(id::text, '-', '')
                WHERE provider_payment_method_reference = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_traveler_payment_method_provider_payment_method_r~",
                table: "milestone_traveler_payment_method",
                column: "provider_payment_method_reference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_milestone_traveler_payment_method_provider_payment_method_r~",
                table: "milestone_traveler_payment_method");

            migrationBuilder.DropColumn(
                name: "provider_name",
                table: "milestone_traveler_payment_method");

            migrationBuilder.DropColumn(
                name: "provider_payment_method_reference",
                table: "milestone_traveler_payment_method");

            migrationBuilder.DropColumn(
                name: "setup_intent_reference",
                table: "milestone_traveler_payment_method");
        }
    }
}
