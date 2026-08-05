import { AlertTriangle } from "lucide-react";
import { Button } from "./Button";

/**
 * DS v2 error state — shows the backend message VERBATIM, "↻ Try again" pill.
 * Pass `isServerError` on 5xx to add the WhatsApp support link (754-248-2435).
 */
export function ErrorState({
  message,
  onRetry,
  isServerError = false,
}: {
  message: string;
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
      <h3 className="m-0 font-display text-xl font-medium leading-snug text-ink">Something went wrong</h3>
      <p className="m-0 max-w-sm font-sans text-[13px] leading-relaxed text-gray-600">{message}</p>
      {onRetry && (
        <Button onClick={onRetry} variant="outline">
          ↻ Try again
        </Button>
      )}
      {isServerError && (
        <a
          className="font-sans text-[13px] font-semibold text-deep-hover underline underline-offset-4"
          href="https://wa.me/17542482435"
          rel="noreferrer"
          target="_blank"
        >
          WhatsApp support · 754-248-2435
        </a>
      )}
    </div>
  );
}
