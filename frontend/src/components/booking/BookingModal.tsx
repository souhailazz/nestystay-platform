import { useMemo, useState } from "react";
import { ArrowRight, CalendarCheck2, CreditCard, ShieldCheck } from "lucide-react";
import { api, formatMoney, type Booking, type BookingQuote, type PropertyListing } from "../../lib/api";
import type { AuthSession } from "../../lib/auth";
import { AppLink } from "../AppLink";
import { Badge } from "../ui/Badge";
import { Button } from "../ui/Button";
import { ErrorState } from "../ui/ErrorState";
import { Field, Input, Select } from "../ui/Input";
import { Modal } from "../ui/Modal";

function isoDate(offsetDays: number) {
  const date = new Date();
  date.setDate(date.getDate() + offsetDays);
  return date.toISOString().slice(0, 10);
}

export function BookingModal({
  open,
  property,
  session,
  onClose,
  onCreated,
}: {
  open: boolean;
  property: PropertyListing | null;
  session: AuthSession | null;
  onClose: () => void;
  onCreated?: (booking: Booking) => void;
}) {
  const [checkIn, setCheckIn] = useState(isoDate(7));
  const [checkOut, setCheckOut] = useState(isoDate(11));
  const [documentType, setDocumentType] = useState("01000000");
  const [quote, setQuote] = useState<BookingQuote | null>(null);
  const [booking, setBooking] = useState<Booking | null>(null);
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const title = property ? `Book ${property.title}` : "Book your stay";
  const canBook = Boolean(property && session && quote);

  const quoteLines = useMemo(() => quote?.priceBreakdown ?? [], [quote]);

  async function handleQuote() {
    if (!property) return;
    setIsBusy(true);
    setError(null);
    setBooking(null);
    try {
      setQuote(await api.quoteBooking({ propertyId: property.id, checkIn, checkOut }));
    } catch (caught) {
      setQuote(null);
      setError(caught instanceof Error ? caught.message : "Quote could not be created.");
    } finally {
      setIsBusy(false);
    }
  }

  async function handleCreateBooking() {
    if (!property || !session) return;
    setIsBusy(true);
    setError(null);
    try {
      const created = await api.createBooking({
        propertyId: property.id,
        guestUserId: session.userId,
        checkIn,
        checkOut,
        documentType,
        ekycMetaInfo: `Nesty Stay web booking for ${property.title}`,
      }, session.accessToken);
      setBooking(created);
      onCreated?.(created);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Booking could not be created.");
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <Modal open={open} title={title} onClose={onClose}>
      {!property && <ErrorState message="Choose a property before opening the booking flow." />}

      {property && (
        <div className="booking-flow">
          <div className="booking-flow__summary">
            <Badge tone={property.guestVerificationEnabled ? "sun" : "green"}>
              {property.guestVerificationEnabled ? "Guest verification" : "Instant approval"}
            </Badge>
            <h3>{property.location}</h3>
            <p>
              {formatMoney(property.nightlyRate, property.currency)} per night with{" "}
              {property.cancellationPolicy.toLowerCase()} cancellation.
            </p>
          </div>

          {!session && (
            <div className="booking-auth-callout">
              <ShieldCheck size={20} />
              <span>Sign in before creating a persisted booking.</span>
              <AppLink className="inline-action" href="/login">
                Login <ArrowRight size={14} />
              </AppLink>
            </div>
          )}

          <div className="form-grid form-grid--two">
            <Field label="Check-in">
              <Input type="date" value={checkIn} onChange={(event) => setCheckIn(event.target.value)} />
            </Field>
            <Field label="Check-out">
              <Input type="date" value={checkOut} onChange={(event) => setCheckOut(event.target.value)} />
            </Field>
            {property.guestVerificationEnabled && (
              <Field label="eKYC document" className="form-grid__full">
                <Select value={documentType} onChange={(event) => setDocumentType(event.target.value)}>
                  <option value="01000000">Passport</option>
                  <option value="02000000">National ID</option>
                  <option value="03000000">Driver license</option>
                </Select>
              </Field>
            )}
          </div>

          <div className="button-row">
            <Button disabled={isBusy} onClick={handleQuote} variant="outline">
              <CalendarCheck2 size={17} />
              {isBusy ? "Checking" : "Get quote"}
            </Button>
            <Button disabled={!canBook || isBusy} onClick={handleCreateBooking}>
              <CreditCard size={17} />
              Create booking
            </Button>
          </div>

          {error && <ErrorState message={error} />}

          {quote && (
            <div className="quote-panel">
              <div>
                <span>Total</span>
                <strong>{formatMoney(quote.totalAmount, quote.currency)}</strong>
              </div>
              <ul>
                {quoteLines.map((line) => (
                  <li key={line.code}>
                    <span>{line.description}</span>
                    <strong>{formatMoney(line.amount, line.currency)}</strong>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {booking && (
            <div className="booking-result">
              <Badge tone={booking.status === "APPROVED" ? "green" : "sun"}>{booking.status}</Badge>
              <h3>Booking saved to PostgreSQL through the API.</h3>
              <p>
                Verification is {booking.verificationStatus.toLowerCase()} and payment is{" "}
                {booking.paymentStatus.toLowerCase()}.
              </p>
              {booking.requiresGuestVerification && booking.verificationStatus !== "PASSED" && (
                <div className="verification-progress-panel">
                  <strong>Nuh Fret</strong>
                  <span>Do not worry - your identity is being verified.</span>
                  <div className="progress-bar"><span /></div>
                  <small>
                    Date hold visible until{" "}
                    {booking.holdExpiresAt ? new Date(booking.holdExpiresAt).toLocaleString() : "host approval"}
                  </small>
                </div>
              )}
              {booking.ekycTransactionUrl && (
                <a href={booking.ekycTransactionUrl} rel="noreferrer" target="_blank">
                  Open eKYC transaction <ArrowRight size={14} />
                </a>
              )}
            </div>
          )}
        </div>
      )}
    </Modal>
  );
}
