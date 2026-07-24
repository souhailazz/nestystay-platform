import { useState, useEffect } from "react";
import { X, Calendar, Users, ShieldCheck, Accessibility, AlertCircle, RefreshCw, ChevronRight, Check } from "lucide-react";
import { api, formatMoney } from "../../lib/api";
import type { PropertyListing as Property } from "../../lib/api";
import type { BookingQuote } from "./types";
import { PatoisPhrase } from "../../lib/patois";

interface BookingModalProps {
  property: Property;
  onClose: () => void;
  onProceedToReview: (quote: BookingQuote, details: { adults: number; children: number; accessibility: string; protection: string }) => void;
}

export function BookingModal({ property, onClose, onProceedToReview }: BookingModalProps) {
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
          protectionPlan: protection
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
    return () => { active = false; };
  }, [property.id, checkIn, checkOut, adults, childrenCount, protection]);

  const nights = checkIn && checkOut ? Math.max(1, Math.ceil((new Date(checkOut).getTime() - new Date(checkIn).getTime()) / (1000 * 3600 * 24))) : 1;
  const isMinimumStayViolated = nights < (property.minimumNights || 1);

  return (
    <div className="modal-backdrop" data-testid="book-01-modal" id="BOOK-01">
      <div className="modal-card booking-modal-content">
        <header className="modal-header">
          <div>
            <span className="badge badge-sun">BOOK-01</span>
            <h2>Select Guests & Dates</h2>
            <PatoisPhrase phrase="Tek Time Pick Yuh Dates" translation="Take your time choosing check-in and guest details." />
          </div>
          <button type="button" className="icon-button" onClick={onClose} aria-label="Close modal">
            <X size={20} />
          </button>
        </header>

        <div className="booking-modal-body">
          {/* Property Summary Header */}
          <div className="property-summary-bar">
            {property.imageUrl && <img src={property.imageUrl} alt={property.title} className="summary-thumb" />}
            <div>
              <h3>{property.title}</h3>
              <p className="subtext">{property.location}, {property.country}</p>
              <div className="price-tag">{formatMoney(property.nightlyRate, property.currency)} <span className="period">/ night</span></div>
            </div>
          </div>

          {/* Dates Selection */}
          <div className="form-grid">
            <div className="field-group">
              <label className="field-label"><Calendar size={16} /> Check-in Date</label>
              <input 
                type="date" 
                className="input-control" 
                value={checkIn} 
                onChange={(e) => setCheckIn(e.target.value)} 
              />
            </div>
            <div className="field-group">
              <label className="field-label"><Calendar size={16} /> Check-out Date</label>
              <input 
                type="date" 
                className="input-control" 
                value={checkOut} 
                onChange={(e) => setCheckOut(e.target.value)} 
              />
            </div>
          </div>

          {/* Guest Steppers */}
          <div className="stepper-grid">
            <div className="stepper-card">
              <div>
                <strong className="stepper-title"><Users size={16} /> Adults</strong>
                <span className="stepper-sub">Ages 13+</span>
              </div>
              <div className="stepper-controls">
                <button type="button" className="stepper-btn" onClick={() => setAdults(Math.max(1, adults - 1))}>-</button>
                <span className="stepper-value">{adults}</span>
                <button type="button" className="stepper-btn" onClick={() => setAdults(adults + 1)}>+</button>
              </div>
            </div>

            <div className="stepper-card">
              <div>
                <strong className="stepper-title"><Users size={16} /> Children / Pickney</strong>
                <span className="stepper-sub">Ages 0 - 12</span>
              </div>
              <div className="stepper-controls">
                <button type="button" className="stepper-btn" onClick={() => setChildrenCount(Math.max(0, childrenCount - 1))}>-</button>
                <span className="stepper-value">{childrenCount}</span>
                <button type="button" className="stepper-btn" onClick={() => setChildrenCount(childrenCount + 1)}>+</button>
              </div>
            </div>
          </div>

          {/* Accessibility Needs */}
          <div className="field-group mt-3">
            <label className="field-label"><Accessibility size={16} /> Accessibility Preferences</label>
            <select 
              className="input-control" 
              value={accessibility} 
              onChange={(e) => setAccessibility(e.target.value)}
            >
              <option value="">No specific accessibility requirements</option>
              <option value="wheelchair">Step-free access & wide doorways (Wheelchair accessible)</option>
              <option value="ground_floor">Ground floor stay only</option>
              <option value="roll_in_shower">Accessible roll-in shower</option>
              <option value="visual_audio_aids">Visual & hearing impairment aids</option>
            </select>
          </div>

          {/* Protection Plan Selection */}
          <div className="field-group mt-3">
            <label className="field-label"><ShieldCheck size={16} /> Guest & Property Protection Plan</label>
            <div className="radio-group">
              <label className={`radio-card ${protection === "insuraguest" ? "active" : ""}`}>
                <input 
                  type="radio" 
                  name="protection" 
                  value="insuraguest" 
                  checked={protection === "insuraguest"} 
                  onChange={(e) => setProtection(e.target.value)} 
                />
                <div>
                  <strong>InsuraGuest Full Protection</strong>
                  <p>$15.00 / night • Covers accidental property damage & accidental injury during stay.</p>
                </div>
              </label>
              <label className={`radio-card ${protection === "standard" ? "active" : ""}`}>
                <input 
                  type="radio" 
                  name="protection" 
                  value="standard" 
                  checked={protection === "standard"} 
                  onChange={(e) => setProtection(e.target.value)} 
                />
                <div>
                  <strong>Standard Protection</strong>
                  <p>Included • Basic host guarantee coverage.</p>
                </div>
              </label>
            </div>
          </div>

          {/* Availability & Validation Errors */}
          {isMinimumStayViolated && (
            <div className="alert-box alert-warning mt-3">
              <AlertCircle size={18} />
              <span>This property requires a minimum stay of {property.minimumNights || 2} nights. Please extend your checkout date.</span>
            </div>
          )}

          {error && (
            <div className="alert-box alert-error mt-3">
              <AlertCircle size={18} />
              <span>{error}</span>
            </div>
          )}

          {/* Server Quote Display */}
          <div className="quote-breakdown-card mt-4">
            <h4>Price Breakdown ({nights} night{nights > 1 ? "s" : ""})</h4>
            {loading ? (
              <div className="loading-shimmer" data-testid="quote-loading">
                <RefreshCw size={18} className="spin" /> Calculating server quote...
              </div>
            ) : quote ? (
              <ul className="price-line-list">
                {quote.priceBreakdown.map((line, idx) => (
                  <li key={idx} className="price-line-item">
                    <span>{line.description}</span>
                    <span>{formatMoney(line.amount, line.currency)}</span>
                  </li>
                ))}
                <li className="price-line-total">
                  <strong>Total Amount</strong>
                  <strong>{formatMoney(quote.totalAmount, quote.currency)}</strong>
                </li>
              </ul>
            ) : (
              <p className="subtext">Select valid dates to view total quote.</p>
            )}
          </div>
        </div>

        <footer className="modal-footer">
          <button type="button" className="btn btn-ghost" onClick={onClose}>Cancel</button>
          <button 
            type="button" 
            className="btn btn-primary" 
            disabled={loading || isMinimumStayViolated || !quote} 
            onClick={() => quote && onProceedToReview(quote, { adults, children: childrenCount, accessibility, protection })}
          >
            Review Booking <ChevronRight size={16} />
          </button>
        </footer>
      </div>
    </div>
  );
}
