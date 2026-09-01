import { useEffect, useMemo, useState } from "react";
import { Check, FileSearch, Search, ShieldAlert, X } from "lucide-react";
import { api, type DirectoryProvider } from "../../lib/api";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { EmptyState } from "../../components/ui/EmptyState";
import { Field, Input, Select, Textarea } from "../../components/ui/Input";
import { LoadingState } from "../../components/ui/LoadingState";
import { StatusChip } from "../../components/ui/StatusChip";

/** Live moderation queue for M4 directory providers. */
export function AdminDirectories({ token }: { token: string }) {
  const [providers, setProviders] = useState<DirectoryProvider[]>([]);
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("All");
  const [selected, setSelected] = useState<DirectoryProvider | null>(null);
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setProviders(await api.getM4DirectoryModerationQueue(token, { status: status === "All" ? undefined : status, query: query || undefined }));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Directory moderation queue could not be loaded.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, [token, status]);

  const visible = useMemo(() => providers.filter((provider) => !query.trim() || `${provider.name} ${provider.category} ${provider.parish} ${provider.kind}`.toLowerCase().includes(query.trim().toLowerCase())), [providers, query]);

  async function moderate(provider: DirectoryProvider, action: "approve" | "reject" | "request-changes") {
    if ((action !== "approve") && !reason.trim()) {
      setError("Add a reason so the provider receives actionable feedback.");
      return;
    }
    setError(null);
    setNotice(null);
    try {
      const updated = await api.moderateM4DirectoryProvider(provider.slug, token, action, reason.trim() || "Approved after document review.");
      setNotice(`${updated.name}: ${updated.status}. Audit reason recorded.`);
      setSelected(null);
      setReason("");
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Provider moderation failed.");
    }
  }

  return <div className="page-container container py-6" data-testid="adm-directory-page" id="ADM-DIRECTORIES">
    <header className="page-header mb-6">
      <span className="badge badge-sun">M4 / PROVIDER MODERATION</span>
      <h2>Directory moderation queue</h2>
      <p className="subtext">Review risk flags, compare submitted details, and publish only verified providers. Every action writes an audit event.</p>
    </header>
    {error && <div className="mb-4 rounded-field bg-coral-tint px-4 py-3 text-sm text-coral-text" role="alert">{error}</div>}
    {notice && <div className="mb-4 rounded-field bg-success-tint px-4 py-3 text-sm text-success-text" role="status">{notice}</div>}
    <Card className="mb-5">
      <div className="grid gap-3 md:grid-cols-[1fr_220px_auto] md:items-end">
        <Field label="Search providers"><div className="relative"><Search className="pointer-events-none absolute left-3 top-3 text-sand-500" size={16} /><Input className="pl-9" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Name, category, parish" /></div></Field>
        <Field label="Status"><Select value={status} onChange={(event) => setStatus(event.target.value)}><option>All</option><option>PendingReview</option><option>ChangesRequested</option><option>Published</option><option>Rejected</option><option>Suspended</option></Select></Field>
        <Button variant="outline" onClick={() => void load()}>Refresh queue</Button>
      </div>
    </Card>
    {loading ? <LoadingState label="Loading provider moderation queue" /> : visible.length === 0 ? <EmptyState title="No providers match this queue." copy="New provider applications will appear here after they save their profile." /> : <div className="grid gap-4 lg:grid-cols-2">
      {visible.map((provider) => <Card className="flex flex-col gap-3" key={provider.id}>
        <div className="flex items-start justify-between gap-3"><div><div className="text-xs font-bold uppercase tracking-wide text-sand-500">{provider.kind} · {provider.category}</div><h3 className="m-0 mt-1 font-display text-2xl">{provider.name}</h3><p className="m-0 text-sm text-sand-600">{provider.parish} · {provider.availabilitySummary}</p></div><StatusChip value={provider.status ?? "Unknown"} /></div>
        <div className="grid gap-2 text-sm sm:grid-cols-2"><span><strong>Badge:</strong> {provider.badgeLevel}</span><span><strong>Rating:</strong> {provider.rating.toFixed(1)} ({provider.reviewCount})</span><span><strong>Verification:</strong> {provider.verificationStatus}</span><span><strong>Risk flags:</strong> {provider.isBrickAndMortar || provider.kind !== "LocalBusiness" ? "None" : "Brick-and-mortar required"}</span></div>
        <p className="m-0 text-sm text-sand-600">{provider.description}</p>
        <div className="mt-auto flex flex-wrap gap-2"><Button variant="outline" onClick={() => setSelected(provider)}><FileSearch size={15} /> Compare documents</Button><Button onClick={() => void moderate(provider, "approve")}><Check size={15} /> Approve</Button><Button variant="ghost" onClick={() => { setSelected(provider); setReason(""); }}><X size={15} /> Review decision</Button></div>
      </Card>)}
    </div>}
    {selected && <div className="modal-backdrop" role="dialog" aria-modal="true"><div className="modal-card max-w-xl"><div className="flex items-start justify-between gap-3"><div><h3 className="m-0">Review {selected.name}</h3><p className="subtext">Compare the submitted profile, risk flags, and verification evidence before changing status.</p></div><ShieldAlert className="text-deep-hover" /></div><div className="my-4 grid gap-2 rounded-field border border-sand-border bg-shell p-3 text-sm"><div><strong>Submitted:</strong> {selected.category} · {selected.parish}</div><div><strong>Contact:</strong> {selected.contactMode}</div><div><strong>Documents:</strong> Provider document uploads are retained in the secure review record.</div></div><Field label="Decision reason"><Textarea value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Explain approval, rejection, or requested changes" /></Field><div className="mt-4 flex flex-wrap justify-end gap-2"><Button variant="outline" onClick={() => setSelected(null)}>Cancel</Button><Button variant="ghost" onClick={() => void moderate(selected, "request-changes")}>Request changes</Button><Button variant="ghost" onClick={() => void moderate(selected, "reject")}>Reject</Button><Button onClick={() => void moderate(selected, "approve")}>Approve</Button></div></div></div>}
  </div>;
}
