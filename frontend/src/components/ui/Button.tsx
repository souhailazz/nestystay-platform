import type { AnchorHTMLAttributes, ButtonHTMLAttributes, ReactNode } from "react";
import { cx } from "../../lib/ui";

/**
 * DS v2 button recipes — pill, min-height 48px, Sora 600.
 * - "dark"        → Primary on light: Deep bg / cream text, hover Deep Hover #0E4A45.
 * - "sun"         → Accent on DARK grounds only: Yellow bg / Deep text, hover #F5C400.
 * - "outline"     → Secondary: 1.5px #C7BC9C outline, hover border Deep.
 * - "ghost"       → Quiet: transparent, ink text, hover shell fill.
 * - "glass"       → On-dark subtle: translucent cream w/ border.
 * - "destructive" → Coral tint bg + coral text.
 * A child element with class "ns-arrow" (e.g. <span className="ns-arrow">→</span>)
 * slides +4px on hover.
 */
type Variant = "sun" | "glass" | "dark" | "outline" | "ghost" | "destructive";

const base =
  "group inline-flex min-h-12 cursor-pointer select-none items-center justify-center gap-2 rounded-pill border border-transparent px-6 font-sans text-[15px] font-semibold leading-none transition-all duration-200 " +
  "focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-deep-hover/25 " +
  "disabled:pointer-events-none disabled:cursor-not-allowed disabled:border-transparent disabled:bg-shell disabled:text-sand-500 disabled:shadow-none " +
  "[&_.ns-arrow]:transition-transform [&_.ns-arrow]:duration-200 hover:[&_.ns-arrow]:translate-x-1";

const variants: Record<Variant, string> = {
  dark: "bg-deep text-cream hover:bg-deep-hover",
  sun: "bg-yellow text-deep hover:bg-yellow-press",
  outline: "border-[1.5px] border-sand-input bg-transparent text-ink hover:border-deep",
  ghost: "bg-transparent text-ink hover:bg-shell",
  glass: "border-cream/25 bg-cream/10 text-on-dark-heading hover:bg-cream/20",
  destructive: "bg-coral-tint text-coral-text hover:bg-[#F6D6D3]",
};

export function buttonClassName(variant: Variant = "sun", className?: string) {
  return cx(base, variants[variant], className);
}

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: Variant;
};

export function Button({ className, variant = "sun", type = "button", ...props }: ButtonProps) {
  return <button className={buttonClassName(variant, className)} type={type} {...props} />;
}

type ButtonLinkProps = AnchorHTMLAttributes<HTMLAnchorElement> & {
  children: ReactNode;
  variant?: Variant;
};

export function ButtonLink({ className, variant = "sun", ...props }: ButtonLinkProps) {
  return <a className={buttonClassName(variant, className)} {...props} />;
}
