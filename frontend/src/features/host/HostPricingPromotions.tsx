import { useEffect, useState, type FormEvent } from "react";
import { Calendar, Check, Plus, Tag, Trash2 } from "lucide-react";
import { api, formatMoney, type HostPricingRule, type HostPromotion, type PropertyListing } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import { announceFeedback } from "../../lib/feedback";

interface HostPricingPromotionsProps {
  view: string;
  token: string;
  hostUserId: string;
}

type FormState = {
  propertyId: string;
  name: string;
  startsOn: string;
  endsOn: string;
  nightlyRate: string;
  minimumStay: string;
  discountPercent: string;
  minimumNights: string;
  badgeLevel: string;
  isActive: boolean;
};

const emptyForm = (propertyId = ""): FormState => ({
  propertyId,
  name: "",
  startsOn: "",
  endsOn: "",
  nightlyRate: "",
  minimumStay: "1",
  discountPercent: "",
  minimumNights: "1",
  badgeLevel: "All",
  isActive: true,
});

export function HostPricingPromotions({ view, token, hostUserId }: HostPricingPromotionsProps) {
  const isPricing = view === "pricing";
  const [pricingRules, setPricingRules] = useState<HostPricingRule[]>([]);
  const [promotions, setPromotions] = useState<HostPromotion[]>([]);
  const [properties, setProperties] = useState<PropertyListing[]>([]);
  const [form, setForm] = useState<FormState>(emptyForm());
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    if (!token || !hostUserId) {
      setLoading(false);
      return () => { active = false; };
    }
    void Promise.all([api.getHostOperations(hostUserId, token), api.getOwnedProperties(token)])
      .then(([operations, ownedProperties]) => {
        if (!active) return;
        setPricingRules(operations.pricingRules);
        setPromotions(operations.promotions);
        setProperties(ownedProperties);
        setForm((current) => current.propertyId ? current : emptyForm(ownedProperties[0]?.id ?? ""));
      })
      .catch((caught) => {
        if (active) setError(caught instanceof Error ? caught.message : "Host pricing data could not be loaded.");
      })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [hostUserId, token]);

  function beginCreate() {
    setEditingId(null);
    setFormOpen(true);
    setNotice(null);
    setError(null);
    setForm(emptyForm(properties[0]?.id ?? ""));
  }

  function beginEdit(rule: HostPricingRule | HostPromotion) {
    setEditingId(rule.id);
    setFormOpen(true);
    setNotice(null);
    setError(null);
    if ("nightlyRate" in rule) {
      setForm({ propertyId: rule.propertyId, name: rule.name, startsOn: rule.startsOn, endsOn: rule.endsOn, nightlyRate: String(rule.nightlyRate), minimumStay: String(rule.minimumStay), discountPercent: "", minimumNights: "1", badgeLevel: "All", isActive: rule.isActive });
    } else {
      setForm({ propertyId: rule.propertyId, name: rule.name, startsOn: rule.startsOn, endsOn: rule.endsOn, nightlyRate: "", minimumStay: "1", discountPercent: String(rule.discountPercent), minimumNights: String(rule.minimumNights), badgeLevel: rule.badgeLevel, isActive: rule.isActive });
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!token || !hostUserId) return;
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      if (isPricing) {
        const body = { propertyId: form.propertyId, name: form.name, startsOn: form.startsOn, endsOn: form.endsOn, nightlyRate: Number(form.nightlyRate), minimumStay: Number(form.minimumStay), isActive: form.isActive };
        const saved = editingId ? await api.updateHostPricingRule(hostUserId, editingId, token, body) : await api.saveHostPricingRule(hostUserId, token, body);
        setPricingRules((current) => editingId ? current.map((item) => item.id === saved.id ? saved : item) : [saved, ...current]);
        announceFeedback(editingId ? "Pricing rule updated." : "Pricing rule created.");
      } else {
        const body = { propertyId: form.propertyId, name: form.name, discountPercent: Number(form.discountPercent), startsOn: form.startsOn, endsOn: form.endsOn, minimumNights: Number(form.minimumNights), badgeLevel: form.badgeLevel, isActive: form.isActive };
        const saved = editingId ? await api.updateHostPromotion(hostUserId, editingId, token, body) : await api.saveHostPromotion(hostUserId, token, body);
        setPromotions((current) => editingId ? current.map((item) => item.id === saved.id ? saved : item) : [saved, ...current]);
        announceFeedback(editingId ? "Promotion updated." : "Promotion created.");
      }
      const wasEditing = Boolean(editingId);
      setEditingId(null);
      setFormOpen(false);
      setNotice(wasEditing ? "Your change was saved and will be applied by the booking quote service." : "Saved. Booking quotes now use this server-authoritative rule.");
      setForm(emptyForm(form.propertyId));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "The change could not be saved.");
    } finally {
      setSaving(false);
    }
  }

  async function remove(id: string) {
    if (!token || !hostUserId) return;
    setSaving(true);
    setError(null);
    try {
      if (isPricing) {
        await api.deleteHostPricingRule(hostUserId, id, token);
        setPricingRules((current) => current.filter((item) => item.id !== id));
      } else {
        await api.deleteHostPromotion(hostUserId, id, token);
        setPromotions((current) => current.filter((item) => item.id !== id));
      }
      if (editingId === id) { setEditingId(null); setFormOpen(false); setForm(emptyForm(form.propertyId)); }
      setNotice(`${isPricing ? "Pricing rule" : "Promotion"} removed. It is retained in the audit history.`);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "The item could not be removed.");
    } finally {
      setSaving(false);
    }
  }

  const items = isPricing ? pricingRules : promotions;

  return (
    <div className="page-container container py-6" data-testid={isPricing ? "host-07-page" : "host-08-page"} id={isPricing ? "HOST-07" : "HOST-08"}>
      <header className="page-header mb-6 flex flex-wrap justify-between items-center gap-4">
        <div>
          <span className="badge badge-sun">{isPricing ? "HOST-07" : "HOST-08"}</span>
          <h2>{isPricing ? "Seasonal Pricing & Calendar Rules" : "Promotions & Discounts"}</h2>
          <PatoisPhrase phrase="Optimize Yuh Rate Dem" translation="Manage server-authoritative price overrides, minimum night rules, and promotional discounts." />
        </div>
        <button type="button" className="btn btn-primary" onClick={beginCreate} disabled={loading || properties.length === 0}>
          <Plus size={16} /> {isPricing ? "Add Pricing Rule" : "Add Promotion"}
        </button>
      </header>

      {loading && <div className="notice-panel" role="status">Loading your saved {isPricing ? "pricing rules" : "promotions"}…</div>}
      {error && <div className="notice-panel" role="alert">{error}</div>}
      {notice && <div className="notice-panel" role="status">{notice}</div>}
      {!loading && properties.length === 0 && <div className="notice-panel">Add a property before creating a pricing rule or promotion.</div>}

      {formOpen && !loading && properties.length > 0 && (
        <form className="card-box mb-5 max-w-3xl" onSubmit={submit}>
          <div className="flex items-center justify-between gap-3">
            <h3 className="m-0">{editingId ? "Edit" : "New"} {isPricing ? "pricing rule" : "promotion"}</h3>
            <button type="button" className="btn btn-ghost btn-sm" onClick={() => { setFormOpen(false); setEditingId(null); }}>Cancel</button>
          </div>
          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <label className="text-sm font-semibold sm:col-span-2">Property<select className="input-control mt-1" required value={form.propertyId} onChange={(event) => setForm({ ...form, propertyId: event.target.value })}>{properties.map((property) => <option key={property.id} value={property.id}>{property.title}</option>)}</select></label>
            <label className="text-sm font-semibold sm:col-span-2">Name<input className="input-control mt-1" required maxLength={100} value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></label>
            <label className="text-sm font-semibold">Starts<input className="input-control mt-1" required type="date" value={form.startsOn} onChange={(event) => setForm({ ...form, startsOn: event.target.value })} /></label>
            <label className="text-sm font-semibold">Ends<input className="input-control mt-1" required type="date" value={form.endsOn} onChange={(event) => setForm({ ...form, endsOn: event.target.value })} /></label>
            {isPricing ? <>
              <label className="text-sm font-semibold">Nightly rate<input className="input-control mt-1" required min="0.01" step="0.01" type="number" value={form.nightlyRate} onChange={(event) => setForm({ ...form, nightlyRate: event.target.value })} /></label>
              <label className="text-sm font-semibold">Minimum nights<input className="input-control mt-1" required min="1" step="1" type="number" value={form.minimumStay} onChange={(event) => setForm({ ...form, minimumStay: event.target.value })} /></label>
            </> : <>
              <label className="text-sm font-semibold">Discount %<input className="input-control mt-1" required min="0.01" max="80" step="0.01" type="number" value={form.discountPercent} onChange={(event) => setForm({ ...form, discountPercent: event.target.value })} /></label>
              <label className="text-sm font-semibold">Minimum nights<input className="input-control mt-1" required min="1" step="1" type="number" value={form.minimumNights} onChange={(event) => setForm({ ...form, minimumNights: event.target.value })} /></label>
              <label className="text-sm font-semibold">Badge eligibility<select className="input-control mt-1" value={form.badgeLevel} onChange={(event) => setForm({ ...form, badgeLevel: event.target.value })}><option>All</option><option>Free</option><option>Verified</option><option>Trusted</option><option>Wellness</option></select></label>
            </>}
            <label className="flex items-center gap-2 text-sm font-semibold sm:col-span-2"><input checked={form.isActive} type="checkbox" onChange={(event) => setForm({ ...form, isActive: event.target.checked })} /> Active in booking quotes</label>
          </div>
          <button className="btn btn-primary mt-5" disabled={saving} type="submit">{saving ? "Saving…" : editingId ? "Save changes" : "Create"}</button>
        </form>
      )}

      {!loading && items.length === 0 ? <div className="card-box max-w-3xl"><p className="m-0">No saved {isPricing ? "pricing rules" : "promotions"} yet. Use the button above to add one.</p></div> : (
        <div className="space-y-4 max-w-3xl">
          {isPricing ? pricingRules.map((rule) => (
            <div key={rule.id} className="card-box flex flex-wrap justify-between items-center gap-4">
              <div><span className="badge badge-sun"><Calendar size={13} /> {rule.name}</span><p className="font-bold mt-1">{rule.startsOn} to {rule.endsOn}</p><p className="subtext">Rate: {formatMoney(rule.nightlyRate, "USD")} / night · Min stay: {rule.minimumStay} nights · {rule.isActive ? "Active" : "Paused"}</p></div>
              <div className="flex gap-2"><button type="button" className="btn btn-outline btn-sm" onClick={() => beginEdit(rule)}>Edit</button><button type="button" className="btn btn-ghost text-coral btn-sm" disabled={saving} onClick={() => void remove(rule.id)}><Trash2 size={16} /> Remove</button></div>
            </div>
          )) : promotions.map((promo) => (
            <div key={promo.id} className="card-box flex flex-wrap justify-between items-center gap-4">
              <div><span className="badge badge-green"><Tag size={13} /> {promo.discountPercent}% OFF</span><h3 className="font-bold mt-1">{promo.name}</h3><p className="subtext">Valid: {promo.startsOn} to {promo.endsOn} · Min stay: {promo.minimumNights} nights · {promo.badgeLevel} hosts · {promo.isActive ? "Active" : "Paused"}</p></div>
              <div className="flex gap-2"><button type="button" className="btn btn-outline btn-sm" onClick={() => beginEdit(promo)}>Edit</button><button type="button" className="btn btn-ghost text-coral btn-sm" disabled={saving} onClick={() => void remove(promo.id)}><Trash2 size={16} /> Remove</button></div>
            </div>
          ))}
        </div>
      )}
      <p className="mt-5 flex items-center gap-2 text-xs text-sand-600"><Check size={14} /> Changes are validated and persisted by the backend before they can affect a quote.</p>
    </div>
  );
}
