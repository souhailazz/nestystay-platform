import { useState, useEffect } from "react";
import { CheckCircle2, XCircle, Calendar, Download, Search, ShieldCheck } from "lucide-react";
import { api, formatMoney, type Booking } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";

interface HostReservationsProps {
  token: string;
}

export function HostReservations({ token }: HostReservationsProps) {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loading, setLoading] = useState(true);

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

  async function handleCapture(id: string) {
    try {
      await api.capturePayment(id, token);
      setBookings(bookings.map(b => b.id === id ? { ...b, paymentStatus: "CAPTURED", status: "APPROVED" } : b));
    } catch (err) {
      alert("Action failed.");
    }
  }

  return (
    <div className="page-container container py-6" data-testid="host-09-page" id="HOST-09">
      <header className="page-header mb-6 flex justify-between items-center">
        <div>
          <span className="badge badge-sun">HOST-09</span>
          <h2>Host Reservation Management</h2>
          <PatoisPhrase phrase="Manage Guest Bookings & Approvals" translation="Approve or decline booking requests and export iCal calendar feeds." />
        </div>
        <button type="button" className="btn btn-outline" onClick={() => alert("iCal Feed URL: https://api.nestystay.com/ical/host-feed.ics")}>
          <Calendar size={16} /> iCal Calendar Feed
        </button>
      </header>

      {loading ? (
        <div className="loading-shimmer p-6 text-center">Loading reservations...</div>
      ) : (
        <div className="card-box">
          <table className="table-styled w-full">
            <thead>
              <tr>
                <th>Booking ID</th>
                <th>Property</th>
                <th>Dates</th>
                <th>Guest Status</th>
                <th>Payment</th>
                <th className="text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {bookings.map((b) => (
                <tr key={b.id}>
                  <td><strong>NSTY-BK-{b.id.substring(0, 8)}</strong></td>
                  <td>{b.propertyTitle}</td>
                  <td>{b.checkIn} to {b.checkOut}</td>
                  <td><span className="badge badge-sun">{b.verificationStatus}</span></td>
                  <td><span className="badge badge-green">{b.paymentStatus}</span></td>
                  <td className="text-right">
                    {b.status === "PENDING" && (
                      <button type="button" className="btn btn-primary btn-sm" onClick={() => handleCapture(b.id)}>
                        Approve Booking
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
