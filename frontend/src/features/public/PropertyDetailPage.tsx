import { useEffect, useState } from "react";
import { Heart, MapPin, Users } from "lucide-react";
import { AppLink } from "../../components/AppLink";
import { PublicFooter, TierBadge } from "../../components/layout/PublicShell";
import { ErrorState } from "../../components/ui/ErrorState";
import { LoadingState } from "../../components/ui/LoadingState";
import { api, formatMoney, type PropertyListing } from "../../lib/api";
import { getStayImage } from "../../lib/stayImages";
import { cx } from "../../lib/ui";
import { BookingModal } from "../../components/booking/BookingModal";
import type { AuthSession } from "../../lib/auth";

interface PropertyDetailPageProps {
  propertyId?: string;
  session: AuthSession | null;
}

function SirenIcon() {
  return (
    <svg aria-hidden="true" fill="none" height="22" viewBox="0 0 30 30" width="22">
      <path d="M15 5 L27 25.5 H3 Z" stroke="#ffffff" strokeLinejoin="round" strokeWidth="2" />
      <path d="M15 12.5 V18.5" stroke="#ffffff" strokeLinecap="round" strokeWidth="2" />
      <circle cx="15" cy="22" fill="#ffffff" r="1.3" />
      <path d="M6.5 4.5 Q4 7 4 10.5 M23.5 4.5 Q26 7 26 10.5" opacity="0.7" stroke="#ffffff" strokeLinecap="round" strokeWidth="1.6" />
    </svg>
  );
}

function SectionTitle({ children }: { children: string }) {
  return <h2 className="m-0 font-display text-[21px] font-medium">{children}</h2>;
}

function isoDatePlus(days: number) {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return date.toISOString().slice(0, 10);
}

/** PUB-04 — Property page (DS v2). Emergency 119 badge sits under the header,
 *  ABOVE the gallery, above the fold — never in a footer (client contract). */
