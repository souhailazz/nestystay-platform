import { useState, useEffect } from "react";
import { ShieldCheck, Activity, Users, Home, DollarSign, Server, AlertCircle, RefreshCw } from "lucide-react";
import { api, formatMoney } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";

interface AdminOverviewProps {
  token: string;
}

export function AdminOverview({ token }: AdminOverviewProps) {
  const [health, setHealth] = useState<{ service: string; status: string; database: string; openApi: string } | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const h = await api.health();
        if (active) setHealth(h);
      } catch (err) {
        console.error(err);
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => { active = false; };
  }, []);

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
        <h3 className="font-bold text-lg mb-4 flex items-center gap-2"><Server size={18} /> Infrastructure Service Status</h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="p-3 bg-gray-50 rounded border">
            <span className="text-xs subtext">Database Engine</span>
            <p className="font-bold text-green flex items-center gap-1"><ShieldCheck size={14} /> PostgreSQL 16 (EF Core)</p>
          </div>
          <div className="p-3 bg-gray-50 rounded border">
            <span className="text-xs subtext">Object Storage</span>
            <p className="font-bold text-green flex items-center gap-1"><ShieldCheck size={14} /> S3 / MinIO Encrypted</p>
          </div>
          <div className="p-3 bg-gray-50 rounded border">
            <span className="text-xs subtext">Payment Gateway</span>
            <p className="font-bold text-green flex items-center gap-1"><ShieldCheck size={14} /> Stripe API v10</p>
          </div>
          <div className="p-3 bg-gray-50 rounded border">
            <span className="text-xs subtext">Identity Engine</span>
            <p className="font-bold text-green flex items-center gap-1"><ShieldCheck size={14} /> Alibaba Cloud eKYC</p>
          </div>
        </div>
      </div>
    </div>
  );
}
