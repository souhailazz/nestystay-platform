import { useEffect, useState } from "react";
import { api, type Booking, type TravelerReview, type TravelerWorkspace } from "../../lib/api";
import { LoadingState } from "../../components/ui/LoadingState";
import { EmptyState } from "../../components/ui/EmptyState";
import { StatusChip } from "../../components/ui/StatusChip";
import { getStayImage } from "../../lib/stayImages";

interface TravelerReviewsNotificationsProps {
  view: string;
  userId: string;
  token: string;
}

const REVIEW_WINDOW_DAYS = 30;

/* TRAV-PEND (DS v2) — pending reviews derived from real bookings (completed stays
   not yet reviewed, 30-day window); submissions go through the reviews API. */
export function TravelerReviewsNotifications({ view, userId, token }: TravelerReviewsNotificationsProps) {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [reviews, setReviews] = useState<TravelerReview[]>([]);
  const [loading, setLoading] = useState(true);
  const [openId, setOpenId] = useState<string | null>(null);
  const [rating, setRating] = useState(5);
  const [text, setText] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const [bookingList, workspace] = await Promise.all([
          api.getBookings(token),
          api.getTravelerWorkspace(userId, token) as Promise<TravelerWorkspace>,
        ]);
        if (active) {
          setBookings(bookingList);
          setReviews(workspace.reviews ?? []);
        }
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
  }, [userId, token, reloadKey]);

  if (loading) return <LoadingState label="Loading your reviews" />;

  const reviewedKeys = new Set(reviews.flatMap((r) => [r.bookingId, r.propertyId].filter(Boolean) as string[]));
  const pending = bookings
    .filter((b) => new Date(b.checkOut).getTime() < Date.now() && !/cancel|reject/i.test(b.status))
    .filter((b) => !reviewedKeys.has(b.id) && !reviewedKeys.has(b.propertyId))
    .map((b) => {
      const deadline = new Date(b.checkOut);
      deadline.setDate(deadline.getDate() + REVIEW_WINDOW_DAYS);
      const daysLeft = Math.ceil((deadline.getTime() - Date.now()) / 86_400_000);
      return { booking: b, daysLeft, closed: daysLeft <= 0 };
    });
  const open = pending.filter((p) => !p.closed);
  const closed = pending.filter((p) => p.closed);

  async function submit(booking: Booking) {
    setBusy(true);
    setError(null);
    try {
      await api.submitReview(userId, token, {
        bookingId: booking.id,
        propertyId: booking.propertyId,
        subjectTitle: booking.propertyTitle ?? "Jamaican stay",
        rating,
        text: text || "Great stay!",
      });
      setOpenId(null);
      setText("");
      setRating(5);
      setReloadKey((k) => k + 1);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Review could not be submitted.");
    } finally {
      setBusy(false);
    }
  }

  if (view === "reviews-given") {
    return (
      <div className="flex flex-col gap-4 font-sans text-ink" data-testid="trav-15-reviews" id="TRAV-15">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
          Your <em className="italic text-deep-hover">reviews.</em>
        </h1>
        {reviews.length === 0 && <EmptyState title="No reviews yet" copy="Reviews you write appear here." />}
        {reviews.map((review) => (
          <div className="flex flex-col gap-2 rounded-card border border-sand-border bg-cream p-5" key={review.id}>
            <div className="flex flex-wrap items-center justify-between gap-2.5">
              <div className="font-display text-[17px] font-medium">{review.subjectTitle}</div>
              <StatusChip value={`★ ${review.rating}/5`} />
            </div>
            <p className="m-0 text-[13.5px] text-gray-600">{review.text}</p>
            {review.hostReply && (
              <div className="rounded-field border-l-4 border-deep-hover bg-shell px-4 py-3 text-[13px]">
                <strong>Host reply:</strong> <span className="text-gray-600">{review.hostReply}</span>
              </div>
            )}
          </div>
        ))}
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" data-testid="trav-16-pending" id="TRAV-PEND">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        You have {open.length} pending <em className="italic text-deep-hover">review{open.length === 1 ? "" : "s"}.</em>
      </h1>
      {error && (
        <div className="rounded-field bg-coral-tint px-4 py-3 text-[13px] text-coral-text" role="alert">
          {error}
        </div>
      )}
      {open.length === 0 && <EmptyState title="Nothing to review" copy="Completed stays show up here for 30 days." />}
      <div className="grid grid-cols-[repeat(auto-fit,minmax(300px,1fr))] gap-4">
        {open.map(({ booking, daysLeft }, index) => (
          <div className="flex flex-col gap-3 self-start rounded-card bg-deep p-[22px]" key={booking.id}>
            <div className="flex items-center gap-3">
              <img alt="" className="block size-16 shrink-0 rounded-[12px] object-cover" src={getStayImage(index).src} />
              <div>
                <div className="font-display text-lg font-medium text-on-dark-heading">{booking.propertyTitle ?? "Jamaican stay"}</div>
                <div className="text-xs text-on-dark-muted">
                  stayed {booking.checkIn} → {booking.checkOut}
                </div>
              </div>
            </div>
            <div className="flex flex-wrap items-center justify-between gap-2.5">
              <span className="rounded-pill border border-amber px-3 py-[5px] text-[11px] font-bold tracking-[0.06em] text-yellow">
                {daysLeft} DAY{daysLeft === 1 ? "" : "S"} LEFT TO REVIEW
              </span>
              <button
                className="inline-flex min-h-[46px] cursor-pointer items-center rounded-pill border-none bg-yellow px-[22px] font-sans text-[13.5px] font-bold text-deep transition-colors hover:bg-yellow-press"
                onClick={() => setOpenId(openId === booking.id ? null : booking.id)}
                type="button"
              >
                Write review
              </button>
            </div>
            {openId === booking.id && (
              <div className="flex flex-col gap-2.5 rounded-field bg-night p-4">
                <label className="flex items-center gap-2.5 text-[13px] font-semibold text-on-dark-body">
                  Rating
                  <select
                    className="min-h-11 rounded-field border border-on-dark-faint/40 bg-transparent px-3 font-sans text-on-dark-heading outline-none [&>option]:text-ink"
                    onChange={(e) => setRating(Number(e.target.value))}
                    value={rating}
                  >
                    {[5, 4, 3, 2, 1].map((r) => (
                      <option key={r} value={r}>
                        {r} ★
                      </option>
                    ))}
                  </select>
                </label>
                <textarea
                  className="min-h-[90px] rounded-field border border-on-dark-faint/40 bg-transparent p-3 font-sans text-sm text-on-dark-heading outline-none placeholder:text-on-dark-faint"
                  onChange={(e) => setText(e.target.value)}
                  placeholder="How was your stay?"
                  value={text}
                />
                <button
                  className="inline-flex min-h-11 cursor-pointer items-center justify-center rounded-pill border-none bg-yellow px-5 font-sans text-[13px] font-bold text-deep transition-colors hover:bg-yellow-press disabled:pointer-events-none disabled:opacity-60"
                  disabled={busy}
                  onClick={() => submit(booking)}
                  type="button"
                >
                  {busy ? "Submitting…" : "Submit review"}
                </button>
              </div>
            )}
          </div>
        ))}
      </div>
      {closed.map(({ booking }, index) => (
        <div className="rounded-card border border-sand-border bg-cream p-[22px]" key={booking.id}>
          <div className="flex items-center gap-3 opacity-60">
            <img alt="" className="block size-16 shrink-0 rounded-[12px] object-cover grayscale-[0.6]" src={getStayImage(index + 2).src} />
            <div className="flex-1">
              <div className="font-display text-lg font-medium">{booking.propertyTitle ?? "Jamaican stay"}</div>
              <div className="text-xs text-gray-600">
                stayed {booking.checkIn} → {booking.checkOut}
              </div>
            </div>
            <span className="rounded-pill bg-shell px-3.5 py-1.5 text-[11px] font-bold tracking-[0.06em] text-sand-500">
              REVIEW WINDOW CLOSED
            </span>
          </div>
        </div>
      ))}
    </div>
  );
}
