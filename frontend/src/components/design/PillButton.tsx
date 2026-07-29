import type { AnchorHTMLAttributes, ButtonHTMLAttributes, ReactNode } from "react";
import { AppLink } from "../AppLink";

export type PillButtonVariant = "sun" | "outline" | "outline-light" | "primary-light" | "ghost";

export function pillButtonClassName(variant: PillButtonVariant, extra = "") {
  return `ns-btn ns-btn--${variant} ${extra}`.trim();
}

type CommonProps = {
  variant?: PillButtonVariant;
  /** Renders the sliding "→" arrow after the label (inside a deep disc for ghost). */
  arrow?: boolean;
  children: ReactNode;
};

export function PillLink({
  variant = "sun",
  arrow = false,
  children,
  className = "",
  ...props
}: CommonProps & AnchorHTMLAttributes<HTMLAnchorElement>) {
  return (
    <AppLink className={pillButtonClassName(variant, className)} {...props}>
      {children}
      {arrow &&
        (variant === "ghost" ? (
          <span aria-hidden="true" className="ns-btn__disc">
            <span className="ns-arrow">→</span>
          </span>
        ) : (
          <span aria-hidden="true" className="ns-arrow">
            →
          </span>
        ))}
    </AppLink>
  );
}

export function PillButton({
  variant = "sun",
  arrow = false,
  children,
  className = "",
  type = "button",
  ...props
}: CommonProps & ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button type={type} className={pillButtonClassName(variant, className)} {...props}>
      {children}
      {arrow && (
        <span aria-hidden="true" className="ns-arrow">
          →
        </span>
      )}
    </button>
  );
}
