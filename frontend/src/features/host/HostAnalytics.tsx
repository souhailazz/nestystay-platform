import { useEffect, useState } from "react";
import { AppLink } from "../../components/AppLink";
import { LoadingState } from "../../components/ui/LoadingState";
import { StatusChip } from "../../components/ui/StatusChip";
import { TierBadge } from "../../components/layout/PublicShell";
import { api, formatMoney, type Booking, type PropertyListing } from "../../lib/api";
import { getStayImage } from "../../lib/stayImages";

interface HostAnalyticsProps {
  token: string;
}

const outlinePill =
  "inline-flex min-h-[46px] items-center rounded-pill border-[1.5px] border-sand-input px-5 font-sans text-[13.5px] font-semibold text-ink transition-colors hover:border-deep";
const deepPill =
  "inline-flex min-h-[46px] items-center gap-2 rounded-pill bg-deep px-[22px] font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover";

/* HOST-01 (DS v2) — "Manage Your Yard" (approved lexicon). Metrics computed
   from the live bookings + properties APIs; logic unchanged. */
export function HostAnalytics({ token }: HostAnalyticsProps) {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [properties, setProperties] = useState<PropertyListing[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const [bList, pList] = await Promise.all([api.getBookings(token), api.getProperties()]);
        if (active) {
          setBookings(bList);
          setProperties(pList);
        }
      } catch (err) {
        console.error(err);
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => {
      active = false;
    };
  }, [token]);

  if (loading) {
    return (
      <div data-testid="host-01-loading">
        <LoadingState label="Loading your host dashboard" />
      </div>
    );
  }

  const revenue = bookings.reduce((sum, b) => sum + (/captur|paid/i.test(b.paymentStatus ?? "") ? b.totalAmount : 0), 0);
  const currency = bookings[0]?.currency ?? "USD";
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

      <div className="grid grid-cols-[repeat(auto-fit,minmax(190px,1fr))] gap-3.5">
        {(
          [
            ["PROPERTIES", String(properties.length)],
            ["BOOKINGS", String(bookings.length)],
            ["REVENUE", formatMoney(revenue, currency)],
            ["AVG RATING", "—"],
          ] as const
        ).map(([label, value]) => (
          <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]" key={label}>
            <div className="text-[11px] font-semibold tracking-[0.16em] text-sand-500">{label}</div>
            <div className="font-display text-[32px] font-medium leading-none">{value}</div>
          </div>
        ))}
      </div>

      {properties.length === 0 && (
        <div className="flex flex-col items-start gap-3 rounded-card border border-dashed border-sand-input bg-cream p-6">
          <div className="font-display text-lg font-medium">No properties yet</div>
          <AppLink className={deepPill} href="/host/properties">
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
                <AppLink className={outlinePill} href="/host/properties/edit">
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
