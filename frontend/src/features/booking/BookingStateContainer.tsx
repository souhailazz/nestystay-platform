import { useState, useEffect } from "react";
import type { AuthController } from "../../hooks/useAuth";
import { BookingReviewPage } from "./BookingReviewPage";
import { BookingCheckoutPage } from "./BookingCheckoutPage";
import { BookingSuccessPage } from "./BookingSuccessPage";
import { BookingFailurePage } from "./BookingFailurePage";
import { BookingRejectedPage } from "./BookingRejectedPage";
import { BookingPendingPage } from "./BookingPendingPage";
import { BookingCancelledPage } from "./BookingCancelledPage";
import { BookingInvoicePage } from "./BookingInvoicePage";
import { BookingReceiptPage } from "./BookingReceiptPage";
import type { BookingQuote } from "./types";
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
    // If on review page without an explicit bookingId, load default property quote
    if (state === "review" && !dummyQuote) {
      api.getProperties().then((props) => {
        if (props.length > 0) {
          api.getBookingQuote({
            propertyId: props[0].id,
            checkIn: new Date(Date.now() + 7 * 86400000).toISOString().split("T")[0],
            checkOut: new Date(Date.now() + 10 * 86400000).toISOString().split("T")[0],
            adults: 2,
            children: 0
          }).then((q: unknown) => setDummyQuote(q as BookingQuote));
        }
      });
    }
  }, [state, dummyQuote]);

  const activeId = currentBookingId || "11111111-1111-4111-8111-111111111111";

  switch (state) {
    case "review":
      return dummyQuote ? (
        <BookingReviewPage
          quote={dummyQuote}
          details={{ adults: 2, children: 0, accessibility: "", protection: "insuraguest" }}
          auth={auth}
          onBackToModal={() => { window.location.href = "/explore"; }}
          onProceedToCheckout={(id) => {
            setCurrentBookingId(id);
            window.history.pushState({}, "", `/booking/${id}/checkout`);
            window.dispatchEvent(new PopStateEvent("popstate"));
          }}
        />
      ) : (
        <div className="container py-6">Loading booking review...</div>
      );

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
