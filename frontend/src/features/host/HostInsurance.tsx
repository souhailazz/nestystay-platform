import { useEffect, useMemo, useState } from "react";
import { ArrowUpRight, Check, ShieldCheck } from "lucide-react";
import { AppLink } from "../../components/AppLink";
import { Button } from "../../components/ui/Button";
import { Card } from "../../components/ui/Card";
import { EmptyState } from "../../components/ui/EmptyState";
import { Field, Input, Textarea } from "../../components/ui/Input";
import { StatusChip } from "../../components/ui/StatusChip";
import { api, formatMoney, type InsuranceClaim, type InsurancePlan, type InsurancePolicyEvent, type InsuranceProperty } from "../../lib/api";

function dateLabel(value?: string | null) {
  return value ? new Date(value).toLocaleDateString(undefined, { dateStyle: "medium" }) : "Not available";
}

function statusCopy(status: string) {
  switch (status.toUpperCase()) {
    case "ACTIVE": return "Coverage is active. Damage deposits and waiver forms are not requested for this insured property.";
    case "PENDING": return "InsuraGuest is processing this policy locally. The property remains visible with coverage pending.";
    case "RENEWAL_DUE": return "Coverage reached its renewal window. Renew to keep the property protected.";
    case "FAILED": return "The provider could not activate this policy. Choose a plan and try again.";
    case "CANCELLED": return "Coverage is cancelled. Normal booking protection rules apply.";
    default: return "No active InsuraGuest coverage.";
  }
}

