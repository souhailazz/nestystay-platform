import type { CSSProperties } from "react";
import { AppLink } from "../AppLink";
import { cx } from "../../lib/ui";

/** Deep brand panel background — faint geometric line pattern fading into Deep. */
export const deepPatternBackground: CSSProperties = {
  backgroundColor: "#062B2B",
  backgroundImage:
    `linear-gradient(rgba(6,43,43,0) 0%, #062B2B 92%), url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='72' height='72'><g fill='none' stroke='white' stroke-opacity='0.05' stroke-width='1.5'><rect x='3' y='3' width='66' height='66'/><path d='M36 3 L69 36 L36 69 L3 36 Z'/><path d='M3 3 A33 33 0 0 1 36 36 A33 33 0 0 1 69 3'/><path d='M3 69 A33 33 0 0 0 36 36 A33 33 0 0 0 69 69'/></g></svg>")`,
  backgroundSize: "100% 100%, 72px 72px",
};

/** Cream/sand roundel around the official emblem (brand rule: always in a roundel). */
export function EmblemRoundel({
  size = 44,
  className,
}: {
  size?: number;
  className?: string;
}) {
  return (
    <span
      className={cx("grid shrink-0 place-items-center overflow-hidden rounded-full bg-sand", className)}
      style={{ width: size, height: size }}
    >
      <img
        alt=""
        aria-hidden="true"
        className="block h-[86%] w-[86%] rounded-full object-contain"
        src="/assets/nestystay-emblem.png"
      />
    </span>
  );
}

/**
 * DS v2 host-tier pill (photo overlays, listing headers):
 * TRUSTED = Deep bg + Yellow text · VERIFIED = Deep Hover bg + white ·
 * WELLNESS = mint tint · FREE = cream/gray outline.
 */
export function TierBadge({ level, className }: { level: string; className?: string }) {
  const tier = level.toLowerCase();
  const base =
    "inline-flex items-center rounded-pill px-3 py-[5px] font-sans text-[11px] font-bold uppercase tracking-[0.06em]";
  if (tier.includes("trust")) return <span className={cx(base, "bg-deep text-yellow", className)}>★ Trusted</span>;
  if (tier.includes("verif")) return <span className={cx(base, "bg-deep-hover text-white", className)}>✓ Verified</span>;
  if (tier.includes("well"))
    return <span className={cx(base, "border border-mint bg-mint-tint text-mint-text", className)}>✦ Wellness</span>;
  return <span className={cx(base, "border-[1.5px] border-sand-input bg-cream text-gray-600", className)}>Free</span>;
}

/** Deep footer required on every public page: nestystay.net · 754-248-2435. */
export function PublicFooter({ variant = "deep" }: { variant?: "deep" | "night" }) {
  return (
    <footer className={variant === "night" ? "border-t border-on-dark-faint/30 bg-footer" : "bg-deep"}>
      <div className="mx-auto flex max-w-[1200px] flex-wrap items-center justify-between gap-4 px-6 py-9">
        <AppLink className="flex items-center gap-3" href="/">
          <EmblemRoundel size={variant === "night" ? 56 : 44} className="bg-shell" />
          <span className="font-sans text-sm font-bold tracking-[0.22em] text-shell">NESTY STAY</span>
        </AppLink>
        <div className="font-sans text-[13px] text-on-dark-muted">
          nestystay.net ·{" "}
          <a className="text-on-dark-muted transition-colors hover:text-on-dark-body" href="https://wa.me/17542482435">
            754-248-2435
          </a>
        </div>
      </div>
    </footer>
  );
}
