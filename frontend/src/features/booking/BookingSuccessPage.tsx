import { useState, useEffect } from "react";
import { Download } from "lucide-react";
import { AppLink } from "../../components/AppLink";
import { LoadingState } from "../../components/ui/LoadingState";
import { api, formatMoney } from "../../lib/api";
import type { BookingDetails } from "./types";
import type { AuthController } from "../../hooks/useAuth";
import {
  BookingScaffold,
  BookingStepper,
  DEGRESSIVE_NOTE,
  PriceSummary,
  PropertyMiniHeader,
  bookingCard,
  bookingDeepCta,
} from "./BookingShell";

interface BookingSuccessPageProps {
  bookingId: string;
  auth: AuthController;
}

const outlinePill =
  "inline-flex min-h-[46px] cursor-pointer items-center gap-2 rounded-pill border-[1.5px] border-sand-input bg-transparent px-5 font-sans text-[13.5px] font-semibold text-ink transition-colors hover:border-deep";

/* BOOK-CONF (DS v2) — confirmation & receipt. Download/API logic unchanged;
   receipt lines are the backend price breakdown. */
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
    return () => {
      active = false;
    };
  }, [bookingId, auth.session?.accessToken]);

  async function handleDownload(kind: "invoice" | "receipt") {
    if (!booking || !auth.session) return;
    try {
      const doc =
        kind === "invoice"
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
      <div className="mx-auto max-w-[1160px] px-6 py-9" data-testid="book-04-loading">
        <LoadingState label="Loading confirmation details" />
      </div>
    );
  }

  if (!booking) {
    return (
      <div className="mx-auto max-w-[1160px] px-6 py-9" data-testid="book-04-error">
        <div className="rounded-field bg-coral-tint px-4 py-3 text-[13px] text-coral-text">
          Booking information not found.
        </div>
      </div>
    );
  }

  const paymentStatus = booking.paymentStatus ?? booking.status ?? "Confirmed";
  const captured = /captured|paid|succeed/i.test(paymentStatus);

  return (
    <div data-testid="book-04-page" id="BOOK-CONF">
      <BookingScaffold
        aside={
          <>
            <PropertyMiniHeader
              ekycRequired={booking.requiresGuestVerification}
              subtitle={`${booking.checkIn} → ${booking.checkOut} · ${booking.nights} night${booking.nights > 1 ? "s" : ""}`}
              title={booking.propertyTitle ?? "Your stay"}
            />
            <PriceSummary currency={booking.currency} lines={booking.priceBreakdown ?? []} total={booking.totalAmount} />
            <div className="text-[11.5px] text-sand-500">{DEGRESSIVE_NOTE}</div>
            {captured && (
              <div className="rounded-field bg-success-tint px-4 py-3 text-center text-[12.5px] text-success-text">
                Payment captured — this booking is locked in.
              </div>
            )}
          </>
        }
        stepper={<BookingStepper current={5} />}
      >
        <div className="flex flex-col gap-2">
          <span className="self-start rounded-pill bg-success-tint px-3.5 py-1.5 text-[11px] font-bold tracking-[0.08em] text-success-text">
            {captured ? "CAPTURED · CONFIRMED" : paymentStatus.toUpperCase()}
          </span>
          <h1 className="m-0 font-display text-[clamp(30px,3.6vw,42px)] font-normal tracking-[-0.01em]">
            You&apos;re all <em className="italic text-deep-hover">set.</em>
          </h1>
          <div className="text-sm text-gray-600">Booking confirmed and paid. A receipt is on its way to your inbox.</div>
        </div>

        {/* Receipt */}
        <div className={bookingCard}>
          <div className="flex flex-wrap items-baseline justify-between gap-2.5">
            <div className="text-[13px] font-semibold">Receipt</div>
            <span className="font-mono text-[12.5px] text-gray-600">NSTY-BK-{booking.id.slice(0, 8).toUpperCase()}</span>
          </div>
          <div className="flex flex-col text-[13.5px]">
            {(booking.priceBreakdown ?? []).map((line, idx) => (
              <div className="flex items-center justify-between gap-2 border-b border-shell py-[7px]" key={idx}>
                <span>{line.description}</span>
                <strong>{formatMoney(line.amount, line.currency)}</strong>
              </div>
            ))}
            <div className="flex items-center justify-between py-[9px]">
              <strong>Paid{booking.paymentProvider ? ` (${booking.paymentProvider})` : ""}</strong>
              <strong className="font-display text-xl">{formatMoney(booking.totalAmount, booking.currency)}</strong>
            </div>
          </div>
          <div className="flex flex-wrap gap-2.5">
            <button className={outlinePill} onClick={() => handleDownload("receipt")} type="button">
              <Download size={15} /> Download receipt (PDF)
            </button>
            <button className={outlinePill} onClick={() => handleDownload("invoice")} type="button">
              <Download size={15} /> Download invoice (PDF)
            </button>
            <AppLink className={outlinePill} href="/traveler/invoices">
              View all invoices
            </AppLink>
          </div>
          {downloadNotice && (
            <div className="rounded-field bg-shell px-4 py-3 text-[13px] text-gray-600" role="status">
              {downloadNotice}
            </div>
          )}
        </div>

        {/* What happens next */}
        <div className={bookingCard}>
          <div className="text-[13px] font-semibold">What happens next</div>
          <div className="text-[13.5px] text-gray-600">
            Check-in details unlock 48h before arrival. Cancellations follow the property&apos;s policy — non-refundable
            fees are marked on your receipt.
          </div>
          <div className="flex flex-wrap gap-2.5">
            <AppLink className={bookingDeepCta} href="/guest-dashboard">
              Go to my trips{" "}
              <span aria-hidden="true">→</span>
            </AppLink>
            <AppLink
              className="inline-flex min-h-[50px] items-center rounded-pill border-[1.5px] border-sand-input px-6 text-[14.5px] font-semibold text-ink transition-colors hover:border-deep"
              href="/explore"
            >
              Keep exploring
            </AppLink>
          </div>
        </div>
      </BookingScaffold>
    </div>
  );
}
