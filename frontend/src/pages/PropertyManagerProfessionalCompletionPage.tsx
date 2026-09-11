import { useCallback, useEffect, useMemo, useState } from "react";
import { Activity, Bell, Boxes, FileArchive, FileText, RefreshCw, ShieldCheck, Upload, Users, Wrench } from "lucide-react";
import { AppLink } from "../components/AppLink";
import { Button, buttonClassName } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { EmptyState } from "../components/ui/EmptyState";
import { Field, Input, Select, Textarea } from "../components/ui/Input";
import { LoadingState } from "../components/ui/LoadingState";
import { PageHeader } from "../components/ui/PageHeader";
import { StatusChip } from "../components/ui/StatusChip";
import { api, type PropertyManagerProfessionalRecord, type PropertyManagerProfessionalReport } from "../lib/api";
import type { AuthController } from "../hooks/useAuth";

const areas = [
  ["utilities", "Utilities & evidence", "Bills, readings, disputes, recurring runs and immutable adjustments", Wrench],
  ["documents", "Documents", "Private preview, versions, expiry reminders and recoverable exports", FileText],
  ["governance", "Governance & proxy", "Evidence, frozen eligibility, quorum proof and proxy history", ShieldCheck],
  ["team", "Team & RBAC", "Invitations, scopes, capabilities and staff history", Users],
  ["reporting", "Reporting & KPIs", "Persisted period totals grouped safely by currency", Activity],
  ["audit", "Unified audit", "Searchable append-only workflow history", Activity],
  ["vendors", "Vendors", "Compliance, contracts, availability and performance", Users],
  ["assets", "Assets", "Property asset register, warranties and maintenance links", Boxes],
  ["inventory", "Inventory", "Stock in/out/adjustment transaction history", Boxes],
  ["incidents", "Incidents", "Private evidence, insurance and resolution lifecycle", ShieldCheck],
  ["community", "Community & gate", "Audience, scheduling, comments and delivery recovery", Bell],
  ["bulk", "Bulk operations", "Preview, dry-run, per-row outcomes and exports", FileArchive],
  ["notifications", "Notifications", "Internal center, preferences, retries and deduplication", Bell],
] as const;
type Area = (typeof areas)[number][0];

const defaultPayload: Record<Area, string> = {
  utilities: '{"utilityType":"WATER","billingPeriod":"2026-09","previousReading":0,"currentReading":0,"rate":0,"fixedFee":0,"tax":0,"total":0,"evidence":[],"disputeStatus":"NONE"}',
  documents: '{"title":"","contentType":"application/pdf","version":1,"previewStatus":"VERIFIED","expiry":"","exportStatus":"NONE"}',
  governance: '{"title":"","quorumPercent":50,"eligibleVoters":[],"attachments":[],"anonymous":true,"resultProofHash":""}',
  team: '{"email":"","role":"OPERATIONS","propertyIds":[],"ownerIds":[],"capabilities":[],"invitationExpiresAt":""}',
  reporting: '{"metric":"","amount":0,"currency":"JMD","period":"2026-09","source":""}',
  audit: '{"action":"","resourceType":"","reason":"","before":{},"after":{}}',
  vendors: '{"name":"","categories":[],"serviceRadiusKm":0,"availability":{},"emergency":false,"contract":{},"licenseExpiry":"","insuranceExpiry":"","rating":0}',
  assets: '{"name":"","location":"","type":"","serial":"","warrantyExpiry":"","maintenanceLinks":[],"documents":[]}',
  inventory: '{"item":"","sku":"","unit":"each","quantity":0,"minimumQuantity":0,"transactionType":"ADJUSTMENT","quantityDelta":0,"reason":""}',
  incidents: '{"incidentNumber":"","category":"","severity":"MEDIUM","occurredAt":"","description":"","privateEvidence":[],"insurance":{},"estimatedLoss":0,"actualCost":0,"resolution":""}',
  community: '{"title":"","audiences":[],"publishAt":"","expiresAt":"","comments":[],"deliveryAttempts":[],"retryCount":0}',
  bulk: '{"operation":"","rows":[],"dryRun":true,"outcomes":[],"exportFormat":"CSV"}',
  notifications: '{"recipientUserId":"","category":"","priority":"NORMAL","title":"","message":"","actionLink":"","channel":"IN_APP","deduplicationKey":""}',
};

