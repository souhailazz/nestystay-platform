import { useState, useEffect } from "react";
import { Plus } from "lucide-react";
import { api, formatMoney, type PhaseTwoPricebookItem, type Campaign } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import { announceFeedback } from "../../lib/feedback";
import { EmptyState } from "../../components/ui/EmptyState";
import { ErrorState } from "../../components/ui/ErrorState";

interface AdminPricebookCampaignsProps {
  view: string;
  token: string;
}

export function AdminPricebookCampaigns({ view, token }: AdminPricebookCampaignsProps) {
  const [pricebook, setPricebook] = useState<PhaseTwoPricebookItem[]>([]);
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        setError(null);
        const [pbList, cList] = await Promise.all([
          api.getPricebook(),
          api.getCampaigns()
        ]);
        if (active) {
          setPricebook(pbList);
          setCampaigns(cList);
        }
      } catch (err) {
        if (active) setError(err);
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => { active = false; };
  }, [token, reloadKey]);

  const isCampaigns = view === "campaigns";
  const campaignStatusClass = (active: boolean) => active ? "badge-green" : "badge-red";

  function renderPricebookContent() {
    if (loading) return <div className="loading-shimmer p-6 text-center">Loading pricebook data...</div>;
    if (error) return <ErrorState message={error} onRetry={() => { setLoading(true); setError(null); setReloadKey((key) => key + 1); }} />;
    if (isCampaigns) {
      return <div className="max-w-3xl space-y-4">
        {campaigns.length === 0 ? <EmptyState title="No campaigns yet" copy="Create a campaign when a promotion is ready to publish." /> : campaigns.map((c) => (
          <div key={c.id} className="card-box flex items-center justify-between">
            <div><span className="badge badge-green">{c.key}</span><h3 className="mt-1 font-bold">{c.name}</h3><p className="subtext">Override Amount: {c.overrideAmount ? `$${c.overrideAmount} USD` : "N/A"}</p></div>
            <span className={`badge ${campaignStatusClass(c.isActive)}`}>{c.isActive ? "ACTIVE" : "INACTIVE"}</span>
          </div>
        ))}
      </div>;
    }
    return <div className="card-box responsive-table-cards">
      <table className="table-styled w-full">
        <thead><tr><th>Key</th><th>Label</th><th>Amount</th><th>Cadence</th><th>Applies To</th><th>Status</th></tr></thead>
        <tbody>{pricebook.length === 0 ? <tr><td colSpan={6}><EmptyState title="No pricebook entries yet" copy="Platform fee and pricing rules will appear here when configured." /></td></tr> : pricebook.map((pb) => (
          <tr key={pb.key}><td data-label="Key"><code>{pb.key}</code></td><td data-label="Label"><strong>{pb.label}</strong></td><td data-label="Amount"><strong>{formatMoney(pb.amount, pb.currency)}</strong></td><td data-label="Cadence">{pb.cadence}</td><td data-label="Applies to">{pb.appliesTo}</td><td data-label="Status"><span className={`badge ${pb.isActive ? "badge-green" : "badge-red"}`}>{pb.isActive ? "Active" : "Inactive"}</span></td></tr>
        ))}</tbody>
      </table>
    </div>;
  }

  return (
    <div className="page-container container py-6" data-testid={isCampaigns ? "adm-11-page" : "adm-10-page"} id={isCampaigns ? "ADM-11" : "ADM-10"}>
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">{isCampaigns ? "ADM-11" : "ADM-10"}</span>
          <h2>{isCampaigns ? "Marketing Campaigns & Special Rates" : "Platform Pricebook & Fee Architecture"}</h2>
          <PatoisPhrase phrase="Fee Schedules & Promotion Rules" translation="Manage central guest fees, badge pricing, founding host tiers, and promotional campaigns." />
        </div>
        <button type="button" className="btn btn-primary" onClick={() => announceFeedback(`${isCampaigns ? "Campaign" : "Pricebook entry"} creation is ready for this workspace.`)}>
          <Plus size={16} /> {isCampaigns ? "Create Campaign" : "Add Pricebook Entry"}
        </button>
      </header>

      {renderPricebookContent()}
    </div>
  );
}
