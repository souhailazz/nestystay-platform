import { useState } from "react";
import { api, formatMoney } from "../../lib/api";
import type { BookingQuote } from "./types";
import type { AuthController } from "../../hooks/useAuth";
import {
  BookingHeading,
  BookingScaffold,
  BookingStepper,
  DEGRESSIVE_NOTE,
  LineChip,
  PriceSummary,
  PropertyMiniHeader,
  bookingCard,
  bookingDeepCta,
  isVerificationLine,
} from "./BookingShell";

interface BookingReviewPageProps {
  quote: BookingQuote;
  details: { adults: number; children: number; accessibility: string; protection: string };
  auth: AuthController;
  onBackToModal: () => void;
  onProceedToCheckout: (bookingId: string, status: string) => void;
}

/* BOOK-02 (DS v2) — quote review. Booking creation logic unchanged; every fee
   line comes from the backend quote (degressive tiers computed server-side). */
export function BookingReviewPage({ quote, details, auth, onBackToModal, onProceedToCheckout }: BookingReviewPageProps) {
  const [billingCountry, setBillingCountry] = useState("JM");
  const [acceptedTerms, setAcceptedTerms] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleCreateBooking() {
    if (!acceptedTerms) {
      setError("Please accept the terms and cancellation policy before proceeding.");
      return;
    }
    if (!auth.session) {
      setError("You must be logged in to complete a booking request.");
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const created = await api.createBooking(
        {
          propertyId: quote.property.id,
          guestUserId: auth.session.userId,
          checkIn: quote.checkIn,
          checkOut: quote.checkOut,
          adults: details.adults,
          children: details.children,
          accessibilityNeeds: details.accessibility,
          protectionPlan: details.protection,
          billingCountry,
          termsAccepted: acceptedTerms,
        },
        auth.session.accessToken,
      );
      onProceedToCheckout(created.id, created.status);
    } catch (err) {
      setError(formatCreateBookingError(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div data-testid="book-02-page" id="BOOK-02">
    <BookingScaffold
      aside={
        <>
          <PropertyMiniHeader
            ekycRequired={quote.requiresGuestVerification}
            subtitle={`${quote.property.location} · ${quote.property.country}`}
            title={quote.property.title}
            wellnessHost={(quote.property.badgeLevel ?? "").toLowerCase().includes("well")}
          />
          <PriceSummary currency={quote.currency} lines={quote.priceBreakdown ?? []} total={quote.totalAmount} />
          <div className="text-[11.5px] text-sand-500">{DEGRESSIVE_NOTE}</div>
          <button className={bookingDeepCta} disabled={loading || !acceptedTerms} onClick={handleCreateBooking} type="button">
            {loading
              ? "Creating reservation…"
              : quote.requiresGuestVerification
                ? "Continue to identity →"
                : "Continue to payment →"}
          </button>
          {!acceptedTerms && (
            <div className="text-center text-[11.5px] text-sand-500">Accept the terms below to continue.</div>
          )}
        </>
      }
      stepper={<BookingStepper current={2} />}
    >
      <button
        className="inline-flex min-h-11 cursor-pointer items-center self-start border-none bg-transparent p-0 font-sans text-[13.5px] font-semibold text-deep-hover hover:text-deep"
        onClick={onBackToModal}
        type="button"
      >
        ← Back to dates
      </button>
      <BookingHeading accent="quote" pre="Review your" />

      {/* What's refundable */}
      <div className={bookingCard}>
        <div className="text-[13px] font-semibold">What&apos;s refundable</div>
        <div className="flex flex-col text-sm">
          {(quote.priceBreakdown ?? []).map((line, idx) => (
            <div className="flex items-center justify-between gap-2.5 border-b border-shell py-2.5" key={idx}>
              <span className="flex flex-wrap items-center gap-2">
                {line.description}
                {isVerificationLine(line) && line.amount === 0 ? (
                  <LineChip tone="blue">Required by host</LineChip>
                ) : line.isRefundable ? (
                  <LineChip tone="green">Refundable</LineChip>
                ) : (
                  <LineChip tone="coral">Non-refundable</LineChip>
                )}
              </span>
              <strong>{formatMoney(line.amount, line.currency)}</strong>
            </div>
          ))}
          <div className="flex items-center justify-between py-3 text-[15px]">
            <strong>Total charged at booking</strong>
            <strong className="font-display text-[22px]">{formatMoney(quote.totalAmount, quote.currency)}</strong>
          </div>
        </div>
      </div>

      {/* Payment schedule */}
      <div className={bookingCard}>
        <div className="text-[13px] font-semibold">Payment schedule</div>
        <div className="text-[13.5px] text-gray-600">
          Your card is <strong className="text-ink">authorized</strong> now and{" "}
          <strong className="text-ink">captured</strong> once the host confirms. If verification or the host declines,
          the authorization is released in full.
        </div>
      </div>

      {/* Guest, billing & terms (required by the booking API) */}
      <div className={bookingCard}>
        <div className="text-[13px] font-semibold">Guest &amp; billing</div>
        <div className="flex flex-col gap-1 text-[13.5px] text-gray-600">
          <div>
            <span className="font-semibold text-ink">{auth.session?.displayName || "Guest User"}</span> ·{" "}
            {auth.session?.email || "guest@nestystay.local"}
          </div>
          <div>
            {details.adults} adult{details.adults > 1 ? "s" : ""}, {details.children} child
            {details.children !== 1 ? "ren" : ""}
            {details.accessibility ? ` · ${details.accessibility.replaceAll("_", " ")}` : ""}
          </div>
          <div>
            Dates: {quote.checkIn} → {quote.checkOut} ({quote.nights} night{quote.nights > 1 ? "s" : ""})
          </div>
        </div>
        <label className="flex max-w-[280px] flex-col gap-1.5">
          <span className="text-[13px] font-semibold text-ink">Billing country</span>
          <select
            className="min-h-12 rounded-field border-[1.5px] border-sand-input bg-white px-3 font-sans text-[14.5px] text-ink outline-none focus:border-deep-hover"
            onChange={(e) => setBillingCountry(e.target.value)}
            value={billingCountry}
          >
            <option value="JM">Jamaica</option>
            <option value="US">United States</option>
            <option value="CA">Canada</option>
            <option value="GB">United Kingdom</option>
          </select>
        </label>
        <div className="rounded-field bg-shell px-4 py-3 text-[13px]">
          <strong>Cancellation policy: {quote.property.cancellationPolicy}</strong>
          <p className="m-0 mt-1 text-gray-600">
            Full refund up to 5 days before check-in. Non-refundable platform fees may apply.
          </p>
        </div>
        <label className="flex cursor-pointer items-start gap-2.5 text-[13.5px]">
          <input
            checked={acceptedTerms}
            className="mt-0.5 size-[18px] accent-deep-hover"
            onChange={(e) => setAcceptedTerms(e.target.checked)}
            type="checkbox"
          />
          <span>
            I agree to the{" "}
            <a className="font-semibold text-deep-hover hover:text-deep" href="/terms" rel="noreferrer" target="_blank">
              Terms of Service
            </a>
            ,{" "}
            <a className="font-semibold text-deep-hover hover:text-deep" href="/privacy" rel="noreferrer" target="_blank">
              Privacy Policy
            </a>
            , and {quote.property.cancellationPolicy} Cancellation Policy.
          </span>
        </label>
      </div>

      {error && (
        <div className="rounded-field bg-coral-tint px-4 py-3 text-[13px] text-coral-text" role="alert">
          {error}
        </div>
      )}
    </BookingScaffold>
    </div>
  );
}

function formatCreateBookingError(error: unknown): string {
  const candidate = error as { status?: number; code?: string; retryAfterSeconds?: number; message?: string };
  if (candidate.status === 429 || candidate.code === "rate_limit_exceeded") {
    return `Too many booking attempts. Please wait ${formatRetryAfter(candidate.retryAfterSeconds)} before trying again.`;
  }

  return error instanceof Error ? error.message : "Failed to create booking.";
}

function formatRetryAfter(seconds?: number): string {
  if (!seconds || seconds <= 0) {
    return "a few minutes";
  }

  if (seconds < 60) {
    return `${seconds} second${seconds === 1 ? "" : "s"}`;
  }

  const minutes = Math.ceil(seconds / 60);
  return `${minutes} minute${minutes === 1 ? "" : "s"}`;
}
