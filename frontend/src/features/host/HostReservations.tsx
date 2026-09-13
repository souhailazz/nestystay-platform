import { useState, useEffect, useMemo } from "react";
import { Calendar } from "lucide-react";
import { api, type Booking } from "../../lib/api";
import { PatoisPhrase } from "../../lib/patois";
import { ListControls, downloadCsv } from "../../components/ui/ListControls";
import { announceFeedback } from "../../lib/feedback";

interface HostReservationsProps {
  token: string;
}

export function HostReservations({ token }: HostReservationsProps) {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loading, setLoading] = useState(true);
  const [notice, setNotice] = useState<string | null>(null);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("all");
  const [sort, setSort] = useState("date");
  const [page, setPage] = useState(0);
  const [selected, setSelected] = useState<string[]>([]);
  const [rejecting, setRejecting] = useState<Booking | null>(null);
  const [rejectReason, setRejectReason] = useState("");

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const list = await api.getBookings(token);
        if (active) setBookings(list);
      } catch (err) {
        if (active) setNotice(err instanceof Error ? err.message : "Unable to load reservations.");
      } finally {
        if (active) setLoading(false);
      }
    }
    load();
    return () => { active = false; };
  }, [token]);

  const filtered = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    return [...bookings]
      .filter((booking) => status === "all" || booking.status.toLowerCase() === status)
      .filter((booking) => !normalized || `${booking.propertyTitle ?? ""} ${booking.id} ${booking.status} ${booking.paymentStatus}`.toLowerCase().includes(normalized))
      .sort((left, right) => sort === "property" ? (left.propertyTitle ?? "").localeCompare(right.propertyTitle ?? "") : left.checkIn.localeCompare(right.checkIn));
  }, [bookings, query, sort, status]);
  useEffect(() => setPage(0), [query, sort, status]);
  const pageSize = 8;
  const visible = filtered.slice(page * pageSize, (page + 1) * pageSize);
  const canHostDecline = (booking: Booking) =>
    (booking.status === "PENDING" || booking.status === "APPROVED") &&
    booking.paymentStatus !== "CAPTURED" &&
    booking.paymentStatus !== "REFUNDED";

  async function handleCapture(id: string) {
    try {
      await api.capturePayment(id, token);
      setBookings((current) => current.map(b => b.id === id ? { ...b, paymentStatus: "CAPTURED", status: "APPROVED" } : b));
      setNotice("Booking approved and payment captured.");
      announceFeedback("Booking approved and payment captured.");
    } catch (err) {
      setNotice(err instanceof Error ? err.message : "Unable to update this booking.");
    }
  }

  async function handleReject() {
    if (!rejecting || !rejectReason.trim()) {
      setNotice("Please provide a clear reason for declining this request.");
      return;
    }
    try {
      const updated = await api.rejectBooking(rejecting.id, token, rejectReason.trim());
      setBookings((current) => current.map((booking) => booking.id === updated.id ? updated : booking));
      setNotice("Booking request declined and dates released.");
      announceFeedback("Booking request declined and dates released.", "info");
      setRejecting(null);
      setRejectReason("");
    } catch (err) {
      setNotice(err instanceof Error ? err.message : "Unable to decline this booking.");
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
        <button type="button" className="btn btn-outline" onClick={() => announceFeedback("Your iCal feed is available from Calendar settings.", "info")}>
          <Calendar size={16} /> iCal Calendar Feed
        </button>
      </header>

      {notice && <div className="notice-panel mb-4" role="alert">{notice}</div>}

      {loading ? (
        <div className="loading-shimmer p-6 text-center">Loading reservations...</div>
      ) : (
        <>
          <ListControls
            className="mb-5"
            label="Filter reservations"
            onExport={() => downloadCsv("nesty-reservations.csv", ["Booking ID", "Property", "Check-in", "Check-out", "Status", "Payment"], filtered.map((booking) => [booking.id, booking.propertyTitle, booking.checkIn, booking.checkOut, booking.status, booking.paymentStatus]))}
            onPageChange={setPage}
            onQueryChange={setQuery}
            onSortChange={setSort}
            page={page}
            pageSize={pageSize}
            query={query}
            sort={sort}
            sortOptions={[{ value: "date", label: "Check-in date" }, { value: "property", label: "Property" }]}
            total={filtered.length}
          />
          <div className="mb-4 flex flex-wrap items-center gap-2" role="group" aria-label="Filter reservation status">
            {[["all", "All"], ["pending", "Pending"], ["approved", "Approved"], ["rejected", "Rejected"]].map(([value, label]) => <button aria-pressed={status === value} className={`btn btn-sm ${status === value ? "btn-primary" : "btn-outline"}`} key={value} onClick={() => setStatus(value)} type="button">{label}</button>)}
          </div>
          <div className="card-box hidden md:block">
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
              {visible.map((b) => (
                <tr key={b.id}>
                  <td><label className="inline-flex items-center gap-2"><input aria-label={`Select booking ${b.id.substring(0, 8)}`} checked={selected.includes(b.id)} onChange={() => setSelected((items) => items.includes(b.id) ? items.filter((item) => item !== b.id) : [...items, b.id])} type="checkbox" /><strong>NSTY-BK-{b.id.substring(0, 8)}</strong></label></td>
                  <td>{b.propertyTitle}</td>
                  <td>{b.checkIn} to {b.checkOut}</td>
                  <td><span className="badge badge-sun">{b.verificationStatus}</span></td>
                  <td><span className="badge badge-green">{b.paymentStatus}</span></td>
                  <td className="text-right">
                    {canHostDecline(b) && (
                      <div className="flex justify-end gap-2"><button type="button" className="btn btn-ghost btn-sm text-coral" onClick={() => { setRejecting(b); setRejectReason(""); }}>Decline</button>{b.status === "PENDING" && <button type="button" className="btn btn-primary btn-sm" onClick={() => void handleCapture(b.id)}>Approve Booking</button>}</div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
          <div className="grid gap-3 md:hidden">{visible.map((b) => <article className="card-box" key={b.id}><div className="flex items-start justify-between gap-3"><div><strong>NSTY-BK-{b.id.substring(0, 8)}</strong><p className="subtext mt-1">{b.propertyTitle}</p></div><span className="badge badge-sun">{b.status}</span></div><div className="mt-3 grid gap-1 text-sm"><span><strong>Dates:</strong> {b.checkIn} to {b.checkOut}</span><span><strong>Verification:</strong> {b.verificationStatus}</span><span><strong>Payment:</strong> {b.paymentStatus}</span>{b.rejectionReason && <span className="text-coral-text"><strong>Reason:</strong> {b.rejectionReason}</span>}</div>{canHostDecline(b) && <div className="mt-4 flex flex-wrap gap-2"><button type="button" className="btn btn-ghost btn-sm text-coral" onClick={() => { setRejecting(b); setRejectReason(""); }}>Decline</button>{b.status === "PENDING" && <button type="button" className="btn btn-primary btn-sm" onClick={() => void handleCapture(b.id)}>Approve Booking</button>}</div>}</article>)}</div>
          {selected.length > 0 && <div className="mt-3 flex flex-wrap items-center justify-between gap-2 rounded-field border border-sand-border bg-shell px-3 py-2 text-sm"><span>{selected.length} reservation{selected.length === 1 ? "" : "s"} selected</span><button className="btn btn-outline btn-sm" onClick={() => setSelected([])} type="button">Clear selection</button></div>}
        </>
      )}
      {rejecting && <div className="modal-backdrop" role="dialog" aria-modal="true"><div className="modal-card w-[min(100%-2rem,520px)]"><h3>Decline booking request</h3><p className="subtext">This reason is shown to the guest and stored in the booking history.</p><textarea aria-label="Reason for declining booking" className="input-control mt-3" rows={4} placeholder="For example: The property is no longer available for these dates." value={rejectReason} onChange={(event) => setRejectReason(event.target.value)} /><div className="mt-4 flex flex-wrap justify-end gap-2"><button type="button" className="btn btn-ghost" onClick={() => setRejecting(null)}>Cancel</button><button type="button" className="btn btn-primary" onClick={() => void handleReject()}>Confirm decline</button></div></div></div>}
    </div>
  );
}
