import { useState, useEffect } from "react";
import { CreditCard, Lock, ShieldCheck, AlertCircle, CheckCircle2, RefreshCw } from "lucide-react";
import { api, formatMoney } from "../../lib/api";
import type { BookingDetails } from "./types";
import { PatoisPhrase } from "../../lib/patois";
import type { AuthController } from "../../hooks/useAuth";

interface BookingCheckoutPageProps {
  bookingId: string;
  auth: AuthController;
  onSuccess: (bookingId: string) => void;
  onFailure: (bookingId: string, reason: string) => void;
}

export function BookingCheckoutPage({ bookingId, auth, onSuccess, onFailure }: BookingCheckoutPageProps) {
  const [booking, setBooking] = useState<BookingDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [processing, setProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  const [cardholderName, setCardholderName] = useState(auth.session?.displayName || "");
  const [postalCode, setPostalCode] = useState("KGN01");
  const [saveMethod, setSaveMethod] = useState(true);
  const [simulate3DS, setSimulate3DS] = useState(false);

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

  async function handlePayment() {
    if (!auth.session || !booking) return;
    if (!cardholderName.trim()) {
      setError("Please enter the cardholder name.");
      return;
    }
    setProcessing(true);
    setError(null);

    try {
      if (simulate3DS) {
        await new Promise((resolve) => setTimeout(resolve, 1200));
      }
      // Capture payment using backend authorization service & idempotency key
      const updated = await api.capturePayment(booking.id, auth.session.accessToken);
      if (updated.paymentStatus === "CAPTURED" || updated.status === "CONFIRMED" || updated.status === "APPROVED") {
        onSuccess(booking.id);
      } else if (updated.paymentStatus === "FAILED" || updated.status === "REJECTED") {
        onFailure(booking.id, "Payment authorization declined by issuing bank.");
      } else {
        onSuccess(booking.id);
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : "Payment failed.";
      setError(msg);
      onFailure(booking.id, msg);
    } finally {
      setProcessing(false);
    }
  }

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

          <form onSubmit={(e) => { e.preventDefault(); handlePayment(); }}>
            <div className="field-group mb-3">
              <label className="field-label">Cardholder Name</label>
              <input 
                type="text" 
                className="input-control" 
                value={cardholderName} 
                onChange={(e) => setCardholderName(e.target.value)}
                placeholder="Full name on card"
                required
              />
            </div>

            {/* Simulated Stripe Element */}
            <div className="field-group mb-3">
              <label className="field-label"><CreditCard size={16} /> Card Details (Stripe Elements Container)</label>
              <div className="stripe-element-input-container">
                <div className="stripe-mock-field">
                  <span>•••• •••• •••• 4242</span>
                  <span>12/28</span>
                  <span>***</span>
                </div>
              </div>
            </div>

            <div className="field-group mb-3">
              <label className="field-label">Billing Postal / Zip Code</label>
              <input 
                type="text" 
                className="input-control" 
                value={postalCode} 
                onChange={(e) => setPostalCode(e.target.value)} 
                required
              />
            </div>

            <label className="checkbox-card mb-3">
              <input 
                type="checkbox" 
                checked={saveMethod} 
                onChange={(e) => setSaveMethod(e.target.checked)} 
              />
              <span>Save this payment method securely for future bookings (SetupIntent)</span>
            </label>

            <label className="checkbox-card mb-4">
              <input 
                type="checkbox" 
                checked={simulate3DS} 
                onChange={(e) => setSimulate3DS(e.target.checked)} 
              />
              <span>Require 3D Secure (3DS) authentication verification</span>
            </label>

            {error && (
              <div className="alert-box alert-error mb-4">
                <AlertCircle size={18} />
                <span>{error}</span>
              </div>
            )}

            <button 
              type="submit" 
              className="btn btn-primary btn-lg w-full" 
              disabled={processing}
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
