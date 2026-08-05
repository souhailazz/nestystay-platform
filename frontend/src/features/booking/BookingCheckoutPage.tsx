import { useState, useEffect } from "react";
import { RefreshCw } from "lucide-react";
import { loadStripe } from "@stripe/stripe-js";
import { Elements, PaymentElement, useStripe, useElements } from "@stripe/react-stripe-js";
import { api, formatMoney } from "../../lib/api";
import type { BookingDetails } from "./types";
import { LoadingState } from "../../components/ui/LoadingState";
import type { AuthController } from "../../hooks/useAuth";
import {
  BookingHeading,
  BookingScaffold,
  BookingStepper,
  DEGRESSIVE_NOTE,
  PriceSummary,
  PropertyMiniHeader,
  bookingCard,
  bookingDeepCta,
} from "./BookingShell";

let cachedStripeKey: string | undefined;
let cachedStripePromise: ReturnType<typeof loadStripe> | null | undefined;

function getStripePromise() {
  const stripePublishableKey = import.meta.env.VITE_STRIPE_PUBLIC_KEY as string | undefined;
  if (!stripePublishableKey) {
    cachedStripeKey = undefined;
    cachedStripePromise = null;
    return null;
  }

  if (cachedStripeKey !== stripePublishableKey || cachedStripePromise === undefined) {
    cachedStripeKey = stripePublishableKey;
    cachedStripePromise = loadStripe(stripePublishableKey);
  }

  return cachedStripePromise;
}

interface BookingCheckoutPageProps {
  bookingId: string;
  auth: AuthController;
  onSuccess: (bookingId: string) => void;
  onFailure: (bookingId: string, reason: string) => void;
}

const errorPanel = "rounded-field bg-coral-tint px-4 py-3 text-[13px] text-coral-text";

/* Stripe payment logic is unchanged — manual capture: authorize now, backend
   captures on host confirm. Declined messages render verbatim. */
function CheckoutForm({
  booking,
  onSuccess,
  onFailure,
}: {
  booking: BookingDetails;
  onSuccess: (id: string) => void;
  onFailure: (id: string, reason: string) => void;
}) {
  const stripe = useStripe();
  const elements = useElements();
  const [error, setError] = useState<string | null>(null);
  const [processing, setProcessing] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!stripe || !elements) return;

    setProcessing(true);
    setError(null);

    const { error: submitError } = await elements.submit();
    if (submitError) {
      setError(submitError.message || "Please fill out the payment details.");
      setProcessing(false);
      return;
    }

    const { error: confirmError, paymentIntent } = await stripe.confirmPayment({
      elements,
      clientSecret: booking.paymentClientSecret!,
      confirmParams: {
        return_url: `${window.location.origin}/booking/${booking.id}/success`,
      },
      redirect: "if_required",
    });

    if (confirmError) {
      const msg = confirmError.message || "Payment authorization failed.";
      setError(msg);
      onFailure(booking.id, msg);
      setProcessing(false);
    } else if (paymentIntent && (paymentIntent.status === "requires_capture" || paymentIntent.status === "succeeded")) {
      onSuccess(booking.id);
    } else {
      setProcessing(false);
    }
  };

  return (
    <form className="flex flex-col gap-3.5" onSubmit={handleSubmit}>
      <div className="rounded-field border-[1.5px] border-dashed border-sand-input bg-white p-4">
        <div className="mb-3 flex items-center justify-between text-[11px] text-sand-500">
          <span className="font-semibold tracking-[0.08em]">SECURE CARD DETAILS</span>
          <span>PCI handled by Stripe</span>
        </div>
        <PaymentElement />
      </div>
      {error && (
        <div className={errorPanel} role="alert">
          {error}
        </div>
      )}
      <button className={bookingDeepCta} disabled={!stripe || processing} type="submit">
        {processing ? (
          <>
            <RefreshCw className="animate-spin" size={17} /> Processing payment…
          </>
        ) : (
          <>Authorize {formatMoney(booking.totalAmount, booking.currency)} →</>
        )}
      </button>
    </form>
  );
}

export function BookingCheckoutPage({ bookingId, auth, onSuccess, onFailure }: BookingCheckoutPageProps) {
  const stripePromise = getStripePromise();
  const [booking, setBooking] = useState<BookingDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    async function load() {
      if (!auth.session) return;
      try {
        const data = await api.getBooking(bookingId, auth.session.accessToken);
        if (active) setBooking(data as unknown as BookingDetails);
      } catch (err) {
        if (active) setError(err instanceof Error ? err.message : "Failed to load booking.");
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => {
      active = false;
    };
  }, [bookingId, auth.session?.accessToken]);

  if (loading) {
    return (
      <div className="mx-auto max-w-[1160px] px-6 py-9" data-testid="book-03-loading">
        <LoadingState label="Loading secure payment checkout" />
      </div>
    );
  }

  if (!booking) {
    return (
      <div className="mx-auto max-w-[1160px] px-6 py-9" data-testid="book-03-error">
        <div className={errorPanel}>Booking not found.</div>
      </div>
    );
  }

  if (!booking.paymentClientSecret) {
    return (
      <div className="mx-auto max-w-[1160px] px-6 py-9" data-testid="book-03-no-secret">
        <div className={errorPanel}>
          Payment cannot be processed right now.{" "}
          {booking.status === "PendingVerification" ? "Please complete verification first." : "Payment secret missing."}
        </div>
      </div>
    );
  }

  if (!stripePromise) {
    return (
      <div className="mx-auto max-w-[1160px] px-6 py-9" data-testid="book-03-stripe-config-missing">
        <div className={errorPanel}>Payment cannot be processed right now. Stripe checkout is not configured.</div>
      </div>
    );
  }

  return (
    <div data-testid="book-03-page" id="BOOK-05">
      <BookingScaffold
        aside={
          <>
            <PropertyMiniHeader
              ekycRequired={booking.requiresGuestVerification}
              subtitle={`Booking ${booking.id.slice(0, 8).toUpperCase()}${booking.checkIn ? ` · ${booking.checkIn} → ${booking.checkOut}` : ""}`}
              title={booking.propertyTitle ?? "Your stay"}
            />
            <PriceSummary currency={booking.currency} lines={booking.priceBreakdown ?? []} total={booking.totalAmount} />
            <div className="text-[11.5px] text-sand-500">{DEGRESSIVE_NOTE}</div>
          </>
        }
        stepper={<BookingStepper current={4} />}
      >
        <BookingHeading accent="payment" pre="Secure" />
        <div className={bookingCard}>
          <div className="flex flex-wrap items-center justify-between gap-2.5">
            <div className="text-[13px] font-semibold">Card details</div>
            <span className="rounded-pill bg-amber-tint px-2.5 py-1 text-[10.5px] font-bold text-amber-text">
              AUTHORIZED — CAPTURED ON HOST CONFIRM
            </span>
          </div>
          <div className="text-[12.5px] text-gray-600">
            Processed directly via Stripe. NestyStay never stores raw card credentials.
          </div>
          <Elements options={{ clientSecret: booking.paymentClientSecret }} stripe={stripePromise}>
            <CheckoutForm booking={booking} onFailure={onFailure} onSuccess={onSuccess} />
          </Elements>
        </div>
      </BookingScaffold>
    </div>
  );
}
