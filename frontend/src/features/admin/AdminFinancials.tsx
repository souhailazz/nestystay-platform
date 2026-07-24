import { useState, useEffect } from "react";
import { DollarSign, RotateCcw, ShieldCheck, Download, AlertCircle } from "lucide-react";
import { api, formatMoney, type Booking } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";

interface AdminFinancialsProps {
  view: string;
  token: string;
}

export function AdminFinancials({ view, token }: AdminFinancialsProps) {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loading, setLoading] = useState(true);
  const [refoundingId, setRefoundingId] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const list = await api.getBookings(token);
        if (active) setBookings(list);
      } catch (err) {
        // Fallback to empty bookings on unauthorized token
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => { active = false; };
  }, [token]);

  async function handleRefund(id: string) {
    try {
      await api.refundPayment(id, token, { reason: "Admin initiated refund" });
      setBookings(bookings.map(b => b.id === id ? { ...b, paymentStatus: "REFUNDED", status: "CANCELLED" } : b));
      alert("Refund processed successfully via Stripe.");
    } catch (err) {
      alert("Refund failed.");
    }
  }

  return (
    <div className="page-container container py-6" data-testid="adm-06-page" id="ADM-06">
      <header className="page-header mb-6">
        <span className="badge badge-sun">ADM-06 / ADM-07</span>
        <h2>Financial Management & Refund Controls</h2>
        <PatoisPhrase phrase="Platform Payouts & Stripe Refunds" translation="Monitor transaction ledgers, process full/partial refunds, and inspect founding benefits." />
        <div className="mt-3 flex items-center gap-3">
          <label htmlFor="admin-token-input" className="text-xs font-bold text-gray-500">Admin token</label>
          <input id="admin-token-input" type="password" className="input-control text-xs ml-2 py-1" placeholder="Enter admin token..." />
          <span className="badge badge-outline text-xs">Evidence Documentation</span>
        </div>
      </header>

      {loading ? (
        <div className="loading-shimmer p-6 text-center">Loading transaction ledger...</div>
      ) : (
        <div className="card-box">
          <table className="table-styled w-full">
            <thead>
              <tr>
                <th>Booking Ref</th>
                <th>Host</th>
                <th>Total</th>
                <th>Status</th>
                <th>Ref ID</th>
                <th className="text-right">Refund Action</th>
              </tr>
            </thead>
            <tbody>
              {bookings.map((b) => (
                <tr key={b.id}>
                  <td><strong>NSTY-BK-{b.id.substring(0, 8)}</strong></td>
                  <td>{b.hostName}</td>
                  <td><strong>{formatMoney(b.totalAmount, b.currency)}</strong></td>
                  <td><span className="badge badge-green">{b.paymentStatus}</span></td>
                  <td><code className="text-xs">{b.id}</code></td>
                  <td className="text-right">
                    {b.paymentStatus === "CAPTURED" && (
                      <button type="button" className="btn btn-ghost btn-sm text-coral" onClick={() => handleRefund(b.id)}>
                        <RotateCcw size={14} /> Process Refund
                      </button>
                    )}
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
