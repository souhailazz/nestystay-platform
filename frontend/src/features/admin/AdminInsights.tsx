import { useEffect, useState } from "react";
import { Download, FileBarChart, Gauge } from "lucide-react";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { EmptyState } from "../../components/ui/EmptyState";
import { ErrorState } from "../../components/ui/ErrorState";
import { LoadingState } from "../../components/ui/LoadingState";
import { api, type AdminOperations } from "../../lib/api";
import { announceFeedback } from "../../lib/feedback";

type AdminInsightsView = "kpis" | "reports";

export function AdminInsights({ view, token }: { view: AdminInsightsView; token: string }) {
  const [data, setData] = useState<AdminOperations | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    setData(null);
    setError(null);
    api.getAdminOperations(token)
      .then((result) => { if (active) setData(result); })
      .catch((caught: unknown) => { if (active) setError(caught); });
    return () => { active = false; };
  }, [token, reloadKey]);

  function exportReport() {
    if (!data) return;
    const rows = [
      ["Metric", "Value"],
      ...data.metrics.map((metric) => [metric.label, metric.value]),
      [],
      ["Audit action", "Subject", "Reason", "Created at"],
      ...data.auditEvents.map((event) => [event.action, event.subjectType, event.reason, new Date(event.createdAt).toISOString()]),
    ];
    const csv = rows.map((row) => row.map((cell) => `"${String(cell ?? "").replaceAll('"', '""')}"`).join(",")).join("\n");
    const link = document.createElement("a");
    link.href = `data:text/csv;charset=utf-8,${encodeURIComponent(csv)}`;
    link.download = `nesty-admin-${view}-${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(link);
    link.click();
    link.remove();
    announceFeedback("Report exported from the live admin dataset.");
  }

  return (
    <div className="page-container container py-6" data-testid={view === "kpis" ? "adm-kpi-page" : "adm-reports-page"}>
      <header className="page-header mb-6 flex flex-wrap items-start justify-between gap-4">
        <div>
          <span className="badge badge-sun">{view === "kpis" ? "ADM-KPI" : "ADM-RPT"}</span>
          <h2>{view === "kpis" ? "Live platform KPIs" : "Operational reports"}</h2>
          <p className="subtext">{view === "kpis" ? "Current metrics from the authenticated admin operations API." : "Exportable operational metrics and audit activity from the authenticated admin operations API."}</p>
        </div>
        {view === "reports" && <Button disabled={!data} variant="outline" onClick={exportReport}><Download size={16} /> Export report</Button>}
      </header>

      {error ? <ErrorState message={error} onRetry={() => setReloadKey((key) => key + 1)} /> : !data ? <LoadingState label="Loading live admin insights" /> : (
        <div className="space-y-6">
          <section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {data.metrics.length === 0 ? <div className="sm:col-span-2 lg:col-span-4"><EmptyState title="No KPI data returned" copy="The admin operations API has not reported metrics for this session." icon={view === "kpis" ? <Gauge size={26} /> : <FileBarChart size={26} />} /></div> : data.metrics.map((metric) => <Card className="space-y-2" key={metric.label}><span className="text-xs font-semibold uppercase tracking-wide text-sand-600">{metric.label}</span><strong className="block font-display text-3xl text-ink">{metric.value}</strong></Card>)}
          </section>
          {view === "reports" && <>
            <Card>
              <h3 className="m-0 font-display text-2xl">Open operational cases</h3>
              {data.cases.length === 0 ? <EmptyState title="No open cases" copy="Admin cases will appear here when a review requires follow-up." /> : <div className="mt-4 grid gap-2">{data.cases.map((item) => <div className="flex flex-wrap items-center justify-between gap-3 rounded-field border border-sand-border p-3" key={item.id}><div><strong>{item.caseType}</strong><p className="m-0 text-xs text-sand-600">{item.subjectType} · {item.reason}</p></div><span className="badge badge-sun">{item.status}</span></div>)}</div>}
            </Card>
            <Card>
              <h3 className="m-0 font-display text-2xl">Recent audit activity</h3>
              {data.auditEvents.length === 0 ? <EmptyState title="No audit activity" copy="Privileged actions will appear here as the platform is used." /> : <div className="mt-4 overflow-x-auto"><table className="table-styled w-full"><thead><tr><th>Action</th><th>Subject</th><th>Reason</th><th>Created</th></tr></thead><tbody>{data.auditEvents.slice(0, 20).map((event) => <tr key={event.id}><td><span className="badge badge-sun">{event.action}</span></td><td>{event.subjectType}</td><td>{event.reason}</td><td>{new Date(event.createdAt).toLocaleString()}</td></tr>)}</tbody></table></div>}
            </Card>
          </>}
        </div>
      )}
    </div>
  );
}
