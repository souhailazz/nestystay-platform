import type { CSSProperties } from "react";
import { AppLink } from "../AppLink";
import { cx } from "../../lib/ui";
import { LEGAL_DETAILS, openCookieSettings } from "../../lib/legal";

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
        decoding="async"
        sizes="128px"
        src="/assets/optimized/nestystay-emblem-128.webp"
        srcSet="/assets/optimized/nestystay-emblem-64.webp 64w, /assets/optimized/nestystay-emblem-96.webp 96w, /assets/optimized/nestystay-emblem-128.webp 128w"
      />
    </span>
  );
}

/**
 * DS v2 host-tier pill (photo overlays, listing headers):
 * TRUSTED = Deep bg + Yellow text · VERIFIED = Deep Hover bg + white ·
 * WELLNESS = mint tint · FREE = cream/gray outline.
 */
export function TierBadge({ level, className }: { level?: string | null; className?: string }) {
  const tier = (level ?? "").toLowerCase();
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
        <div className="flex flex-wrap items-center justify-end gap-x-4 gap-y-2 font-sans text-[13px] text-on-dark-muted">
          <span>nestystay.net · <a className="text-on-dark-muted transition-colors hover:text-on-dark-body" href={LEGAL_DETAILS.supportTel}>{LEGAL_DETAILS.supportPhone}</a></span>
          <nav aria-label="Explore NestyStay" className="flex flex-wrap gap-x-3 gap-y-1">
            <AppLink className="underline-offset-2 hover:text-on-dark-body hover:underline" href="/explore">Explore stays</AppLink>
            <AppLink className="underline-offset-2 hover:text-on-dark-body hover:underline" href="/experiences">Experiences</AppLink>
            <AppLink className="underline-offset-2 hover:text-on-dark-body hover:underline" href="/journal">Jamaica Journal</AppLink>
            <AppLink className="underline-offset-2 hover:text-on-dark-body hover:underline" href="/trust">Trust &amp; safety</AppLink>
            <AppLink className="underline-offset-2 hover:text-on-dark-body hover:underline" href="/help">Help</AppLink>
            <AppLink className="underline-offset-2 hover:text-on-dark-body hover:underline" href="/contact">Contact</AppLink>
          </nav>
          <nav aria-label="Legal and support" className="flex flex-wrap gap-x-3 gap-y-1">
            <AppLink className="underline-offset-2 hover:text-on-dark-body hover:underline" href="/privacy">Privacy</AppLink>
            <AppLink className="underline-offset-2 hover:text-on-dark-body hover:underline" href="/terms">Terms</AppLink>
            <AppLink className="underline-offset-2 hover:text-on-dark-body hover:underline" href="/cookies">Cookies</AppLink>
            <AppLink className="underline-offset-2 hover:text-on-dark-body hover:underline" href="/refund-policy">Refunds</AppLink>
            <button className="underline-offset-2 hover:text-on-dark-body hover:underline" onClick={openCookieSettings} type="button">Cookie settings</button>
          </nav>
        </div>
      </div>
    </footer>
  );
}
