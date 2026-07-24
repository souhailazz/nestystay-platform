import { useState, useEffect } from "react";
import { Check, X, ShieldCheck, Award, MapPin, Eye, AlertCircle } from "lucide-react";
import { api, formatMoney, type PropertyListing } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";

interface AdminPropertiesProps {
  view: string;
  token: string;
}

export function AdminProperties({ view, token }: AdminPropertiesProps) {
  const [properties, setProperties] = useState<PropertyListing[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedProp, setSelectedProp] = useState<PropertyListing | null>(null);
  const [modReason, setModReason] = useState("");

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const list = await api.getProperties();
        if (active) setProperties(list);
      } catch (err) {
        console.error(err);
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => { active = false; };
  }, []);

  async function handleApprove(id: string) {
    alert(`Property ${id} approved.`);
    setSelectedProp(null);
  }

  async function handleReject(id: string) {
    if (!modReason) return alert("Please provide a rejection reason.");
    alert(`Property ${id} rejected with reason: ${modReason}`);
    setSelectedProp(null);
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
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {properties.map((prop) => (
            <div key={prop.id} className="card-box flex flex-col justify-between">
              <div>
                <div className="flex justify-between items-start mb-2">
                  <span className="badge badge-green">{prop.badgeLevel} Badge</span>
                  <span className="badge badge-sun">Submitted</span>
                </div>
                <h3 className="font-bold text-xl">{prop.title}</h3>
                <p className="subtext mt-1"><MapPin size={14} className="inline" /> {prop.location}, {prop.country}</p>
                <p className="mt-2 text-sm">Host: <strong>{prop.hostName}</strong></p>
                <div className="mt-3 text-lg font-bold text-sun">
                  {formatMoney(prop.nightlyRate, prop.currency)} <span className="text-xs font-normal text-gray-500">/ night</span>
                </div>
              </div>

              <div className="flex justify-between items-center mt-6 pt-3 border-t">
                <a href={`/properties/${prop.id}`} className="btn btn-outline btn-sm">
                  <Eye size={14} /> Review Details
                </a>
                <div className="flex gap-2">
                  <button type="button" className="btn btn-primary btn-sm" onClick={() => handleApprove(prop.id)}>
                    <Check size={14} /> Approve
                  </button>
                  <button type="button" className="btn btn-ghost btn-sm text-coral" onClick={() => setSelectedProp(prop)}>
                    <X size={14} /> Reject
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {selectedProp && (
        <div className="modal-backdrop">
          <div className="modal-card">
            <h3>Reject Property Submission</h3>
            <p className="subtext mb-3">Provide a clear feedback reason for {selectedProp.title}.</p>
            <textarea className="input-control mb-4" rows={3} placeholder="Reason for rejection..." value={modReason} onChange={(e) => setModReason(e.target.value)} />
            <div className="flex justify-end gap-2">
              <button type="button" className="btn btn-ghost" onClick={() => setSelectedProp(null)}>Cancel</button>
              <button type="button" className="btn btn-primary" onClick={() => handleReject(selectedProp.id)}>Confirm Rejection</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
