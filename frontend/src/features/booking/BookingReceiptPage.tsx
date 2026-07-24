import { useState, useEffect } from "react";
import { Download, CheckCircle, Printer } from "lucide-react";
import { api, formatMoney } from "../../lib/api";
import type { BookingDetails } from "./types";
import type { AuthController } from "../../hooks/useAuth";

interface BookingReceiptPageProps {
  bookingId: string;
  auth: AuthController;
}

export function BookingReceiptPage({ bookingId, auth }: BookingReceiptPageProps) {
  const [booking, setBooking] = useState<BookingDetails | null>(null);
  const [downloading, setDownloading] = useState(false);

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

  async function handleDownload() {
    if (!booking || !auth.session) return;
    setDownloading(true);
    try {
      const file = await api.downloadBookingReceipt(booking.id, auth.session.accessToken);
      const url = URL.createObjectURL(file.blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = file.fileName;
      document.body.appendChild(a);
      a.click();
      a.remove();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (err) {
      alert(`Download failed: ${err instanceof Error ? err.message : "Error"}`);
    } finally {
      setDownloading(false);
    }
  }

  if (!booking) return <div className="container py-6" data-testid="book-10-loading">Loading receipt...</div>;

  return (
    <div className="page-container container py-6" data-testid="book-10-page" id="BOOK-10">
      <div className="receipt-document-card card-box max-w-3xl mx-auto printable-area">
        <header className="receipt-header flex justify-between items-center mb-6 border-b pb-4">
          <div>
            <span className="badge badge-green">CAPTURED</span>
            <h1 className="text-2xl font-bold text-green mt-1">Payment Receipt</h1>
            <p className="subtext">Receipt No: NSTY-RCP-{booking.id.substring(0, 8).toUpperCase()}</p>
          </div>
          <div className="text-right">
            <span className="badge badge-sun">BOOK-10</span>
            <p className="subtext mt-1">Payment Date: {new Date().toLocaleDateString()}</p>
          </div>
        </header>

        <div className="grid grid-cols-2 gap-4 mb-6">
          <div>
            <h3>Paid By</h3>
            <p>{auth.session?.displayName || "Guest User"}</p>
            <p className="subtext">{auth.session?.email || "guest@nestystay.local"}</p>
          </div>
          <div className="text-right">
            <h3>Payment Reference</h3>
            <p>Provider: Stripe</p>
            <p className="subtext">Auth Ref: {booking.paymentAuthorizationReference || "ch_stripe_mock_auth"}</p>
            <p className="subtext">Card: Visa ending in 4242</p>
          </div>
        </div>

        <div className="receipt-total-card bg-green-light p-4 rounded mb-6 text-center">
          <span className="subtext">Amount Captured</span>
          <h2 className="text-3xl font-bold text-green">{formatMoney(booking.totalAmount, booking.currency)}</h2>
        </div>

        <footer className="receipt-footer flex justify-between items-center border-t pt-4 no-print">
          <button type="button" className="btn btn-ghost" onClick={() => window.print()}>
            <Printer size={16} /> Print Receipt
          </button>
          <button type="button" className="btn btn-primary" disabled={downloading} onClick={handleDownload}>
            <Download size={16} /> {downloading ? "Downloading PDF..." : "Download PDF Receipt"}
          </button>
        </footer>
      </div>
    </div>
  );
}
