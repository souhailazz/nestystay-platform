import { useState, useEffect } from "react";
import { Lock, ShieldCheck, AlertCircle, RefreshCw } from "lucide-react";
import { loadStripe } from "@stripe/stripe-js";
import { Elements, PaymentElement, useStripe, useElements } from "@stripe/react-stripe-js";
import { api, formatMoney } from "../../lib/api";
import type { BookingDetails } from "./types";
import { PatoisPhrase } from "../../lib/patois";
import type { AuthController } from "../../hooks/useAuth";

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

function CheckoutForm({ booking, onSuccess, onFailure }: { booking: BookingDetails, onSuccess: (id: string) => void, onFailure: (id: string, reason: string) => void }) {
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

    // Because this is manual capture, the backend created the PaymentIntent with capture_method: manual.
    // confirmPayment will authorize the card.
    const { error: confirmError, paymentIntent } = await stripe.confirmPayment({
      elements,
      clientSecret: booking.paymentClientSecret!,
      confirmParams: {
        return_url: `${window.location.origin}/booking/${booking.id}/success`,
      },
      redirect: 'if_required' // we handle success directly if redirect is not required
    });

    if (confirmError) {
      const msg = confirmError.message || "Payment authorization failed.";
      setError(msg);
      onFailure(booking.id, msg);
      setProcessing(false);
    } else if (paymentIntent && paymentIntent.status === "requires_capture") {
      onSuccess(booking.id);
    } else if (paymentIntent && paymentIntent.status === "succeeded") {
      onSuccess(booking.id);
    } else {
      setProcessing(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="mt-4">
      <PaymentElement />
      {error && (
        <div className="alert-box alert-error mb-4 mt-4">
          <AlertCircle size={18} />
          <span>{error}</span>
        </div>
      )}
      <button
        type="submit"
        className="btn btn-primary btn-lg w-full mt-6"
        disabled={!stripe || processing}
      >
        {processing ? (
          <>
            <RefreshCw size={18} className="spin" /> Processing Payment...
          </>
        ) : (
          <>
            <Lock size={18} /> Pay {formatMoney(booking.totalAmount, booking.currency)} Now
          </>
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
    return () => { active = false; };
  }, [bookingId, auth.session?.accessToken]);

  if (loading) {
    return (
      <div className="container py-6" data-testid="book-03-loading">
        <div className="loading-shimmer p-6 text-center">
          <RefreshCw size={24} className="spin mb-2" />
          <p>Loading secure payment checkout...</p>
        </div>
      </div>
    );
  }

  if (!booking) {
    return (
      <div className="container py-6" data-testid="book-03-error">
        <div className="alert-box alert-error">
          <AlertCircle size={20} />
          <span>Booking not found.</span>
        </div>
      </div>
    );
  }

  if (!booking.paymentClientSecret) {
    return (
      <div className="container py-6" data-testid="book-03-no-secret">
        <div className="alert-box alert-error">
          <AlertCircle size={20} />
          <span>Payment cannot be processed right now. {booking.status === "PendingVerification" ? "Please complete verification first." : "Payment secret missing."}</span>
        </div>
      </div>
    );
  }

  if (!stripePromise) {
    return (
      <div className="container py-6" data-testid="book-03-stripe-config-missing">
        <div className="alert-box alert-error">
          <AlertCircle size={20} />
          <span>Payment cannot be processed right now. Stripe checkout is not configured.</span>
        </div>
      </div>
    );
  }

  return (
    <div className="page-container container py-6" data-testid="book-03-page" id="BOOK-03">
      <header className="page-header mb-4">
        <span className="badge badge-sun">BOOK-03</span>
        <h2>Secure Stripe Checkout</h2>
        <PatoisPhrase phrase="Pay Safe & Secure" translation="Your transaction is encrypted with bank-level security." />
      </header>

      <div className="layout-grid-2-1">
        <div className="checkout-form-card card-box">
          <div className="security-banner mb-4">
            <ShieldCheck size={20} className="text-green" />
            <div>
              <strong>Encrypted Payment Gateway</strong>
              <p className="subtext">Processed directly via Stripe. NestyStay never stores raw card credentials.</p>
            </div>
          </div>

          <Elements stripe={stripePromise} options={{ clientSecret: booking.paymentClientSecret }}>
            <CheckoutForm booking={booking} onSuccess={onSuccess} onFailure={onFailure} />
          </Elements>
        </div>

        {/* Sidebar Summary */}
        <div className="checkout-sidebar">
          <div className="card-box sticky-top">
            <h3>Booking Reference</h3>
            <p className="subtext">ID: {booking.id}</p>
            <hr className="my-3" />
            <div className="info-row">
              <span>Property:</span>
              <strong>{booking.propertyTitle}</strong>
            </div>
            <div className="info-row">
              <span>Dates:</span>
              <span>{booking.checkIn} to {booking.checkOut}</span>
            </div>
            <div className="info-row">
              <span>Nights:</span>
              <span>{booking.nights}</span>
            </div>
            <hr className="my-3" />
            <div className="info-row text-lg">
              <strong>Total Amount:</strong>
              <strong className="text-sun">{formatMoney(booking.totalAmount, booking.currency)}</strong>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
