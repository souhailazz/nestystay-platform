using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NestyStay.Infrastructure.Persistence;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NestyStayDbContext))]
[Migration("20260830150405_AlignSignedContractDomainBadgeSeeds")]
public partial class AlignSignedContractDomainBadgeSeeds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE badge_definition SET unlocks_json = '[\"Listings\",\"Calendar\",\"Messaging\",\"QR code access\",\"97% payout\"]' WHERE key = 'host-free';");
        migrationBuilder.Sql("UPDATE badge_definition SET unlocks_json = '[\"Trusted badge\",\"Trades directory\",\"Search boost\",\"Referral program\"]' WHERE key = 'host-trusted';");
        migrationBuilder.Sql("UPDATE badge_definition SET unlocks_json = '[\"Police directory\",\"Wellness visits\",\"In-person guest ID check\",\"Drive-by property patrol\",\"Wellness badge\",\"Police Verified filter\"]' WHERE key = 'host-wellness';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE badge_definition SET unlocks_json = '[\"Listings\",\"Calendar\",\"Messaging\",\"QR\",\"Stripe\",\"InsuraGuest\",\"97% payout\"]' WHERE key = 'host-free';");
        migrationBuilder.Sql("UPDATE badge_definition SET unlocks_json = '[\"Trades directory\",\"Search boost\",\"Referral program\"]' WHERE key = 'host-trusted';");
        migrationBuilder.Sql("UPDATE badge_definition SET unlocks_json = '[\"Police directory\",\"Wellness visits\",\"Wellness badge\",\"Security verified filter\"]' WHERE key = 'host-wellness';");
    }
}
