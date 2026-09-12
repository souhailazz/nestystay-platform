import { useEffect, useState } from "react";
import { AppLink } from "../../components/AppLink";
import { LoadingState } from "../../components/ui/LoadingState";
import { ErrorState } from "../../components/ui/ErrorState";
import { StatusChip } from "../../components/ui/StatusChip";
import { TierBadge } from "../../components/layout/PublicShell";
import { api, formatMoney, type Booking, type PropertyListing } from "../../lib/api";
import { getStayImage } from "../../lib/stayImages";

interface HostAnalyticsProps {
  token: string;
  hostUserId: string;
}

const outlinePill =
  "inline-flex min-h-[46px] items-center rounded-pill border-[1.5px] border-sand-input px-5 font-sans text-[13.5px] font-semibold text-ink transition-colors hover:border-deep";
const deepPill =
  "inline-flex min-h-[46px] items-center gap-2 rounded-pill bg-deep px-[22px] font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover";

function isCancelledBooking(booking: Booking) {
  return /cancel|reject/i.test(booking.status) || /cancel|fail/i.test(booking.paymentStatus ?? "");
}

/* HOST-01 (DS v2) — "Manage Your Yard" (approved lexicon). Metrics computed
   from the live bookings + properties APIs; logic unchanged. */
export function HostAnalytics({ token, hostUserId }: HostAnalyticsProps) {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [properties, setProperties] = useState<PropertyListing[]>([]);
  const [operations, setOperations] = useState<import("../../lib/api").HostOperations | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    async function load() {
      try {
        const [bList, pList, hostOps] = await Promise.all([api.getBookings(token), api.getOwnedProperties(token), hostUserId ? api.getHostOperations(hostUserId, token) : Promise.resolve(null)]);
        if (active) {
          setBookings(bList);
          setProperties(pList);
          setOperations(hostOps);
        }
      } catch (err) {
        if (active) setError(err instanceof Error ? err.message : "Could not load host analytics.");
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => {
      active = false;
    };
  }, [hostUserId, reloadKey, token]);

  if (loading) {
    return (
      <div data-testid="host-01-loading">
        <LoadingState label="Loading your host dashboard" />
      </div>
    );
  }

  const revenue = operations?.analytics.revenue ?? bookings.reduce((sum, b) => sum + (/captur|paid/i.test(b.paymentStatus ?? "") ? b.totalAmount : 0), 0);
  const currency = bookings[0]?.currency ?? "USD";
  const activeBookings = bookings.filter((booking) => !isCancelledBooking(booking));
  const now = new Date();
  const horizon = new Date(now);
  horizon.setDate(horizon.getDate() + 30);
  const occupiedNights = activeBookings.reduce((sum, booking) => {
    const start = Math.max(new Date(booking.checkIn).getTime(), now.getTime());
    const end = Math.min(new Date(booking.checkOut).getTime(), horizon.getTime());
    return sum + Math.max(0, Math.ceil((end - start) / 86_400_000));
  }, 0);
  const occupancy = properties.length > 0 ? Math.min(100, Math.round((occupiedNights / (properties.length * 30)) * 100)) : 0;
  const listingHealth = properties.length > 0
    ? Math.round(properties.reduce((sum, property) => {
      const checks = [property.title, property.location, property.country, property.imageUrl, property.highlights?.length ? "yes" : ""];
      return sum + (checks.filter(Boolean).length / checks.length) * 100;
    }, 0) / properties.length)
    : 0;
  const revenueBars = Array.from({ length: 6 }, (_, index) => {
    const month = new Date(now.getFullYear(), now.getMonth() - (5 - index), 1);
    const nextMonth = new Date(month.getFullYear(), month.getMonth() + 1, 1);
    const amount = bookings
      .filter((booking) => /captur|paid/i.test(booking.paymentStatus ?? ""))
      .filter((booking) => {
        const checkIn = new Date(booking.checkIn);
        return checkIn >= month && checkIn < nextMonth;
      })
      .reduce((sum, booking) => sum + booking.totalAmount, 0);
    return { label: month.toLocaleDateString(undefined, { month: "short" }), amount };
  });
  const maxRevenueBar = Math.max(...revenueBars.map((bar) => bar.amount), 1);
  const upcomingByProperty = new Map<string, number>();
  const pendingByProperty = new Map<string, number>();
  for (const b of bookings) {
    if (/pending/i.test(b.status)) {
      pendingByProperty.set(b.propertyId, (pendingByProperty.get(b.propertyId) ?? 0) + 1);
    } else if (new Date(b.checkOut).getTime() >= Date.now() && !/cancel|reject/i.test(b.status)) {
      upcomingByProperty.set(b.propertyId, (upcomingByProperty.get(b.propertyId) ?? 0) + 1);
    }
  }

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" data-testid="host-01-page" id="HOST-01">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        Manage Your <em className="italic text-deep-hover">Yard</em>
      </h1>
      {error && <ErrorState message={error} onRetry={() => setReloadKey((key) => key + 1)} />}

          <div className="grid grid-cols-[repeat(auto-fit,minmax(190px,1fr))] gap-3.5">
        {(
          [
            ["PROPERTIES", String(properties.length)],
            ["BOOKINGS", String(bookings.length)],
            ["REVENUE", formatMoney(revenue, currency)],
            ["AVG NIGHTLY RATE", formatMoney(operations?.analytics.averageNightlyRate ?? 0, currency)],
            ["AVAILABLE PAYOUT", formatMoney(operations?.payouts?.availableAmount ?? 0, operations?.payouts?.currency ?? currency)],
            ["PAID OUT", formatMoney(operations?.payouts?.paidAmount ?? 0, operations?.payouts?.currency ?? currency)],
          ] as const
        ).map(([label, value]) => (
          <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]" key={label}>
            <div className="text-[11px] font-semibold tracking-[0.16em] text-sand-500">{label}</div>
            <div className="font-display text-[32px] font-medium leading-none">{value}</div>
          </div>
        ))}
      </div>

      <div className="grid gap-3.5 lg:grid-cols-[1fr_1fr]">
        <section aria-labelledby="host-occupancy-heading" className="rounded-card border border-sand-border bg-cream p-[22px]">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h2 className="m-0 font-display text-[19px] font-medium" id="host-occupancy-heading">Occupancy (next 30 days)</h2>
            <span className="font-display text-[26px] font-medium text-deep-hover">{occupancy}%</span>
          </div>
          <div aria-label={`${occupancy}% occupancy`} className="mt-4 h-3 overflow-hidden rounded-pill bg-shell" role="progressbar" aria-valuemax={100} aria-valuemin={0} aria-valuenow={occupancy}>
            <div className="h-full rounded-pill bg-deep transition-[width]" style={{ width: `${occupancy}%` }} />
          </div>
          <p className="mt-3 mb-0 text-[12.5px] text-gray-600">Based on accepted, paid, and held bookings across your live listings.</p>
        </section>
        <section aria-labelledby="host-revenue-heading" className="rounded-card border border-sand-border bg-cream p-[22px]">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h2 className="m-0 font-display text-[19px] font-medium" id="host-revenue-heading">Revenue trend</h2>
            <span className="text-[12px] text-gray-600">Last 6 months</span>
          </div>
          <div aria-label="Revenue by month" className="mt-4 flex h-28 items-end gap-2" role="img">
            {revenueBars.map((bar) => (
              <div className="flex min-w-0 flex-1 flex-col items-center gap-1" key={bar.label}>
                <span className="text-[10px] text-gray-600">{formatMoney(bar.amount, currency)}</span>
                <div className="flex h-16 w-full items-end rounded-t-field bg-shell">
                  <div className="w-full rounded-t-field bg-deep" style={{ height: `${Math.max(4, (bar.amount / maxRevenueBar) * 100)}%` }} />
                </div>
                <span className="text-[10px] font-semibold text-sand-500">{bar.label}</span>
              </div>
            ))}
          </div>
        </section>
      </div>

      <section aria-labelledby="host-actions-heading" className="rounded-card border border-sand-border bg-shell p-[22px]">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="m-0 font-display text-[19px] font-medium" id="host-actions-heading">Dashboard actions</h2>
            <p className="m-0 mt-1 text-[12.5px] text-gray-600">Keep your listings and guest requests moving.</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <AppLink className={deepPill} href="/host/properties/new">+ Create listing</AppLink>
            <AppLink className={outlinePill} href="/bookings">Review bookings</AppLink>
            <AppLink className={outlinePill} href="/host/reports">View reports</AppLink>
          </div>
        </div>
        <div className="mt-4 flex flex-wrap gap-2 text-[12px]">
          {pendingByProperty.size > 0 && <StatusChip value={`${Array.from(pendingByProperty.values()).reduce((a, b) => a + b, 0)} booking requests need review`} />}
          {listingHealth < 100 && <StatusChip value={`Listing health ${listingHealth}%`} />}
          {pendingByProperty.size === 0 && listingHealth === 100 && <StatusChip value="Everything is up to date" />}
        </div>
      </section>

      <section aria-labelledby="host-payout-heading" className="rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div><h2 className="m-0 font-display text-[19px] font-medium" id="host-payout-heading">Payout visibility</h2><p className="m-0 mt-1 text-[12.5px] text-gray-600">Manual settlement ledger backed by captured bookings. Stripe Connect activation remains a client-provider step.</p></div>
          <StatusChip value={operations?.payouts?.settlementMode ?? "Loading"} />
        </div>
        <div className="mt-4 grid gap-3 sm:grid-cols-3">
          <div className="rounded-field bg-shell p-3"><div className="text-xs text-sand-500">Pending</div><strong>{formatMoney(operations?.payouts?.pendingAmount ?? 0, operations?.payouts?.currency ?? currency)}</strong></div>
          <div className="rounded-field bg-shell p-3"><div className="text-xs text-sand-500">Available</div><strong>{formatMoney(operations?.payouts?.availableAmount ?? 0, operations?.payouts?.currency ?? currency)}</strong></div>
          <div className="rounded-field bg-shell p-3"><div className="text-xs text-sand-500">History</div><strong>{operations?.payouts?.history.length ?? 0} records</strong></div>
        </div>
        {operations?.payouts?.history.length ? <div className="mt-3 flex flex-col gap-2">{operations.payouts.history.slice(0, 5).map((payout) => <div className="flex flex-wrap items-center justify-between gap-2 rounded-field border border-sand-border bg-white px-3 py-2 text-sm" key={payout.id}><span className="font-mono">{payout.bookingId.slice(0, 8)}</span><span>{formatMoney(payout.netAmount, payout.currency)}</span><StatusChip value={payout.status} /></div>)}</div> : <div className="mt-3 text-sm text-gray-600">Captured bookings will create payout records here.</div>}
      </section>

      {properties.length === 0 && (
        <div className="flex flex-col items-start gap-3 rounded-card border border-dashed border-sand-input bg-cream p-6">
          <div className="font-display text-lg font-medium">No properties yet</div>
          <AppLink className={deepPill} href="/host/properties/new">
            + Add your first property
          </AppLink>
        </div>
      )}

      {properties.map((property, index) => {
        const upcoming = upcomingByProperty.get(property.id) ?? 0;
        const pending = pendingByProperty.get(property.id) ?? 0;
        return (
          <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]" key={property.id}>
            <div className="flex flex-wrap items-center gap-3.5">
              <img alt="" className="block size-[88px] shrink-0 rounded-field object-cover" src={property.imageUrl ?? getStayImage(index).src} />
              <div className="min-w-[220px] flex-1">
                <div className="font-display text-[19px] font-medium">{property.title}</div>
                <div className="text-[12.5px] text-gray-600">
                  {property.location} · {formatMoney(property.nightlyRate, property.currency)}/night
                </div>
                <div className="mt-2 flex flex-wrap gap-1.5">
                  <StatusChip value={property.isArchived ? "Draft" : "Published"} />
                  <TierBadge className="!px-2.5 !py-1 !text-[10.5px]" level={property.badgeLevel} />
                  {pending > 0 && <StatusChip value={`${pending} pending request${pending === 1 ? "" : "s"}`} />}
                  {upcoming > 0 && (
                    <span className="rounded-pill bg-shell px-2.5 py-1 text-[10.5px] font-bold uppercase tracking-[0.06em] text-gray-600">
                      {upcoming} upcoming
                    </span>
                  )}
                  {property.isArchived && (
                    <span className="rounded-pill bg-shell px-2.5 py-1 text-[10.5px] font-bold uppercase tracking-[0.06em] text-sand-500">
                      Not visible to travelers
                    </span>
                  )}
                </div>
              </div>
              <div className="flex flex-wrap gap-2">
                <AppLink className={outlinePill} href={`/host/properties/edit?id=${property.id}`}>
                  Edit
                </AppLink>
                <AppLink className={deepPill} href="/bookings">
                  Bookings
                </AppLink>
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}
