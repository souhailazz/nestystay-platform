import { useMemo, useState, useEffect } from "react";
import { Download } from "lucide-react";
import { api, formatMoney, type Booking } from "../../lib/api";
import { LoadingState } from "../../components/ui/LoadingState";
import { EmptyState } from "../../components/ui/EmptyState";
import { StatusChip } from "../../components/ui/StatusChip";

interface TravelerPaymentHistoryProps {
  token: string;
}

/* TRAV-INV (DS v2) — invoice table from real bookings: NST-YYYY-XXXX refs,
   client-side year filter, per-row download + Download all. Logic unchanged. */
export function TravelerPaymentHistory({ token }: TravelerPaymentHistoryProps) {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loading, setLoading] = useState(true);
  const [year, setYear] = useState("all");
  const [notice, setNotice] = useState<string | null>(null);

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
    return () => {
      active = false;
    };
  }, [token]);

  const years = useMemo(() => {
    const values = new Set(bookings.map((b) => b.checkIn.slice(0, 4)));
    return Array.from(values).sort().reverse();
  }, [bookings]);
  const invoices = useMemo(
    () => (year === "all" ? bookings : bookings.filter((b) => b.checkIn.startsWith(year))),
    [bookings, year],
  );

  async function download(kind: "invoice" | "receipt", bookingId: string) {
    setNotice(null);
    try {
      const file =
        kind === "invoice"
          ? await api.downloadBookingInvoice(bookingId, token)
          : await api.downloadBookingReceipt(bookingId, token);
      const url = URL.createObjectURL(file.blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = file.fileName;
      document.body.appendChild(a);
      a.click();
      a.remove();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (err) {
      setNotice(`Download failed: ${err instanceof Error ? err.message : "Error"}`);
    }
  }

  async function downloadAll() {
    for (const booking of invoices) {
      // sequential to keep the browser happy with multiple blobs
      await download("invoice", booking.id);
    }
  }

  if (loading) return <LoadingState label="Loading your invoices" />;

  const gridCols = "grid-cols-[1.1fr_1.4fr_1fr_0.7fr_0.8fr_1.1fr]";

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" data-testid="trav-10-page" id="TRAV-INV">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">Invoices</h1>
        <div className="flex flex-wrap gap-2.5">
          <select
            aria-label="Filter by year"
            className="min-h-[46px] rounded-pill border-[1.5px] border-sand-input bg-cream px-[18px] font-sans text-[13.5px] font-semibold text-ink outline-none focus:border-deep-hover"
            onChange={(e) => setYear(e.target.value)}
            value={year}
          >
            <option value="all">All years</option>
            {years.map((y) => (
              <option key={y} value={y}>
                {y}
              </option>
            ))}
          </select>
          <button
            className="inline-flex min-h-[46px] cursor-pointer items-center gap-2 rounded-pill border-none bg-deep px-[22px] font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover disabled:pointer-events-none disabled:bg-shell disabled:text-sand-500"
            disabled={invoices.length === 0}
            onClick={downloadAll}
            type="button"
          >
            <Download size={15} /> Download all
          </button>
        </div>
      </div>

      {notice && (
        <div className="rounded-field bg-coral-tint px-4 py-3 text-[13px] text-coral-text" role="alert">
          {notice}
        </div>
      )}

      {invoices.length === 0 ? (
        <EmptyState title="No invoices for this filter" copy="Invoices appear here after you book a stay." />
      ) : (
        <div className="overflow-hidden rounded-card border border-sand-border bg-cream">
          <div className={`grid ${gridCols} gap-3 bg-shell px-5 py-3 text-[11px] font-bold tracking-[0.1em] text-sand-500`}>
            <span>INVOICE</span>
            <span>PROPERTY</span>
            <span>DATES</span>
            <span>AMOUNT</span>
            <span>STATUS</span>
            <span />
          </div>
          {invoices.map((booking, index) => (
            <div
              className={`grid ${gridCols} items-center gap-3 border-b border-shell px-5 py-[13px] text-[13px] last:border-b-0 ${index % 2 === 1 ? "bg-[#FAF6EA]" : ""}`}
              key={booking.id}
            >
              <span className="font-mono text-xs">
                NST-{booking.checkIn.slice(0, 4)}-{booking.id.slice(0, 4).toUpperCase()}
              </span>
              <span>{booking.propertyTitle ?? booking.propertyId}</span>
              <span className="text-gray-600">
                {booking.checkIn} → {booking.checkOut}
              </span>
              <strong>{formatMoney(booking.totalAmount, booking.currency)}</strong>
              <span>
                <StatusChip value={booking.paymentStatus === "CAPTURED" ? "Paid" : booking.paymentStatus} />
              </span>
              <span className="flex justify-end gap-3">
                <button
                  className="inline-flex min-h-11 cursor-pointer items-center gap-1 border-none bg-transparent p-0 text-[12.5px] font-bold text-deep-hover hover:text-deep"
                  onClick={() => download("invoice", booking.id)}
                  type="button"
                >
                  <Download size={13} /> Invoice
                </button>
                <button
                  className="inline-flex min-h-11 cursor-pointer items-center gap-1 border-none bg-transparent p-0 text-[12.5px] font-bold text-deep-hover hover:text-deep"
                  onClick={() => download("receipt", booking.id)}
                  type="button"
                >
                  <Download size={13} /> Receipt
                </button>
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
