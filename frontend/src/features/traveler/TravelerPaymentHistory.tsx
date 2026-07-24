import { useState, useEffect } from "react";
import { Download, Filter, FileText, Calendar } from "lucide-react";
import { api, formatMoney, type Booking } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";

interface TravelerPaymentHistoryProps {
  token: string;
}

export function TravelerPaymentHistory({ token }: TravelerPaymentHistoryProps) {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loading, setLoading] = useState(true);
  const [yearFilter, setYearFilter] = useState("2026");

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const list = await api.getBookings(token);
        if (active) setBookings(list);
      } catch (err) {
        console.error(err);
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => { active = false; };
  }, [token]);

  async function handleDownloadInvoice(bookingId: string) {
    try {
      const file = await api.downloadBookingInvoice(bookingId, token);
      const url = URL.createObjectURL(file.blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = file.fileName;
      document.body.appendChild(a);
      a.click();
      a.remove();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (err) {
      alert("Download failed.");
    }
  }

  async function handleDownloadReceipt(bookingId: string) {
    try {
      const file = await api.downloadBookingReceipt(bookingId, token);
      const url = URL.createObjectURL(file.blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = file.fileName;
      document.body.appendChild(a);
      a.click();
      a.remove();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (err) {
      alert("Download failed.");
    }
  }

  return (
    <div className="page-container container py-6" data-testid="trav-10-page" id="TRAV-10">
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">TRAV-10 / TRAV-11</span>
          <h2>Payment History & Invoices</h2>
          <PatoisPhrase phrase="Financial Transactions & Records" translation="View all past billing transactions, download receipts, and export yearly invoices." />
        </div>
        <div className="flex gap-2" id="TRAV-11">
          <select className="input-control" value={yearFilter} onChange={(e) => setYearFilter(e.target.value)}>
            <option value="2026">2026</option>
            <option value="2025">2025</option>
          </select>
          <button type="button" className="btn btn-outline" onClick={() => alert("Bulk export initiated.")}>
            <Download size={16} /> Bulk Export PDF
          </button>
        </div>
      </header>

      {loading ? (
        <div className="loading-shimmer p-6 text-center">Loading payment history...</div>
      ) : bookings.length === 0 ? (
        <div className="card-box text-center py-8">
          <p>No transaction history found.</p>
        </div>
      ) : (
        <div className="card-box">
          <table className="table-styled w-full">
            <thead>
              <tr>
                <th>Booking Ref</th>
                <th>Property</th>
                <th>Dates</th>
                <th>Status</th>
                <th>Total</th>
                <th className="text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {bookings.map((b) => (
                <tr key={b.id}>
                  <td><strong>NSTY-BK-{b.id.substring(0, 8)}</strong></td>
                  <td>{b.propertyTitle}</td>
                  <td>{b.checkIn} to {b.checkOut}</td>
                  <td><span className="badge badge-green">{b.paymentStatus}</span></td>
                  <td><strong>{formatMoney(b.totalAmount, b.currency)}</strong></td>
                  <td className="text-right flex justify-end gap-2">
                    <button type="button" className="btn btn-ghost btn-sm" onClick={() => handleDownloadInvoice(b.id)}>
                      <FileText size={14} /> Invoice
                    </button>
                    <button type="button" className="btn btn-outline btn-sm" onClick={() => handleDownloadReceipt(b.id)}>
                      <Download size={14} /> Receipt
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
