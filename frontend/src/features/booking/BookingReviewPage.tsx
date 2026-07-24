import { useState } from "react";
import { ArrowLeft, ShieldCheck, Check, Calendar, Users, MapPin, AlertCircle, Lock } from "lucide-react";
import { api, formatMoney } from "../../lib/api";
import type { BookingQuote } from "./types";
import { PatoisPhrase } from "../../lib/patois";
import type { AuthController } from "../../hooks/useAuth";

interface BookingReviewPageProps {
  quote: BookingQuote;
  details: { adults: number; children: number; accessibility: string; protection: string };
  auth: AuthController;
  onBackToModal: () => void;
  onProceedToCheckout: (bookingId: string) => void;
}

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
      const created = await api.createBooking({
        propertyId: quote.property.id,
        guestUserId: auth.session.userId,
        checkIn: quote.checkIn,
        checkOut: quote.checkOut,
        adults: details.adults,
        children: details.children,
        accessibilityNeeds: details.accessibility,
        protectionPlan: details.protection,
        billingCountry,
        termsAccepted: acceptedTerms
      }, auth.session.accessToken);
      onProceedToCheckout(created.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create booking.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="page-container container py-6" data-testid="book-02-page" id="BOOK-02">
      <button type="button" className="btn btn-ghost mb-4" onClick={onBackToModal}>
        <ArrowLeft size={16} /> Back to Guest Selection
      </button>

      <div className="layout-grid-2-1">
        {/* Left Main Review Section */}
        <div className="review-main-content">
          <header className="page-header mb-4">
            <span className="badge badge-sun">BOOK-02</span>
            <h2>Review Your Booking</h2>
            <PatoisPhrase phrase="Check Everything Good Good" translation="Double check all your reservation details before paying." />
          </header>

          {/* Guest Details */}
          <div className="card-box mb-4">
            <h3>Guest Details</h3>
            <div className="info-row">
              <span className="info-label">Name:</span>
              <strong>{auth.session?.displayName || "Guest User"}</strong>
            </div>
            <div className="info-row">
              <span className="info-label">Email:</span>
              <span>{auth.session?.email || "guest@nestystay.local"}</span>
            </div>
            <div className="info-row">
              <span className="info-label">Guests:</span>
              <span>{details.adults} Adult{details.adults > 1 ? "s" : ""}, {details.children} Child{details.children !== 1 ? "ren" : ""}</span>
            </div>
            {details.accessibility && (
              <div className="info-row">
                <span className="info-label">Accessibility:</span>
                <span>{details.accessibility}</span>
              </div>
            )}
          </div>

          {/* Stay & Protection Info */}
          <div className="card-box mb-4">
            <h3>Trip Details</h3>
            <div className="info-row">
              <span className="info-label"><Calendar size={16} /> Dates:</span>
              <span>{quote.checkIn} to {quote.checkOut} ({quote.nights} night{quote.nights > 1 ? "s" : ""})</span>
            </div>
            <div className="info-row">
              <span className="info-label"><ShieldCheck size={16} /> Selected Protection:</span>
              <span className="badge badge-green">{details.protection === "insuraguest" ? "InsuraGuest Full Protection" : "Standard Protection"}</span>
            </div>
          </div>

          {/* Billing & Terms */}
          <div className="card-box mb-4">
            <h3>Billing Country & Policy Terms</h3>
            <div className="field-group mb-3">
              <label className="field-label">Billing Country</label>
              <select className="input-control" value={billingCountry} onChange={(e) => setBillingCountry(e.target.value)}>
                <option value="JM">Jamaica 🇯🇲</option>
                <option value="US">United States 🇺🇸</option>
                <option value="CA">Canada 🇨🇦</option>
                <option value="GB">United Kingdom 🇬🇧</option>
              </select>
            </div>

            <div className="cancellation-policy-box mb-3">
              <strong>Cancellation Policy: {quote.property.cancellationPolicy}</strong>
              <p className="subtext mt-1">
                Full refund up to 5 days before check-in. Non-refundable platform fees may apply.
              </p>
            </div>

            <label className="checkbox-card">
              <input 
                type="checkbox" 
                checked={acceptedTerms} 
                onChange={(e) => setAcceptedTerms(e.target.checked)} 
              />
              <span>
                I agree to the <a href="/terms" target="_blank" rel="noreferrer">Terms of Service</a>, <a href="/privacy" target="_blank" rel="noreferrer">Privacy Policy</a>, and {quote.property.cancellationPolicy} Cancellation Policy.
              </span>
            </label>
          </div>

          {error && (
            <div className="alert-box alert-error mb-4">
              <AlertCircle size={18} />
              <span>{error}</span>
            </div>
          )}

          <button 
            type="button" 
            className="btn btn-primary btn-lg w-full" 
            disabled={loading || !acceptedTerms} 
            onClick={handleCreateBooking}
          >
            <Lock size={18} /> {loading ? "Creating Reservation..." : "Proceed to Secure Checkout"}
          </button>
        </div>

        {/* Right Sidebar: Property & Price Summary */}
        <div className="review-sidebar">
          <div className="card-box sticky-top">
            <div className="property-sidebar-header mb-3">
              <h3>{quote.property.title}</h3>
              <p className="subtext"><MapPin size={14} /> {quote.property.location}, {quote.property.country}</p>
            </div>

            <h4>Price Summary</h4>
            <ul className="price-line-list">
              {quote.priceBreakdown.map((line, idx) => (
                <li key={idx} className="price-line-item">
                  <span>{line.description}</span>
                  <span>{formatMoney(line.amount, line.currency)}</span>
                </li>
              ))}
              <li className="price-line-total">
                <strong>Total Due Now</strong>
                <strong>{formatMoney(quote.totalAmount, quote.currency)}</strong>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
