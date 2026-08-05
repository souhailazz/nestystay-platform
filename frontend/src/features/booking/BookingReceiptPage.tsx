import { useState, useEffect } from "react";
import { Download, Printer } from "lucide-react";
import { api, formatMoney } from "../../lib/api";
import type { BookingDetails } from "./types";
import { LoadingState } from "../../components/ui/LoadingState";
import type { AuthController } from "../../hooks/useAuth";

interface BookingReceiptPageProps {
  bookingId: string;
  auth: AuthController;
}

/* BOOK-CONF receipt view (DS v2) — download/print logic unchanged. */
export function BookingReceiptPage({ bookingId, auth }: BookingReceiptPageProps) {
  const [booking, setBooking] = useState<BookingDetails | null>(null);
  const [downloading, setDownloading] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

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
    return () => {
      active = false;
    };
  }, [bookingId, auth.session?.accessToken]);

  async function handleDownload() {
    if (!booking || !auth.session) return;
    setDownloading(true);
    setNotice(null);
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
      setNotice(`Download failed: ${err instanceof Error ? err.message : "Error"}`);
    } finally {
      setDownloading(false);
    }
  }

  if (!booking) {
    return (
      <div className="mx-auto max-w-3xl px-6 py-9" data-testid="book-10-loading">
        <LoadingState label="Loading receipt" />
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-3xl px-6 py-9 font-sans text-[15px] leading-[1.55] text-ink" data-testid="book-10-page" id="BOOK-10">
      <div className="printable-area flex flex-col gap-5 rounded-card border border-sand-border bg-cream p-7">
        <header className="flex flex-wrap items-start justify-between gap-4 border-b border-shell pb-4">
          <div>
            <span className="rounded-pill bg-success-tint px-3 py-1 text-[11px] font-bold tracking-[0.06em] text-success-text">
              {(booking.paymentStatus ?? "Captured").toUpperCase()}
            </span>
            <h1 className="m-0 mt-2 font-display text-[26px] font-medium">Payment receipt</h1>
            <p className="m-0 mt-1 font-mono text-[12.5px] text-gray-600">
              NSTY-RCP-{booking.id.substring(0, 8).toUpperCase()}
            </p>
          </div>
          <div className="text-right text-[13px] text-gray-600">
            {booking.checkIn} → {booking.checkOut}
            <br />
            {booking.nights} night{booking.nights > 1 ? "s" : ""}
          </div>
        </header>

        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <div className="text-[13px] font-semibold">Paid by</div>
            <p className="m-0 mt-1">{auth.session?.displayName || "Guest User"}</p>
            <p className="m-0 text-[13px] text-gray-600">{auth.session?.email || "guest@nestystay.local"}</p>
          </div>
          <div className="sm:text-right">
            <div className="text-[13px] font-semibold">Payment reference</div>
            <p className="m-0 mt-1">{booking.paymentProvider ?? "Stripe"}</p>
            {booking.paymentAuthorizationReference && (
              <p className="m-0 font-mono text-[12px] text-gray-600">{booking.paymentAuthorizationReference}</p>
            )}
          </div>
        </div>

        <div className="flex flex-col text-[13.5px]">
          {(booking.priceBreakdown ?? []).map((line, idx) => (
            <div className="flex items-center justify-between gap-2 border-b border-shell py-[7px]" key={idx}>
              <span>{line.description}</span>
              <strong>{formatMoney(line.amount, line.currency)}</strong>
            </div>
          ))}
        </div>

        <div className="rounded-field bg-success-tint px-4 py-4 text-center">
          <div className="text-[12px] font-semibold tracking-[0.08em] text-success-text">AMOUNT CAPTURED</div>
          <div className="font-display text-[32px] font-medium text-success-text">
            {formatMoney(booking.totalAmount, booking.currency)}
          </div>
        </div>

        {notice && (
          <div className="rounded-field bg-coral-tint px-4 py-3 text-[13px] text-coral-text" role="alert">
            {notice}
          </div>
        )}

        <footer className="no-print flex flex-wrap items-center justify-between gap-3 border-t border-shell pt-4">
          <button
            className="inline-flex min-h-11 cursor-pointer items-center gap-2 rounded-pill border-none bg-transparent px-4 font-sans text-sm font-semibold text-ink transition-colors hover:bg-shell"
            onClick={() => window.print()}
            type="button"
          >
            <Printer size={16} /> Print receipt
          </button>
          <button
            className="inline-flex min-h-12 cursor-pointer items-center gap-2 rounded-pill border-none bg-deep px-6 font-sans text-sm font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover disabled:pointer-events-none disabled:bg-shell disabled:text-sand-500"
            disabled={downloading}
            onClick={handleDownload}
            type="button"
          >
            <Download size={16} /> {downloading ? "Downloading PDF…" : "Download PDF receipt"}
          </button>
        </footer>
      </div>
    </div>
  );
}
