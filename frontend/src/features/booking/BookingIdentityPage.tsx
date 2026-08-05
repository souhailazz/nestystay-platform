import { useEffect, useState } from "react";
import { Lock } from "lucide-react";
import { navigate } from "../../components/AppLink";
import { LoadingState } from "../../components/ui/LoadingState";
import { api } from "../../lib/api";
import { cx } from "../../lib/ui";
import type { AuthController } from "../../hooks/useAuth";
import type { BookingDetails } from "./types";
import {
  BookingHeading,
  BookingScaffold,
  BookingStepper,
  DEGRESSIVE_NOTE,
  PriceSummary,
  PropertyMiniHeader,
  bookingCard,
  bookingDeepCta,
} from "./BookingShell";

interface BookingIdentityPageProps {
  bookingId: string;
  auth: AuthController;
}

const DOCUMENTS = [
  ["passport", "Passport", "Fastest — photo page only", true],
  ["national_id", "National ID", "Front and back required", false],
  ["driver_license", "Driver license", "Front and back required", false],
] as const;

/* BOOK-03 (DS v2) — eKYC document choice. The verification session itself is
   created by the backend on booking creation; the external transaction link
   (booking.ekycTransactionUrl) opens from here or from BOOK-07. */
export function BookingIdentityPage({ bookingId, auth }: BookingIdentityPageProps) {
  const [booking, setBooking] = useState<BookingDetails | null>(null);
  const [documentType, setDocumentType] = useState<string>("passport");

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

  function handleContinue() {
    if (booking?.ekycTransactionUrl) {
      window.open(booking.ekycTransactionUrl, "_blank", "noopener");
    }
    navigate(`/booking/${bookingId}/pending`);
  }

  return (
    <BookingScaffold
      aside={
        booking ? (
          <>
            <PropertyMiniHeader
              ekycRequired={booking.requiresGuestVerification}
              subtitle={`Booking ${booking.id.slice(0, 8).toUpperCase()}`}
              title={booking.propertyTitle ?? "Your stay"}
            />
            <PriceSummary currency={booking.currency} lines={booking.priceBreakdown ?? []} total={booking.totalAmount} />
            <div className="text-[11.5px] text-sand-500">{DEGRESSIVE_NOTE}</div>
            <button className={bookingDeepCta} onClick={handleContinue} type="button">
              Hold dates &amp; verify →
            </button>
          </>
        ) : (
          <LoadingState label="Loading booking" />
        )
      }
      stepper={<BookingStepper current={3} />}
    >
      <BookingHeading accent="identity" pre="Verify your" />
      <div className="max-w-[520px] text-sm text-gray-600">
        This host requires traveler verification for every booking. Pick the document you&apos;ll present — you&apos;ll
        be redirected to our secure verification partner.
      </div>

      <div className="flex flex-col gap-3">
        {DOCUMENTS.map(([value, title, copy, recommended]) => (
          <label
            className={cx(
              "flex cursor-pointer items-center gap-4 rounded-[18px] bg-cream px-5 py-[18px] transition-shadow",
              documentType === value
                ? "border-2 border-deep-hover shadow-[0_0_0_3px_rgba(14,74,69,0.1)]"
                : "border-[1.5px] border-sand-border hover:border-sand-input",
            )}
            key={value}
          >
            <input
              checked={documentType === value}
              className="size-[22px] accent-deep-hover"
              name="doc"
              onChange={() => setDocumentType(value)}
              type="radio"
              value={value}
            />
            <div className="flex-1">
              <div className="text-[15px] font-semibold">{title}</div>
              <div className="text-[12.5px] text-gray-600">{copy}</div>
            </div>
            {recommended && (
              <span className="rounded-pill bg-success-tint px-2.5 py-1 text-[10px] font-bold text-success-text">
                RECOMMENDED
              </span>
            )}
          </label>
        ))}
      </div>

      <div className={bookingCard}>
        <div className="flex items-start gap-3">
          <Lock className="mt-0.5 shrink-0 text-deep-hover" size={18} />
          <div className="text-[12.5px] text-gray-600">
            Verification happens on our partner&apos;s secure page (external redirect). NestyStay never stores your
            document images. Your dates are <strong className="text-ink">held for 60 minutes</strong> while you verify.
          </div>
        </div>
      </div>
    </BookingScaffold>
  );
}
