import { useState, useEffect } from "react";
import { X, RefreshCw } from "lucide-react";
import { api, formatMoney } from "../../lib/api";
import type { PropertyListing as Property } from "../../lib/api";
import type { BookingQuote } from "./types";
import { usePatois } from "../../lib/patois";
import { cx } from "../../lib/ui";
import { LineChip } from "./BookingShell";

interface BookingModalProps {
  property: Property;
  onClose: () => void;
  onProceedToReview: (quote: BookingQuote, details: { adults: number; children: number; accessibility: string; protection: string }) => void;
}

/* BOOK-01 (DS v2) — dates & guests. All quote logic unchanged: every price line
   comes from POST /bookings/quote; nothing is computed locally. */

const fieldInput =
  "min-h-12 w-full rounded-field border-[1.5px] border-sand-input bg-white px-4 font-sans text-[14.5px] text-ink outline-none transition-[border-color,box-shadow] focus:border-deep-hover focus:shadow-[0_0_0_3px_rgba(14,74,69,0.12)]";
const fieldLabel = "font-sans text-[13px] font-semibold text-ink";

function StepperControl({
  label,
  sub,
  value,
  onDecrement,
  onIncrement,
}: {
  label: string;
  sub: string;
  value: number;
  onDecrement: () => void;
  onIncrement: () => void;
}) {
  const btn =
    "grid size-11 cursor-pointer place-items-center rounded-full border-[1.5px] border-sand-input bg-white text-lg text-deep-hover transition-colors hover:border-deep-hover";
  return (
    <div className="flex items-center justify-between gap-3 rounded-field border border-sand-border bg-white px-4 py-3">
      <div>
        <strong className="block text-sm font-semibold">{label}</strong>
        <span className="text-xs text-gray-600">{sub}</span>
      </div>
      <div className="flex items-center gap-3">
        <button aria-label={`Fewer ${label}`} className={btn} onClick={onDecrement} type="button">
          −
        </button>
        <span className="w-6 text-center text-[15px] font-semibold">{value}</span>
        <button aria-label={`More ${label}`} className={btn} onClick={onIncrement} type="button">
          +
        </button>
      </div>
    </div>
  );
}

