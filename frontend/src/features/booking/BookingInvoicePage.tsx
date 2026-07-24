import { useState, useEffect } from "react";
import { Download, FileText, Printer } from "lucide-react";
import { api, formatMoney } from "../../lib/api";
import type { BookingDetails } from "./types";
import type { AuthController } from "../../hooks/useAuth";

interface BookingInvoicePageProps {
  bookingId: string;
  auth: AuthController;
}

export function BookingInvoicePage({ bookingId, auth }: BookingInvoicePageProps) {
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
      const file = await api.downloadBookingInvoice(booking.id, auth.session.accessToken);
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

  if (!booking) return <div className="container py-6" data-testid="book-09-loading">Loading invoice...</div>;

  return (
    <div className="page-container container py-6" data-testid="book-09-page" id="BOOK-09">
      <div className="invoice-document-card card-box max-w-3xl mx-auto printable-area">
        <header className="invoice-header flex justify-between items-center mb-6 border-b pb-4">
          <div>
            <h1 className="text-2xl font-bold text-sun">NestyStay Invoice</h1>
            <p className="subtext">Invoice No: NSTY-INV-{booking.id.substring(0, 8).toUpperCase()}</p>
          </div>
          <div className="text-right">
            <span className="badge badge-sun">BOOK-09</span>
            <p className="subtext mt-1">Date: {new Date().toLocaleDateString()}</p>
          </div>
        </header>

        <div className="grid grid-cols-2 gap-4 mb-6">
          <div>
            <h3>Traveler / Guest</h3>
            <p>{auth.session?.displayName || "Guest User"}</p>
            <p className="subtext">{auth.session?.email || "guest@nestystay.local"}</p>
          </div>
          <div className="text-right">
            <h3>Property & Host</h3>
            <p>{booking.propertyTitle}</p>
            <p className="subtext">Host: {booking.hostName}</p>
          </div>
        </div>

        <table className="table-styled w-full mb-6">
          <thead>
            <tr>
              <th>Description</th>
              <th className="text-right">Amount</th>
            </tr>
          </thead>
          <tbody>
            {booking.priceBreakdown.map((line, idx) => (
              <tr key={idx}>
                <td>{line.description}</td>
                <td className="text-right">{formatMoney(line.amount, line.currency)}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr className="font-bold border-t">
              <td>Total Invoice Amount</td>
              <td className="text-right">{formatMoney(booking.totalAmount, booking.currency)}</td>
            </tr>
          </tfoot>
        </table>

        <footer className="invoice-footer flex justify-between items-center border-t pt-4 no-print">
          <button type="button" className="btn btn-ghost" onClick={() => window.print()}>
            <Printer size={16} /> Print Web Invoice
          </button>
          <button type="button" className="btn btn-primary" disabled={downloading} onClick={handleDownload}>
            <Download size={16} /> {downloading ? "Downloading PDF..." : "Download PDF Invoice"}
          </button>
        </footer>
      </div>
    </div>
  );
}
