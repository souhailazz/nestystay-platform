import { useState, useEffect } from "react";
import { Ban, RefreshCw, FileText } from "lucide-react";
import { api, formatMoney } from "../../lib/api";
import type { BookingDetails } from "./types";
import { PatoisPhrase } from "../../lib/patois";
import type { AuthController } from "../../hooks/useAuth";

interface BookingCancelledPageProps {
  bookingId: string;
  auth: AuthController;
}

export function BookingCancelledPage({ bookingId, auth }: BookingCancelledPageProps) {
  const [booking, setBooking] = useState<BookingDetails | null>(null);

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

  return (
    <div className="page-container container py-6" data-testid="book-08-page" id="BOOK-08">
      <div className="status-hero-card cancelled-hero mb-6">
        <Ban size={48} className="text-coral mb-2" />
        <h2>Booking Cancelled</h2>
        <PatoisPhrase phrase="Suh It Guh" translation="This reservation has been cancelled. Refund processing details are summarized below." />
      </div>

      <div className="layout-grid-2-1">
        <div className="card-box">
          <h3>Refund Summary</h3>
          <div className="info-row">
            <span>Refund Status:</span>
            <span className="badge badge-sun">{booking?.refundedAmount ? "REFUNDED" : "PROCESSING"}</span>
          </div>
          <div className="info-row">
            <span>Refunded Amount:</span>
            <strong>{booking ? formatMoney(booking.refundedAmount || booking.totalAmount, booking.currency) : "$0.00"}</strong>
          </div>
          {booking?.refundReason && (
            <div className="info-row">
              <span>Refund Reason:</span>
              <span>{booking.refundReason}</span>
            </div>
          )}
          {booking?.paymentRefundReference && (
            <div className="info-row">
              <span>Stripe Refund Ref:</span>
              <span>{booking.paymentRefundReference}</span>
            </div>
          )}
        </div>

        <div className="card-box">
          <h3>Property Info</h3>
          {booking && (
            <>
              <p><strong>Property:</strong> {booking.propertyTitle}</p>
              <p><strong>Dates:</strong> {booking.checkIn} to {booking.checkOut}</p>
              <a href="/explore" className="btn btn-outline w-full mt-3">Book Another Stay</a>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
