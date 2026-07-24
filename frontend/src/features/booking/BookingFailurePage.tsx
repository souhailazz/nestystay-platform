import { useState, useEffect } from "react";
import { AlertTriangle, Clock, RefreshCw, Search } from "lucide-react";
import { api, formatMoney } from "../../lib/api";
import type { BookingDetails } from "./types";
import { PatoisPhrase } from "../../lib/patois";
import type { AuthController } from "../../hooks/useAuth";

interface BookingFailurePageProps {
  bookingId: string;
  auth: AuthController;
}

export function BookingFailurePage({ bookingId, auth }: BookingFailurePageProps) {
  const [booking, setBooking] = useState<BookingDetails | null>(null);
  const [timeLeft, setTimeLeft] = useState(15 * 60);

  useEffect(() => {
    let active = true;
    async function load() {
      if (!auth.session) return;
      try {
        const data = await api.getBooking(bookingId, auth.session.accessToken);
        if (active) setBooking(data as unknown as BookingDetails);
      } catch (err) {
        console.error(err);
      }
    }
    load();
    return () => { active = false; };
  }, [bookingId, auth.session?.accessToken]);

  useEffect(() => {
    const timer = setInterval(() => {
      setTimeLeft((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  const minutes = Math.floor(timeLeft / 60);
  const seconds = timeLeft % 60;

  return (
    <div className="page-container container py-6" data-testid="book-05-page" id="BOOK-05">
      <div className="status-hero-card failure-hero mb-6">
        <AlertTriangle size={48} className="text-coral mb-2" />
        <h2>Dutty Tough! Payment Failed</h2>
        <PatoisPhrase phrase="Nuh Stress, Try Again" translation="Your card was declined or authorization failed. Your dates are held for 15 minutes." />
      </div>

      <div className="layout-grid-2-1">
        <div className="card-box">
          <div className="hold-countdown-banner mb-4">
            <Clock size={20} className="text-sun" />
            <div>
              <strong>Inventory Hold Countdown: {minutes}:{seconds < 10 ? `0${seconds}` : seconds}</strong>
              <p className="subtext">Your selected property dates are reserved temporarily while you retry payment.</p>
            </div>
          </div>

          <h3>Failure Summary</h3>
          <p className="subtext mb-3">
            Reason: Payment authorization could not be completed with the card provider.
          </p>

          <div className="action-buttons-group">
            <a href={`/booking/${bookingId}/checkout`} className="btn btn-primary">
              <RefreshCw size={16} /> Retry Payment
            </a>
            <a href="/explore" className="btn btn-outline">
              <Search size={16} /> Find Similar Stays
            </a>
          </div>
        </div>

        <div className="card-box">
          <h3>Booking Information</h3>
          {booking ? (
            <>
              <p><strong>Property:</strong> {booking.propertyTitle}</p>
              <p><strong>Check-in:</strong> {booking.checkIn}</p>
              <p><strong>Check-out:</strong> {booking.checkOut}</p>
              <p><strong>Amount Due:</strong> {formatMoney(booking.totalAmount, booking.currency)}</p>
            </>
          ) : (
            <p className="subtext">Loading booking summary...</p>
          )}
        </div>
      </div>
    </div>
  );
}
