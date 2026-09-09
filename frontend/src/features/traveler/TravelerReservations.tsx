import { useState, useEffect } from "react";
import QRCode from "qrcode";
import { Calendar, MapPin, QrCode, MessageSquare, Download, RotateCcw, AlertCircle, CheckCircle2, XCircle } from "lucide-react";
import { Button } from "../../components/ui/Button";
import { api, formatMoney, type Booking, type QrAccess, type QrHistoryEvent, type QrIssueResult } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";

interface TravelerReservationsProps {
  view: string;
  token: string;
}

export function TravelerReservations({ view, token }: TravelerReservationsProps) {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedBooking, setSelectedBooking] = useState<Booking | null>(null);
  const [qrByBooking, setQrByBooking] = useState<Record<string, QrIssueResult | null>>({});
  const [qrImageByBooking, setQrImageByBooking] = useState<Record<string, string | null>>({});
  const [qrError, setQrError] = useState<string | null>(null);
  const [qrRevokeReason, setQrRevokeReason] = useState("No longer needed");
  const [qrAccessList, setQrAccessList] = useState<QrAccess[]>([]);
  const [qrHistory, setQrHistory] = useState<Record<string, QrHistoryEvent[]>>({});
  const [now, setNow] = useState(() => Date.now());

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

  useEffect(() => {
    if (view !== "qr") return;
    let active = true;
    void api.listBookingQrs(token).then((list) => { if (active) setQrAccessList(list); }).catch(() => undefined);
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => { active = false; window.clearInterval(timer); };
  }, [token, view]);

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

  const selectedQr = selectedBooking ? qrByBooking[selectedBooking.id] ?? null : null;
  const selectedQrImage = selectedBooking ? qrImageByBooking[selectedBooking.id] ?? null : null;
  const selectedBookingEligible = selectedBooking
    ? ["APPROVED", "CONFIRMED", "Approved", "Confirmed"].includes(selectedBooking.status) || selectedBooking.paymentStatus === "CAPTURED"
    : false;

  async function generateQr() {
    if (!selectedBooking) return;
    setQrError(null);
    try {
      const issued = await api.issueBookingQr(selectedBooking.id, token);
      const gateUrl = `${window.location.origin}/gate/qr?token=${encodeURIComponent(issued.token)}&propertyId=${encodeURIComponent(issued.propertyId)}`;
      const image = await QRCode.toDataURL(gateUrl, { margin: 1, width: 220 });
      setQrByBooking((current) => ({ ...current, [selectedBooking.id]: issued }));
      setQrImageByBooking((current) => ({ ...current, [selectedBooking.id]: image }));
      setQrAccessList((current) => [{ ...issued, isRevoked: false, validationCount: 0, lastValidatedAt: null }, ...current.filter((item) => item.id !== issued.id)]);
    } catch (caught) {
      setQrError(caught instanceof Error ? caught.message : "Gate pass could not be issued.");
    }
  }

  async function revokeQr() {
    if (!selectedQr || !selectedBooking) return;
    setQrError(null);
    try {
      const reason = qrRevokeReason.trim();
      if (!reason) {
        setQrError("Enter a reason before revoking this gate pass.");
        return;
      }
      await api.revokeBookingQr(selectedQr.id, token, reason);
      setQrByBooking((current) => ({ ...current, [selectedBooking.id]: null }));
      setQrImageByBooking((current) => ({ ...current, [selectedBooking.id]: null }));
      setQrAccessList((current) => current.map((item) => item.id === selectedQr.id ? { ...item, isRevoked: true, status: "Revoked", revokeReason: reason } : item));
    } catch (caught) {
      setQrError(caught instanceof Error ? caught.message : "Gate pass could not be revoked.");
    }
  }

  return (
    <div className="page-container container py-6" data-testid={view === "qr" ? "traveler-qr-page" : `${screenId.toLowerCase()}-page`} id={screenId}>
      <header className="page-header mb-6">
        <span className="badge badge-sun">{screenId}</span>
        <h2>{view === "qr" ? "Gate QR passes" : "My Reservations"}</h2>
        <PatoisPhrase phrase={view === "qr" ? "Gate access" : "Yuh Booking History"} translation={view === "qr" ? "Generate or revoke a secure pass for an approved booking." : "Manage all your upcoming, completed, and cancelled trips."} />
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
              {view === "qr" && (
                <div className="qr-gate-card bg-sun-light p-3 rounded text-center mb-3" data-testid="traveler-qr-controls">
                  <QrCode size={32} className="mx-auto mb-1 text-sun" />
                  <strong className="text-sm">Gate Access Pass</strong>
                  <p className="subtext text-xs mt-1">Show this QR code at property security gate. The scan opens the live gate validator.</p>
                  {!selectedBookingEligible && <p className="subtext text-xs mt-2">QR access becomes available after this booking is approved.</p>}
                  {selectedBookingEligible && !selectedQr && <Button onClick={() => void generateQr()} variant="outline">Generate gate QR</Button>}
                  {selectedQr && (
                    <div className="mt-3 flex flex-wrap items-center justify-center gap-3">
                      {selectedQrImage && <img alt="Secure booking gate QR" height={140} src={selectedQrImage} width={140} />}
                      <div className="text-left text-xs">
                        <div className="font-semibold">Gate QR active</div>
                        <div>Valid through {new Date(selectedQr.expiresAt).toLocaleDateString()}</div>
                        <div aria-live="polite">{Math.max(0, new Date(selectedQr.expiresAt).getTime() - now) > 0 ? `Expires in ${Math.floor(Math.max(0, new Date(selectedQr.expiresAt).getTime() - now) / 3600000)}h ${Math.floor((Math.max(0, new Date(selectedQr.expiresAt).getTime() - now) % 3600000) / 60000)}m` : "Expired"}</div>
                        <div className="mt-2 flex flex-wrap gap-2">
                          <a className="btn btn-outline" href={`/gate/qr?token=${encodeURIComponent(selectedQr.token)}&propertyId=${encodeURIComponent(selectedQr.propertyId)}`}>Open gate validator</a>
                          <label className="sr-only" htmlFor="qr-revoke-reason">Revoke reason</label>
                          <input id="qr-revoke-reason" className="min-h-10 rounded-field border border-sand-input bg-white px-3" value={qrRevokeReason} onChange={(event) => setQrRevokeReason(event.target.value)} />
                          <Button onClick={() => void revokeQr()} variant="outline">Revoke</Button>
                        </div>
                      </div>
                    </div>
                  )}
                  {qrError && <p className="mt-2 text-xs text-error-text" role="alert">{qrError}</p>}
                </div>
              )}

              <div className="action-buttons-group flex flex-col gap-2">
                <a href="/messages" className="btn btn-primary w-full">
                  <MessageSquare size={16} /> Contact Host
                </a>
                <a href={`/booking/${selectedBooking.id}/invoice`} className="btn btn-outline w-full">
                  <Download size={16} /> View Invoice
                </a>
              </div>

              {view === "qr" && qrAccessList.length > 0 && (
                <div className="mt-5 border-t pt-4" data-testid="traveler-qr-history-list">
                  <h4 className="mb-2 font-semibold">Your QR history</h4>
                  <div className="space-y-2 text-xs">
                    {qrAccessList.map((entry) => (
                      <div className="rounded-field border border-sand-border p-3" key={entry.id}>
                        <div className="flex flex-wrap items-center justify-between gap-2"><span className="font-semibold">{entry.status}</span><span>{new Date(entry.expiresAt).toLocaleString()}</span></div>
                        <div className="mt-1 text-sand-600">Scans: {entry.validationCount}{entry.revokeReason ? ` · Reason: ${entry.revokeReason}` : ""}</div>
                        <button className="mt-1 font-semibold underline" onClick={() => void api.getBookingQrHistory(entry.id, token).then((events) => setQrHistory((current) => ({ ...current, [entry.id]: events }))).catch(() => setQrError("QR history could not be loaded."))} type="button">View event history</button>
                        {qrHistory[entry.id] && <div className="mt-2 space-y-1 border-l-2 border-sand-border pl-2">{qrHistory[entry.id].map((event) => <div key={event.id}>{event.eventType} · {event.status} · {new Date(event.occurredAt).toLocaleString()}{event.reason ? ` · ${event.reason}` : ""}</div>)}</div>}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
