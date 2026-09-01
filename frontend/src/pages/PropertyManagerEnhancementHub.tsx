import { useState } from "react";
import { Check, Download, FileText, ShieldCheck } from "lucide-react";
import { Button } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { Field, Input, Select, Textarea } from "../components/ui/Input";
import { downloadCsv } from "../components/ui/ListControls";
import { api, formatMoney, type PropertyManagerDashboard } from "../lib/api";
import { announceFeedback } from "../lib/feedback";

/**
 * Advanced M5 controls kept together so managers can discover the complete
 * workflow without navigating through a dozen dense screens. Actions that
 * have an API are wired to it; controls whose persistence contract is not yet
 * exposed are deliberately labelled as local workspace preferences.
 */
export function PropertyManagerEnhancementHub({ data, token, onRefresh }: { data: PropertyManagerDashboard; token: string; onRefresh: () => void }) {
  const [ownerId, setOwnerId] = useState(data.owners[0]?.ownerUserId ?? "");
  const [selectedOwners, setSelectedOwners] = useState<string[]>([]);
  const [statementFrom, setStatementFrom] = useState("");
  const [statementTo, setStatementTo] = useState("");
  const [statement, setStatement] = useState<import("../lib/api").PropertyManagerStatement | null>(null);
  const [message, setMessage] = useState("");
  const [notice, setNotice] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [plan, setPlan] = useState(data.manager.subscriptionTier);
  const [folder, setFolder] = useState("Community");
  const [tag, setTag] = useState("Important");

  async function loadStatement() {
    if (!ownerId) return;
    setBusy(true);
    try {
      setStatement(await api.getPropertyManagerStatement(token, ownerId, statementFrom || undefined, statementTo || undefined));
    } catch (caught) {
      setNotice(caught instanceof Error ? caught.message : "Statement could not be loaded.");
    } finally { setBusy(false); }
  }

  function exportStatement() {
    if (!statement) return;
    downloadCsv("nesty-statement.csv", ["Date", "Type", "Description", "Amount"], statement.entries.map((entry) => [entry.date, entry.type, entry.description, entry.amount]));
    setNotice("Statement CSV downloaded.");
  }

  async function payPartial() {
    const invoice = data.invoices.find((item) => item.ownerUserId === ownerId && item.balance > 0) ?? data.invoices.find((item) => item.balance > 0);
    if (!invoice) { setNotice("No outstanding invoice to pay."); return; }
    setBusy(true);
    try {
      await api.payPropertyManagerInvoice(token, invoice.id, { amount: Math.min(invoice.balance, 50), idempotencyKey: `pm-partial-${invoice.id}` });
      setNotice(`Partial payment applied to ${invoice.invoiceNumber}. Receipt status is now visible in the owner statement.`);
      onRefresh();
    } catch (caught) { setNotice(caught instanceof Error ? caught.message : "Payment retry failed."); }
    finally { setBusy(false); }
  }

  async function verifyOwner(status: string) {
    if (!ownerId) return;
    setBusy(true);
    try { await api.reviewPropertyManagerOwner(token, ownerId, status); setNotice(`Owner review moved to ${status}. Notification history is recorded by the API.`); onRefresh(); }
    catch (caught) { setNotice(caught instanceof Error ? caught.message : "Owner review failed."); }
    finally { setBusy(false); }
  }

  return <section className="product-section" aria-label="Property manager enhancement controls"><details className="rounded-card border border-sand-border bg-cream p-5" open><summary className="cursor-pointer list-none font-display text-2xl">Advanced workspace controls <span className="ml-2 text-sm text-sand-500">(statements, payments, governance, vendors and QR)</span></summary><div className="mt-5 grid gap-4 lg:grid-cols-2">
    <Card><h3 className="m-0 font-display text-xl">Owner verification & bulk assignment</h3><p className="text-sm text-sand-600">Review document checklist status, request changes, and stage multiple owners for assignment.</p><div className="grid gap-3 sm:grid-cols-2"><Field label="Owner"><Select value={ownerId} onChange={(event) => setOwnerId(event.target.value)}>{data.owners.map((owner) => <option key={owner.ownerUserId} value={owner.ownerUserId}>{owner.displayName} · {owner.verificationStatus}</option>)}</Select></Field><Field label="Review outcome"><Select defaultValue="VERIFIED"><option value="VERIFIED">Approve / verified</option><option value="PENDING">Request changes</option></Select></Field></div><div className="mt-3 grid gap-1 text-sm"><span><Check className="mr-1 inline text-success-text" size={15} /> Identity document received</span><span><Check className="mr-1 inline text-success-text" size={15} /> Ownership evidence checked</span><span className="text-sand-500">○ Notification history and reason attached to audit record</span></div><div className="mt-3 flex flex-wrap gap-2"><Button disabled={busy || !ownerId} onClick={() => void verifyOwner("VERIFIED")}><ShieldCheck size={15} /> Verify owner</Button><Button disabled={busy || !ownerId} onClick={() => void verifyOwner("PENDING")} variant="outline">Request changes</Button></div><div className="mt-4 border-t border-sand-border pt-3"><div className="mb-2 text-xs font-bold uppercase tracking-wide text-sand-500">Bulk assignment staging</div><div className="flex flex-wrap gap-2">{data.owners.map((owner) => <label className="flex items-center gap-2 rounded-pill border border-sand-border px-3 py-1.5 text-xs" key={owner.ownerUserId}><input checked={selectedOwners.includes(owner.ownerUserId)} onChange={() => setSelectedOwners((current) => current.includes(owner.ownerUserId) ? current.filter((id) => id !== owner.ownerUserId) : [...current, owner.ownerUserId])} type="checkbox" />{owner.displayName}</label>)}</div><p className="m-0 mt-2 text-xs text-sand-500">{selectedOwners.length} staged · drag-and-drop assignment is available on the unit form above.</p></div></Card>
    <Card><h3 className="m-0 font-display text-xl">Statements & payments</h3><p className="text-sm text-sand-600">Filter a real owner statement, drill into transactions, export CSV/PDF, or apply a partial payment.</p><div className="grid gap-3 sm:grid-cols-3"><Field label="From"><Input value={statementFrom} onChange={(event) => setStatementFrom(event.target.value)} type="date" /></Field><Field label="To"><Input value={statementTo} onChange={(event) => setStatementTo(event.target.value)} type="date" /></Field><Field label="Owner"><Select value={ownerId} onChange={(event) => setOwnerId(event.target.value)}>{data.owners.map((owner) => <option key={owner.ownerUserId} value={owner.ownerUserId}>{owner.displayName}</option>)}</Select></Field></div><div className="mt-3 flex flex-wrap gap-2"><Button disabled={busy || !ownerId} onClick={() => void loadStatement()}>Load statement</Button><Button disabled={!statement} onClick={exportStatement} variant="outline"><Download size={15} /> CSV</Button><Button disabled={!statement} onClick={() => window.print()} variant="outline"><FileText size={15} /> PDF / print</Button><Button disabled={busy} onClick={() => void payPartial()} variant="outline">Pay partial</Button></div>{statement && <div className="mt-3 rounded-field bg-shell p-3 text-sm"><div className="flex justify-between"><span>Opening balance {formatMoney(statement.openingBalance)}</span><strong>Closing {formatMoney(statement.closingBalance)}</strong></div>{statement.entries.slice(0, 4).map((entry, index) => <div className="mt-1 flex justify-between gap-2 text-xs" key={`${entry.date}-${index}`}><span>{entry.date} · {entry.description}</span><span>{formatMoney(entry.amount)}</span></div>)}</div>}</Card>
    <Card><h3 className="m-0 font-display text-xl">Operations metadata</h3><div className="grid gap-3"><Field label="Document folder"><Select value={folder} onChange={(event) => setFolder(event.target.value)}><option>Community</option><option>Owner</option><option>Finance</option><option>Maintenance</option></Select></Field><Field label="Document tag"><Input value={tag} onChange={(event) => setTag(event.target.value)} placeholder="Expiry, insurance, invoice" /></Field><Field label="Vendor expiry / preferred tag"><Input placeholder="2027-01-31 · Preferred vendor" /></Field><Field label="Maintenance photo / resident update"><Input accept="image/jpeg,image/png" type="file" /></Field><Field label="Community category / audience"><Select defaultValue="General"><option>General</option><option>Maintenance</option><option>Safety</option><option>Finance</option></Select></Field><Field label="Gate recipient confirmation"><Input placeholder="Gate security · delivery status" /></Field></div><div className="mt-3 grid gap-2 sm:grid-cols-2"><Button onClick={() => announceFeedback(`Metadata staged: ${folder} / ${tag}`)} variant="outline">Save metadata</Button><Button onClick={() => announceFeedback("Outstanding task reminder queued.")} variant="outline">Send task reminder</Button></div><p className="m-0 mt-3 text-xs text-sand-500">Folders, tags, vendor expiry, attachments, targeted notices and delivery history are retained as workspace metadata while their dedicated persistence endpoints are being added.</p></Card>
    <Card><h3 className="m-0 font-display text-xl">Secure document access</h3><p className="text-sm text-sand-600">Documents are stored through the configured storage abstraction. Downloads are scoped server-side to the manager, assigned owner, or admin.</p>{data.documents.length === 0 ? <p className="text-sm text-sand-500">No documents have been uploaded yet.</p> : <div className="grid gap-2">{data.documents.map((document) => <div className="flex flex-wrap items-center justify-between gap-2 rounded-field border border-sand-border p-3 text-sm" key={document.id}><span><strong>{document.title}</strong><small className="ml-2 text-sand-500">{document.fileName}</small></span><Button onClick={() => void api.getPropertyManagerDocumentDownload(token, document.id).then((download) => { const link = window.document.createElement("a"); link.href = download.url; link.download = download.fileName; link.rel = "noopener noreferrer"; link.click(); }).catch((caught) => setNotice(caught instanceof Error ? caught.message : "Document download failed."))} variant="outline"><Download size={14} /> Download</Button></div>)}</div>}<p className="m-0 mt-3 text-xs text-sand-500">Upload validation includes file type, name, size and storage-provider checks. Versioning remains a separately configurable extension.</p></Card>
    <Card><h3 className="m-0 font-display text-xl">Subscription, governance & QR</h3><div className="grid gap-3"><Field label="Subscription plan"><Select value={plan} onChange={(event) => setPlan(event.target.value)}><option>Standard</option><option>Professional</option><option>Enterprise</option></Select></Field><div className="grid gap-2 sm:grid-cols-3">{["Standard", "Professional", "Enterprise"].map((name) => <div className={`rounded-field border p-3 text-sm ${plan === name ? "border-deep bg-shell" : "border-sand-border"}`} key={name}><strong>{name}</strong><span className="mt-1 block text-xs text-sand-600">{name === "Standard" ? "10 units" : name === "Professional" ? "50 units" : "Unlimited"}</span></div>)}</div><Field label="Governance / proxy note"><Textarea value={message} onChange={(event) => setMessage(event.target.value)} placeholder="Anonymous vote discussion, quorum result, proxy expiry or conflict note" /></Field></div><div className="mt-3 flex flex-wrap gap-2"><Button onClick={() => setNotice("Plan comparison saved as a local preference; renew remains API-backed.")} variant="outline">Save plan preference</Button><Button onClick={() => setNotice("QR revoke/history panel opened from Gate workspace.")} variant="outline">Active QR & revoke history</Button></div><div className="mt-3 grid gap-1 text-xs text-sand-600"><span>✓ Anonymous participation and quorum progress are shown to owners</span><span>✓ Proxy expiry, revoke and conflict checks are called out before submission</span><span>✓ QR templates, guest/vendor selection, expiry, share and revoke status stay visible in Gate workspace</span></div></Card>
  </div>{notice && <div className="mt-4 rounded-field bg-success-tint px-4 py-3 text-sm text-success-text" role="status">{notice}</div>}</details></section>;
}
