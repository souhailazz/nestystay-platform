import { useEffect, useMemo, useState } from "react";
import { Heart, Map, Search, Users } from "lucide-react";
import { AppLink } from "../../components/AppLink";
import { PublicFooter, TierBadge } from "../../components/layout/PublicShell";
import { ErrorState } from "../../components/ui/ErrorState";
import { ListControls, downloadCsv } from "../../components/ui/ListControls";
import { api, formatMoney, type PropertyListing } from "../../lib/api";
import { getStayImage } from "../../lib/stayImages";
import { cx } from "../../lib/ui";
import { BookingModal } from "../../components/booking/BookingModal";
import type { AuthSession } from "../../lib/auth";

interface PublicSearchMapProps {
  view: string;
  session: AuthSession | null;
}

const BADGE_FILTERS = [["all", "All"], ["free", "Free"], ["verified", "✓ Verified"], ["trusted", "★ Trusted"], ["wellness", "✦ Wellness"]] as const;
type BadgeFilter = (typeof BADGE_FILTERS)[number][0];
const chipBase = "min-h-11 cursor-pointer rounded-pill px-5 font-sans text-[13.5px] font-semibold transition-colors";

function ExploreLoadingState() {
  return (
    <div aria-busy="true" className="grid grid-cols-1 gap-[18px] sm:grid-cols-2 xl:grid-cols-3" role="status">
      <span className="sr-only">Searching available stays…</span>
      {Array.from({ length: 6 }, (_, index) => (
        <article aria-hidden="true" className="overflow-hidden rounded-card border border-sand-border bg-cream p-0" key={index}>
          <div className="aspect-[3/2] animate-pulse bg-shell" />
          <div className="grid min-h-[210px] gap-3 p-[18px]">
            <div className="h-5 w-3/4 animate-pulse rounded bg-shell" />
            <div className="h-4 w-1/2 animate-pulse rounded bg-shell" />
            <div className="h-8 w-full animate-pulse rounded bg-shell" />
            <div className="h-4 w-2/3 animate-pulse rounded bg-shell" />
            <div className="mt-auto h-11 w-full animate-pulse rounded-field bg-shell" />
          </div>
        </article>
      ))}
    </div>
  );
}

function isoDate(offsetDays: number) {
  const date = new Date();
  date.setDate(date.getDate() + offsetDays);
  return date.toISOString().slice(0, 10);
}

function stayNights(checkIn: string, checkOut: string) {
  const nights = Math.round((new Date(`${checkOut}T00:00:00`).getTime() - new Date(`${checkIn}T00:00:00`).getTime()) / 86400000);
  return Number.isFinite(nights) && nights > 0 ? nights : 1;
}

function estimatedStayTotal(property: PropertyListing, nights: number) {
  const staySubtotal = property.nightlyRate * nights;
  const guestPlatformFee = staySubtotal * 0.09;
  return staySubtotal + guestPlatformFee + (property.cleaningFee ?? 0) + (property.serviceFee ?? 0);
}

