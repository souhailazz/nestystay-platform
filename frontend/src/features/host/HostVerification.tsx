import { useEffect, useState } from "react";
import { FileCheck2, ShieldCheck } from "lucide-react";
import { api, type HostVerification } from "../../lib/api";

export function HostVerification({ token }: { token: string }) {
  const [verification, setVerification] = useState<HostVerification | null>(null);
  const [documentType, setDocumentType] = useState("Passport");
  const [notes, setNotes] = useState("");
  const [notice, setNotice] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let active = true;
    api.getHostVerification(token).then((result) => { if (active) { setVerification(result); setDocumentType(result.documentType ?? "Passport"); setNotes(result.reason ?? ""); } }).catch((caught) => { if (active) setError(caught instanceof Error ? caught.message : "Host verification could not be loaded."); });
    return () => { active = false; };
  }, [token]);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true); setNotice(null); setError(null);
    try {
      const result = await api.submitHostVerification(token, { documentType, notes });
      setVerification(result);
      setNotice("Your host verification submission is now pending review.");
    } catch (caught) { setError(caught instanceof Error ? caught.message : "Host verification could not be submitted."); }
    finally { setBusy(false); }
  }

  return <div className="page-container container py-6" data-testid="host-verification-page"><header className="page-header mb-6"><span className="badge badge-sun">HOST-VERIFICATION</span><h2>Host identity verification</h2><p className="subtext">Keep your verification status current so guests can understand the trust behind each listing.</p></header><div className="grid gap-5 lg:grid-cols-[1.1fr_0.9fr]"><section className="card-box"><div className="flex items-center gap-3"><span className="grid size-12 place-items-center rounded-full bg-success-tint text-success-text"><ShieldCheck size={24} /></span><div><h3 className="m-0">Current status</h3><p className="subtext m-0">{verification?.status ?? "Loading status…"}</p></div></div>{verification && <div className="mt-5 grid gap-3">{verification.checklist.map((item, index) => <div className="flex items-start gap-3 rounded-field border border-sand-border bg-shell p-3 text-sm" key={item}><FileCheck2 className="mt-0.5 shrink-0 text-success" size={17} /><span><strong>Step {index + 1}</strong><br />{item}</span></div>)}</div>}<p className="mt-5 text-sm text-gray-600">Only the verification status is shown publicly. Identity documents and notes stay in the secure account workflow.</p></section><form className="card-box" onSubmit={submit}><h3>Submit or renew verification</h3><p className="subtext">Choose the document type you are submitting. Upload the document through the secure identity workflow before you submit.</p><label className="mt-4 block text-sm font-semibold">Document type<select className="input-control mt-1" value={documentType} onChange={(event) => setDocumentType(event.target.value)}><option>Passport</option><option>National ID</option><option>Driver License</option></select></label><label className="mt-4 block text-sm font-semibold">Notes<textarea className="input-control mt-1" rows={4} maxLength={500} placeholder="Optional context for the review team" value={notes} onChange={(event) => setNotes(event.target.value)} /></label><button className="btn btn-primary mt-4" disabled={busy} type="submit">{busy ? "Submitting…" : "Submit for review"}</button>{notice && <div className="notice-panel mt-4" role="status">{notice}</div>}{error && <div className="notice-panel mt-4" role="alert">{error}</div>}</form></div></div>;
}