export function PropertyManagerProfessionalCompletionPage({ auth }: { auth: AuthController }) {
  const token = auth.session?.accessToken ?? "";
  const [area, setArea] = useState<Area>("utilities");
  const [rows, setRows] = useState<PropertyManagerProfessionalRecord[]>([]);
  const [report, setReport] = useState<PropertyManagerProfessionalReport | null>(null);
  const [payload, setPayload] = useState(defaultPayload.utilities);
  const [status, setStatus] = useState("OPEN");
  const [resourceType, setResourceType] = useState("WORKFLOW");
  const [ownerId, setOwnerId] = useState("");
  const [propertyId, setPropertyId] = useState("");
  const [search, setSearch] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [history, setHistory] = useState<{ id: string; action: string; reason: string; createdAt: string }[] | null>(null);
  const [dashboard, setDashboard] = useState<Awaited<ReturnType<typeof api.getPropertyManagerDashboard>> | null>(null);
  const current = useMemo(() => areas.find(item => item[0] === area)!, [area]);

  const load = useCallback(async () => {
    setBusy(true); setError(null);
    try {
      const [nextRows, nextReport, nextDashboard] = await Promise.all([
        api.listPropertyManagerProfessionalRecords(token, area, { propertyId: propertyId || undefined, ownerUserId: ownerId || undefined, search: search || undefined }),
        api.getPropertyManagerProfessionalReport(token),
        api.getPropertyManagerDashboard(token),
      ]);
      setRows(nextRows); setReport(nextReport); setDashboard(nextDashboard);
    } catch (e) { setError(e instanceof Error ? e.message : "Could not load this workspace."); }
    finally { setBusy(false); }
  }, [token, area, ownerId, propertyId, search]);

  useEffect(() => { setPayload(defaultPayload[area]); void load(); }, [area]);

  const create = async () => {
    setBusy(true); setError(null); setNotice(null);
    try {
      JSON.parse(payload);
      await api.createPropertyManagerProfessionalRecord(token, area, { resourceType, status, payloadJson: payload, ownerUserId: ownerId || undefined, propertyId: propertyId || undefined, currency: area === "reporting" || area === "utilities" ? "JMD" : undefined, idempotencyKey: crypto.randomUUID(), reason: `Created in ${current[1]} workspace` });
      setNotice("Record saved with an audit event and server-side scope checks."); await load();
    } catch (e) { setError(e instanceof Error ? e.message : "The record could not be saved."); }
    finally { setBusy(false); }
  };

  if (!auth.session) return <div className="product-page"><PageHeader eyebrow="Property Manager" title="Sign in required" copy="Sign in to open the professional operations workspace." /></div>;
  return <div className="product-page overflow-x-hidden"><PageHeader eyebrow="Property Manager · professional completion" title="One workspace for every operational record" copy="All records below are persisted in PostgreSQL, manager-scoped, capability checked and recoverable through their history." actions={<div className="flex flex-wrap gap-2"><AppLink className={buttonClassName("outline")} href="/pm/dashboard">Back to dashboard</AppLink><Button disabled={busy} onClick={() => void load()}><RefreshCw size={16} /> Refresh</Button></div>} />
    <div className="mb-5 flex gap-2 overflow-x-auto pb-2" role="tablist" aria-label="Professional PMS areas">{areas.map(([key, label, , Icon]) => <button className={`flex min-h-11 shrink-0 items-center gap-2 rounded-field border px-3 text-sm font-semibold ${area === key ? "border-deep bg-deep text-white" : "border-sand-border bg-white text-deep"}`} key={key} onClick={() => setArea(key)} role="tab" aria-selected={area === key}><Icon size={15} />{label}</button>)}</div>
    {error && <div className="mb-4 rounded-field bg-coral-tint px-4 py-3 text-sm text-coral-text" role="alert">{error}<Button className="ml-3" variant="outline" onClick={() => void load()}>Retry</Button></div>}{notice && <div className="mb-4 rounded-field bg-success-tint px-4 py-3 text-sm text-success-text" role="status">{notice}</div>}
    <section className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_minmax(18rem,26rem)]"><Card><div className="flex flex-wrap items-start justify-between gap-3"><div><h2 className="m-0 font-display text-2xl">{current[1]}</h2><p className="m-0 mt-1 text-sm text-sand-600">{current[2]}</p></div><StatusChip value={busy ? "SYNCING" : "READY"} /></div><div className="mt-5 grid gap-3 sm:grid-cols-2"><Field label="Resource type"><Input value={resourceType} onChange={event => setResourceType(event.target.value)} /></Field><Field label="Status"><Select value={status} onChange={event => setStatus(event.target.value)}><option>OPEN</option><option>PENDING</option><option>IN_PROGRESS</option><option>APPROVED</option><option>REJECTED</option><option>COMPLETED</option><option>EXPIRED</option></Select></Field><Field label="Owner scope (optional)"><Select value={ownerId} onChange={event => setOwnerId(event.target.value)}><option value="">All / not linked</option>{dashboard?.owners.map(owner => <option key={owner.ownerUserId} value={owner.ownerUserId}>{owner.displayName}</option>)}</Select></Field><Field label="Property scope (optional)"><Select value={propertyId} onChange={event => setPropertyId(event.target.value)}><option value="">All / not linked</option>{dashboard?.properties.map(property => <option key={property.id} value={property.id}>{property.title} · {property.unitNumber}</option>)}</Select></Field></div><Field className="mt-3" label="Workflow payload (JSON)"><Textarea className="min-h-44 font-mono text-xs" value={payload} onChange={event => setPayload(event.target.value)} aria-describedby="payload-help" /><span id="payload-help" className="mt-1 block text-xs text-sand-600">The server validates JSON, stores it privately, and records an immutable event. Add evidence references, amounts, currency and policy fields as required for this area.</span></Field><div className="mt-3 flex flex-wrap items-center gap-2"><Button disabled={busy || !resourceType.trim()} onClick={() => void create()}><Upload size={16} /> Save workflow record</Button><Button variant="outline" disabled={busy} onClick={() => setPayload(defaultPayload[area])}>Reset template</Button></div></Card>
      <Card><h2 className="m-0 font-display text-xl">Period report</h2>{report ? <><div className="mt-4 grid gap-2 text-sm"><div className="flex justify-between"><span>Records</span><strong>{report.records.length}</strong></div>{Object.entries(report.totalsByCurrency).map(([currency, amount]) => <div className="flex justify-between" key={currency}><span>Total ({currency})</span><strong>{amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}</strong></div>)}{Object.entries(report.countsByArea).slice(0, 6).map(([name, count]) => <div className="flex justify-between text-xs text-sand-600" key={name}><span>{name}</span><span>{count}</span></div>)}</div><p className="mt-4 text-xs text-sand-600">Currencies are grouped; no silent conversion is performed.</p></> : <LoadingState label="Calculating report…" />}</Card></section>
    <Card className="mt-5"><div className="flex flex-wrap items-center justify-between gap-3"><div><h2 className="m-0 font-display text-xl">Persisted {current[1]} records</h2><p className="m-0 mt-1 text-sm text-sand-600">Search, filter, inspect history and retry after recoverable failures.</p></div><Input className="max-w-xs" value={search} onChange={event => setSearch(event.target.value)} placeholder="Search records" aria-label="Search records" /></div>{busy && rows.length === 0 ? <LoadingState label="Loading records…" /> : rows.length === 0 ? <EmptyState title="No records yet" copy="Create the first workflow record above. Empty states are distinct from server failures." /> : <div className="mt-4 grid gap-3">{rows.map(row => <article className="rounded-field border border-sand-border p-3" key={row.id}><div className="flex flex-wrap items-start justify-between gap-3"><div><strong>{row.resourceType}</strong><p className="m-0 mt-1 text-xs text-sand-600">{row.status} · version {row.rowVersion} · {new Date(row.updatedAt).toLocaleString()}</p></div><StatusChip value={row.status} /></div><pre className="mt-3 max-h-32 overflow-auto rounded-field bg-shell p-3 text-xs">{row.payloadJson}</pre><div className="mt-3 flex flex-wrap gap-2"><Button variant="outline" disabled={busy} onClick={() => void api.getPropertyManagerProfessionalHistory(token, row.id).then(events => setHistory(events)).catch(e => setError(e instanceof Error ? e.message : "Could not load history."))}><Activity size={15} /> View history</Button><span className="self-center text-xs text-sand-600">{row.propertyId ? `Property ${row.propertyId.slice(0, 8)}` : "Portfolio scope"}</span></div></article>)}</div>}</Card>
    {history && <div className="fixed inset-0 z-50 flex items-end justify-center bg-deep/40 p-4 sm:items-center" role="dialog" aria-modal="true" aria-label="Workflow history"><Card className="max-h-[80vh] w-full max-w-2xl overflow-auto"><div className="flex items-center justify-between"><h2 className="m-0 font-display text-xl">Append-only history</h2><Button variant="outline" onClick={() => setHistory(null)}>Close</Button></div><div className="mt-4 grid gap-2">{history.map(event => <div className="rounded-field border border-sand-border p-3 text-sm" key={event.id}><div className="flex justify-between gap-2"><strong>{event.action}</strong><span className="text-xs text-sand-600">{new Date(event.createdAt).toLocaleString()}</span></div><p className="m-0 mt-1 text-xs text-sand-600">{event.reason}</p></div>)}</div></Card></div>}
  </div>;
}
