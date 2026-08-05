import { cx } from "../../lib/ui";

/* Contractual StatusBadge semantics (DS v2):
   green = approved/verified/captured · coral = rejected/failed/cancelled ·
   amber ("yellow" status, rendered Amber on light) = pending/authorized ·
   blue = scheduled/assigned · mint = wellness. Values render VERBATIM. */

export type StatusTone = "green" | "coral" | "amber" | "blue" | "mint" | "slate";

export function statusToneOf(value: string): StatusTone {
  const v = value.toLowerCase();
  if (/wellness/.test(v)) return "mint";
  if (/reject|fail|declin|cancel|refund|expired|revoked/.test(v)) return "coral";
  if (/pending|authoriz|progress|await|hold|submitted|queued|requested/.test(v)) return "amber";
  if (/schedul|assign/.test(v)) return "blue";
  if (/approve|confirm|captur|pass|verif|paid|active|complete|success|locked/.test(v)) return "green";
  return "slate";
}

const toneClasses: Record<StatusTone, string> = {
  green: "bg-success-tint text-success-text",
  coral: "bg-coral-tint text-coral-text",
  amber: "bg-amber-tint text-amber-text",
  blue: "bg-info-tint text-info-text",
  mint: "bg-mint-tint text-mint-text",
  slate: "bg-shell text-sand-500",
};

export function StatusChip({
  value,
  label,
  className,
}: {
  value: string;
  label?: string;
  className?: string;
}) {
  return (
    <span
      className={cx(
        "inline-flex items-center rounded-pill px-2.5 py-1 font-sans text-[10.5px] font-bold uppercase tracking-[0.06em]",
        toneClasses[statusToneOf(value)],
        className,
      )}
    >
      {label ? `${label}: ${value}` : value}
    </span>
  );
}
