import { useEffect, useMemo, useState } from "react";
import { Check, Search, ShieldCheck, X } from "lucide-react";
import { PatoisPhrase } from "../../lib/patois";
import { api, type HostVerificationQueueItem } from "../../lib/api";
import { EmptyState } from "../../components/ui/EmptyState";
import { ErrorState } from "../../components/ui/ErrorState";
import { Modal } from "../../components/ui/Modal";

interface AdminUsersProps {
  view: string;
  token: string;
}

function statusClass(status: string) {
  return status.toLowerCase() === "approved" ? "badge-green" : status.toLowerCase() === "rejected" ? "badge-coral" : "badge-sun";
}

export function AdminUsers({ token }: AdminUsersProps) {
  const [hosts, setHosts] = useState<HostVerificationQueueItem[]>([]);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("all");
  const [selectedHost, setSelectedHost] = useState<HostVerificationQueueItem | null>(null);
  const [decision, setDecision] = useState<"Approved" | "Rejected">("Rejected");
  const [reason, setReason] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    api.getHostVerificationQueue(token)
      .then((items) => { if (active) setHosts(items); })
      .catch((caught) => { if (active) setError(caught instanceof Error ? caught.message : "Host verification queue could not be loaded."); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [token]);

  const filtered = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    return hosts.filter((host) => {
      const matchesQuery = !normalized || `${host.displayName} ${host.email} ${host.documentType ?? ""} ${host.status}`.toLowerCase().includes(normalized);
      return matchesQuery && (status === "all" || host.status.toLowerCase() === status);
    });
  }, [hosts, query, status]);

  async function reviewHost(host: HostVerificationQueueItem, nextStatus: "Approved" | "Rejected", nextReason = "") {
    if (nextStatus === "Rejected" && !nextReason.trim()) {
      setError("A clear reason is required when rejecting host verification.");
      return;
    }
    try {
      const updated = await api.reviewHostVerification(host.userId, token, { status: nextStatus, reason: nextReason.trim() || undefined });
      setHosts((current) => current.map((item) => item.userId === updated.userId ? updated : item));
      setSelectedHost(null);
      setReason("");
      setNotice(nextStatus === "Approved" ? "Host verification approved and persisted." : "Host verification rejected and the reason was saved.");
      setError(null);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Host verification decision could not be saved.");
    }
  }

  return (
    <div className="page-container container py-6" data-testid="adm-02-page" id="ADM-02">
      <header className="page-header mb-6 flex flex-wrap items-end justify-between gap-4">
        <div>
          <span className="badge badge-sun">ADM-02 / ADM-03</span>
          <h2>Host Accounts &amp; Identity Review</h2>
          <PatoisPhrase phrase="Manage Platform Accounts & Roles" translation="Review persisted host verification submissions and record an auditable decision." />
        </div>
        <label className="relative w-full max-w-sm"><Search aria-hidden="true" className="absolute left-3 top-3 text-sand-500" size={16} /><span className="sr-only">Search host verification</span><input aria-label="Search host verification" className="input-control pl-9" placeholder="Search name, email, document..." value={query} onChange={(event) => setQuery(event.target.value)} /></label>
      </header>

      {notice && <div className="notice-panel mb-4" role="status">{notice}</div>}
      {error && <ErrorState message={error} />}
      <div className="mb-4 flex flex-wrap gap-2" role="group" aria-label="Filter host verification status">
        {[['all', 'All'], ['pending', 'Pending'], ['approved', 'Approved'], ['rejected', 'Rejected']].map(([value, label]) => <button aria-pressed={status === value} className={`btn btn-sm ${status === value ? "btn-primary" : "btn-outline"}`} key={value} onClick={() => setStatus(value)} type="button">{label}</button>)}
      </div>

      {loading ? <div className="loading-shimmer p-6 text-center">Loading host verification queue...</div> : filtered.length === 0 ? <EmptyState title="No host submissions found" copy="New host identity submissions will appear here after they are sent for review." /> : <>
        <div className="card-box hidden overflow-x-auto md:block">
          <table className="table-styled w-full min-w-[760px]">
            <thead><tr><th>Host</th><th>Document</th><th>Status</th><th>Submitted</th><th className="text-right">Actions</th></tr></thead>
            <tbody>{filtered.map((host) => <tr key={host.userId}><td><strong>{host.displayName}</strong><p className="subtext text-xs">{host.email}</p></td><td>{host.documentType ?? "Not submitted"}</td><td><span className={`badge ${statusClass(host.status)}`}>{host.status}</span>{host.reason && <p className="subtext mt-1 max-w-[260px] text-xs">{host.reason}</p>}</td><td>{host.submittedAt ? new Date(host.submittedAt).toLocaleDateString() : "—"}</td><td className="text-right"><div className="flex justify-end gap-2"><button className="btn btn-outline btn-sm" onClick={() => { setSelectedHost(host); setDecision("Rejected"); setReason(host.status === "Rejected" ? host.reason ?? "" : ""); }} type="button">Review</button>{host.status === "Pending" && <button className="btn btn-primary btn-sm" onClick={() => void reviewHost(host, "Approved")} type="button"><Check size={14} /> Approve</button>}</div></td></tr>)}</tbody>
          </table>
        </div>
        <div className="grid gap-3 md:hidden">{filtered.map((host) => <article className="card-box" key={host.userId}><div className="flex items-start justify-between gap-3"><div><strong>{host.displayName}</strong><p className="subtext m-0 mt-1 break-all text-xs">{host.email}</p></div><span className={`badge ${statusClass(host.status)}`}>{host.status}</span></div><div className="mt-3 grid gap-1 text-sm"><span><strong>Document:</strong> {host.documentType ?? "Not submitted"}</span><span><strong>Submitted:</strong> {host.submittedAt ? new Date(host.submittedAt).toLocaleDateString() : "—"}</span>{host.reason && <span className="text-coral-text"><strong>Reason:</strong> {host.reason}</span>}</div><button className="btn btn-outline mt-4 w-full" onClick={() => { setSelectedHost(host); setDecision("Rejected"); setReason(host.status === "Rejected" ? host.reason ?? "" : ""); }} type="button">Review host</button></article>)}</div>
      </>}

      <Modal open={Boolean(selectedHost)} title={selectedHost ? `Review ${selectedHost.displayName}` : "Review host"} onClose={() => setSelectedHost(null)} variant="sheet">
        {selectedHost && <>
          <p className="subtext">The decision is persisted against this host and visible in their verification status.</p>
          <div className="my-4 grid gap-2 rounded-field border border-sand-border bg-shell p-3 text-sm"><span><strong>Email:</strong> {selectedHost.email}</span><span><strong>Document:</strong> {selectedHost.documentType ?? "Not submitted"}</span><span><strong>Current status:</strong> <span className={`badge ${statusClass(selectedHost.status)}`}>{selectedHost.status}</span></span>{selectedHost.submittedAt && <span><strong>Submitted:</strong> {new Date(selectedHost.submittedAt).toLocaleString()}</span>}</div>
          <div className="mb-3 flex flex-wrap gap-2"><button className={`btn btn-sm ${decision === "Approved" ? "btn-primary" : "btn-outline"}`} onClick={() => setDecision("Approved")} type="button"><Check size={14} /> Approve</button><button className={`btn btn-sm ${decision === "Rejected" ? "btn-primary" : "btn-outline"}`} onClick={() => setDecision("Rejected")} type="button"><X size={14} /> Reject</button></div>
          <label className="field-label" htmlFor="host-verification-decision-reason">Decision reason {decision === "Rejected" ? "(required)" : "(optional)"}</label><textarea id="host-verification-decision-reason" className="input-control mt-1" rows={4} placeholder={decision === "Rejected" ? "Explain what the host needs to correct." : "Optional reviewer note"} value={reason} onChange={(event) => setReason(event.target.value)} />
          <div className="mt-4 flex flex-wrap justify-end gap-2"><button className="btn btn-ghost" onClick={() => setSelectedHost(null)} type="button">Cancel</button><button className="btn btn-primary" onClick={() => void reviewHost(selectedHost, decision, reason)} type="button">Save decision</button></div>
        </>}
      </Modal>
    </div>
  );
}