export function PropertyDetailPage({ propertyId, session }: PropertyDetailPageProps) {
  const [property, setProperty] = useState<PropertyListing | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [savedToWishlist, setSavedToWishlist] = useState(false);
  const [wishlistItemId, setWishlistItemId] = useState<string | null>(null);
  const [wishlistCollectionId, setWishlistCollectionId] = useState<string | null>(null);
  const [checkIn, setCheckIn] = useState(isoDatePlus(30));
  const [checkOut, setCheckOut] = useState(isoDatePlus(34));
  const [adults, setAdults] = useState(2);
  const [children, setChildren] = useState(0);
  const [quote, setQuote] = useState<Awaited<ReturnType<typeof api.quoteBooking>> | null>(null);
  const [availability, setAvailability] = useState<Awaited<ReturnType<typeof api.getPropertyAvailability>> | null>(null);
  const [reviews, setReviews] = useState<Awaited<ReturnType<typeof api.getPropertyReviews>>>([]);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    async function load() {
      try {
        if (!propertyId) throw new Error("This property could not be found.");
        const found = await api.getProperty(propertyId);
        if (active) setProperty(found);
      } catch (err) {
        console.error(err);
        if (active) setError(err instanceof Error ? err.message : "Could not load this property.");
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => {
      active = false;
    };
  }, [propertyId, reloadKey]);

  useEffect(() => {
    if (!property) return;
    let active = true;
    api.getPropertyAvailability(property.id, isoDatePlus(0), isoDatePlus(60)).then((result) => { if (active) setAvailability(result); }).catch(() => { if (active) setAvailability(null); });
    api.getPropertyReviews(property.id).then((result) => { if (active) setReviews(result); }).catch(() => { if (active) setReviews([]); });
    if (session) {
      api.getTravelerWorkspace(session.userId, session.accessToken).then((workspace) => {
        if (!active) return;
        const collections = [...workspace.wishlistCollections].sort((a, b) => a.sortOrder - b.sortOrder);
        const collection = collections.find((candidate) => candidate.items.some((item) => item.propertyId === property.id)) ?? collections[0];
        const item = collections.flatMap((candidate) => candidate.items).find((candidate) => candidate.propertyId === property.id);
        if (collection) setWishlistCollectionId(collection.id);
        if (item) { setSavedToWishlist(true); setWishlistItemId(item.id); }
      }).catch(() => undefined);
    }
    return () => { active = false; };
  }, [property?.id, session?.userId, session?.accessToken]);

  useEffect(() => {
    if (!property || !checkIn || !checkOut) return;
    const handle = window.setTimeout(() => {
      api.quoteBooking({ propertyId: property.id, checkIn, checkOut, adults, children }).then(setQuote).catch(() => setQuote(null));
    }, 250);
    return () => window.clearTimeout(handle);
  }, [property?.id, checkIn, checkOut, adults, children]);

  if (loading) {
    return (
      <div className="mx-auto max-w-[1200px] px-6 py-9">
        <LoadingState label="Loading property details" />
      </div>
    );
  }

  if (error || !property) {
    return (
      <div className="mx-auto max-w-[1200px] px-6 py-9">
        <ErrorState message={error ?? "This property could not be found."} onRetry={() => setReloadKey((k) => k + 1)} />
      </div>
    );
  }

  const nightly = property.nightlyRate;
  const nights = quote?.nights ?? Math.max(1, new Date(`${checkOut}T00:00:00`).getTime() - new Date(`${checkIn}T00:00:00`).getTime()) / 86400000;
  const quoteTotal = quote?.totalAmount ?? nightly * Number(nights);
  const galleryUrls = property.galleryUrls?.filter(Boolean) ?? [];
  const gallery = (galleryUrls.length > 0 ? galleryUrls : [property.imageUrl, getStayImage(1).src, getStayImage(2).src].filter(Boolean)) as string[];
  const heroImage = gallery[0] ?? getStayImage(0).src;

  async function toggleWishlist() {
    const currentProperty = property;
    if (!currentProperty) return;
    if (!session) {
      window.location.href = `/login?returnTo=${encodeURIComponent(window.location.pathname)}`;
      return;
    }
    try {
      if (savedToWishlist && wishlistItemId) {
        await api.removeWishlistItem(session.userId, wishlistItemId, session.accessToken);
        setSavedToWishlist(false);
        setWishlistItemId(null);
        return;
      }
      let collectionId = wishlistCollectionId;
      if (!collectionId) {
        const collection = await api.createWishlistCollection(session.userId, session.accessToken, { name: "Saved stays" });
        collectionId = collection.id;
        setWishlistCollectionId(collectionId);
      }
      const item = await api.addWishlistItem(session.userId, collectionId, session.accessToken, { propertyId: currentProperty.id, propertyTitle: currentProperty.title });
      setSavedToWishlist(true);
      setWishlistItemId(item.id);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "This stay could not be saved.");
    }
  }

  return (
    <div className="font-sans text-[15px] leading-[1.55] text-ink">
      {/* HEADER */}
      <header className="mx-auto flex max-w-[1200px] flex-col gap-3 px-6 pt-9">
        <AppLink
          className="inline-flex min-h-11 items-center self-start text-[13.5px] font-semibold text-deep-hover hover:text-deep"
          href="/explore"
        >
          ← Back to Explore
        </AppLink>
        <div className="flex flex-wrap items-start justify-between gap-5">
          <div className="flex flex-col gap-2">
            <h1 className="m-0 font-display text-[clamp(28px,3.6vw,40px)] font-normal tracking-[-0.015em]">
              {property.title}
            </h1>
            <div className="text-[14.5px] text-gray-600">
              {property.location} · {property.country} · Hosted by {property.hostName}
            </div>
            <div className="flex flex-wrap gap-2">
              <TierBadge level={property.badgeLevel} />
              {property.guestVerificationEnabled && (
                <span className="inline-flex items-center rounded-pill bg-success-tint px-3 py-[5px] text-[11px] font-bold uppercase tracking-[0.06em] text-success-text">
                  ✓ eKYC required
                </span>
              )}
              {property.insuraGuestEnabled && (
                <span className="inline-flex items-center rounded-pill bg-info-tint px-3 py-[5px] text-[11px] font-bold uppercase tracking-[0.06em] text-info-text">
                  InsuraGuest
                </span>
              )}
            </div>
          </div>
          <button
            aria-pressed={savedToWishlist}
            className={cx(
              "flex min-h-11 cursor-pointer items-center gap-2 rounded-field border-[1.5px] bg-cream px-[18px] font-sans text-sm font-semibold transition-colors",
              savedToWishlist ? "border-coral text-coral" : "border-sand-input text-gray-600 hover:border-coral hover:text-coral",
            )}
            onClick={() => void toggleWishlist()}
            type="button"
          >
            <Heart fill={savedToWishlist ? "currentColor" : "none"} size={16} /> {savedToWishlist ? "Saved" : "Save"}
          </button>
        </div>

        {/* RULE 3: emergency badge — under header, ABOVE gallery, above the fold. Never in a footer. */}
        <div className="mt-1.5 flex flex-wrap items-center gap-3 rounded-field bg-emergency px-5 py-3.5 text-white">
          <span className="flex items-center gap-2.5 text-base font-bold">
            <SirenIcon /> Jamaica Emergency: 119
          </span>
          <span className="text-[13px] opacity-90">Police, fire &amp; ambulance — island-wide, toll-free.</span>
        </div>
      </header>

      {/* GALLERY */}
      <section className="mx-auto flex max-w-[1200px] flex-col gap-2.5 px-6 pt-5">
        <div className="min-h-[220px] overflow-hidden rounded-card">
          <img
            alt={`${property.title} — main photo`}
            className="block aspect-[21/9] h-full w-full object-cover"
            src={heroImage}
          />
        </div>
        <div className="grid grid-cols-[repeat(auto-fit,minmax(150px,1fr))] gap-2.5">
          {gallery.slice(0, 5).map((image, i) => (
            <div className="relative aspect-[4/3] overflow-hidden rounded-field" key={image}>
              <img alt={`${property.title} photo ${i + 1}`} className="block h-full w-full object-cover" src={image} />
              {i === 4 && gallery.length > 5 && <span className="absolute inset-0 flex items-center justify-center bg-deep/55 text-[14.5px] font-semibold text-white">+ {gallery.length - 5} photos</span>}
            </div>
          ))}
        </div>
      </section>

      {/* BODY */}
      <main className="mx-auto flex max-w-[1200px] flex-wrap items-start gap-8 px-6 pb-16 pt-9">
        <div className="flex min-w-0 flex-[1_1_560px] flex-col gap-8">
          <div className="flex flex-col gap-2.5">
            <SectionTitle>About this stay</SectionTitle>
            <p className="m-0 max-w-[640px] text-gray-600">{property.description || `${property.title} in ${property.location}, hosted by ${property.hostName}. All messaging and payments stay on-platform.`}</p>
            <div className="flex flex-wrap gap-2 text-sm text-gray-600"><span className="rounded-pill bg-shell px-3 py-1.5">{property.bedrooms ?? 1} bedroom{(property.bedrooms ?? 1) === 1 ? "" : "s"}</span><span className="rounded-pill bg-shell px-3 py-1.5">{property.bathrooms ?? 1} bathroom{(property.bathrooms ?? 1) === 1 ? "" : "s"}</span><span className="rounded-pill bg-shell px-3 py-1.5">Up to {property.maxGuests ?? 2} guests</span></div>
          </div>

          {(property.amenities?.length || property.sleepingArrangements?.length || property.houseRules?.length) ? <div className="grid gap-5 md:grid-cols-3"><div><SectionTitle>Amenities</SectionTitle><ul className="mt-3 space-y-2 text-sm text-gray-600">{(property.amenities ?? []).map((item) => <li key={item}>✓ {item}</li>)}</ul></div><div><SectionTitle>Sleeping arrangements</SectionTitle><ul className="mt-3 space-y-2 text-sm text-gray-600">{(property.sleepingArrangements ?? []).map((item) => <li key={item}>• {item}</li>)}</ul></div><div><SectionTitle>House rules</SectionTitle><ul className="mt-3 space-y-2 text-sm text-gray-600">{(property.houseRules ?? []).map((item) => <li key={item}>• {item}</li>)}</ul></div></div> : null}

          {property.highlights.length > 0 && (
            <div className="flex flex-col gap-3.5">
              <SectionTitle>Highlights</SectionTitle>
              <div className="grid grid-cols-[repeat(auto-fill,minmax(200px,1fr))] gap-2.5">
                {property.highlights.map((h) => (
                  <div
                    className="flex items-center gap-2.5 rounded-field border border-sand-border bg-cream px-4 py-3 text-sm"
                    key={h}
                  >
                    <span className="text-success">✓</span>
                    {h}
                  </div>
                ))}
              </div>
            </div>
          )}

          {property.guestVerificationEnabled && (
            <div className="flex flex-col gap-2.5">
              <SectionTitle>Traveler verification</SectionTitle>
              <div className="flex max-w-[640px] flex-col gap-1.5 rounded-field border border-sand-border bg-cream px-5 py-[18px]">
                <div className="flex flex-wrap items-center gap-2.5 text-[15px] font-semibold">
                  <span className="rounded-pill bg-success-tint px-2.5 py-1 text-[10.5px] font-bold text-success-text">
                    eKYC REQUIRED
                  </span>
                  This host verifies traveler identity before confirming.
                </div>
                <p className="m-0 text-[13.5px] text-gray-600">
                  After booking, you&apos;ll be redirected to a secure verification page. Accepted documents: Passport
                  · National ID · Driver license. Your dates are held for 60 minutes while you verify.
                </p>
              </div>
            </div>
          )}

          <div className="flex flex-col gap-2.5">
            <SectionTitle>Cancellation policy</SectionTitle>
            <div className="max-w-[640px] rounded-field border border-sand-border bg-cream px-5 py-[18px]">
              <div className="text-[15px] font-semibold">{property.cancellationPolicy}</div>
              <p className="m-0 mt-1.5 text-[13.5px] text-gray-600">
                {property.cancellationPolicy.toLowerCase() === "flexible" ? "Full refund up to 24 hours before check-in." : property.cancellationPolicy.toLowerCase() === "moderate" ? "Full refund up to 5 days before check-in; later cancellations may receive a partial refund." : property.cancellationPolicy.toLowerCase() === "strict" ? "A limited refund is available only within the policy window shown at checkout." : "The host has published a custom cancellation policy; review the exact terms before reserving."} The traveler fee is non-refundable where applicable. Refunds follow this policy automatically.
            </p>
            </div>
          </div>

          <div className="flex flex-col gap-2.5"><SectionTitle>Location</SectionTitle>{property.latitude != null && property.longitude != null ? <div className="overflow-hidden rounded-card border border-sand-border bg-cream"><iframe title={`Approximate map location for ${property.title}`} className="h-[260px] w-full border-0" loading="lazy" src={`https://www.openstreetmap.org/export/embed.html?bbox=${property.longitude - 0.06}%2C${property.latitude - 0.04}%2C${property.longitude + 0.06}%2C${property.latitude + 0.04}&layer=mapnik&marker=${property.latitude}%2C${property.longitude}`} /><div className="flex items-center gap-2 px-4 py-3 text-sm text-gray-600"><MapPin size={15} /> Approximate location · {property.location}</div></div> : <div className="rounded-field border border-sand-border bg-shell p-4 text-sm text-gray-600">Approximate map location will be shown after the host publishes coordinates.</div>}</div>

          {availability && <div className="flex flex-col gap-2.5"><SectionTitle>Availability</SectionTitle><div className="grid grid-cols-7 gap-1.5 rounded-field border border-sand-border bg-cream p-3">{availability.days.slice(0, 35).map((day) => <div className={`rounded px-1 py-2 text-center text-[10px] font-semibold ${day.status === "AVAILABLE" ? "bg-success-tint text-success-text" : day.status === "HELD" ? "bg-amber-tint text-amber-text" : "bg-coral-tint text-coral-text"}`} key={day.date} title={day.label ?? day.status}>{new Date(`${day.date}T00:00:00`).getDate()}</div>)}</div></div>}

          <div className="flex flex-col gap-2.5"><SectionTitle>Guest reviews</SectionTitle>{reviews.length === 0 ? <div className="rounded-field border border-sand-border bg-shell p-4 text-sm text-gray-600">No published reviews yet.</div> : <div className="grid gap-3">{reviews.map((review) => <article className="rounded-field border border-sand-border bg-cream p-4" key={review.id}><div className="flex flex-wrap justify-between gap-2"><strong>{review.guestDisplayName}</strong><span className="text-sm text-gray-600">{"★".repeat(Math.max(0, Math.min(5, review.rating)))} · {new Date(review.createdAt).toLocaleDateString()}</span></div><p className="m-0 mt-2 text-sm text-gray-600">{review.text}</p>{review.hostReply && <p className="m-0 mt-2 border-l-2 border-deep pl-3 text-sm text-gray-600"><strong>Host reply:</strong> {review.hostReply}</p>}</article>)}</div>}</div>

          <div className="flex flex-col gap-2.5">
            <SectionTitle>Your host</SectionTitle>
            <div className="flex max-w-[640px] flex-wrap items-center gap-4 rounded-field border border-sand-border bg-cream px-5 py-[18px]">
              <span className="flex size-[52px] items-center justify-center rounded-full bg-deep font-display text-[19px] text-white">
                {property.hostName.slice(0, 1).toUpperCase()}
              </span>
              <div className="min-w-[200px] flex-1">
                <div className="flex flex-wrap items-center gap-2 text-[15px] font-semibold">
                  {property.hostName} · <TierBadge level={property.badgeLevel} />
                </div>
                <div className="mt-0.5 text-[13px] text-gray-600">
                  Hosting on NestyStay · responds within an hour · all messaging stays on-platform
                </div>
              </div>
              <AppLink
                className="inline-flex min-h-11 items-center rounded-field border-[1.5px] border-deep-hover px-[18px] text-sm font-semibold text-deep-hover transition-colors hover:bg-shell"
                href="/messages"
              >
                Message host
              </AppLink>
            </div>
          </div>
        </div>

        {/* BOOKING PANEL (sticky) */}
        <aside className="sticky top-[88px] flex max-w-[400px] flex-[1_1_320px] flex-col gap-4 rounded-card border border-sand-border bg-cream p-6 shadow-[0_6px_24px_rgba(6,43,43,0.08)]">
          <div className="flex items-baseline justify-between">
            <span className="text-[22px]">
              <strong>{formatMoney(nightly, property.currency)}</strong>{" "}
              <span className="text-sm text-sand-500">/ night</span>
            </span>
            <span className="text-[13px] text-gray-600">{property.minimumNights ? `${property.minimumNights}+ nights` : "Flexible"}</span>
          </div>
          <div className="grid grid-cols-2 gap-2"><label className="min-h-11 rounded-[12px] border-[1.5px] border-sand-input px-3 py-2"><span className="block text-[10.5px] font-semibold uppercase tracking-[0.06em] text-sand-500">Check-in</span><input className="w-full border-none bg-transparent text-sm font-semibold outline-none" min={isoDatePlus(0)} onChange={(event) => setCheckIn(event.target.value)} type="date" value={checkIn} /></label><label className="min-h-11 rounded-[12px] border-[1.5px] border-sand-input px-3 py-2"><span className="block text-[10.5px] font-semibold uppercase tracking-[0.06em] text-sand-500">Check-out</span><input className="w-full border-none bg-transparent text-sm font-semibold outline-none" min={checkIn} onChange={(event) => setCheckOut(event.target.value)} type="date" value={checkOut} /></label><label className="col-span-full flex min-h-11 items-center justify-between rounded-[12px] border-[1.5px] border-sand-input px-3 py-2"><span className="flex items-center gap-2"><Users size={15} /><span><span className="block text-[10.5px] font-semibold uppercase tracking-[0.06em] text-sand-500">Guests</span><select className="border-none bg-transparent text-sm font-semibold outline-none" value={adults + children} onChange={(event) => setAdults(Math.max(1, Number(event.target.value)))}>{Array.from({ length: property.maxGuests ?? 2 }, (_, index) => index + 1).map((count) => <option key={count} value={count}>{count} guest{count === 1 ? "" : "s"}</option>)}</select></span></span></label></div>
          <div className="flex flex-col gap-[9px] border-t border-shell pt-3.5 text-sm">
            <div className="flex justify-between">
              <span>
                {formatMoney(nightly, property.currency)} × {nights} nights
              </span>
              <span>{formatMoney(quote?.staySubtotal ?? nightly * Number(nights), property.currency)}</span>
            </div>
            <div className="flex items-center justify-between gap-2 text-gray-600">
              <span>
                Traveler fee{" "}
                <span className="rounded-pill bg-coral-tint px-[7px] py-0.5 text-[10.5px] font-semibold text-coral-text">
                  non-refundable
                </span>
              </span>
              <span>{formatMoney(quote?.guestPlatformFee ?? 0, property.currency)}</span>
            </div>
            {(property.cleaningFee ?? 0) > 0 && <div className="flex justify-between text-gray-600"><span>Cleaning fee</span><span>{formatMoney(property.cleaningFee ?? 0, property.currency)}</span></div>}
            {(property.serviceFee ?? 0) > 0 && <div className="flex justify-between text-gray-600"><span>Service fee</span><span>{formatMoney(property.serviceFee ?? 0, property.currency)}</span></div>}
            {property.guestVerificationEnabled && (
              <div className="flex justify-between text-gray-600">
                <span>eKYC verification</span>
                <span>{formatMoney(0, property.currency)}</span>
              </div>
            )}
            <div className="flex justify-between border-t border-shell pt-2.5 text-[15.5px] font-bold">
              <span>Total</span>
              <span>{formatMoney(quoteTotal, property.currency)}</span>
            </div>
          </div>
          <button
            className="min-h-12 cursor-pointer rounded-field border-none bg-deep font-sans text-[15.5px] font-bold text-white transition-colors hover:bg-deep-hover"
            onClick={() => setShowModal(true)}
            type="button"
          >
            Book this stay
          </button>
          <div className="text-center text-xs text-sand-500">
            You won&apos;t be charged until verification completes.
            {property.guestVerificationEnabled && " Dates held 60 minutes during eKYC."}
          </div>
        </aside>
      </main>

      <PublicFooter />

      <BookingModal
        open={showModal}
        onClose={() => setShowModal(false)}
        onCreated={(created) => {
          const nextStep = ["PENDING", "PENDING_VERIFICATION", "PENDINGVERIFICATION"].includes(created.status.trim().toUpperCase()) ? "identity" : "checkout";
          window.location.href = `/booking/${created.id}/${nextStep}`;
        }}
        property={property}
        session={session}
      />
    </div>
  );
}