export function HostInsurance({ token }: { token: string }) {
  const [plans, setPlans] = useState<InsurancePlan[]>([]);
  const [properties, setProperties] = useState<InsuranceProperty[]>([]);
  const [claimsByProperty, setClaimsByProperty] = useState<Record<string, InsuranceClaim[]>>({});
  const [eventsByProperty, setEventsByProperty] = useState<Record<string, InsurancePolicyEvent[]>>({});
  const [selectedPlan, setSelectedPlan] = useState<Record<string, string>>({});
  const [claimProperty, setClaimProperty] = useState<string | null>(null);
  const [incidentAt, setIncidentAt] = useState(new Date().toISOString().slice(0, 16));
  const [claimDescription, setClaimDescription] = useState("");
  const [claimEvidence, setClaimEvidence] = useState("[]");
  const [loading, setLoading] = useState(true);
  const [busyProperty, setBusyProperty] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [nextPlans, owned] = await Promise.all([api.getInsurancePlans(), api.getOwnedProperties(token)]);
      const nextProperties = await Promise.all(owned.map(async (property) => {
        const [insurance, claims, events] = await Promise.all([
          api.getInsuranceProperty(property.id, token),
          api.getInsuranceClaims(property.id, token),
          api.getInsurancePolicyEvents(property.id, token),
        ]);
        return { insurance, claims, events };
      }));
      setPlans(nextPlans);
      setProperties(nextProperties.map((item) => item.insurance));
      setClaimsByProperty(Object.fromEntries(nextProperties.map((item) => [item.insurance.propertyId, item.claims])));
      setEventsByProperty(Object.fromEntries(nextProperties.map((item) => [item.insurance.propertyId, item.events])));
      setSelectedPlan((current) => Object.fromEntries(nextProperties.map(({ insurance }) => [insurance.propertyId, current[insurance.propertyId] ?? nextPlans[0]?.code ?? ""])));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Insurance coverage could not be loaded.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void load(); }, [token]);

  const activeCount = useMemo(() => properties.filter((item) => item.status.toUpperCase() === "ACTIVE").length, [properties]);

  async function activate(property: InsuranceProperty) {
    setBusyProperty(property.propertyId);
    setError(null);
    setNotice(null);
    try {
      const policy = await api.activateInsurance(property.propertyId, token, { planCode: selectedPlan[property.propertyId], idempotencyKey: crypto.randomUUID() });
      setNotice(`${property.propertyTitle}: ${policy.status.toLowerCase()} policy state saved.`);
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Insurance policy could not be activated.");
    } finally {
      setBusyProperty(null);
    }
  }

  async function cancel(property: InsuranceProperty) {
    setBusyProperty(property.propertyId);
    setError(null);
    setNotice(null);
    try {
      await api.cancelInsurance(property.propertyId, token, crypto.randomUUID());
      setNotice(`${property.propertyTitle}: coverage cancelled.`);
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Insurance policy could not be cancelled.");
    } finally {
      setBusyProperty(null);
    }
  }

  async function renew(property: InsuranceProperty) {
    setBusyProperty(property.propertyId);
    setError(null);
    setNotice(null);
    try {
      await api.renewInsurance(property.propertyId, token, crypto.randomUUID());
      setNotice(`${property.propertyTitle}: coverage renewed for another month.`);
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Insurance policy could not be renewed.");
    } finally {
      setBusyProperty(null);
    }
  }

  async function submitClaim(event: React.FormEvent) {
    event.preventDefault();
    if (!claimProperty) return;
    setBusyProperty(claimProperty);
    setError(null);
    setNotice(null);
    try {
      const claim = await api.submitInsuranceClaim(claimProperty, token, { incidentAt: new Date(incidentAt).toISOString(), description: claimDescription, evidenceJson: claimEvidence });
      setNotice(`Claim ${claim.providerReference ?? claim.id} submitted locally for provider review.`);
      setClaimDescription("");
      setClaimEvidence("[]");
      setClaimProperty(null);
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Claim could not be submitted.");
    } finally {
      setBusyProperty(null);
    }
  }

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" data-testid="host-insurance-page" id="HOST-INSURANCE">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="mb-1 text-[11px] font-bold uppercase tracking-[0.16em] text-sand-500">HOST-INSURANCE · Coverage</p>
          <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal">InsuraGuest <em className="italic text-deep-hover">protection</em></h1>
          <p className="mb-0 mt-2 max-w-2xl text-[14px] leading-6 text-gray-600">Optional month-to-month property protection. Choose a contract plan, keep coverage state visible, and submit a claim within 72 hours when needed.</p>
        </div>
        <AppLink className="inline-flex min-h-[44px] items-center gap-2 rounded-pill border border-sand-input bg-cream px-4 text-[13px] font-semibold text-ink" href="/host-dashboard">Back to dashboard <ArrowUpRight size={16} /></AppLink>
      </div>

      {error && <div className="rounded-card border border-coral bg-coral-tint p-4 text-sm text-coral-text" role="alert">{error}</div>}
      {notice && <div className="rounded-card border border-mint bg-mint-tint p-4 text-sm text-mint-text" role="status">{notice}</div>}
      <div className="grid gap-3 sm:grid-cols-3">
        <Card><p className="m-0 text-xs uppercase tracking-[0.12em] text-sand-500">Available plans</p><strong className="mt-1 block font-display text-3xl">{plans.length}</strong></Card>
        <Card><p className="m-0 text-xs uppercase tracking-[0.12em] text-sand-500">Managed properties</p><strong className="mt-1 block font-display text-3xl">{properties.length}</strong></Card>
        <Card><p className="m-0 text-xs uppercase tracking-[0.12em] text-sand-500">Active coverage</p><strong className="mt-1 block font-display text-3xl">{activeCount}</strong></Card>
      </div>

      <section aria-labelledby="insurance-plans-heading">
        <div className="mb-3 flex items-center gap-2"><ShieldCheck className="text-deep-hover" size={20} /><h2 className="m-0 font-display text-2xl" id="insurance-plans-heading">Contract plans</h2></div>
        {loading ? <div aria-busy="true" className="grid gap-3 md:grid-cols-3">{[1, 2, 3].map((item) => <div className="h-44 animate-pulse rounded-card border border-sand-border bg-shell" key={item} />)}</div> : <div className="grid gap-3 md:grid-cols-3">{plans.map((plan) => <Card className="flex h-full flex-col" key={plan.code}><div className="flex items-start justify-between gap-2"><div><p className="m-0 text-xs font-bold uppercase tracking-[0.12em] text-sand-500">{plan.market === "US" ? "US" : "Non-US"}</p><h3 className="m-0 mt-1 font-display text-2xl">{formatMoney(plan.monthlyAmount, plan.currency)}<span className="font-sans text-sm text-sand-500"> / month</span></h3></div><StatusChip value={plan.code} /></div><p className="mt-3 text-sm text-sand-600">{plan.coverageSummary}</p><div className="mt-auto flex items-center gap-2 pt-3 text-xs font-semibold text-sand-600"><Check size={15} /> Month-to-month coverage</div></Card>)}</div>}
      </section>

      <section aria-labelledby="property-coverage-heading">
        <div className="mb-3 flex items-center justify-between gap-3"><h2 className="m-0 font-display text-2xl" id="property-coverage-heading">Property coverage</h2><span className="text-sm text-sand-600">Policy state is persisted per property.</span></div>
        {!loading && properties.length === 0 ? <EmptyState title="No properties yet" copy="Create a property before configuring InsuraGuest coverage." /> : <div className="grid gap-4">{properties.map((property) => { const policy = property.policy; const status = property.status.toUpperCase(); const busy = busyProperty === property.propertyId; return <Card key={property.propertyId}><div className="flex flex-wrap items-start justify-between gap-3"><div><h3 className="m-0 font-display text-xl">{property.propertyTitle}</h3><p className="m-0 mt-1 text-sm text-sand-600">{statusCopy(property.status)}</p></div><StatusChip value={property.status.replaceAll("_", " ")} /></div>{policy && <div className="mt-4 grid gap-2 rounded-field bg-shell p-3 text-sm sm:grid-cols-2"><span>Provider <strong className="ml-1">{policy.provider}</strong></span><span>Plan <strong className="ml-1">{policy.planCode}</strong></span><span>Monthly <strong className="ml-1">{formatMoney(policy.monthlyAmount, policy.currency)}</strong></span><span>Effective <strong className="ml-1">{dateLabel(policy.effectiveAt)}</strong></span><span>Renews <strong className="ml-1">{dateLabel(policy.renewsAt)}</strong></span><span>Provider reference <strong className="ml-1">{policy.providerReference ?? "Pending reference"}</strong></span></div>}<div className="mt-4 flex flex-wrap items-end gap-2"><Field label="Plan"><select aria-label={`Insurance plan for ${property.propertyTitle}`} className="min-h-11 rounded-field border-[1.5px] border-sand-input bg-white px-3 text-sm" disabled={busy || status === "ACTIVE"} onChange={(event) => setSelectedPlan((current) => ({ ...current, [property.propertyId]: event.target.value }))} value={selectedPlan[property.propertyId] ?? plans[0]?.code ?? ""}>{plans.map((plan) => <option key={plan.code} value={plan.code}>{plan.code} · {formatMoney(plan.monthlyAmount, plan.currency)}</option>)}</select></Field>{status === "ACTIVE" || status === "RENEWAL_DUE" ? <><Button disabled={busy} onClick={() => void renew(property)} variant="outline">{busy ? "Saving…" : "Renew monthly"}</Button><Button disabled={busy} onClick={() => void cancel(property)} variant="outline">Cancel coverage</Button>{status === "ACTIVE" && <Button disabled={busy} onClick={() => setClaimProperty(property.propertyId)} variant="dark">Submit claim</Button>}</> : <Button disabled={busy || !selectedPlan[property.propertyId]} onClick={() => void activate(property)} variant="dark">{busy ? "Saving…" : status === "FAILED" || status === "CANCELLED" ? "Try coverage again" : "Activate coverage"}</Button>}</div>{(eventsByProperty[property.propertyId]?.length ?? 0) > 0 && <div className="mt-4 rounded-field border border-sand-border p-3"><h4 className="m-0 text-sm font-semibold">Policy lifecycle</h4><div className="mt-2 grid gap-1 text-xs text-sand-600">{eventsByProperty[property.propertyId].map((item) => <div className="flex flex-wrap gap-1" key={item.id}><strong>{item.fromStatus.replaceAll("_", " ")} → {item.toStatus.replaceAll("_", " ")}</strong><span>· {item.reason}</span></div>)}</div></div>}{(claimsByProperty[property.propertyId]?.length ?? 0) > 0 && <div className="mt-4 rounded-field border border-sand-border p-3"><h4 className="m-0 text-sm font-semibold">Claims</h4><div className="mt-2 grid gap-1 text-xs text-sand-600">{claimsByProperty[property.propertyId].map((claim) => <div className="flex flex-wrap justify-between gap-2" key={claim.id}><span>{new Date(claim.incidentAt).toLocaleDateString()} · {claim.description}</span><StatusChip value={claim.status} /></div>)}</div></div>}{claimProperty === property.propertyId && <form className="mt-4 grid gap-3 rounded-field border border-sand-border bg-cream p-4" onSubmit={submitClaim}><h4 className="m-0 font-display text-lg">Submit a claim</h4><p className="m-0 text-xs text-sand-600">Local provider workflow only. Approval or rejection by a real insurer is not implied.</p><Field label="Incident date and time"><Input required max={new Date().toISOString().slice(0, 16)} onChange={(event) => setIncidentAt(event.target.value)} type="datetime-local" value={incidentAt} /></Field><Field label="Description"><Textarea required onChange={(event) => setClaimDescription(event.target.value)} value={claimDescription} /></Field><Field label="Evidence references (JSON)"><Textarea aria-describedby="claim-evidence-help" onChange={(event) => setClaimEvidence(event.target.value)} value={claimEvidence} /><span className="text-xs text-sand-500" id="claim-evidence-help">Use existing private object keys when available; do not paste identity documents here.</span></Field><div className="flex flex-wrap gap-2"><Button disabled={busy || !claimDescription.trim()} type="submit" variant="dark">Submit claim</Button><Button onClick={() => setClaimProperty(null)} type="button" variant="outline">Close</Button></div></form>}</Card>; })}</div>}
      </section>
    </div>
  );
}
