import { useState, useEffect } from "react";
import type { AuthController } from "../../hooks/useAuth";
import { BookingReviewPage } from "./BookingReviewPage";
import { BookingIdentityPage } from "./BookingIdentityPage";
import { BookingCheckoutPage } from "./BookingCheckoutPage";
import { BookingSuccessPage } from "./BookingSuccessPage";
import { BookingFailurePage } from "./BookingFailurePage";
import { BookingRejectedPage } from "./BookingRejectedPage";
import { BookingPendingPage } from "./BookingPendingPage";
import { BookingCancelledPage } from "./BookingCancelledPage";
import { BookingInvoicePage } from "./BookingInvoicePage";
import { BookingReceiptPage } from "./BookingReceiptPage";
import type { BookingDetails, BookingQuote } from "./types";
import { api } from "../../lib/api";

interface BookingStateContainerProps {
  state: string;
  bookingId?: string;
  auth: AuthController;
}

export function BookingStateContainer({ state, bookingId, auth }: BookingStateContainerProps) {
  const [currentBookingId, setCurrentBookingId] = useState<string | undefined>(bookingId);
  const [dummyQuote, setDummyQuote] = useState<BookingQuote | null>(null);

  useEffect(() => {
    if (bookingId) {
      setCurrentBookingId(bookingId);
    }
  }, [bookingId]);

  useEffect(() => {
    if (state === "review" && !dummyQuote) {
      let active = true;
      async function loadReviewQuote() {
        if (bookingId && auth.session) {
          const booking = await api.getBooking(bookingId, auth.session.accessToken) as unknown as BookingDetails;
          if (active) setDummyQuote(toQuote(booking));
          return;
        }

        const props = await api.getProperties();
        if (props.length > 0) {
          const q = await api.getBookingQuote({
            propertyId: props[0].id,
            checkIn: new Date(Date.now() + 7 * 86400000).toISOString().split("T")[0],
            checkOut: new Date(Date.now() + 10 * 86400000).toISOString().split("T")[0],
            adults: 2,
            children: 0
          });
          if (active) setDummyQuote(q as unknown as BookingQuote);
        }
      }

      loadReviewQuote();
      return () => { active = false; };
    }
  }, [state, dummyQuote, bookingId, auth.session?.accessToken]);

  if (state !== "review" && !auth.session) {
    return (
      <div className="mx-auto max-w-[1160px] px-6 py-9 font-sans text-ink" data-testid="booking-auth-required">
        <div className="rounded-field bg-coral-tint px-4 py-3 text-[13px] text-coral-text" role="alert">
          Sign in to continue this protected booking flow.
        </div>
        <div className="mt-4 flex flex-wrap gap-2.5">
          <a className="inline-flex min-h-12 items-center rounded-pill bg-deep px-6 font-sans text-sm font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover" href="/login">
            Log in
          </a>
          <a className="inline-flex min-h-12 items-center rounded-pill border-[1.5px] border-sand-input px-6 font-sans text-sm font-semibold text-ink transition-colors hover:border-deep" href="/register">
            Create account
          </a>
        </div>
      </div>
    );
  }

  const activeId = currentBookingId || "11111111-1111-4111-8111-111111111111";

  switch (state) {
    case "review":
      return dummyQuote ? (
        <BookingReviewPage
          quote={dummyQuote}
          details={{ adults: 2, children: 0, accessibility: "", protection: "insuraguest" }}
          auth={auth}
          onBackToModal={() => { window.location.href = "/explore"; }}
          onProceedToCheckout={(id, status) => {
            setCurrentBookingId(id);
            if (status === "PendingVerification") {
              // eKYC required: document choice (BOOK-03) before the pending/hold screen.
              window.history.pushState({}, "", `/booking/${id}/identity`);
            } else {
              window.history.pushState({}, "", `/booking/${id}/checkout`);
            }
            window.dispatchEvent(new PopStateEvent("popstate"));
          }}
        />
      ) : (
        <div className="mx-auto max-w-[1160px] px-6 py-9 text-[13.5px] text-gray-600">Loading booking review…</div>
      );

    case "identity":
      return <BookingIdentityPage bookingId={activeId} auth={auth} />;

    case "checkout":
      return (
        <BookingCheckoutPage
          bookingId={activeId}
          auth={auth}
          onSuccess={(id) => {
            window.history.pushState({}, "", `/booking/${id}/success`);
            window.dispatchEvent(new PopStateEvent("popstate"));
          }}
          onFailure={(id, reason) => {
            window.history.pushState({}, "", `/booking/${id}/failure?reason=${encodeURIComponent(reason)}`);
            window.dispatchEvent(new PopStateEvent("popstate"));
          }}
        />
      );

    case "success":
      return <BookingSuccessPage bookingId={activeId} auth={auth} />;

    case "failure":
      return <BookingFailurePage bookingId={activeId} auth={auth} />;

    case "rejected":
      return <BookingRejectedPage bookingId={activeId} auth={auth} />;

    case "pending":
      return <BookingPendingPage bookingId={activeId} auth={auth} />;

    case "cancelled":
      return <BookingCancelledPage bookingId={activeId} auth={auth} />;

    case "invoice":
      return <BookingInvoicePage bookingId={activeId} auth={auth} />;

    case "receipt":
      return <BookingReceiptPage bookingId={activeId} auth={auth} />;

    default:
      return <BookingSuccessPage bookingId={activeId} auth={auth} />;
  }
}

function toQuote(booking: BookingDetails): BookingQuote {
  return {
    property: {
      id: booking.propertyId,
      title: booking.propertyTitle ?? "Booked stay",
      location: "Jamaica",
      country: "Jamaica",
      hostName: booking.hostName ?? "NestyStay host",
      badgeLevel: "Verified",
      guestVerificationEnabled: booking.requiresGuestVerification,
      insuraGuestEnabled: false,
      cancellationPolicy: "Moderate",
    },
    checkIn: booking.checkIn,
    checkOut: booking.checkOut,
    nights: booking.nights,
    nightlyRate: booking.nightlyRate,
    staySubtotal: booking.staySubtotal,
    guestPlatformFee: booking.guestPlatformFee,
    totalAmount: booking.totalAmount,
    currency: booking.currency,
    requiresGuestVerification: booking.requiresGuestVerification,
    datesAvailable: true,
    holdExpiresAt: booking.holdExpiresAt,
    priceBreakdown: booking.priceBreakdown,
  };
}
