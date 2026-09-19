import { AlertTriangle } from "lucide-react";
import { Button } from "./Button";
import { userSafeErrorMessage } from "../../lib/errorMessages";
import { LEGAL_DETAILS } from "../../lib/legal";

/**
 * DS v2 error state — translates transport/backend failures into user-safe copy.
 * Pass `isServerError` on 5xx to add safe WhatsApp and phone support links.
 */
export function ErrorState({
  message,
  onRetry,
  isServerError = false,
}: {
  message: unknown;
  onRetry?: () => void;
  isServerError?: boolean;
}) {
  return (
    <div
      className="grid place-items-center gap-3 rounded-card border border-coral/30 bg-cream px-8 py-12 text-center"
      role="alert"
    >
      <span className="grid size-14 place-items-center rounded-full bg-coral-tint text-coral-text">
        <AlertTriangle aria-hidden="true" size={26} strokeWidth={1.5} />
      </span>
      <h2 className="m-0 font-display text-xl font-medium leading-snug text-ink">Something went wrong</h2>
      <p className="m-0 max-w-sm font-sans text-[13px] leading-relaxed text-gray-600">{userSafeErrorMessage(message)}</p>
      {onRetry && (
        <Button onClick={onRetry} variant="outline">
          ↻ Try again
        </Button>
      )}
      {isServerError && (
        <div className="flex flex-wrap justify-center gap-3 text-[13px] font-semibold">
          <a className="text-deep-hover underline underline-offset-4" href={LEGAL_DETAILS.whatsappUrl} rel="noreferrer" target="_blank">WhatsApp support</a>
          <a className="text-deep-hover underline underline-offset-4" href={LEGAL_DETAILS.supportTel}>Call {LEGAL_DETAILS.supportPhone}</a>
        </div>
      )}
    </div>
  );
}
