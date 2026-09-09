import { useEffect, useMemo, useState } from "react";
import { AlertTriangle, BadgeCheck, CheckSquare, Clock3, History, Search, ShieldCheck, Square, XCircle } from "lucide-react";
import { AppLink } from "../../components/AppLink";
import { StatusChip } from "../../components/ui/StatusChip";
import { api, formatMoney, type AuditEvent, type BadgeAssignment, type BadgeDefinition, type BadgeRenewal } from "../../lib/api";

interface AdminBadgesProps {
  token: string;
}

function dateLabel(value?: string | null) {
  if (!value) return "—";
  return new Date(value).toLocaleDateString(undefined, { dateStyle: "medium" });
}

function daysUntil(value: string) {
  return Math.ceil((new Date(value).getTime() - Date.now()) / 86_400_000);
}

export function AdminBadges({ token }: AdminBadgesProps) {
  const [definitions, setDefinitions] = useState<BadgeDefinition[]>([]);
  const [assignments, setAssignments] = useState<BadgeAssignment[]>([]);
  const [renewals, setRenewals] = useState<BadgeRenewal[]>([]);
  const [audit, setAudit] = useState<AuditEvent[]>([]);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("all");
  const [selected, setSelected] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const results = await Promise.allSettled([
        api.getBadgeDefinitions(),
        api.getBadgeAssignments(token),
        api.getBadgeRenewals(token),
        api.getAuditLog(token),
      ]);
      const [definitionsResult, assignmentsResult, renewalsResult, auditResult] = results;
      if (definitionsResult.status === "rejected" || assignmentsResult.status === "rejected" || renewalsResult.status === "rejected") {
        const failure = [definitionsResult, assignmentsResult, renewalsResult].find((item) => item.status === "rejected");
        throw failure?.reason instanceof Error ? failure.reason : new Error("Badge operations could not be loaded.");
      }
      const nextDefinitions = definitionsResult.value;
      const nextAssignments = assignmentsResult.value;
      const nextRenewals = renewalsResult.value;
      const nextAudit = auditResult.status === "fulfilled" ? auditResult.value : [];
      setDefinitions(nextDefinitions);
      setAssignments(nextAssignments);
      setRenewals(nextRenewals);
      setAudit(nextAudit.filter((item) => item.subjectType.toLowerCase().includes("badge") || item.action.toLowerCase().includes("badge") || item.action.toLowerCase().includes("pricebook")));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Badge operations could not be loaded.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    // Browser sessions use the HttpOnly cookie, so the compatibility token
    // field may intentionally be empty. The API request still carries the
    // cookie credentials and remains server-authorized.
    void load();
  }, [token]);

  const filteredAssignments = useMemo(() => assignments.filter((assignment) => {
    const haystack = `${assignment.subjectId} ${assignment.subjectType} ${assignment.level} ${assignment.badgeKey}`.toLowerCase();
    return haystack.includes(query.trim().toLowerCase()) && (status === "all" || assignment.status.toLowerCase() === status);
  }), [assignments, query, status]);

  const expiringSoon = assignments.filter((assignment) => assignment.status.toLowerCase() === "active" && daysUntil(assignment.expiresAt) <= 30);
  const pendingRenewals = renewals.filter((renewal) => renewal.paymentStatus.toLowerCase() === "pending");
  const allVisibleSelected = filteredAssignments.length > 0 && filteredAssignments.every((assignment) => selected.includes(assignment.id));

  function toggleSelection(id: string) {
    setSelected((current) => current.includes(id) ? current.filter((item) => item !== id) : [...current, id]);
  }

  function toggleAllVisible() {
    setSelected(allVisibleSelected ? [] : filteredAssignments.map((assignment) => assignment.id));
  }

  async function bulkAction(kind: "expire" | "suspend") {
    if (!selected.length) return;
    const label = kind === "expire" ? "expire" : "suspend";
    if (!window.confirm(`Are you sure you want to ${label} ${selected.length} badge assignment${selected.length === 1 ? "" : "s"}?`)) return;
    const reason = window.prompt("Add an audit reason for this bulk action:", `Bulk ${label} from badge management queue.`)?.trim();
    if (!reason) return;
    setBusy(true);
    setNotice(null);
    setError(null);
    try {
      for (const id of selected) {
        if (kind === "expire") await api.expireBadgeAssignment(id, token, reason);
        else await api.suspendBadgeAssignment(id, token, reason);
      }
      setSelected([]);
      setNotice(`${selected.length} assignment${selected.length === 1 ? "" : "s"} ${label}d. Each change is recorded in the audit log.`);
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : `Could not ${label} selected assignments.`);
    } finally {
      setBusy(false);
    }
  }

  async function singleAction(kind: "expire" | "suspend", assignment: BadgeAssignment) {
    setSelected([assignment.id]);
    if (!window.confirm(`Confirm ${kind} for ${assignment.subjectType} ${assignment.subjectId}?`)) return;
    const reason = window.prompt("Add an audit reason:", `${kind === "expire" ? "Expiry" : "Suspension"} reviewed by administrator.`)?.trim();
    if (!reason) return;
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      if (kind === "expire") await api.expireBadgeAssignment(assignment.id, token, reason);
      else await api.suspendBadgeAssignment(assignment.id, token, reason);
      setNotice(`${assignment.level} assignment ${kind}d.`);
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : `Could not ${kind} assignment.`);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" data-testid="admin-badges-page" id="ADM-BADGES">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="mb-1 text-[11px] font-bold uppercase tracking-[0.16em] text-sand-500">ADM-BADGES · M2 controls</p>
          <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal">Badge <em className="italic text-deep-hover">management</em></h1>
          <p className="mb-0 mt-2 max-w-2xl text-[14px] leading-6 text-gray-600">Search assignments, act on expiry queues, monitor renewals, and inspect the immutable price and assignment audit trail.</p>
        </div>
        <AppLink className="inline-flex min-h-[44px] items-center gap-2 rounded-pill border border-sand-input bg-cream px-4 text-[13px] font-semibold hover:bg-shell" href="/admin/ops/pricebook">Open pricebook</AppLink>
      </div>

      {loading && <div className="grid gap-3 md:grid-cols-3" aria-label="Loading badge management" aria-busy="true">{[1, 2, 3].map((item) => <div className="h-24 animate-pulse rounded-card border border-sand-border bg-shell" key={item} />)}</div>}
      {error && <div className="rounded-card border border-coral bg-coral-tint p-4 text-[13px] text-coral-text" role="alert">{error}</div>}
      {notice && <div className="rounded-card border border-mint bg-mint-tint p-4 text-[13px] text-mint-text" role="status">{notice}</div>}

      {!loading && (
        <>
          <div className="grid gap-3 md:grid-cols-4">
            <div className="rounded-card border border-sand-border bg-cream p-5"><BadgeCheck className="text-mint-text" size={20} /><strong className="mt-3 block font-display text-2xl">{assignments.length}</strong><span className="text-[12px] text-gray-600">Total assignments</span></div>
            <div className="rounded-card border border-sand-border bg-cream p-5"><ShieldCheck className="text-success-text" size={20} /><strong className="mt-3 block font-display text-2xl">{assignments.filter((item) => item.status.toLowerCase() === "active").length}</strong><span className="text-[12px] text-gray-600">Active now</span></div>
            <div className={`rounded-card border p-5 ${expiringSoon.length ? "border-amber bg-amber-tint" : "border-sand-border bg-cream"}`}><AlertTriangle className={expiringSoon.length ? "text-amber-text" : "text-sand-500"} size={20} /><strong className="mt-3 block font-display text-2xl">{expiringSoon.length}</strong><span className="text-[12px] text-gray-600">Expire within 30 days</span></div>
            <div className="rounded-card border border-sand-border bg-cream p-5"><Clock3 className="text-amber-text" size={20} /><strong className="mt-3 block font-display text-2xl">{pendingRenewals.length}</strong><span className="text-[12px] text-gray-600">Pending renewals</span></div>
          </div>

          {expiringSoon.length > 0 && <div className="rounded-card border border-amber bg-amber-tint p-4 text-[13px] text-amber-text" role="alert"><strong>Expiry queue:</strong> {expiringSoon.length} active assignment{expiringSoon.length === 1 ? "" : "s"} need attention. Review the expiry date and contact the host before access is removed.</div>}

          <section className="rounded-card border border-sand-border bg-cream p-5" aria-labelledby="assignment-heading">
            <div className="flex flex-wrap items-end justify-between gap-3">
              <div><p className="mb-1 text-[11px] font-bold uppercase tracking-[0.16em] text-sand-500">Search & bulk actions</p><h2 className="m-0 font-display text-[23px] font-medium" id="assignment-heading">Assignments</h2></div>
              <div className="flex flex-wrap gap-2">
                <button className="inline-flex min-h-[40px] items-center gap-2 rounded-pill border border-sand-input bg-white px-3 text-[12px] font-semibold disabled:opacity-50" disabled={!selected.length || busy} onClick={() => void bulkAction("suspend")} type="button"><XCircle size={15} /> Suspend selected</button>
                <button className="inline-flex min-h-[40px] items-center gap-2 rounded-pill bg-deep px-3 text-[12px] font-semibold text-on-dark-heading disabled:opacity-50" disabled={!selected.length || busy} onClick={() => void bulkAction("expire")} type="button"><AlertTriangle size={15} /> Expire selected</button>
              </div>
            </div>
            <div className="mt-4 flex flex-wrap gap-3">
              <label className="relative min-w-[240px] flex-1"><Search className="absolute left-3 top-3 text-sand-500" size={16} /><input aria-label="Search badge assignments" className="min-h-[42px] w-full rounded-field border border-sand-input bg-white pl-9 pr-3 text-[13px] outline-none focus:border-deep-hover" onChange={(event) => setQuery(event.target.value)} placeholder="Search subject, badge, or ID" value={query} /></label>
              <select aria-label="Filter assignment status" className="min-h-[42px] rounded-field border border-sand-input bg-white px-3 text-[13px]" onChange={(event) => setStatus(event.target.value)} value={status}><option value="all">All statuses</option><option value="active">Active</option><option value="expired">Expired</option><option value="suspended">Suspended</option></select>
            </div>
            <div className="mt-4 overflow-x-auto rounded-field border border-sand-border bg-white">
              <table className="w-full min-w-[880px] text-left text-[12px]"><thead className="bg-shell text-[10px] uppercase tracking-[0.08em] text-sand-500"><tr><th className="px-3 py-3"><button aria-label="Select all visible assignments" className="text-sand-500" onClick={toggleAllVisible} type="button">{allVisibleSelected ? <CheckSquare size={16} /> : <Square size={16} />}</button></th><th className="px-3 py-3">Subject</th><th className="px-3 py-3">Badge</th><th className="px-3 py-3">Status</th><th className="px-3 py-3">Paid through</th><th className="px-3 py-3">Renewal</th><th className="px-3 py-3 text-right">Actions</th></tr></thead><tbody>{filteredAssignments.map((assignment) => { const renewal = renewals.find((item) => item.badgeAssignmentId === assignment.id && item.paymentStatus.toLowerCase() === "pending"); return <tr className="border-t border-sand-border" key={assignment.id}><td className="px-3 py-3"><button aria-label={`Select ${assignment.subjectId}`} className="text-sand-500" onClick={() => toggleSelection(assignment.id)} type="button">{selected.includes(assignment.id) ? <CheckSquare size={16} /> : <Square size={16} />}</button></td><td className="px-3 py-3"><strong>{assignment.subjectType}</strong><span className="mt-1 block font-mono text-[10px] text-gray-500">{assignment.subjectId}</span></td><td className="px-3 py-3"><strong>{assignment.level}</strong><span className="mt-1 block text-[10px] text-gray-500">{formatMoney(assignment.amountCharged, assignment.currency)}</span></td><td className="px-3 py-3"><StatusChip value={assignment.status} /></td><td className="px-3 py-3">{dateLabel(assignment.paidThrough)}</td><td className="px-3 py-3">{renewal ? <><StatusChip value="Pending" /><span className="mt-1 block text-[10px] text-gray-500">{dateLabel(renewal.reminderDueAt)}</span></> : "—"}</td><td className="px-3 py-3 text-right"><div className="flex justify-end gap-2"><button className="text-[11px] font-semibold text-coral-text underline disabled:opacity-40" disabled={busy || assignment.status.toLowerCase() !== "active"} onClick={() => void singleAction("suspend", assignment)} type="button">Suspend</button><button className="text-[11px] font-semibold text-deep underline disabled:opacity-40" disabled={busy || assignment.status.toLowerCase() !== "active"} onClick={() => void singleAction("expire", assignment)} type="button">Expire</button></div></td></tr>; })}</tbody></table>
              {filteredAssignments.length === 0 && <div className="p-8 text-center text-[13px] text-gray-500">No badge assignments match this search.</div>}
            </div>
          </section>

          <section className="grid gap-4 lg:grid-cols-[1fr_1.2fr]">
            <div className="rounded-card border border-sand-border bg-cream p-5"><div className="mb-3 flex items-center gap-2"><BadgeCheck className="text-mint-text" size={19} /><h2 className="m-0 font-display text-[22px] font-medium">Badge catalog</h2></div><div className="flex flex-col gap-2">{definitions.map((definition) => <div className="flex items-center justify-between gap-3 rounded-field border border-sand-border bg-white p-3" key={definition.id}><div><strong>{definition.level}</strong><span className="mt-1 block text-[11px] text-gray-500">{definition.appliesTo} · {formatMoney(definition.annualPrice, definition.currency)}</span></div><StatusChip value={`${definition.unlocks.length} unlocks`} /></div>)}</div><AppLink className="mt-4 inline-flex text-[12px] font-semibold text-deep underline" href="/admin/ops/pricebook">Edit active pricebook →</AppLink></div>
            <div className="rounded-card border border-sand-border bg-cream p-5"><div className="mb-3 flex items-center gap-2"><History className="text-sand-500" size={19} /><h2 className="m-0 font-display text-[22px] font-medium">Price & assignment history</h2></div><div className="max-h-72 overflow-y-auto"><ul className="m-0 flex flex-col gap-2">{audit.slice(0, 20).map((event) => <li className="rounded-field border border-sand-border bg-white p-3 text-[11px]" key={event.id}><div className="flex flex-wrap items-center justify-between gap-2"><strong>{event.action}</strong><span className="text-gray-500">{dateLabel(event.createdAt)}</span></div><p className="mb-0 mt-1 text-gray-600">{event.reason}</p></li>)}{audit.length === 0 && <li className="text-[12px] text-gray-500">No badge or pricebook audit events yet.</li>}</ul></div></div>
          </section>
        </>
      )}
    </div>
  );
}
