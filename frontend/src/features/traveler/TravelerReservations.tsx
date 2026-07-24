import { useState, useEffect } from "react";
import { Calendar, MapPin, QrCode, MessageSquare, Download, RotateCcw, AlertCircle, CheckCircle2, XCircle } from "lucide-react";
import { api, formatMoney, type Booking } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";

interface TravelerReservationsProps {
  view: string;
  token: string;
}

export function TravelerReservations({ view, token }: TravelerReservationsProps) {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedBooking, setSelectedBooking] = useState<Booking | null>(null);
  const [qrCodeUrl, setQrCodeUrl] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const list = await api.getBookings(token);
        if (active) {
          setBookings(list);
          if (list.length > 0) setSelectedBooking(list[0]);
        }
      } catch (err) {
        console.error(err);
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => { active = false; };
  }, [token]);

  const filteredBookings = bookings.filter((b) => {
    if (view === "reservations-upcoming") return b.status === "APPROVED" || b.status === "CONFIRMED" || b.paymentStatus === "CAPTURED";
    if (view === "reservations-past") return b.status === "COMPLETED" || (b.paymentStatus === "CAPTURED" && new Date(b.checkOut) < new Date());
    if (view === "reservations-cancelled") return b.status === "REJECTED" || b.status === "CANCELLED" || b.paymentStatus === "CANCELLED";
    return true;
  });

  const screenIdMap: Record<string, string> = {
    "reservations-upcoming": "TRAV-03",
    "reservations-past": "TRAV-04",
    "reservations-cancelled": "TRAV-05",
    "reservation-detail": "TRAV-06",
  };
  const screenId = screenIdMap[view] || "TRAV-03";

  return (
    <div className="page-container container py-6" data-testid={`${screenId.toLowerCase()}-page`} id={screenId}>
      <header className="page-header mb-6">
        <span className="badge badge-sun">{screenId}</span>
        <h2>My Reservations</h2>
        <PatoisPhrase phrase="Yuh Booking History" translation="Manage all your upcoming, completed, and cancelled trips." />
      </header>

      {/* Filter Tabs */}
      <div className="tab-nav-bar mb-6 flex gap-2 border-b pb-2">
        <a href="/traveler/reservations/upcoming" className={`tab-btn ${view === "reservations-upcoming" ? "active font-bold border-b-2 border-sun" : ""}`}>
          Upcoming Trips
        </a>
        <a href="/traveler/reservations/past" className={`tab-btn ${view === "reservations-past" ? "active font-bold border-b-2 border-sun" : ""}`}>
          Past Trips
        </a>
        <a href="/traveler/reservations/cancelled" className={`tab-btn ${view === "reservations-cancelled" ? "active font-bold border-b-2 border-sun" : ""}`}>
          Cancelled
        </a>
      </div>

      {loading ? (
        <div className="loading-shimmer p-6 text-center">Loading reservations...</div>
      ) : filteredBookings.length === 0 ? (
        <div className="card-box text-center py-8">
          <p className="text-lg font-medium">No {view.replace("reservations-", "")} reservations found.</p>
          <a href="/explore" className="btn btn-primary mt-3">Book a Stay</a>
        </div>
      ) : (
        <div className="layout-grid-2-1">
          {/* Booking Cards List */}
          <div className="space-y-4">
            {filteredBookings.map((booking) => (
              <div 
                key={booking.id} 
                className={`card-box cursor-pointer hover:border-sun transition ${selectedBooking?.id === booking.id ? "border-sun bg-sun-light" : ""}`}
                onClick={() => setSelectedBooking(booking)}
              >
                <div className="flex justify-between items-start mb-2">
                  <div>
                    <span className="badge badge-sun">{booking.status}</span>
                    <h3 className="font-bold text-lg mt-1">{booking.propertyTitle}</h3>
                    <p className="subtext"><Calendar size={14} className="inline" /> {booking.checkIn} to {booking.checkOut}</p>
                  </div>
                  <strong className="text-sun">{formatMoney(booking.totalAmount, booking.currency)}</strong>
                </div>
              </div>
            ))}
          </div>

          {/* Reservation Detail View */}
          {selectedBooking && (
            <div className="card-box sticky-top">
              <h3>Reservation Detail</h3>
              <p className="subtext">ID: {selectedBooking.id}</p>

              <hr className="my-3" />

              <div className="info-row">
                <span>Property:</span>
                <strong>{selectedBooking.propertyTitle}</strong>
              </div>
              <div className="info-row">
                <span>Dates:</span>
                <span>{selectedBooking.checkIn} to {selectedBooking.checkOut} ({selectedBooking.nights} nights)</span>
              </div>
              <div className="info-row">
                <span>Host:</span>
                <span>{selectedBooking.hostName}</span>
              </div>
              <div className="info-row">
                <span>Payment:</span>
                <span className="badge badge-green">{selectedBooking.paymentStatus}</span>
              </div>

              <hr className="my-3" />

              {/* QR Gate Pass Access */}
              <div className="qr-gate-card bg-sun-light p-3 rounded text-center mb-3">
                <QrCode size={32} className="mx-auto mb-1 text-sun" />
                <strong className="text-sm">Gate Access Pass</strong>
                <p className="subtext text-xs mt-1">Show this QR code at property security gate.</p>
              </div>

              <div className="action-buttons-group flex flex-col gap-2">
                <a href="/messages" className="btn btn-primary w-full">
                  <MessageSquare size={16} /> Contact Host
                </a>
                <a href={`/booking/${selectedBooking.id}/invoice`} className="btn btn-outline w-full">
                  <Download size={16} /> View Invoice
                </a>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
