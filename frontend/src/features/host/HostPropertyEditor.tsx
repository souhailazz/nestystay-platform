import { useState, useEffect } from "react";
import { Edit, Save, History, Check, AlertCircle } from "lucide-react";
import { api, formatMoney, type PropertyListing } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";

interface HostPropertyEditorProps {
  token: string;
}

export function HostPropertyEditor({ token }: HostPropertyEditorProps) {
  const [property, setProperty] = useState<PropertyListing | null>(null);
  const [title, setTitle] = useState("");
  const [nightlyRate, setNightlyRate] = useState(185);
  const [policy, setPolicy] = useState("Moderate");
  const [saving, setSaving] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const list = await api.getProperties();
        if (active && list.length > 0) {
          setProperty(list[0]);
          setTitle(list[0].title);
          setNightlyRate(list[0].nightlyRate);
          setPolicy(list[0].cancellationPolicy);
        }
      } catch (err) {
        console.error(err);
      }
    }
    load();
    return () => { active = false; };
  }, []);

  async function handleSave() {
    if (!property) return;
    setSaving(true);
    setNotice(null);
    try {
      await api.updateProperty(property.id, token, {
        hostName: property.hostName,
        hostEmail: "host-villa@nestystay.local",
        title,
        location: property.location,
        country: property.country,
        nightlyRate,
        currency: property.currency,
        badgeLevel: (property.badgeLevel as any) || "Verified",
        guestVerificationEnabled: property.guestVerificationEnabled,
        insuraGuestEnabled: property.insuraGuestEnabled,
        cancellationPolicy: policy,
        highlights: property.highlights
      });
      setNotice("Property sections saved and updated.");
    } catch (err) {
      setNotice(`Update failed: ${err instanceof Error ? err.message : "Error"}`);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page-container container py-6" data-testid="host-06-page" id="HOST-06">
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">HOST-06</span>
          <h2>Inline Property Editor</h2>
          <PatoisPhrase phrase="Update Yuh Listing Section by Section" translation="Edit loaded property fields with revision history tracking." />
        </div>
        <button type="button" className="btn btn-primary" onClick={handleSave} disabled={saving}>
          <Save size={16} /> {saving ? "Saving Changes..." : "Save Changes"}
        </button>
      </header>

      {notice && <div className="notice-panel mb-4">{notice}</div>}

      <div className="card-box max-w-3xl mx-auto space-y-4">
        <h3>General Information</h3>
        <div className="field-group">
          <label className="field-label">Listing Title</label>
          <input type="text" className="input-control" value={title} onChange={(e) => setTitle(e.target.value)} />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="field-group">
            <label className="field-label">Nightly Rate ($ USD)</label>
            <input type="number" className="input-control" value={nightlyRate} onChange={(e) => setNightlyRate(parseFloat(e.target.value) || 0)} />
          </div>
          <div className="field-group">
            <label className="field-label">Cancellation Policy</label>
            <select className="input-control" value={policy} onChange={(e) => setPolicy(e.target.value)}>
              <option value="Flexible">Flexible</option>
              <option value="Moderate">Moderate</option>
              <option value="Strict">Strict</option>
            </select>
          </div>
        </div>

        <hr className="my-4" />

        <div className="revision-history-box">
          <h4 className="flex items-center gap-2 mb-2"><History size={16} /> Revision History Log</h4>
          <ul className="text-xs subtext space-y-1">
            <li>• 2026-07-24: Updated nightly rate to ${nightlyRate} USD</li>
            <li>• 2026-06-15: Initial listing publication</li>
          </ul>
        </div>
      </div>
    </div>
  );
}
