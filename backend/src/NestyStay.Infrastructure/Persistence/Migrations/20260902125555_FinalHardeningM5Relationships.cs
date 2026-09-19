using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestyStay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinalHardeningM5Relationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_vote_proxy_id",
                table: "milestone_manager_vote",
                column: "proxy_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_vendor_manager_user_id",
                table: "milestone_manager_vendor",
                column: "manager_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_utility_charge_invoice_id",
                table: "milestone_manager_utility_charge",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_utility_charge_owner_user_id",
                table: "milestone_manager_utility_charge",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_utility_charge_property_id",
                table: "milestone_manager_utility_charge",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_qr_scan_gate_guard_user_id",
                table: "milestone_manager_qr_scan",
                column: "gate_guard_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_qr_access_manager_user_id",
                table: "milestone_manager_qr_access",
                column: "manager_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_qr_access_owner_user_id",
                table: "milestone_manager_qr_access",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_qr_access_property_id",
                table: "milestone_manager_qr_access",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_proxy_owner_user_id",
                table: "milestone_manager_proxy",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_proxy_proxy_user_id",
                table: "milestone_manager_proxy",
                column: "proxy_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_property_owner_user_id",
                table: "milestone_manager_property",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_payment_owner_user_id",
                table: "milestone_manager_payment",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_owner_owner_user_id",
                table: "milestone_manager_owner",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_notice_target_owner_user_id",
                table: "milestone_manager_notice",
                column: "target_owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_maintenance_owner_user_id",
                table: "milestone_manager_maintenance",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_maintenance_property_id",
                table: "milestone_manager_maintenance",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_maintenance_vendor_id",
                table: "milestone_manager_maintenance",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_ledger_entry_invoice_id",
                table: "milestone_manager_ledger_entry",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_ledger_entry_owner_user_id",
                table: "milestone_manager_ledger_entry",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_ledger_entry_property_id",
                table: "milestone_manager_ledger_entry",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_invoice_property_id",
                table: "milestone_manager_invoice",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_gate_message_manager_user_id",
                table: "milestone_manager_gate_message",
                column: "manager_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_gate_message_property_id",
                table: "milestone_manager_gate_message",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_eligible_voter_owner_user_id",
                table: "milestone_manager_eligible_voter",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_document_owner_user_id",
                table: "milestone_manager_document",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_milestone_manager_document_property_id",
                table: "milestone_manager_document",
                column: "property_id");

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_document_milestone_manager_property_prope~",
                table: "milestone_manager_document",
                column: "property_id",
                principalTable: "milestone_manager_property",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_document_milestone_user_manager_user_id",
                table: "milestone_manager_document",
                column: "manager_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_document_milestone_user_owner_user_id",
                table: "milestone_manager_document",
                column: "owner_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_eligible_voter_milestone_manager_proposal~",
                table: "milestone_manager_eligible_voter",
                column: "proposal_id",
                principalTable: "milestone_manager_proposal",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_eligible_voter_milestone_user_owner_user_~",
                table: "milestone_manager_eligible_voter",
                column: "owner_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_gate_message_milestone_manager_property_p~",
                table: "milestone_manager_gate_message",
                column: "property_id",
                principalTable: "milestone_manager_property",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_gate_message_milestone_user_manager_user_~",
                table: "milestone_manager_gate_message",
                column: "manager_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_invoice_milestone_manager_property_proper~",
                table: "milestone_manager_invoice",
                column: "property_id",
                principalTable: "milestone_manager_property",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_invoice_milestone_user_manager_user_id",
                table: "milestone_manager_invoice",
                column: "manager_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_invoice_milestone_user_owner_user_id",
                table: "milestone_manager_invoice",
                column: "owner_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_invoice_line_milestone_manager_invoice_in~",
                table: "milestone_manager_invoice_line",
                column: "invoice_id",
                principalTable: "milestone_manager_invoice",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_ledger_entry_milestone_manager_invoice_in~",
                table: "milestone_manager_ledger_entry",
                column: "invoice_id",
                principalTable: "milestone_manager_invoice",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_ledger_entry_milestone_manager_property_p~",
                table: "milestone_manager_ledger_entry",
                column: "property_id",
                principalTable: "milestone_manager_property",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_ledger_entry_milestone_user_manager_user_~",
                table: "milestone_manager_ledger_entry",
                column: "manager_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_ledger_entry_milestone_user_owner_user_id",
                table: "milestone_manager_ledger_entry",
                column: "owner_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_maintenance_milestone_manager_property_pr~",
                table: "milestone_manager_maintenance",
                column: "property_id",
                principalTable: "milestone_manager_property",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_maintenance_milestone_manager_property_ve~",
                table: "milestone_manager_maintenance",
                column: "vendor_id",
                principalTable: "milestone_manager_property",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_maintenance_milestone_user_manager_user_id",
                table: "milestone_manager_maintenance",
                column: "manager_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_maintenance_milestone_user_owner_user_id",
                table: "milestone_manager_maintenance",
                column: "owner_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_notice_milestone_user_manager_user_id",
                table: "milestone_manager_notice",
                column: "manager_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_notice_milestone_user_target_owner_user_id",
                table: "milestone_manager_notice",
                column: "target_owner_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_owner_milestone_user_manager_user_id",
                table: "milestone_manager_owner",
                column: "manager_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_owner_milestone_user_owner_user_id",
                table: "milestone_manager_owner",
                column: "owner_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_payment_milestone_manager_invoice_invoice~",
                table: "milestone_manager_payment",
                column: "invoice_id",
                principalTable: "milestone_manager_invoice",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_payment_milestone_user_manager_user_id",
                table: "milestone_manager_payment",
                column: "manager_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_payment_milestone_user_owner_user_id",
                table: "milestone_manager_payment",
                column: "owner_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_property_milestone_user_manager_user_id",
                table: "milestone_manager_property",
                column: "manager_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_property_milestone_user_owner_user_id",
                table: "milestone_manager_property",
                column: "owner_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_proposal_milestone_user_manager_user_id",
                table: "milestone_manager_proposal",
                column: "manager_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_proxy_milestone_manager_proposal_proposal~",
                table: "milestone_manager_proxy",
                column: "proposal_id",
                principalTable: "milestone_manager_proposal",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_proxy_milestone_user_owner_user_id",
                table: "milestone_manager_proxy",
                column: "owner_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_proxy_milestone_user_proxy_user_id",
                table: "milestone_manager_proxy",
                column: "proxy_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_qr_access_milestone_manager_property_prop~",
                table: "milestone_manager_qr_access",
                column: "property_id",
                principalTable: "milestone_manager_property",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_qr_access_milestone_user_manager_user_id",
                table: "milestone_manager_qr_access",
                column: "manager_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_qr_access_milestone_user_owner_user_id",
                table: "milestone_manager_qr_access",
                column: "owner_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_qr_scan_milestone_manager_qr_access_qr_ac~",
                table: "milestone_manager_qr_scan",
                column: "qr_access_id",
                principalTable: "milestone_manager_qr_access",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_qr_scan_milestone_user_gate_guard_user_id",
                table: "milestone_manager_qr_scan",
                column: "gate_guard_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_utility_charge_milestone_manager_invoice_~",
                table: "milestone_manager_utility_charge",
                column: "invoice_id",
                principalTable: "milestone_manager_invoice",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_utility_charge_milestone_manager_property~",
                table: "milestone_manager_utility_charge",
                column: "property_id",
                principalTable: "milestone_manager_property",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_utility_charge_milestone_user_manager_use~",
                table: "milestone_manager_utility_charge",
                column: "manager_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_utility_charge_milestone_user_owner_user_~",
                table: "milestone_manager_utility_charge",
                column: "owner_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_vendor_milestone_user_manager_user_id",
                table: "milestone_manager_vendor",
                column: "manager_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_vote_milestone_manager_proposal_proposal_~",
                table: "milestone_manager_vote",
                column: "proposal_id",
                principalTable: "milestone_manager_proposal",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_manager_vote_milestone_manager_proxy_proxy_id",
                table: "milestone_manager_vote",
                column: "proxy_id",
                principalTable: "milestone_manager_proxy",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milestone_property_manager_milestone_user_manager_user_id",
                table: "milestone_property_manager",
                column: "manager_user_id",
                principalTable: "milestone_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_document_milestone_manager_property_prope~",
                table: "milestone_manager_document");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_document_milestone_user_manager_user_id",
                table: "milestone_manager_document");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_document_milestone_user_owner_user_id",
                table: "milestone_manager_document");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_eligible_voter_milestone_manager_proposal~",
                table: "milestone_manager_eligible_voter");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_eligible_voter_milestone_user_owner_user_~",
                table: "milestone_manager_eligible_voter");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_gate_message_milestone_manager_property_p~",
                table: "milestone_manager_gate_message");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_gate_message_milestone_user_manager_user_~",
                table: "milestone_manager_gate_message");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_invoice_milestone_manager_property_proper~",
                table: "milestone_manager_invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_invoice_milestone_user_manager_user_id",
                table: "milestone_manager_invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_invoice_milestone_user_owner_user_id",
                table: "milestone_manager_invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_invoice_line_milestone_manager_invoice_in~",
                table: "milestone_manager_invoice_line");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_ledger_entry_milestone_manager_invoice_in~",
                table: "milestone_manager_ledger_entry");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_ledger_entry_milestone_manager_property_p~",
                table: "milestone_manager_ledger_entry");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_ledger_entry_milestone_user_manager_user_~",
                table: "milestone_manager_ledger_entry");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_ledger_entry_milestone_user_owner_user_id",
                table: "milestone_manager_ledger_entry");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_maintenance_milestone_manager_property_pr~",
                table: "milestone_manager_maintenance");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_maintenance_milestone_manager_property_ve~",
                table: "milestone_manager_maintenance");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_maintenance_milestone_user_manager_user_id",
                table: "milestone_manager_maintenance");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_maintenance_milestone_user_owner_user_id",
                table: "milestone_manager_maintenance");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_notice_milestone_user_manager_user_id",
                table: "milestone_manager_notice");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_notice_milestone_user_target_owner_user_id",
                table: "milestone_manager_notice");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_owner_milestone_user_manager_user_id",
                table: "milestone_manager_owner");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_owner_milestone_user_owner_user_id",
                table: "milestone_manager_owner");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_payment_milestone_manager_invoice_invoice~",
                table: "milestone_manager_payment");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_payment_milestone_user_manager_user_id",
                table: "milestone_manager_payment");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_payment_milestone_user_owner_user_id",
                table: "milestone_manager_payment");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_property_milestone_user_manager_user_id",
                table: "milestone_manager_property");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_property_milestone_user_owner_user_id",
                table: "milestone_manager_property");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_proposal_milestone_user_manager_user_id",
                table: "milestone_manager_proposal");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_proxy_milestone_manager_proposal_proposal~",
                table: "milestone_manager_proxy");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_proxy_milestone_user_owner_user_id",
                table: "milestone_manager_proxy");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_proxy_milestone_user_proxy_user_id",
                table: "milestone_manager_proxy");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_qr_access_milestone_manager_property_prop~",
                table: "milestone_manager_qr_access");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_qr_access_milestone_user_manager_user_id",
                table: "milestone_manager_qr_access");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_qr_access_milestone_user_owner_user_id",
                table: "milestone_manager_qr_access");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_qr_scan_milestone_manager_qr_access_qr_ac~",
                table: "milestone_manager_qr_scan");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_qr_scan_milestone_user_gate_guard_user_id",
                table: "milestone_manager_qr_scan");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_utility_charge_milestone_manager_invoice_~",
                table: "milestone_manager_utility_charge");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_utility_charge_milestone_manager_property~",
                table: "milestone_manager_utility_charge");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_utility_charge_milestone_user_manager_use~",
                table: "milestone_manager_utility_charge");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_utility_charge_milestone_user_owner_user_~",
                table: "milestone_manager_utility_charge");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_vendor_milestone_user_manager_user_id",
                table: "milestone_manager_vendor");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_vote_milestone_manager_proposal_proposal_~",
                table: "milestone_manager_vote");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_manager_vote_milestone_manager_proxy_proxy_id",
                table: "milestone_manager_vote");

            migrationBuilder.DropForeignKey(
                name: "FK_milestone_property_manager_milestone_user_manager_user_id",
                table: "milestone_property_manager");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_vote_proxy_id",
                table: "milestone_manager_vote");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_vendor_manager_user_id",
                table: "milestone_manager_vendor");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_utility_charge_invoice_id",
                table: "milestone_manager_utility_charge");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_utility_charge_owner_user_id",
                table: "milestone_manager_utility_charge");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_utility_charge_property_id",
                table: "milestone_manager_utility_charge");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_qr_scan_gate_guard_user_id",
                table: "milestone_manager_qr_scan");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_qr_access_manager_user_id",
                table: "milestone_manager_qr_access");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_qr_access_owner_user_id",
                table: "milestone_manager_qr_access");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_qr_access_property_id",
                table: "milestone_manager_qr_access");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_proxy_owner_user_id",
                table: "milestone_manager_proxy");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_proxy_proxy_user_id",
                table: "milestone_manager_proxy");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_property_owner_user_id",
                table: "milestone_manager_property");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_payment_owner_user_id",
                table: "milestone_manager_payment");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_owner_owner_user_id",
                table: "milestone_manager_owner");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_notice_target_owner_user_id",
                table: "milestone_manager_notice");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_maintenance_owner_user_id",
                table: "milestone_manager_maintenance");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_maintenance_property_id",
                table: "milestone_manager_maintenance");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_maintenance_vendor_id",
                table: "milestone_manager_maintenance");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_ledger_entry_invoice_id",
                table: "milestone_manager_ledger_entry");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_ledger_entry_owner_user_id",
                table: "milestone_manager_ledger_entry");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_ledger_entry_property_id",
                table: "milestone_manager_ledger_entry");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_invoice_property_id",
                table: "milestone_manager_invoice");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_gate_message_manager_user_id",
                table: "milestone_manager_gate_message");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_gate_message_property_id",
                table: "milestone_manager_gate_message");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_eligible_voter_owner_user_id",
                table: "milestone_manager_eligible_voter");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_document_owner_user_id",
                table: "milestone_manager_document");

            migrationBuilder.DropIndex(
                name: "IX_milestone_manager_document_property_id",
                table: "milestone_manager_document");
        }
    }
}
