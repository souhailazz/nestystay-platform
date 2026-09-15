import { useState, useEffect } from "react";
import { Check, X, ShieldCheck, Award, MapPin, Eye, AlertCircle } from "lucide-react";
import { api, formatMoney, type PropertyListing } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import { announceFeedback } from "../../lib/feedback";
import { EmptyState } from "../../components/ui/EmptyState";
import { ErrorState } from "../../components/ui/ErrorState";
import { Modal } from "../../components/ui/Modal";

interface AdminPropertiesProps {
  view: string;
  token: string;
}

export function AdminProperties({ view, token }: AdminPropertiesProps) {
  const [properties, setProperties] = useState<PropertyListing[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedProp, setSelectedProp] = useState<PropertyListing | null>(null);
  const [modReason, setModReason] = useState("");
  const [error, setError] = useState<unknown>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        setError(null);
        const list = await api.getPropertyModerationQueue(token);
        if (active) setProperties(list);
      } catch (err) {
        if (active) setError(err);
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => { active = false; };
  }, [reloadKey]);

  async function handleApprove(id: string) {
    try {
      await api.moderateProperty(id, token, { status: "Approved" });
      announceFeedback("Property approved and is live in Explore.");
      setProperties((current) => current.map((property) => property.id === id ? { ...property, moderationStatus: "Approved", moderationReason: null } : property));
    } catch (err) {
      setError(err);
    }
  }

  async function handleReject(id: string) {
    if (!modReason.trim()) {
      announceFeedback("Please provide a rejection reason.", "error");
      return;
    }
    try {
      await api.moderateProperty(id, token, { status: "Rejected", reason: modReason.trim() });
      announceFeedback("Property rejected and hidden from Explore.", "info");
      setProperties((current) => current.map((property) => property.id === id ? { ...property, moderationStatus: "Rejected", moderationReason: modReason.trim() } : property));
      setModReason("");
      setSelectedProp(null);
    } catch (err) {
      setError(err);
    }
  }

  return (
    <div className="page-container container py-6" data-testid="adm-04-page" id="ADM-04">
      <header className="page-header mb-6">
        <span className="badge badge-sun">ADM-04 / ADM-05</span>
        <h2>Property Moderation & Accreditation</h2>
        <PatoisPhrase phrase="Review & Verify Stays" translation="Approve property submissions, assign verified badges, and enforce listing compliance." />
      </header>

      {loading ? (
        <div className="loading-shimmer p-6 text-center">Loading property moderation queue...</div>
      ) : error ? (
        <ErrorState message={error} onRetry={() => { setLoading(true); setError(null); setReloadKey((key) => key + 1); }} />
      ) : properties.length === 0 ? (
        <EmptyState title="No properties awaiting review" copy="New property submissions will appear in this moderation queue." />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {properties.map((prop) => (
            <div key={prop.id} className="card-box flex flex-col justify-between">
              <div>
                <div className="flex justify-between items-start mb-2">
                  <span className="badge badge-green">{prop.badgeLevel} Badge</span>
                  <span className={`badge ${prop.moderationStatus === "Approved" ? "badge-green" : prop.moderationStatus === "Rejected" ? "badge-coral" : "badge-sun"}`}>{prop.moderationStatus ?? "Pending"}</span>
                </div>
                <h3 className="font-bold text-xl">{prop.title}</h3>
                <p className="subtext mt-1"><MapPin size={14} className="inline" /> {prop.location}, {prop.country}</p>
                <p className="mt-2 text-sm">Host: <strong>{prop.hostName}</strong></p>
                <div className="mt-3 text-lg font-bold text-sun">
                  {formatMoney(prop.nightlyRate, prop.currency)} <span className="text-xs font-normal text-gray-500">/ night</span>
                </div>
                {prop.moderationReason && <p className="mt-2 rounded-field bg-coral-tint px-3 py-2 text-sm text-coral-text"><strong>Reason:</strong> {prop.moderationReason}</p>}
              </div>

              <div className="flex justify-between items-center mt-6 pt-3 border-t">
                <a href={`/properties/${prop.id}`} className="btn btn-outline btn-sm">
                  <Eye size={14} /> Review Details
                </a>
                <div className="flex gap-2">
                  <button type="button" className="btn btn-primary btn-sm" disabled={prop.moderationStatus === "Approved"} onClick={() => void handleApprove(prop.id)}>
                    <Check size={14} /> Approve
                  </button>
                  <button type="button" className="btn btn-ghost btn-sm text-coral" onClick={() => { setModReason(prop.moderationReason ?? ""); setSelectedProp(prop); }}>
                    <X size={14} /> Reject
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      <Modal open={Boolean(selectedProp)} title="Reject property submission" onClose={() => setSelectedProp(null)} variant="sheet">
        {selectedProp && <>
          <p className="subtext mb-3">Provide a clear feedback reason for {selectedProp.title}.</p>
          <label className="field-label" htmlFor="property-rejection-reason">Rejection reason</label>
          <textarea id="property-rejection-reason" aria-describedby="property-rejection-help" className="input-control mb-1 mt-1" rows={4} placeholder="Explain what the host needs to correct." value={modReason} onChange={(e) => setModReason(e.target.value)} />
          <p id="property-rejection-help" className="m-0 text-xs text-sand-500">This reason is saved with the moderation decision and shown to the host.</p>
          <div className="mt-4 flex flex-wrap justify-end gap-2">
            <button type="button" className="btn btn-ghost" onClick={() => setSelectedProp(null)}>Cancel</button>
            <button type="button" className="btn btn-primary" onClick={() => void handleReject(selectedProp.id)}>Confirm rejection</button>
          </div>
        </>}
      </Modal>
    </div>
  );
}