/** PUB-02 — live marketplace search. Dates, guests, and availability are sent to the API. */
export function PublicSearchMap({ session }: PublicSearchMapProps) {
  const initialFilters = useMemo(() => {
    const params = new URLSearchParams(window.location.search);
    const validDate = (value: string | null) => value && /^\d{4}-\d{2}-\d{2}$/.test(value)
      && !Number.isNaN(Date.parse(value)) ? value : null;
    const checkIn = validDate(params.get("checkIn")) ?? isoDate(7);
    const requestedCheckOut = validDate(params.get("checkOut"));
    const count = (key: string, fallback: number, min: number) => {
      const value = Number(params.get(key) ?? fallback);
      return Number.isInteger(value) && value >= min && value <= 16 ? value : fallback;
    };
    return {
      search: params.get("search") ?? "",
      checkIn,
      checkOut: requestedCheckOut && requestedCheckOut > checkIn ? requestedCheckOut : isoDate(11),
      adults: count("adults", 2, 1),
      children: count("children", 0, 0),
    };
  }, []);
  const [properties, setProperties] = useState<PropertyListing[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [query, setQuery] = useState(initialFilters.search);
  const [search, setSearch] = useState(initialFilters.search);
  const [checkIn, setCheckIn] = useState(initialFilters.checkIn);
  const [checkOut, setCheckOut] = useState(initialFilters.checkOut);
  const [adults, setAdults] = useState(initialFilters.adults);
  const [children, setChildren] = useState(initialFilters.children);
  const [badge, setBadge] = useState<BadgeFilter>("all");
  const [sort, setSort] = useState("relevance");
  const [page, setPage] = useState(0);
  const [saved, setSaved] = useState<Record<string, boolean>>({});
  const [wishlistItems, setWishlistItems] = useState<Record<string, string>>({});
  const [wishlistCollectionId, setWishlistCollectionId] = useState<string | null>(null);
  const [savingId, setSavingId] = useState<string | null>(null);
  const [bookingProp, setBookingProp] = useState<PropertyListing | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    api.getProperties({ search, checkIn, checkOut, adults, children })
      .then((list) => { if (active) setProperties(list); })
      .catch((err) => { if (active) setError(err instanceof Error ? err.message : "Could not load stays."); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [search, checkIn, checkOut, adults, children, reloadKey]);

  useEffect(() => {
    let active = true;
    if (!session) {
      setSaved({});
      setWishlistItems({});
      setWishlistCollectionId(null);
      return () => { active = false; };
    }
    api.getTravelerWorkspace(session.userId, session.accessToken).then((workspace) => {
      if (!active) return;
      const collections = [...workspace.wishlistCollections].sort((a, b) => a.sortOrder - b.sortOrder);
      const collection = collections[0];
      if (collection) setWishlistCollectionId(collection.id);
      const nextSaved: Record<string, boolean> = {};
      const nextItems: Record<string, string> = {};
      collections.flatMap((itemCollection) => itemCollection.items).forEach((item) => { nextSaved[item.propertyId] = true; nextItems[item.propertyId] = item.id; });
      setSaved(nextSaved);
      setWishlistItems(nextItems);
    }).catch(() => { /* the click handler still creates a collection on demand */ });
    return () => { active = false; };
  }, [session?.userId, session?.accessToken]);

  const filtered = useMemo(() => {
    const results = properties.filter((p) => badge === "all" || p.badgeLevel.toLowerCase().includes(badge));
    return [...results].sort((left, right) => {
      if (sort === "price-low") return left.nightlyRate - right.nightlyRate;
      if (sort === "price-high") return right.nightlyRate - left.nightlyRate;
      if (sort === "name") return left.title.localeCompare(right.title);
      return 0;
    });
  }, [properties, badge, sort]);

  useEffect(() => setPage(0), [badge, search, sort, checkIn, checkOut, adults, children]);
  const pageSize = 9;
  const visibleProperties = filtered.slice(page * pageSize, (page + 1) * pageSize);

  async function toggleSave(property: PropertyListing) {
    if (!session) {
      window.location.href = `/login?returnTo=${encodeURIComponent(window.location.pathname + window.location.search)}`;
      return;
    }
    setSavingId(property.id);
    try {
      if (saved[property.id] && wishlistItems[property.id]) {
        await api.removeWishlistItem(session.userId, wishlistItems[property.id], session.accessToken);
        setSaved((current) => ({ ...current, [property.id]: false }));
        setWishlistItems((current) => { const next = { ...current }; delete next[property.id]; return next; });
      } else {
        let collectionId = wishlistCollectionId;
        if (!collectionId) {
          const created = await api.createWishlistCollection(session.userId, session.accessToken, { name: "Saved stays" });
          collectionId = created.id;
          setWishlistCollectionId(collectionId);
        }
        const item = await api.addWishlistItem(session.userId, collectionId, session.accessToken, { propertyId: property.id, propertyTitle: property.title });
        setSaved((current) => ({ ...current, [property.id]: true }));
        setWishlistItems((current) => ({ ...current, [property.id]: item.id }));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "This stay could not be saved.");
    } finally {
      setSavingId(null);
    }
  }

  return (
    <div className="font-sans text-[15px] leading-[1.55] text-ink">
      <header className="mx-auto flex max-w-[1200px] flex-col gap-5 px-6 pb-2 pt-11">
        <div className="flex flex-wrap items-baseline justify-between gap-4">
          <div><p className="m-0 text-xs font-bold uppercase tracking-[0.14em] text-deep-hover">Live marketplace</p><h1 className="m-0 font-display text-[clamp(28px,3.4vw,38px)] font-normal tracking-[-0.015em]">Explore stays</h1></div>
          <AppLink className="inline-flex min-h-11 items-center gap-2 text-[14.5px] font-semibold text-deep-hover hover:text-deep" href="/explore/map"><Map size={16} /> Open interactive map →</AppLink>
        </div>
        <form className="grid gap-3 rounded-card border border-sand-border bg-cream p-4 shadow-[0_8px_24px_rgba(96,74,20,0.06)] md:grid-cols-[minmax(220px,1.5fr)_repeat(2,minmax(145px,0.75fr))_minmax(130px,0.7fr)_auto]" onSubmit={(event) => { event.preventDefault(); setSearch(query); }}>
          <label className="flex min-h-12 items-center gap-2.5 rounded-field border-[1.5px] border-sand-input bg-white px-4 transition-colors focus-within:border-deep-hover"><Search aria-hidden="true" className="shrink-0 text-sand-500" size={17} /><span className="sr-only">Location or property</span><input aria-label="Search by location, parish, or property name" className="min-h-11 min-w-0 flex-1 border-none bg-transparent text-[15px] outline-none placeholder:text-sand-500" onChange={(event) => setQuery(event.target.value)} placeholder="Location, parish or stay name" type="search" value={query} /></label>
          <label className="rounded-field border-[1.5px] border-sand-input bg-white px-3 py-2"><span className="block text-[10.5px] font-bold uppercase tracking-[0.08em] text-sand-500">Check-in</span><input aria-label="Check-in date" className="min-h-7 w-full border-none bg-transparent text-sm font-semibold outline-none" min={isoDate(0)} onChange={(event) => setCheckIn(event.target.value)} type="date" value={checkIn} /></label>
          <label className="rounded-field border-[1.5px] border-sand-input bg-white px-3 py-2"><span className="block text-[10.5px] font-bold uppercase tracking-[0.08em] text-sand-500">Check-out</span><input aria-label="Check-out date" className="min-h-7 w-full border-none bg-transparent text-sm font-semibold outline-none" min={checkIn} onChange={(event) => setCheckOut(event.target.value)} type="date" value={checkOut} /></label>
          <label className="rounded-field border-[1.5px] border-sand-input bg-white px-3 py-2"><span className="flex items-center gap-1 text-[10.5px] font-bold uppercase tracking-[0.08em] text-sand-500"><Users size={12} /> Guests</span><select aria-label="Guest count" className="min-h-7 w-full border-none bg-transparent text-sm font-semibold outline-none" value={adults + children} onChange={(event) => setAdults(Math.max(1, Number(event.target.value)))}>{Array.from({ length: 8 }, (_, index) => index + 1).map((count) => <option key={count} value={count}>{count} guest{count === 1 ? "" : "s"}</option>)}</select></label>
          <button className="min-h-12 cursor-pointer rounded-field border-none bg-deep px-6 font-semibold text-white transition-colors hover:bg-deep-hover" type="submit">Search</button>
        </form>
        <div className="flex flex-wrap items-center gap-2"><span className="mr-1 text-xs font-semibold uppercase tracking-[0.08em] text-sand-500">Host badge</span>{BADGE_FILTERS.map(([value, label]) => <button className={cx(chipBase, badge === value ? "border-none bg-deep text-white" : "border-[1.5px] border-sand-input bg-cream text-gray-600 hover:border-deep-hover hover:text-deep-hover")} key={value} onClick={() => setBadge(value)} type="button">{label}</button>)}{!loading && <span className="ml-auto text-[13px] text-sand-500">{filtered.length} {filtered.length === 1 ? "stay" : "stays"}</span>}</div>
        <ListControls className="mt-1" label="Filter visible stays" onExport={() => downloadCsv("nesty-stays.csv", ["Title", "Location", "Country", "Badge", "Nightly rate"], filtered.map((property) => [property.title, property.location, property.country, property.badgeLevel, property.nightlyRate]))} onPageChange={setPage} onQueryChange={(value) => { setQuery(value); setSearch(value); }} onSortChange={setSort} page={page} pageSize={pageSize} query={query} sort={sort} sortOptions={[{ value: "relevance", label: "Relevance" }, { value: "name", label: "Name" }, { value: "price-low", label: "Price: low to high" }, { value: "price-high", label: "Price: high to low" }]} total={filtered.length} />
      </header>
      <main className="mx-auto max-w-[1200px] px-6 pb-14 pt-7">
        {loading ? <ExploreLoadingState /> : error ? <ErrorState message={error} onRetry={() => setReloadKey((key) => key + 1)} /> : filtered.length === 0 ? <div className="flex flex-col items-center gap-2.5 rounded-card border border-dashed border-sand-input bg-cream px-6 py-10 text-center"><Search size={20} className="text-sand-500" /><div className="font-display text-lg font-medium">No available stays match those dates</div><div className="max-w-[360px] text-[13.5px] text-gray-600">Try another parish, different dates, or a smaller guest group.</div><button className="mt-1.5 min-h-11 cursor-pointer rounded-field border-[1.5px] border-deep-hover bg-transparent px-5 font-semibold text-deep-hover" onClick={() => { setBadge("all"); setQuery(""); setSearch(""); setCheckIn(isoDate(7)); setCheckOut(isoDate(11)); setAdults(2); setChildren(0); }} type="button">Clear filters</button></div> : <div className="grid grid-cols-[repeat(auto-fill,minmax(300px,1fr))] gap-[18px]">{visibleProperties.map((prop, index) => { const reviewLabel = prop.reviewCount ? `★ ${(prop.ratingAverage ?? 0).toFixed(1)} (${prop.reviewCount})` : "New"; const nights = stayNights(checkIn, checkOut); const verificationLabel = prop.hostVerificationStatus === "Approved" ? "Host identity verified" : prop.hostVerificationStatus === "Pending" ? "Host verification pending" : "Host identity not yet verified"; return <article className="flex flex-col overflow-hidden rounded-card border border-sand-border bg-cream shadow-[0_1px_2px_rgba(96,74,20,0.08)] transition-shadow hover:shadow-[0_16px_34px_rgba(96,74,20,0.16)]" key={prop.id}><div className="relative aspect-[3/2]"><img alt={`${prop.title} — ${prop.location}`} className="block h-full w-full object-cover" height={533} loading="lazy" src={prop.imageUrl ?? prop.galleryUrls?.[0] ?? getStayImage(index).src} width={800} /><TierBadge className="absolute left-3.5 top-3.5" level={prop.badgeLevel} /><button aria-label={`${saved[prop.id] ? "Remove" : "Save"} ${prop.title}`} aria-pressed={Boolean(saved[prop.id])} className={cx("absolute right-2.5 top-2.5 grid size-11 cursor-pointer place-items-center rounded-full border-none bg-white/90 transition-colors", saved[prop.id] ? "text-coral" : "text-gray-600 hover:text-coral")} disabled={savingId === prop.id} onClick={() => void toggleSave(prop)} type="button"><Heart fill={saved[prop.id] ? "currentColor" : "none"} size={18} /></button></div><div className="flex flex-1 flex-col gap-2 p-4 px-[18px]"><div className="flex items-baseline justify-between gap-2.5"><span className="font-display text-[17px] font-medium">{prop.title}</span><span className="whitespace-nowrap text-[13px] text-gray-600">{reviewLabel}</span></div><div className="text-[13px] text-gray-600">{prop.location} · {prop.parish || prop.country}</div><div className="flex flex-wrap gap-1.5 text-[11.5px] font-semibold text-gray-600"><span className="rounded-pill bg-shell px-2.5 py-1">{prop.bedrooms ?? 1} bedroom{(prop.bedrooms ?? 1) === 1 ? "" : "s"}</span><span className="rounded-pill bg-shell px-2.5 py-1">Up to {prop.maxGuests ?? 2} guests</span>{(prop.amenities ?? []).slice(0, 2).map((amenity) => <span className="rounded-pill bg-shell px-2.5 py-1" key={amenity}>{amenity}</span>)}</div><div className="flex flex-wrap gap-2 text-[12px] text-gray-600"><span className="rounded-pill bg-success-tint px-2.5 py-1">{verificationLabel}</span>{prop.guestVerificationEnabled && <span className="rounded-pill bg-info-tint px-2.5 py-1">eKYC required</span>}</div><div className="flex flex-wrap items-baseline justify-between gap-2 border-t border-shell pt-2"><span className="text-base"><strong>{formatMoney(prop.nightlyRate, prop.currency)}</strong> <span className="text-[13px] text-sand-500">/ night</span></span><span className="text-right text-[12px] text-gray-600"><strong>Est. total {formatMoney(estimatedStayTotal(prop, nights), prop.currency)}</strong><br />{nights} night{nights === 1 ? "" : "s"}, incl. fees</span></div><div className="flex items-center justify-end gap-2.5 pt-1"><AppLink className="inline-flex min-h-11 items-center rounded-[12px] border-[1.5px] border-deep-hover px-4 text-[13.5px] font-semibold text-deep-hover" href={`/properties/${prop.id}`}>Details</AppLink><button className="inline-flex min-h-11 cursor-pointer items-center rounded-[12px] border-none bg-deep px-4 font-semibold text-white" onClick={() => setBookingProp(prop)} type="button">Book</button></div></div></article>; })}</div>}
      </main>
      <PublicFooter />
      <BookingModal open={Boolean(bookingProp)} onClose={() => setBookingProp(null)} onCreated={(created) => { const nextStep = ["PENDING", "PENDING_VERIFICATION", "PENDINGVERIFICATION"].includes(created.status.trim().toUpperCase()) ? "identity" : "checkout"; window.location.href = `/booking/${created.id}/${nextStep}`; }} property={bookingProp} session={session} />
    </div>
  );
}
