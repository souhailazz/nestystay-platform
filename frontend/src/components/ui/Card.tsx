import type { HTMLAttributes } from "react";
import { cx } from "../../lib/ui";

/**
 * DS v2 cards.
 * - Default: cream bg, 1px sand border, radius 20px, warm card shadow.
 * - `photo`: radius 26px + photo shadow, for image cards. Pair with <CardScrim />
 *   (deep-green gradient), a badge overlay top-left and a 44px heart top-right.
 */
export function Card({
  className,
  children,
  photo = false,
  ...props
}: HTMLAttributes<HTMLElement> & { photo?: boolean }) {
  return (
    <article
      className={cx(
        "relative overflow-hidden",
        photo
          ? "rounded-photo shadow-photo"
          : "rounded-card border border-sand-border bg-cream shadow-card",
        className,
      )}
      {...props}
    >
      {children}
    </article>
  );
}

/** Deep-green gradient scrim for photo cards (place over the image, content above it). */
export function CardScrim({ className }: { className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={cx(
        "pointer-events-none absolute inset-0 bg-[linear-gradient(185deg,rgba(6,43,43,0)_40%,rgba(6,43,43,0.82)_100%)]",
        className,
      )}
    />
  );
}
