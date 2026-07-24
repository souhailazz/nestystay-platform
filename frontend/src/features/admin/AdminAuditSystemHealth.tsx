import { useState } from "react";
import { ShieldCheck, Download, Activity, FileText, Search } from "lucide-react";
import { PatoisPhrase } from "../../lib/patois";
import type { AuditTrailLog } from "./types";

interface AdminAuditSystemHealthProps {
  view: string;
  token: string;
}

export function AdminAuditSystemHealth({ view, token }: AdminAuditSystemHealthProps) {
  const [logs, setLogs] = useState<AuditTrailLog[]>([
    {
      id: "log-1",
      timestamp: "2026-07-24 16:00:00",
      actorEmail: "admin@nestystay.local",
      action: "UPDATE_PRICEBOOK",
      resourceType: "PricebookItem",
      resourceId: "guest_fee",
      details: "Updated base guest fee to $15.00 USD"
    },
    {
      id: "log-2",
      timestamp: "2026-07-24 15:30:00",
      actorEmail: "host-villa@nestystay.local",
      action: "CREATE_PROPERTY",
      resourceType: "PropertyListing",
      resourceId: "p-100",
      details: "Created Ocho Rios Verified Villa"
    }
  ]);

  const isHealth = view === "health";

  function exportAuditCSV() {
    const csv = "data:text/csv;charset=utf-8,ID,Timestamp,Actor,Action,Resource,Details\n" + 
      logs.map(l => `${l.id},${l.timestamp},${l.actorEmail},${l.action},${l.resourceType},"${l.details}"`).join("\n");
    const link = document.createElement("a");
    link.href = encodeURI(csv);
    link.download = `audit-trail-${Date.now()}.csv`;
    document.body.appendChild(link);
    link.click();
    link.remove();
  }

  return (
    <div className="page-container container py-6" data-testid={isHealth ? "adm-09-page" : "adm-08-page"} id={isHealth ? "ADM-09" : "ADM-08"}>
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">{isHealth ? "ADM-09" : "ADM-08"}</span>
          <h2>{isHealth ? "System Health & Telemetry" : "Immutable Audit Trail Log"}</h2>
          <PatoisPhrase phrase="System Integrity & Traceability" translation="Review all security actions, role changes, financial overrides, and server diagnostics." />
        </div>
        {!isHealth && (
          <button type="button" className="btn btn-outline" onClick={exportAuditCSV}>
            <Download size={16} /> Export Audit Log CSV
          </button>
        )}
      </header>

      {isHealth ? (
        <div className="card-box space-y-4">
          <h3>Telemetry & Diagnostics</h3>
          <div className="info-row">
            <span>PostgreSQL Pool:</span>
            <span className="badge badge-green">12/50 Connections</span>
          </div>
          <div className="info-row">
            <span>Redis Cache Hit Rate:</span>
            <span className="badge badge-green">98.4%</span>
          </div>
          <div className="info-row">
            <span>Stripe Webhook Delivery:</span>
            <span className="badge badge-green">100% Success</span>
          </div>
        </div>
      ) : (
        <div className="card-box">
          <table className="table-styled w-full">
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>Actor</th>
                <th>Action</th>
                <th>Resource</th>
                <th>Details</th>
              </tr>
            </thead>
            <tbody>
              {logs.map((l) => (
                <tr key={l.id}>
                  <td><code className="text-xs">{l.timestamp}</code></td>
                  <td>{l.actorEmail}</td>
                  <td><span className="badge badge-sun">{l.action}</span></td>
                  <td>{l.resourceType} ({l.resourceId})</td>
                  <td className="subtext text-xs">{l.details}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
