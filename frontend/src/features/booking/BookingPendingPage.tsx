import { useState, useEffect } from "react";
import { MessageSquare } from "lucide-react";
import { AppLink } from "../../components/AppLink";
import { api } from "../../lib/api";
import type { BookingDetails } from "./types";
import { usePatois } from "../../lib/patois";
import type { AuthController } from "../../hooks/useAuth";
import {
  BookingHeading,
  BookingScaffold,
  BookingStepper,
  DEGRESSIVE_NOTE,
  PriceSummary,
  PropertyMiniHeader,
  bookingCard,
} from "./BookingShell";

interface BookingPendingPageProps {
  bookingId: string;
  auth: AuthController;
}

const HOLD_TOTAL_MS = 60 * 60_000;

function useHoldCountdown(holdExpiresAt?: string) {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    if (!holdExpiresAt) return;
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, [holdExpiresAt]);
  if (!holdExpiresAt) return null;
  const remaining = Math.max(0, new Date(holdExpiresAt).getTime() - now);
  const minutes = Math.floor(remaining / 60_000);
  const seconds = Math.floor((remaining % 60_000) / 1000);
  return {
    label: `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`,
    fraction: Math.min(1, Math.max(0, remaining / HOLD_TOTAL_MS)),
    expired: remaining <= 0,
  };
}

/* BOOK-07 (DS v2) — pending. Two states from booking data: verification in
   progress (Nuh Fret block + always-visible 60-min hold countdown driven by
   holdExpiresAt) and generic host-pending with the events timeline. */
export function BookingPendingPage({ bookingId, auth }: BookingPendingPageProps) {
  const { showPatois } = usePatois();
  const [booking, setBooking] = useState<BookingDetails | null>(null);

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

  const verificationInProgress = Boolean(
    booking?.requiresGuestVerification &&
      booking.verificationStatus &&
      !/passed|approved|verified/i.test(booking.verificationStatus),
  );
  const countdown = useHoldCountdown(booking?.holdExpiresAt);
  const timeline = booking?.timeline ?? [];

  return (
    <div data-testid="book-07-page" id="BOOK-07">
      <BookingScaffold
        aside={
          booking ? (
            <>
              <PropertyMiniHeader
                ekycRequired={booking.requiresGuestVerification}
                subtitle={`Booking ${booking.id.slice(0, 8).toUpperCase()} · ${booking.checkIn} → ${booking.checkOut}`}
                title={booking.propertyTitle ?? "Your stay"}
              />
              <PriceSummary currency={booking.currency} lines={booking.priceBreakdown ?? []} total={booking.totalAmount} />
              <div className="text-[11.5px] text-sand-500">{DEGRESSIVE_NOTE}</div>
              <AppLink
                className="inline-flex min-h-12 items-center justify-center gap-2 rounded-pill border-[1.5px] border-sand-input px-5 text-sm font-semibold text-ink transition-colors hover:border-deep"
                href="/messages"
              >
                <MessageSquare size={16} /> Message host
              </AppLink>
            </>
          ) : (
            <div className="py-6 text-center text-[13px] text-gray-600">Loading booking…</div>
          )
        }
        stepper={<BookingStepper current={verificationInProgress ? 3 : 4} />}
      >
        <BookingHeading accent="there" pre="Nearly" />

        {/* STATE A — verification in progress (patois block, hold countdown) */}
        {verificationInProgress && (
          <div className="flex flex-col gap-4 rounded-card bg-deep p-7">
            <div className="flex flex-wrap items-start justify-between gap-3.5">
              <div>
                {showPatois ? (
                  <>
                    <div className="font-display text-[32px] italic text-yellow">Nuh Fret</div>
                    <div className="mt-0.5 text-[13.5px] text-on-dark-muted">
                      Don&apos;t worry — your identity is being verified.
                    </div>
                  </>
                ) : (
                  <div className="text-[17px] font-semibold text-on-dark-heading">Your identity is being verified.</div>
                )}
              </div>
              {countdown && (
                <div className="text-right">
                  <div className="text-[10.5px] font-bold tracking-[0.16em] text-on-dark-faint">DATES HELD FOR</div>
                  <div className="font-display text-[34px] font-medium text-on-dark-heading">
                    {countdown.expired ? "00:00" : countdown.label}
                  </div>
                  <div className="text-[11px] text-on-dark-faint">of 60:00 · holdExpiresAt</div>
                </div>
              )}
            </div>
            {countdown && (
              <div className="h-2 overflow-hidden rounded-pill bg-on-dark-heading/10">
                <div
                  className="h-full rounded-pill bg-yellow transition-[width] duration-1000"
                  style={{ width: `${Math.round(countdown.fraction * 100)}%` }}
                />
              </div>
            )}
            <div className="flex flex-wrap items-center gap-3">
              {booking?.ekycTransactionUrl ? (
                <a
                  className="inline-flex min-h-12 items-center gap-2 rounded-pill bg-yellow px-6 text-sm font-bold text-deep transition-colors hover:bg-yellow-press"
                  href={booking.ekycTransactionUrl}
                  rel="noopener noreferrer"
                  target="_blank"
                >
                  Open eKYC transaction ↗
                </a>
              ) : (
                <span className="inline-flex min-h-12 items-center rounded-pill bg-on-dark-heading/10 px-6 text-sm font-semibold text-on-dark-muted">
                  Preparing verification session…
                </span>
              )}
              <span className="text-xs text-on-dark-faint">External secure page — returns here automatically</span>
            </div>
          </div>
        )}

        {/* STATE B — waiting on the host */}
        <div className={bookingCard}>
          <div className="flex flex-wrap items-center justify-between gap-2.5">
            <div className="font-display text-xl font-medium">Waiting on the host</div>
            <span className="rounded-pill bg-amber-tint px-3 py-[5px] text-[11px] font-bold tracking-[0.06em] text-amber-text">
              {(booking?.status ?? "Pending").toUpperCase()}
            </span>
          </div>
          <div className="text-[13.5px] text-gray-600">
            {verificationInProgress
              ? "Once verification passes, your card stays authorized — the host then has 24 hours to confirm. You'll get a notification either way."
              : "Verification passed. Your card is authorized — the host has 24 hours to confirm. You'll get a notification either way."}
          </div>
          {timeline.length > 0 && (
            <div className="flex flex-col">
              {timeline.map((item, idx) => {
                const isLast = idx === timeline.length - 1;
                return (
                  <div className="grid grid-cols-[20px_1fr] gap-3.5" key={idx}>
                    <div className="flex flex-col items-center">
                      <span className={`mt-[3px] size-3 rounded-full ${isLast ? "bg-amber" : "bg-success"}`} />
                      {!isLast && <span className="w-0.5 flex-1 bg-sand-border" />}
                    </div>
                    <div className={isLast ? "" : "pb-4"}>
                      <div className="text-[13.5px] font-semibold">{item}</div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </BookingScaffold>
    </div>
  );
}
