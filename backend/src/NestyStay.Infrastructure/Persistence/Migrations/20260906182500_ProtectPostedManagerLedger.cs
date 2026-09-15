using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NestyStay.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NestyStayDbContext))]
[Migration("20260906182500_ProtectPostedManagerLedger")]
public sealed class ProtectPostedManagerLedger : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE FUNCTION reject_manager_ledger_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
          RAISE EXCEPTION 'Posted owner ledger entries are immutable; post a reversal or adjustment'
            USING ERRCODE = '23514';
        END $$;
        CREATE TRIGGER protect_manager_ledger
          BEFORE UPDATE OR DELETE ON milestone_manager_ledger_entry
          FOR EACH ROW EXECUTE FUNCTION reject_manager_ledger_mutation();
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP TRIGGER protect_manager_ledger ON milestone_manager_ledger_entry;
        DROP FUNCTION reject_manager_ledger_mutation();
        """);
}