export function BookingModal({ property, onClose, onProceedToReview }: BookingModalProps) {
  const { showPatois } = usePatois();
  const [checkIn, setCheckIn] = useState(() => {
    const today = new Date();
    today.setDate(today.getDate() + 7);
    return today.toISOString().split("T")[0];
  });
  const [checkOut, setCheckOut] = useState(() => {
    const today = new Date();
    today.setDate(today.getDate() + 10);
    return today.toISOString().split("T")[0];
  });
  const [adults, setAdults] = useState(2);
  const [childrenCount, setChildrenCount] = useState(0);
  const [accessibility, setAccessibility] = useState("");
  const [protection, setProtection] = useState("insuraguest");

  const [quote, setQuote] = useState<BookingQuote | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    async function fetchQuote() {
      setLoading(true);
      setError(null);
      try {
        const result = await api.getBookingQuote({
          propertyId: property.id,
          checkIn,
          checkOut,
          adults,
          children: childrenCount,
          accessibilityNeeds: accessibility,
          protectionPlan: protection,
        });
        if (active) {
          setQuote(result as unknown as BookingQuote);
        }
      } catch (err) {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to calculate quote.");
        }
      } finally {
        if (active) setLoading(false);
      }
    }
    fetchQuote();
    return () => {
      active = false;
    };
  }, [property.id, checkIn, checkOut, adults, childrenCount, protection]);

  const nights = checkIn && checkOut ? Math.max(1, Math.ceil((new Date(checkOut).getTime() - new Date(checkIn).getTime()) / (1000 * 3600 * 24))) : 1;
  const isMinimumStayViolated = nights < (property.minimumNights || 1);

  return (
    <div className="fixed inset-0 z-[200] grid place-items-center overflow-y-auto bg-[rgba(6,43,43,0.45)] p-5 font-sans text-[15px] leading-[1.55] text-ink">
      <div className="flex max-h-[calc(100vh-40px)] w-[min(680px,100%)] flex-col overflow-hidden rounded-[22px] bg-cream shadow-modal">
        <header className="flex items-start justify-between gap-4 border-b border-shell p-6 pb-5">
          <div>
            <h2 className="m-0 font-display text-[26px] font-normal">
              Choose your <em className="italic text-deep-hover">dates</em>
            </h2>
            {showPatois && (
              <p aria-label="Tek Time - English: Take your time choosing check-in and guest details." className="m-0 mt-1 text-[13px]">
                <span className="font-display italic text-deep-hover">Tek Time</span>{" "}
                <span className="text-gray-600">— take your time choosing check-in and guest details.</span>
              </p>
            )}
          </div>
          <button
            aria-label="Close modal"
            className="grid size-11 shrink-0 cursor-pointer place-items-center rounded-pill border border-sand-border bg-transparent text-ink transition-colors hover:bg-shell"
            onClick={onClose}
            type="button"
          >
            <X size={18} />
          </button>
        </header>

        <div className="flex flex-col gap-4 overflow-y-auto p-6">
          {/* Property summary */}
          <div className="flex items-center gap-3 rounded-field border border-sand-border bg-white p-3">
            {property.imageUrl && (
              <img alt={property.title} className="block size-16 shrink-0 rounded-[12px] object-cover" src={property.imageUrl} />
            )}
            <div>
              <h3 className="m-0 font-display text-[17px] font-medium">{property.title}</h3>
              <p className="m-0 text-[12.5px] text-gray-600">
                {property.location}, {property.country}
              </p>
              <div className="mt-0.5 text-sm">
                <strong>{formatMoney(property.nightlyRate, property.currency)}</strong>{" "}
                <span className="text-xs text-sand-500">/ night</span>
              </div>
            </div>
          </div>

          {/* Dates */}
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <label className="flex flex-col gap-1.5">
              <span className={fieldLabel}>Check-in</span>
              <input className={fieldInput} onChange={(e) => setCheckIn(e.target.value)} type="date" value={checkIn} />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={fieldLabel}>Check-out</span>
              <input className={fieldInput} onChange={(e) => setCheckOut(e.target.value)} type="date" value={checkOut} />
            </label>
          </div>

          {/* Guests */}
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <StepperControl
              label="Adults"
              onDecrement={() => setAdults(Math.max(1, adults - 1))}
              onIncrement={() => setAdults(adults + 1)}
              sub="Ages 13+"
              value={adults}
            />
            <StepperControl
              label="Children / Pickney"
              onDecrement={() => setChildrenCount(Math.max(0, childrenCount - 1))}
              onIncrement={() => setChildrenCount(childrenCount + 1)}
              sub="Ages 0 – 12"
              value={childrenCount}
            />
          </div>

          {/* Accessibility */}
          <label className="flex flex-col gap-1.5">
            <span className={fieldLabel}>Accessibility preferences</span>
            <select className={fieldInput} onChange={(e) => setAccessibility(e.target.value)} value={accessibility}>
              <option value="">No specific accessibility requirements</option>
              <option value="wheelchair">Step-free access &amp; wide doorways (Wheelchair accessible)</option>
              <option value="ground_floor">Ground floor stay only</option>
              <option value="roll_in_shower">Accessible roll-in shower</option>
              <option value="visual_audio_aids">Visual &amp; hearing impairment aids</option>
            </select>
          </label>

          {/* Protection plan */}
          <div className="flex flex-col gap-1.5">
            <span className={fieldLabel}>Guest &amp; property protection plan</span>
            <div className="flex flex-col gap-2.5">
              {(
                [
                  ["insuraguest", "InsuraGuest Full Protection", "$15.00 / night · Covers accidental property damage & accidental injury during stay."],
                  ["standard", "Standard Protection", "Included · Basic host guarantee coverage."],
                ] as const
              ).map(([value, title, copy]) => (
                <label
                  className={cx(
                    "flex cursor-pointer items-center gap-4 rounded-[18px] bg-white px-5 py-4 transition-shadow",
                    protection === value
                      ? "border-2 border-deep-hover shadow-[0_0_0_3px_rgba(14,74,69,0.1)]"
                      : "border-[1.5px] border-sand-border hover:border-sand-input",
                  )}
                  key={value}
                >
                  <input
                    checked={protection === value}
                    className="size-[22px] accent-deep-hover"
                    name="protection"
                    onChange={(e) => setProtection(e.target.value)}
                    type="radio"
                    value={value}
                  />
                  <div>
                    <strong className="block text-[15px] font-semibold">{title}</strong>
                    <p className="m-0 text-[12.5px] text-gray-600">{copy}</p>
                  </div>
                </label>
              ))}
            </div>
          </div>

          {/* Availability & validation */}
          {quote && quote.datesAvailable && !loading && (
            <div className="rounded-field bg-success-tint px-4 py-3 text-[13px] text-success-text">
              ✓ Available — {quote.nights} night{quote.nights === 1 ? "" : "s"}, {quote.checkIn} to {quote.checkOut}
            </div>
          )}
          {quote && !quote.datesAvailable && !loading && (
            <div className="rounded-field bg-coral-tint px-4 py-3 text-[13px] text-coral-text">
              Those dates are already booked. Try different dates.
            </div>
          )}
          {isMinimumStayViolated && (
            <div className="rounded-field bg-amber-tint px-4 py-3 text-[13px] text-amber-text">
              This property requires a minimum stay of {property.minimumNights || 2} nights. Please extend your
              checkout date.
            </div>
          )}
          {error && (
            <div className="rounded-field bg-coral-tint px-4 py-3 text-[13px] text-coral-text" role="alert">
              {error}
            </div>
          )}

          {/* Server quote */}
          <div className="flex flex-col gap-2 rounded-card border border-sand-border bg-white p-5">
            <h4 className="m-0 text-[13px] font-semibold">
              Price breakdown ({nights} night{nights === 1 ? "" : "s"})
            </h4>
            {loading ? (
              <div className="flex items-center gap-2.5 py-2 text-[13.5px] text-gray-600" data-testid="quote-loading">
                <RefreshCw className="animate-spin" size={17} /> Calculating server quote…
              </div>
            ) : quote ? (
              <div className="flex flex-col text-[13.5px]">
                {quote.priceBreakdown.map((line, idx) => (
                  <div className="flex items-center justify-between gap-2 border-b border-shell py-[7px]" key={idx}>
                    <span className="flex flex-wrap items-center gap-1.5">
                      {line.description}
                      {!line.isRefundable && line.amount > 0 && <LineChip tone="coral">Non-refundable</LineChip>}
                    </span>
                    <strong>{formatMoney(line.amount, line.currency)}</strong>
                  </div>
                ))}
                <div className="flex items-center justify-between py-[9px]">
                  <strong>Total ({quote.currency.toUpperCase()})</strong>
                  <strong className="font-display text-xl">{formatMoney(quote.totalAmount, quote.currency)}</strong>
                </div>
              </div>
            ) : (
              <p className="m-0 text-[13px] text-gray-600">Select valid dates to view total quote.</p>
            )}
          </div>
        </div>

        <footer className="flex items-center justify-end gap-2.5 border-t border-shell p-5 px-6">
          <button
            className="min-h-12 cursor-pointer rounded-pill border-none bg-transparent px-5 font-sans text-sm font-semibold text-ink transition-colors hover:bg-shell"
            onClick={onClose}
            type="button"
          >
            Cancel
          </button>
          <button
            className="group inline-flex min-h-12 cursor-pointer items-center gap-2 rounded-pill border-none bg-deep px-[26px] font-sans text-[15px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover disabled:pointer-events-none disabled:bg-shell disabled:text-sand-500"
            disabled={loading || isMinimumStayViolated || !quote}
            onClick={() => quote && onProceedToReview(quote, { adults, children: childrenCount, accessibility, protection })}
            type="button"
          >
            Continue to quote{" "}
            <span aria-hidden="true" className="inline-block transition-transform duration-200 group-hover:translate-x-1">
              →
            </span>
          </button>
        </footer>
      </div>
    </div>
  );
}
