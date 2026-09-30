import { useState, useEffect } from "react";
import { XCircle, Search, ShieldCheck } from "lucide-react";
import { api } from "../../lib/api";
import type { BookingDetails } from "./types";
import { PatoisPhrase } from "../../lib/patois";
import type { AuthController } from "../../hooks/useAuth";

interface BookingRejectedPageProps {
  bookingId: string;
  auth: AuthController;
}

export function BookingRejectedPage({ bookingId, auth }: BookingRejectedPageProps) {
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

  const isVerificationRejection = booking?.rejectionSource === "GuestVerification" || /fail|expired/i.test(booking?.verificationStatus ?? "");
  const isHostRejection = booking?.rejectionSource === "Host";
  let title = "Booking request could not be approved";
  if (isVerificationRejection) {
    title = "Identity verification was not approved";
  } else if (isHostRejection) {
    title = "Booking request declined by the host";
  }
  let translation = "The booking request was declined. Your dates were released and no payment was captured.";
  if (isVerificationRejection) {
    const reason = booking?.rejectionReason ? `: ${booking.rejectionReason}` : ".";
    translation = `The configured identity verification provider did not approve this verification${reason} Your dates were released and no payment was captured.`;
  } else if (isHostRejection && booking?.rejectionReason) {
    translation = `The host declined this booking request: ${booking.rejectionReason}`;
  } else if (booking?.rejectionReason) {
    translation = `NestyStay recorded this decision reason: ${booking.rejectionReason}`;
  }

  return (
    <div className="page-container container py-6" data-testid="book-06-page" id="BOOK-06">
      <div className="status-hero-card rejected-hero mb-6">
        <XCircle size={48} className="text-coral mb-2" />
        <h2>{title}</h2>
        <PatoisPhrase phrase="Nuh Fret, Zero Charges" translation={translation} />
      </div>

      <div className="card-box max-w-xl mx-auto">
        <div className="info-badge-banner mb-4">
          <ShieldCheck size={20} className="text-green" />
          <span>Zero-charge guarantee: Your payment authorization was released immediately.</span>
        </div>

        {booking && (
          <div className="info-summary mb-4">
            <p><strong>Property:</strong> {booking.propertyTitle}</p>
            <p><strong>Requested Dates:</strong> {booking.checkIn} to {booking.checkOut}</p>
            <p><strong>Status:</strong> <span className="badge badge-coral">{booking.status}</span></p>
            {booking.rejectionSource && <p><strong>Decision source:</strong> {booking.rejectionSource === "GuestVerification" ? "Identity verification provider" : booking.rejectionSource}</p>}
            {booking.rejectionReason && <p><strong>Decision reason:</strong> {booking.rejectionReason}</p>}
          </div>
        )}

        <div className="text-center mt-4">
          <a href="/explore" className="btn btn-primary btn-lg">
            <Search size={18} /> Explore Alternative Stays
          </a>
        </div>
      </div>
    </div>
  );
}
