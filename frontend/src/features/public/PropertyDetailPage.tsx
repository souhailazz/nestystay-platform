import { useEffect, useMemo, useState } from "react";
import { Heart } from "lucide-react";
import { AppLink } from "../../components/AppLink";
import { PublicFooter, TierBadge } from "../../components/layout/PublicShell";
import { ErrorState } from "../../components/ui/ErrorState";
import { LoadingState } from "../../components/ui/LoadingState";
import { api, formatMoney, type PropertyListing } from "../../lib/api";
import { getStayImage } from "../../lib/stayImages";
import { cx } from "../../lib/ui";
import { BookingModal } from "../booking/BookingModal";

interface PropertyDetailPageProps {
  propertyId?: string;
}

const NIGHTS = 4;

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
export function PropertyDetailPage({ propertyId }: PropertyDetailPageProps) {
  const [property, setProperty] = useState<PropertyListing | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [savedToWishlist, setSavedToWishlist] = useState(false);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    async function load() {
      try {
        const list = await api.getProperties();
        const found = list.find((p) => p.id === propertyId) || list[0];
        if (active) setProperty(found ?? null);
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

  const checkIn = useMemo(() => isoDatePlus(30), []);
  const checkOut = useMemo(() => isoDatePlus(30 + NIGHTS), []);

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
  const subtotal = nightly * NIGHTS;
  const fee = subtotal * 0.1;
  const total = subtotal + fee;
  const gallery = [0, 1, 2, 3].map((i) => getStayImage(i));
  const heroImage = property.imageUrl ?? getStayImage(0).src;

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
            onClick={() => setSavedToWishlist((s) => !s)}
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
          {gallery.map((image, i) => (
            <div className="relative aspect-[4/3] overflow-hidden rounded-field" key={image.src}>
              <img alt={image.alt} className="block h-full w-full object-cover" src={image.src} />
              {i === gallery.length - 1 && (
                <span className="absolute inset-0 flex items-center justify-center bg-deep/55 text-[14.5px] font-semibold text-white">
                  + 14 photos
                </span>
              )}
            </div>
          ))}
        </div>
      </section>

      {/* BODY */}
      <main className="mx-auto flex max-w-[1200px] flex-wrap items-start gap-8 px-6 pb-16 pt-9">
        <div className="flex min-w-0 flex-[1_1_560px] flex-col gap-8">
          <div className="flex flex-col gap-2.5">
            <SectionTitle>About this stay</SectionTitle>
            <p className="m-0 max-w-[640px] text-gray-600">
              {property.title} in {property.location} — hosted by {property.hostName}, with{" "}
              {property.highlights.slice(0, 2).join(" and ").toLowerCase() || "island comforts"} and a{" "}
              {property.cancellationPolicy.toLowerCase()} cancellation policy. All messaging and payments stay
              on-platform.
            </p>
          </div>

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
                Full refund of the nightly rate up to 5 days before check-in. The traveler fee is non-refundable.
                Refunds follow this policy automatically.
              </p>
            </div>
          </div>

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
          <div className="grid grid-cols-2 gap-2">
            <div className="min-h-11 rounded-[12px] border-[1.5px] border-sand-input px-3 py-2">
              <div className="text-[10.5px] font-semibold uppercase tracking-[0.06em] text-sand-500">Check-in</div>
              <div className="text-sm font-semibold">{checkIn}</div>
            </div>
            <div className="min-h-11 rounded-[12px] border-[1.5px] border-sand-input px-3 py-2">
              <div className="text-[10.5px] font-semibold uppercase tracking-[0.06em] text-sand-500">Check-out</div>
              <div className="text-sm font-semibold">{checkOut}</div>
            </div>
            <div className="col-span-full flex min-h-11 items-center justify-between rounded-[12px] border-[1.5px] border-sand-input px-3 py-2">
              <div>
                <div className="text-[10.5px] font-semibold uppercase tracking-[0.06em] text-sand-500">Guests</div>
                <div className="text-sm font-semibold">2 guests</div>
              </div>
              <span className="text-sand-500">▾</span>
            </div>
          </div>
          <div className="flex flex-col gap-[9px] border-t border-shell pt-3.5 text-sm">
            <div className="flex justify-between">
              <span>
                {formatMoney(nightly, property.currency)} × {NIGHTS} nights
              </span>
              <span>{formatMoney(subtotal, property.currency)}</span>
            </div>
            <div className="flex items-center justify-between gap-2 text-gray-600">
              <span>
                Traveler fee (10%){" "}
                <span className="rounded-pill bg-coral-tint px-[7px] py-0.5 text-[10.5px] font-semibold text-coral-text">
                  non-refundable
                </span>
              </span>
              <span>{formatMoney(fee, property.currency)}</span>
            </div>
            {property.guestVerificationEnabled && (
              <div className="flex justify-between text-gray-600">
                <span>eKYC verification</span>
                <span>{formatMoney(0, property.currency)}</span>
              </div>
            )}
            <div className="flex justify-between border-t border-shell pt-2.5 text-[15.5px] font-bold">
              <span>Total</span>
              <span>{formatMoney(total, property.currency)}</span>
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

      {showModal && (
        <BookingModal
          onClose={() => setShowModal(false)}
          onProceedToReview={() => (window.location.href = "/booking/11111111-1111-4111-8111-111111111111/review")}
          property={property}
        />
      )}
    </div>
  );
}
