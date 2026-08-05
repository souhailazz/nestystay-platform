import { useEffect, useState } from "react";
import { AppLink } from "../../components/AppLink";
import { LoadingState } from "../../components/ui/LoadingState";
import { StatusChip } from "../../components/ui/StatusChip";
import { api, formatMoney, type Booking } from "../../lib/api";
import { getStayImage } from "../../lib/stayImages";

interface TravelerDashboardProps {
  userId: string;
  token: string;
}

const outlinePill =
  "inline-flex min-h-[46px] items-center self-start rounded-pill border-[1.5px] border-sand-input px-5 font-sans text-[13.5px] font-semibold text-ink transition-colors hover:border-deep";

function isFinished(booking: Booking) {
  return new Date(booking.checkOut).getTime() < Date.now();
}

function isCancelled(booking: Booking) {
  return /cancel|reject/i.test(booking.status);
}

function needsVerification(booking: Booking) {
  return booking.requiresGuestVerification && !/pass|approv|verif/i.test(booking.verificationStatus ?? "");
}

/* TRAV-01 (DS v2) — traveler dashboard. GET /bookings (mine); every row shows
   the contractual TRIPLE status: booking / verification / payment, verbatim. */
export function TravelerDashboard({ userId: _userId, token }: TravelerDashboardProps) {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    async function loadData() {
      try {
        const list = await api.getBookings(token);
        if (active) setBookings(list);
      } catch (err) {
        console.error(err);
      } finally {
        if (active) setLoading(false);
      }
    }
    loadData();
    return () => {
      active = false;
    };
  }, [token]);

  if (loading) {
    return (
      <div data-testid="trav-01-loading">
        <LoadingState label="Loading your traveler dashboard" />
      </div>
    );
  }

  const upcoming = bookings.filter((b) => !isFinished(b) && !isCancelled(b));
  const completed = bookings.filter((b) => isFinished(b) && !isCancelled(b));
  const totalSpent = bookings
    .filter((b) => /captur|paid/i.test(b.paymentStatus ?? ""))
    .reduce((sum, b) => sum + b.totalAmount, 0);
  const currency = bookings[0]?.currency ?? "USD";

  return (
    <div className="flex flex-col gap-5" data-testid="trav-01-page" id="TRAV-01">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        Your <em className="italic text-deep-hover">trips</em>
      </h1>

      {/* Stat cards */}
      <div className="grid grid-cols-[repeat(auto-fit,minmax(190px,1fr))] gap-3.5">
        {(
          [
            ["UPCOMING", String(upcoming.length)],
            ["COMPLETED", String(completed.length)],
            ["TOTAL SPENT", formatMoney(totalSpent, currency)],
          ] as const
        ).map(([label, value]) => (
          <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]" key={label}>
            <div className="text-[11px] font-semibold tracking-[0.16em] text-sand-500">{label}</div>
            <div className="font-display text-[34px] font-medium leading-none">{value}</div>
          </div>
        ))}
      </div>

      {/* Booking rows with triple status */}
      {bookings.length === 0 ? (
        <div className="flex flex-col items-start gap-3 rounded-card border border-dashed border-sand-input bg-cream p-6">
          <div className="font-display text-lg font-medium">No trips yet</div>
          <p className="m-0 text-[13.5px] text-gray-600">Ready for your next Jamaican getaway?</p>
          <AppLink
            className="inline-flex min-h-[46px] items-center rounded-pill bg-deep px-6 font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover"
            href="/explore"
          >
            Explore stays
          </AppLink>
        </div>
      ) : (
        bookings.map((booking, index) => {
          const image = getStayImage(index);
          const resume = needsVerification(booking);
          const timeline = (booking as Booking & { timeline?: string[] }).timeline ?? [];
          return (
            <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]" key={booking.id}>
              <div className="flex flex-wrap items-start gap-3.5">
                <img alt="" className="block size-24 shrink-0 rounded-field object-cover" src={image.src} />
                <div className="min-w-[220px] flex-1">
                  <div className="flex flex-wrap items-baseline justify-between gap-2.5">
                    <div className="font-display text-[19px] font-medium">{booking.propertyTitle ?? "Jamaican stay"}</div>
                    <span className="font-mono text-[11.5px] text-sand-500">NSTY-BK-{booking.id.slice(0, 8).toUpperCase()}</span>
                  </div>
                  <div className="text-[12.5px] text-gray-600">
                    {booking.checkIn} → {booking.checkOut} · {booking.nights} night{booking.nights === 1 ? "" : "s"} ·{" "}
                    {formatMoney(booking.totalAmount, booking.currency)}
                  </div>
                  <div className="mt-2 flex flex-wrap gap-1.5">
                    <StatusChip label="Booking" value={booking.status} />
                    <StatusChip label="Verification" value={booking.verificationStatus} />
                    <StatusChip label="Payment" value={booking.paymentStatus} />
                  </div>
                </div>
                <AppLink className={outlinePill} href={`/booking/${booking.id}/${resume ? "pending" : "success"}`}>
                  {resume ? "Resume verification" : "Details"}
                </AppLink>
              </div>
              {index === 0 && timeline.length > 0 && (
                <div className="flex flex-col border-t border-shell pt-3">
                  {timeline.map((item, i) => {
                    const last = i === timeline.length - 1;
                    return (
                      <div className="grid grid-cols-[20px_1fr] gap-3.5" key={i}>
                        <div className="flex flex-col items-center">
                          <span className="mt-[3px] size-3 rounded-full bg-success" />
                          {!last && <span className="w-0.5 flex-1 bg-sand-border" />}
                        </div>
                        <div className={last ? "" : "pb-3"}>
                          <div className="text-[13px] font-semibold">{item}</div>
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          );
        })
      )}
    </div>
  );
}
