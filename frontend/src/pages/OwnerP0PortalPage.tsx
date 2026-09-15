import { useEffect, useState } from "react";
import { ArrowRight, FileText, RefreshCw, Wallet } from "lucide-react";
import { AppLink } from "../components/AppLink";
import { Button, buttonClassName } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { EmptyState } from "../components/ui/EmptyState";
import { LoadingState } from "../components/ui/LoadingState";
import { PageHeader } from "../components/ui/PageHeader";
import { StatusChip } from "../components/ui/StatusChip";
import type { AuthController } from "../hooks/useAuth";
import { api, formatMoney } from "../lib/api";

function ErrorNotice({ error }: { error: unknown }) {
  if (!error) return null;
  return (
    <div className="rounded-field bg-coral-tint px-4 py-3 text-sm text-coral-text" role="alert">
      {error instanceof Error ? error.message : "The financial portal could not be loaded."}
    </div>
  );
}

export function OwnerP0PortalPage({ auth }: { auth: AuthController }) {
  const token = auth.session?.accessToken ?? "";
  const sessionUserId = auth.session?.userId ?? "";
  const [portal, setPortal] = useState<Awaited<ReturnType<typeof api.getP0OwnerPortal>> | null>(null);
  const [blocks, setBlocks] = useState<Awaited<ReturnType<typeof api.listOwnerOperationalBlocks>>>([]);
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);
  const [decisionReasons, setDecisionReasons] = useState<Record<string, string>>({});
  const [blockForm, setBlockForm] = useState({ propertyId: "", startsAt: "", endsAt: "", reason: "Owner stay", notes: "" });

  const load = () => {
    if (!sessionUserId) return;
    setError(null);
    void Promise.all([api.getP0OwnerPortal(token), api.listOwnerOperationalBlocks(token)])
      .then(([nextPortal, nextBlocks]) => {
        setPortal(nextPortal);
        setBlocks(nextBlocks);
        setBlockForm((current) => ({ ...current, propertyId: current.propertyId || nextPortal.properties[0]?.id || "" }));
      })
      .catch(setError);
  };

  useEffect(load, [token, sessionUserId]);

  async function decide(id: string, status: string, rowVersion: number) {
    setBusy(true);
    setError(null);
    try {
      await api.decideP0Approval(token, id, {
        status,
        reason: decisionReasons[id]?.trim() || (status === "APPROVED" ? "Owner approved in portal" : status === "REJECTED" ? "Owner rejected in portal" : "Owner requested more information"),
        rowVersion,
        idempotencyKey: `owner-decision-${id}-${status.toLowerCase()}`,
      });
      load();
    } catch (cause) {
      setError(cause);
    } finally {
      setBusy(false);
    }
  }

  async function openEvidence(documentId: string) {
    setError(null);
    try {
      const result = await api.getPropertyManagerDocumentDownload(token, documentId);
      window.open(result.url, "_blank", "noopener,noreferrer");
    } catch (cause) {
      setError(cause);
    }
  }

  async function createBlock() {
    if (!blockForm.propertyId || !blockForm.startsAt || !blockForm.endsAt || !blockForm.reason.trim()) return;
    setBusy(true);
    setError(null);
    try {
      await api.createOwnerOperationalBlock(token, {
        propertyId: blockForm.propertyId,
        startsAt: new Date(blockForm.startsAt).toISOString(),
        endsAt: new Date(blockForm.endsAt).toISOString(),
        reason: blockForm.reason.trim(),
        notes: blockForm.notes.trim(),
        category: "OWNER_STAY",
      });
      setBlockForm((current) => ({ ...current, startsAt: "", endsAt: "", notes: "" }));
      load();
    } catch (cause) {
      setError(cause);
    } finally {
      setBusy(false);
    }
  }

  async function cancelBlock(id: string, rowVersion: number) {
    setBusy(true);
    setError(null);
    try {
      await api.cancelOwnerOperationalBlock(token, id, { reason: "Owner cancelled block", rowVersion });
      load();
    } catch (cause) {
      setError(cause);
    } finally {
      setBusy(false);
    }
  }

  if (!auth.session) {
    return (
      <EmptyState
        title="Owner portal requires a session."
        action={<AppLink className={buttonClassName("sun")} href="/login">Sign in <ArrowRight size={16} /></AppLink>}
      />
    );
  }

  return (
    <div className="product-page">
      <PageHeader
        eyebrow="Owner portal · P0"
        title="Your agreements, approvals and money"
        copy="Every item below is filtered by your owner identity and manager relationship."
        actions={<div className="flex flex-wrap gap-2"><Button variant="outline" disabled={busy} onClick={load}><RefreshCw size={16} /> Refresh</Button><AppLink className={buttonClassName("outline")} href="/owner/dashboard">Back to owner home</AppLink></div>}
      />
      <ErrorNotice error={error} />

      {!portal ? (
        <LoadingState label="Loading owner financial records" />
      ) : (
        <>
          <section className="mb-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Card><small>Units</small><strong className="mt-1 block font-display text-3xl">{portal.properties.length}</strong></Card>
            <Card><small>Closing balance</small><strong className="mt-1 block font-display text-3xl">{formatMoney(portal.statement.closingBalance, portal.statement.currency)}</strong></Card>
            <Card><small>Transactions</small><strong className="mt-1 block font-display text-3xl">{portal.transactions.length}</strong></Card>
            <Card><small>Pending payouts</small><strong className="mt-1 block font-display text-3xl">{portal.payouts.filter((item) => !["PAID", "CANCELLED"].includes(item.status)).length}</strong></Card>
          </section>

          <Card className="mb-6">
            <h2 className="font-display text-2xl">Owner stays and property blocks</h2>
            <p className="mt-1 text-sm text-sand-600">Block dates for your properties; the manager and booking API enforce overlap protection.</p>
            <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
              <label className="text-sm">Property<select className="mt-1 block w-full rounded-field border border-line p-2" value={blockForm.propertyId} onChange={(event) => setBlockForm({ ...blockForm, propertyId: event.target.value })}>{portal.properties.map((property) => <option key={property.id} value={property.id}>{property.title}</option>)}</select></label>
              <label className="text-sm">Starts<input className="mt-1 block w-full rounded-field border border-line p-2" type="datetime-local" value={blockForm.startsAt} onChange={(event) => setBlockForm({ ...blockForm, startsAt: event.target.value })} /></label>
              <label className="text-sm">Ends<input className="mt-1 block w-full rounded-field border border-line p-2" type="datetime-local" value={blockForm.endsAt} onChange={(event) => setBlockForm({ ...blockForm, endsAt: event.target.value })} /></label>
              <label className="text-sm">Reason<input className="mt-1 block w-full rounded-field border border-line p-2" value={blockForm.reason} onChange={(event) => setBlockForm({ ...blockForm, reason: event.target.value })} /></label>
              <Button type="button" disabled={busy || !blockForm.propertyId || !blockForm.startsAt || !blockForm.endsAt || !blockForm.reason.trim()} onClick={() => void createBlock()}>Create block</Button>
            </div>
            <label className="mt-3 block text-sm">Notes<input className="mt-1 block w-full rounded-field border border-line p-2" value={blockForm.notes} onChange={(event) => setBlockForm({ ...blockForm, notes: event.target.value })} /></label>
            <div className="mt-4 space-y-2">
              {blocks.length === 0 ? <p className="text-sm text-sand-600">No owner blocks.</p> : blocks.map((block) => (
                <div className="flex flex-wrap items-center justify-between gap-2 rounded-field border border-sand-border p-3 text-sm" key={block.id}>
                  <span><strong>{block.category}</strong> · {new Date(block.startsAt).toLocaleString()} → {new Date(block.endsAt).toLocaleString()} · {block.reason}{block.notes ? ` · ${block.notes}` : ""}</span>
                  {block.status === "ACTIVE" ? <Button type="button" variant="outline" disabled={busy} onClick={() => void cancelBlock(block.id, block.rowVersion)}>Cancel</Button> : <StatusChip value={block.status} />}
                </div>
              ))}
            </div>
          </Card>

          <section className="mb-6 grid gap-5 lg:grid-cols-2">
            <Card>
              <h2 className="font-display text-2xl">Properties and profitability</h2>
              {portal.properties.length === 0 ? <EmptyState title="No properties assigned" /> : portal.properties.map((property) => {
                const row = portal.propertyFinancials.find((item) => item.propertyId === property.id);
                return <div className="mb-3 rounded-field border border-sand-border p-3" key={property.id}><div className="flex justify-between gap-2"><strong>{property.title} · {property.unitNumber}</strong><StatusChip value={property.occupancyStatus} /></div><p className="m-0 text-sm text-sand-600">{property.address}</p>{row && <div className="mt-2 grid grid-cols-3 gap-2 text-xs"><span>Income<strong className="block">{formatMoney(row.income, portal.statement.currency)}</strong></span><span>Expenses<strong className="block">{formatMoney(row.expenses, portal.statement.currency)}</strong></span><span>Owner net<strong className="block">{formatMoney(row.ownerNet, portal.statement.currency)}</strong></span></div>}</div>;
              })}
            </Card>
            <Card>
              <h2 className="font-display text-2xl"><FileText className="mr-2 inline" size={18} />Agreements</h2>
              {portal.agreements.length === 0 ? <EmptyState title="No agreements" /> : portal.agreements.map((item) => <div className="mb-2 rounded-field border border-sand-border p-3" key={item.id}><div className="flex items-center justify-between gap-2"><strong>Version {item.version}</strong><StatusChip value={item.status} /></div><p className="m-0 text-xs text-sand-600">{item.effectiveFrom} → {item.effectiveTo ?? "open-ended"} · {item.currency}</p>{item.documentKey && <small className="text-sand-500">Document: {item.documentKey}</small>}</div>)}
            </Card>
          </section>

          <section className="mb-6 grid gap-5 lg:grid-cols-2">
            <Card>
              <h2 className="font-display text-2xl">Statement and transactions</h2>
              <p className="mt-1 text-sm text-sand-600">{portal.statement.from} → {portal.statement.to} · {portal.statement.status}</p>
              <div className="grid grid-cols-2 gap-2 rounded-field bg-shell p-3 text-sm sm:grid-cols-4"><span>Income<strong className="block">{formatMoney(portal.statement.income, portal.statement.currency)}</strong></span><span>Expenses<strong className="block">{formatMoney(portal.statement.expenses, portal.statement.currency)}</strong></span><span>Fees<strong className="block">{formatMoney(portal.statement.managementFees, portal.statement.currency)}</strong></span><span>Closing<strong className="block">{formatMoney(portal.statement.closingBalance, portal.statement.currency)}</strong></span></div>
              <div className="mt-4 max-h-80 space-y-2 overflow-auto">{portal.transactions.length === 0 ? <p className="text-sm text-sand-600">No transactions yet.</p> : portal.transactions.map((transaction) => <details className="rounded-field border border-sand-border p-3 text-sm" key={transaction.id}><summary className="cursor-pointer"><span className="font-semibold">{transaction.sourceType}</span> · {transaction.accountingDate} · {formatMoney(transaction.totalDebit, transaction.currency)} <StatusChip value={transaction.reconciliationStatus} /></summary><p className="m-0 mt-2 text-xs text-sand-600">{transaction.memo} · {transaction.journalNumber}</p>{transaction.lines.map((line) => <div className="mt-1 flex justify-between gap-2 text-xs" key={line.id}><span>{line.accountCode}</span><span>{line.debit ? `Dr ${formatMoney(line.debit, transaction.currency)}` : `Cr ${formatMoney(line.credit, transaction.currency)}`}</span></div>)}</details>)}</div>
            </Card>
            <Card>
              <h2 className="font-display text-2xl"><Wallet className="mr-2 inline" size={18} />Approvals and payouts</h2>
              {portal.approvals.filter((item) => item.status === "REQUIRED").map((item) => <div className="mb-3 rounded-field border border-sand-border p-3 text-sm" key={item.id}><div className="flex items-center justify-between gap-2"><strong>{item.description}</strong><StatusChip value={item.status} /></div><p className="m-0 mt-1">{formatMoney(item.amount, item.currency)}{item.sourceType && item.sourceId ? ` · ${item.sourceType.toLowerCase()} ${item.sourceId}` : ""}</p>{item.expiresAt && <p className="m-0 mt-1 text-xs text-sand-600">Decision deadline: {new Date(item.expiresAt).toLocaleString()}</p>}{(item.evidence ?? []).length > 0 && <div className="mt-2 flex flex-wrap gap-2" aria-label="Approval evidence">{item.evidence?.map((document) => <Button key={document.id} type="button" variant="outline" onClick={() => void openEvidence(document.id)}>Open {document.title || document.fileName}</Button>)}</div>}<label className="mt-2 block text-xs font-semibold">Decision reason<input className="mt-1 block w-full rounded-field border border-line p-2 font-normal" value={decisionReasons[item.id] ?? ""} onChange={(event) => setDecisionReasons((current) => ({ ...current, [item.id]: event.target.value }))} placeholder="Explain your decision" /></label><div className="mt-2 flex flex-wrap gap-2"><Button disabled={busy} onClick={() => void decide(item.id, "APPROVED", item.rowVersion)}>Approve</Button><Button variant="destructive" disabled={busy || !(decisionReasons[item.id]?.trim())} onClick={() => void decide(item.id, "REJECTED", item.rowVersion)}>Reject</Button><Button variant="ghost" disabled={busy || !(decisionReasons[item.id]?.trim())} onClick={() => void decide(item.id, "CHANGES_REQUESTED", item.rowVersion)}>Request changes</Button></div><details className="mt-2 text-xs"><summary className="cursor-pointer font-semibold">Approval history</summary>{item.history.map((event) => <div className="mt-1" key={event.id}>{new Date(event.createdAt).toLocaleString()} · {event.eventType} · {event.fromStatus || "—"} → {event.toStatus} · {event.reason}</div>)}</details></div>)}
              {portal.payouts.map((item) => <div className="mb-2 rounded-field border border-sand-border p-3 text-sm" key={item.id}><div className="flex items-center justify-between gap-2"><span>{formatMoney(item.amount, item.currency)} · {item.periodFrom} → {item.periodTo}</span><StatusChip value={item.status} /></div>{item.providerReference && <small className="text-sand-500">Reference: {item.providerReference}</small>}<details className="mt-2 text-xs"><summary className="cursor-pointer font-semibold">Payout history</summary>{item.history.map((event) => <div className="mt-1" key={event.id}>{event.eventType} · {event.fromStatus || "—"} → {event.toStatus} · {event.reason}</div>)}</details></div>)}
              {portal.approvals.length === 0 && portal.payouts.length === 0 && <EmptyState title="No decisions yet" copy="Manager approvals and payout updates will appear here." />}
            </Card>
          </section>
        </>
      )}
    </div>
  );
}

export default OwnerP0PortalPage;
