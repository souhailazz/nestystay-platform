using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations;

/// <summary>
/// Database-enforced invariants for the P0 accounting model. The preceding
/// EF migration owns the tables and indexes; this migration adds guards that
/// EF cannot express (balanced journals, one-sided lines and immutable rows).
/// </summary>
public partial class AddPropertyManagerP0Foundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            name: "ck_p0_journal_balanced",
            table: "milestone_p0_journal",
            sql: "total_debit = total_credit AND total_debit > 0");

        migrationBuilder.AddCheckConstraint(
            name: "ck_p0_journal_line_one_side",
            table: "milestone_p0_journal_line",
            sql: "(debit > 0 AND credit = 0) OR (credit > 0 AND debit = 0)");

        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION reject_p0_journal_mutation()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                RAISE EXCEPTION 'Posted P0 journal rows are immutable; use a reversal';
            END;
            $$;
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER reject_p0_journal_update_delete
            BEFORE UPDATE OR DELETE ON milestone_p0_journal
            FOR EACH ROW EXECUTE FUNCTION reject_p0_journal_mutation();
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER reject_p0_journal_line_update_delete
            BEFORE UPDATE OR DELETE ON milestone_p0_journal_line
            FOR EACH ROW EXECUTE FUNCTION reject_p0_journal_mutation();
            """);

        // Reconciliation, statement and decision history are append-only as
        // well.  Corrections are represented by a new event or a reversal;
        // operators must never be able to erase the audit trail.
        migrationBuilder.Sql("""
            DO $$
            DECLARE table_name text;
            BEGIN
                FOREACH table_name IN ARRAY ARRAY[
                    'milestone_p0_reconciliation',
                    'milestone_p0_statement_snapshot',
                    'milestone_p0_owner_lifecycle_event',
                    'milestone_manager_property_assignment_history',
                    'milestone_p0_payout_event',
                    'milestone_p0_approval_event',
                    'milestone_p0_staff_event'
                ] LOOP
                    EXECUTE format(
                        'CREATE TRIGGER reject_%s_update_delete BEFORE UPDATE OR DELETE ON %I FOR EACH ROW EXECUTE FUNCTION reject_p0_journal_mutation()',
                        replace(table_name, 'milestone_', ''), table_name);
                END LOOP;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS reject_p0_journal_line_update_delete ON milestone_p0_journal_line;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS reject_p0_journal_update_delete ON milestone_p0_journal;");
        migrationBuilder.Sql("""
            DO $$
            DECLARE table_name text;
            BEGIN
                FOREACH table_name IN ARRAY ARRAY[
                    'milestone_p0_reconciliation',
                    'milestone_p0_statement_snapshot',
                    'milestone_p0_owner_lifecycle_event',
                    'milestone_manager_property_assignment_history',
                    'milestone_p0_payout_event',
                    'milestone_p0_approval_event',
                    'milestone_p0_staff_event'
                ] LOOP
                    EXECUTE format('DROP TRIGGER IF EXISTS reject_%s_update_delete ON %I', replace(table_name, 'milestone_', ''), table_name);
                END LOOP;
            END $$;
            """);
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS reject_p0_journal_mutation();");
        migrationBuilder.DropCheckConstraint(name: "ck_p0_journal_line_one_side", table: "milestone_p0_journal_line");
        migrationBuilder.DropCheckConstraint(name: "ck_p0_journal_balanced", table: "milestone_p0_journal");
    }
}
