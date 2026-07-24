import { useState, useEffect } from "react";
import { CheckCircle2, Download, MessageSquare, Calendar, MapPin, RefreshCw, AlertCircle } from "lucide-react";
import { api, formatMoney } from "../../lib/api";
import type { BookingDetails } from "./types";
import { PatoisPhrase } from "../../lib/patois";
import type { AuthController } from "../../hooks/useAuth";

interface BookingSuccessPageProps {
  bookingId: string;
  auth: AuthController;
}

export function BookingSuccessPage({ bookingId, auth }: BookingSuccessPageProps) {
  const [booking, setBooking] = useState<BookingDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [downloadNotice, setDownloadNotice] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    async function load() {
      if (!auth.session) return;
      try {
        const data = await api.getBooking(bookingId, auth.session.accessToken);
        if (active) setBooking(data as unknown as BookingDetails);
      } catch (err) {
        console.error(err);
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => { active = false; };
  }, [bookingId, auth.session?.accessToken]);

  async function handleDownload(kind: "invoice" | "receipt") {
    if (!booking || !auth.session) return;
    try {
      const doc = kind === "invoice" 
        ? await api.downloadBookingInvoice(booking.id, auth.session.accessToken)
        : await api.downloadBookingReceipt(booking.id, auth.session.accessToken);
      const url = URL.createObjectURL(doc.blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = doc.fileName;
      document.body.appendChild(a);
      a.click();
      a.remove();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
      setDownloadNotice(`${kind === "invoice" ? "Invoice" : "Receipt"} downloaded: ${doc.fileName}`);
    } catch (err) {
      setDownloadNotice(`Download failed: ${err instanceof Error ? err.message : "Error downloading document."}`);
    }
  }

  if (loading) {
    return (
      <div className="container py-6" data-testid="book-04-loading">
        <div className="loading-shimmer p-6 text-center">
          <RefreshCw size={24} className="spin mb-2" />
          <p>Loading confirmation details...</p>
        </div>
      </div>
    );
  }

  if (!booking) {
    return (
      <div className="container py-6" data-testid="book-04-error">
        <div className="alert-box alert-error">
          <AlertCircle size={20} />
          <span>Booking information not found.</span>
        </div>
      </div>
    );
  }

  return (
    <div className="page-container container py-6" data-testid="book-04-page" id="BOOK-04">
      <div className="status-hero-card success-hero mb-6">
        <CheckCircle2 size={48} className="text-green mb-2" />
        <h2>Irie! Booking Confirmed</h2>
        <PatoisPhrase phrase="Everything Gud Good!" translation="Your reservation and payment have been successfully confirmed." />
        <p className="booking-ref-badge mt-2">Booking Reference: <strong>NSTY-BK-{booking.id.substring(0, 8).toUpperCase()}</strong></p>
      </div>

      <div className="layout-grid-2-1">
        <div className="card-box">
          <h3>Reservation Details</h3>
          <div className="info-row">
            <span>Property:</span>
            <strong>{booking.propertyTitle}</strong>
          </div>
          <div className="info-row">
            <span>Dates:</span>
            <span>{booking.checkIn} to {booking.checkOut} ({booking.nights} nights)</span>
          </div>
          <div className="info-row">
            <span>Host:</span>
            <span>{booking.hostName}</span>
          </div>
          <div className="info-row">
            <span>Payment Status:</span>
            <span className="badge badge-green">{booking.paymentStatus}</span>
          </div>
          <div className="info-row">
            <span>Total Paid:</span>
            <strong>{formatMoney(booking.totalAmount, booking.currency)}</strong>
          </div>

          <hr className="my-4" />

          <h3>Document Downloads</h3>
          <div className="action-buttons-group">
            <button type="button" className="btn btn-outline" onClick={() => handleDownload("invoice")}>
              <Download size={16} /> Download Invoice PDF (BOOK-09)
            </button>
            <button type="button" className="btn btn-outline" onClick={() => handleDownload("receipt")}>
              <Download size={16} /> Download Receipt PDF (BOOK-10)
            </button>
          </div>
          {downloadNotice && <div className="notice-panel mt-3">{downloadNotice}</div>}
        </div>

        <div className="card-box">
          <h3>Status Timeline</h3>
          <ul className="timeline-list">
            {booking.timeline.map((item, idx) => (
              <li key={idx} className="timeline-item">
                <span className="timeline-dot" />
                <span>{item}</span>
              </li>
            ))}
          </ul>

          <hr className="my-4" />
          <a href={`/messages`} className="btn btn-primary w-full">
            <MessageSquare size={16} /> Contact Host
          </a>
        </div>
      </div>
    </div>
  );
}
