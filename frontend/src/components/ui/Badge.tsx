import type { HTMLAttributes } from "react";
import { cx } from "../../lib/ui";

/**
 * DS v2 StatusBadge — pill, uppercase Sora 700 12px, tint bg + dark text of the
 * same hue. Yellow statuses render Amber on light grounds (yellow never on light).
 * On dark grounds pass `onDark` for the outlined variant (1px hue border + light
 * hue text). Tone map:
 * - green → approved / captured / verified   - coral → rejected / failed / cancelled
 * - sun   → pending / authorized (amber)     - blue  → scheduled / assigned
 * - mint  → wellness                          - ink   → TRUSTED (Deep bg + Yellow text)
 * - cream / slate → neutral (FREE tier, meta)
 */
type Tone = "green" | "sun" | "cream" | "coral" | "ink" | "blue" | "slate" | "mint";

const base =
  "inline-flex items-center justify-center gap-1.5 rounded-pill px-3 py-1 font-sans text-xs font-bold uppercase leading-[1.4] tracking-[0.08em]";

const solid: Record<Tone, string> = {
  green: "bg-success-tint text-success-text",
  mint: "bg-mint-tint text-mint-text",
  coral: "bg-coral-tint text-coral-text",
  sun: "bg-amber-tint text-amber-text",
  blue: "bg-info-tint text-info-text",
  cream: "bg-shell text-sand-500",
  slate: "bg-shell text-gray-600",
  ink: "bg-deep text-yellow",
};

const outlined: Record<Tone, string> = {
  green: "border border-success bg-transparent text-success-tint",
  mint: "border border-mint bg-transparent text-mint-tint",
  coral: "border border-coral bg-transparent text-coral-tint",
  sun: "border border-amber bg-transparent text-amber-tint",
  blue: "border border-info bg-transparent text-info-tint",
  cream: "border border-on-dark-faint bg-transparent text-on-dark-warm",
  slate: "border border-on-dark-faint bg-transparent text-on-dark-muted",
  ink: "border border-yellow bg-transparent text-yellow",
};

export function Badge({
  className,
  ...props
}: HTMLAttributes<HTMLSpanElement> & { tone?: Tone; onDark?: boolean }) {
  const { tone = "green", onDark = false, ...rest } = props;
  return <span className={cx(base, (onDark ? outlined : solid)[tone], className)} {...rest} />;
}
