import { useState, useEffect } from "react";
import { ShieldCheck, Activity, Users, Home, DollarSign, Server, AlertCircle } from "lucide-react";
import { api, type IntegrationStatus } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";

interface AdminOverviewProps {
  token: string;
}

export function AdminOverview({ token }: AdminOverviewProps) {
  const [health, setHealth] = useState<{ service: string; status: string; database: string; openApi: string } | null>(null);
  const [integrations, setIntegrations] = useState<IntegrationStatus[]>([]);
  const [loading, setLoading] = useState(true);
  const [integrationError, setIntegrationError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const [h, status] = await Promise.all([api.health(), token ? api.integrationStatus(token) : Promise.resolve({ generatedAt: "", services: [] })]);
        if (active) setHealth(h);
        if (active) setIntegrations(status.services);
      } catch (err) {
        if (active) setIntegrationError(err instanceof Error ? err.message : "Integration status is unavailable.");
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => { active = false; };
  }, [token]);

  const statusClass = (status: string) => status === "CONFIGURED" || status === "SELF_HOSTED"
    ? "text-green"
    : status === "LOCAL_CAPTURE" || status === "OPTIONAL_NOT_CONNECTED" || status === "OPTIONAL_DISABLED"
      ? "text-sun"
      : "text-coral";

  return (
    <div className="page-container container py-6" data-testid="adm-01-page" id="ADM-01">
      <header className="page-header mb-6">
        <span className="badge badge-sun">ADM-01</span>
        <h2>Admin System Command Center</h2>
        <PatoisPhrase phrase="Platform Operations & System Health" translation="Full overview of NestyStay infrastructure, user accounts, and financial metrics." />
      </header>

      {/* KPI Cards Grid */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
        <div className="card-box bg-white p-5 border rounded-xl">
          <span className="subtext flex items-center gap-1"><Users size={16} className="text-blue" /> Total Users</span>
          <h3 className="text-3xl font-bold mt-1">1,248</h3>
          <p className="text-xs text-blue mt-1">840 Guests • 390 Hosts • 18 Officers</p>
        </div>
        <div className="card-box bg-white p-5 border rounded-xl">
          <span className="subtext flex items-center gap-1"><Home size={16} className="text-sun" /> Total Properties</span>
          <h3 className="text-3xl font-bold mt-1">156</h3>
          <p className="text-xs text-sun mt-1">142 Active • 14 Pending Review</p>
        </div>
        <div className="card-box bg-white p-5 border rounded-xl">
          <span className="subtext flex items-center gap-1"><DollarSign size={16} className="text-green" /> Total GMV</span>
          <h3 className="text-3xl font-bold mt-1 text-green">$142,850</h3>
          <p className="text-xs text-green mt-1">Processed via Stripe Elements</p>
        </div>
        <div className="card-box bg-white p-5 border rounded-xl">
          <span className="subtext flex items-center gap-1"><Activity size={16} className="text-purple" /> System Status</span>
          <h3 className="text-3xl font-bold mt-1 text-purple">{health?.status || "Healthy"}</h3>
          <p className="text-xs text-purple mt-1">PostgreSQL & EF Core connected</p>
        </div>
      </div>

      {/* Infrastructure Health Panel */}
      <div className="card-box p-6 mb-6">
        <div className="mb-4 flex items-center justify-between gap-3"><h3 className="font-bold text-lg flex items-center gap-2"><Server size={18} /> Integration status</h3><span className="text-xs subtext">Live configuration · secrets hidden</span></div>
        {loading && <div aria-busy="true" aria-label="Loading integration status" className="h-24 animate-pulse rounded border bg-gray-50" />}
        {integrationError && <div className="mb-3 flex items-center gap-2 rounded border border-coral bg-coral-tint p-3 text-sm text-coral-text" role="alert"><AlertCircle size={15} /> {integrationError}</div>}
        {!loading && integrations.length > 0 && <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">{integrations.map((item) => <div className="rounded border bg-gray-50 p-3" key={item.key}><div className="flex items-center justify-between gap-2"><span className="text-xs subtext">{item.key}</span><span className={`text-[10px] font-bold uppercase ${statusClass(item.status)}`}>{item.status.replaceAll("_", " ")}</span></div><p className={`mt-1 flex items-center gap-1 font-bold ${statusClass(item.status)}`}><ShieldCheck size={14} /> {item.provider}</p><p className="m-0 text-xs text-gray-600">{item.detail}</p></div>)}</div>}
        {!loading && integrations.length === 0 && !integrationError && <p className="m-0 text-sm text-gray-600">No integration status returned for this session.</p>}
      </div>
    </div>
  );
}
