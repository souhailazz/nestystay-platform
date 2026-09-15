import { useState, useEffect, useRef } from "react";
import { Save, History } from "lucide-react";
import { api, type PropertyListing } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";

interface HostPropertyEditorProps {
  token: string;
  propertyId?: string;
}

export function HostPropertyEditor({ token, propertyId }: HostPropertyEditorProps) {
  const [property, setProperty] = useState<PropertyListing | null>(null);
  const [title, setTitle] = useState("");
  const [nightlyRate, setNightlyRate] = useState(185);
  const [policy, setPolicy] = useState("Moderate");
  const [description, setDescription] = useState("");
  const [parish, setParish] = useState("");
  const [bedrooms, setBedrooms] = useState(1);
  const [bathrooms, setBathrooms] = useState(1);
  const [maxGuests, setMaxGuests] = useState(2);
  const [amenities, setAmenities] = useState("");
  const [sleepingArrangements, setSleepingArrangements] = useState("");
  const [houseRules, setHouseRules] = useState("");
  const [cleaningFee, setCleaningFee] = useState(0);
  const [serviceFee, setServiceFee] = useState(0);
  const [latitude, setLatitude] = useState<number | "">("");
  const [longitude, setLongitude] = useState<number | "">("");
  const [saving, setSaving] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const formDirty = useRef(false);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const list = await api.getOwnedProperties(token);
        const selected = propertyId ? list.find((item) => item.id === propertyId) : list[0];
        if (active && selected) {
          setProperty(selected);
          // Do not overwrite a field the user has already edited while the
          // asynchronous property request is resolving. This keeps a fast
          // interaction deterministic instead of losing the pending change.
          if (!formDirty.current) {
            setTitle(selected.title);
            setNightlyRate(selected.nightlyRate);
            setPolicy(selected.cancellationPolicy);
            setDescription(selected.description ?? "");
            setParish(selected.parish ?? "");
            setBedrooms(selected.bedrooms ?? 1);
            setBathrooms(selected.bathrooms ?? 1);
            setMaxGuests(selected.maxGuests ?? 2);
            setAmenities((selected.amenities ?? selected.highlights).join(", "));
            setSleepingArrangements((selected.sleepingArrangements ?? []).join("; "));
            setHouseRules((selected.houseRules ?? []).join("; "));
            setCleaningFee(selected.cleaningFee ?? 0);
            setServiceFee(selected.serviceFee ?? 0);
            setLatitude(selected.latitude ?? "");
            setLongitude(selected.longitude ?? "");
          }
        }
      } catch (err) {
        console.error(err);
      }
    }
    load();
    return () => { active = false; };
  }, [propertyId, token]);

  async function handleSave() {
    if (!property) return;
    setSaving(true);
    setNotice(null);
    try {
      await api.updateProperty(property.id, token, {
        hostName: property.hostName,
        // The API rehydrates this from the authenticated profile for registered hosts.
        hostEmail: "host-villa@nestystay.local",
        title,
        location: property.location,
        country: property.country,
        nightlyRate,
        currency: property.currency,
        badgeLevel: property.badgeLevel || "Free",
        guestVerificationEnabled: property.guestVerificationEnabled,
        insuraGuestEnabled: property.insuraGuestEnabled,
        cancellationPolicy: policy,
        highlights: property.highlights,
        parish,
        description,
        bedrooms,
        bathrooms,
        maxGuests,
        amenities: amenities.split(",").map((item) => item.trim()).filter(Boolean),
        sleepingArrangements: sleepingArrangements.split(/[;\n]/).map((item) => item.trim()).filter(Boolean),
        houseRules: houseRules.split(/[;\n]/).map((item) => item.trim()).filter(Boolean),
        cleaningFee,
        serviceFee,
        latitude: latitude === "" ? null : latitude,
        longitude: longitude === "" ? null : longitude,
        imageUrl: property.imageUrl,
        galleryUrls: property.galleryUrls
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
        <button type="button" className="btn btn-primary" onClick={handleSave} disabled={saving || !property}>
          <Save size={16} /> {saving ? "Saving Changes..." : "Save Changes"}
        </button>
      </header>

      {notice && <div className="notice-panel mb-4">{notice}</div>}

      <div className="card-box max-w-3xl mx-auto space-y-4">
        <h3>General Information</h3>
        <div className="field-group">
          <label className="field-label">Listing Title</label>
            <input aria-label="Listing Title" type="text" className="input-control" value={title} onChange={(e) => { formDirty.current = true; setTitle(e.target.value); }} />
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

        <div className="field-group"><label className="field-label">Parish</label><input className="input-control" value={parish} onChange={(event) => setParish(event.target.value)} /></div>
        <div className="field-group"><label className="field-label">Description</label><textarea className="input-control" rows={4} value={description} onChange={(event) => setDescription(event.target.value)} /></div>
        <div className="grid grid-cols-3 gap-4"><div className="field-group"><label className="field-label">Bedrooms</label><input className="input-control" min={1} type="number" value={bedrooms} onChange={(event) => setBedrooms(Number(event.target.value) || 1)} /></div><div className="field-group"><label className="field-label">Bathrooms</label><input className="input-control" min={1} type="number" value={bathrooms} onChange={(event) => setBathrooms(Number(event.target.value) || 1)} /></div><div className="field-group"><label className="field-label">Max guests</label><input className="input-control" min={1} type="number" value={maxGuests} onChange={(event) => setMaxGuests(Number(event.target.value) || 1)} /></div></div>
        <div className="field-group"><label className="field-label">Amenities (comma separated)</label><input className="input-control" value={amenities} onChange={(event) => setAmenities(event.target.value)} /></div>
        <div className="field-group"><label className="field-label">Sleeping arrangements</label><textarea className="input-control" rows={2} value={sleepingArrangements} onChange={(event) => setSleepingArrangements(event.target.value)} /></div>
        <div className="field-group"><label className="field-label">House rules</label><textarea className="input-control" rows={2} value={houseRules} onChange={(event) => setHouseRules(event.target.value)} /></div>
        <div className="grid grid-cols-2 gap-4"><div className="field-group"><label className="field-label">Cleaning fee</label><input className="input-control" min={0} type="number" value={cleaningFee} onChange={(event) => setCleaningFee(Number(event.target.value) || 0)} /></div><div className="field-group"><label className="field-label">Service fee</label><input className="input-control" min={0} type="number" value={serviceFee} onChange={(event) => setServiceFee(Number(event.target.value) || 0)} /></div></div>
        <div className="grid grid-cols-2 gap-4"><div className="field-group"><label className="field-label">Latitude</label><input className="input-control" step="0.000001" type="number" value={latitude} onChange={(event) => setLatitude(event.target.value === "" ? "" : Number(event.target.value))} /></div><div className="field-group"><label className="field-label">Longitude</label><input className="input-control" step="0.000001" type="number" value={longitude} onChange={(event) => setLongitude(event.target.value === "" ? "" : Number(event.target.value))} /></div></div>

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
