import { useState, useEffect } from "react";
import { Clock, AlertCircle, MessageSquare } from "lucide-react";
import { api, formatMoney } from "../../lib/api";
import type { BookingDetails } from "./types";
import { PatoisPhrase } from "../../lib/patois";
import type { AuthController } from "../../hooks/useAuth";

interface BookingPendingPageProps {
  bookingId: string;
  auth: AuthController;
}

export function BookingPendingPage({ bookingId, auth }: BookingPendingPageProps) {
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
    <div className="page-container container py-6" data-testid="book-07-page" id="BOOK-07">
      <div className="status-hero-card pending-hero mb-6">
        <Clock size={48} className="text-sun mb-2" />
        <h2>Pending Host Approval</h2>
        <PatoisPhrase phrase="Nuh Fret, Tek Time" translation="Your booking request has been submitted to the host. They have 24 hours to accept." />
      </div>

      <div className="layout-grid-2-1">
        <div className="card-box">
          <h3>Verification & Host Status</h3>
          <div className="info-row">
            <span>Verification Status:</span>
            <span className="badge badge-sun">{booking?.verificationStatus || "Pending"}</span>
          </div>
          <div className="info-row">
            <span>Payment Hold:</span>
            <span className="badge badge-green">Authorized (Not Captured)</span>
          </div>

          <hr className="my-4" />

          <h3>Timeline</h3>
          <ul className="timeline-list">
            {booking?.timeline.map((item, idx) => (
              <li key={idx} className="timeline-item">
                <span className="timeline-dot" />
                <span>{item}</span>
              </li>
            ))}
          </ul>
        </div>

        <div className="card-box">
          <h3>Need Assistance?</h3>
          <p className="subtext mb-3">You can send a direct message to the host or cancel this pending request anytime before host approval.</p>
          <a href="/messages" className="btn btn-outline w-full mb-2">
            <MessageSquare size={16} /> Message Host
          </a>
        </div>
      </div>
    </div>
  );
}
