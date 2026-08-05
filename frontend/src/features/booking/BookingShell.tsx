import type { ReactNode } from "react";
import { formatMoney } from "../../lib/api";
import { getStayImage } from "../../lib/stayImages";
import { cx } from "../../lib/ui";
import type { BookingPriceLine } from "./types";

/* Shared DS v2 chrome for the booking flow (BOOK-01 → BOOK-CONF):
   stepper, two-column scaffold, sticky price sidebar, deep footer.
   Price rows only ever render backend quote/booking line items. */

export const bookingDeepCta =
  "inline-flex min-h-[50px] cursor-pointer items-center justify-center gap-2.5 rounded-pill border-none bg-deep px-[26px] font-sans text-[15px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover disabled:pointer-events-none disabled:bg-shell disabled:text-sand-500";

export const bookingCard =
  "flex flex-col gap-3.5 rounded-card border border-sand-border bg-cream p-6";

const STEP_LABELS = ["Dates", "Quote", "Identity", "Payment", "Done"] as const;

export function BookingStepper({ current }: { current: number }) {
  return (
    <div className="flex flex-wrap items-center gap-1.5">
      {STEP_LABELS.map((label, i) => {
        const step = i + 1;
        if (step < current) {
          return (
            <span
              className="inline-flex min-h-11 items-center gap-2 rounded-pill border border-sand-input bg-cream px-4 text-[13px] font-semibold text-success-text"
              key={label}
            >
              ✓ {label}
            </span>
          );
        }
        if (step === current) {
          return (
            <span
              className="inline-flex min-h-11 items-center gap-2 rounded-pill bg-deep px-[18px] text-[13px] font-bold text-on-dark-heading"
              key={label}
            >
              <span className="inline-flex size-5 items-center justify-center rounded-full bg-yellow text-[11px] font-bold text-deep">
                {step}
              </span>
              {label}
            </span>
          );
        }
        return (
          <span
            className="inline-flex min-h-11 items-center gap-2 rounded-pill border border-dashed border-sand-input px-4 text-[13px] font-semibold text-sand-500"
            key={label}
          >
            {step} · {label}
          </span>
        );
      })}
    </div>
  );
}

export function BookingHeading({ pre, accent }: { pre: string; accent: string }) {
  return (
    <h1 className="m-0 font-display text-[clamp(30px,3.6vw,42px)] font-normal tracking-[-0.01em]">
      {pre} <em className="italic text-deep-hover">{accent}</em>
    </h1>
  );
}

export function LineChip({ tone, children }: { tone: "coral" | "green" | "blue" | "amber"; children: ReactNode }) {
  const tones = {
    coral: "bg-coral-tint text-coral-text",
    green: "bg-success-tint text-success-text",
    blue: "bg-info-tint text-info-text",
    amber: "bg-amber-tint text-amber-text",
  } as const;
  return (
    <span className={cx("rounded-pill px-2 py-0.5 text-[10px] font-bold uppercase tracking-[0.02em]", tones[tone])}>
      {children}
    </span>
  );
}

export function isVerificationLine(line: BookingPriceLine) {
  return /kyc|verif/i.test(`${line.code} ${line.description}`);
}

/** Sidebar price rows — backend line items only, never locally computed fees. */
export function PriceSummary({
  lines,
  total,
  currency,
  totalLabel = "Total",
}: {
  lines: BookingPriceLine[];
  total: number;
  currency: string;
  totalLabel?: string;
}) {
  return (
    <div className="flex flex-col text-[13.5px]">
      {lines.map((line, idx) => (
        <div className="flex items-center justify-between gap-2 border-b border-shell py-[7px]" key={`${line.code}-${idx}`}>
          <span className="flex flex-wrap items-center gap-1.5">
            {line.description}
            {!line.isRefundable && line.amount > 0 && <LineChip tone="coral">Non-refundable</LineChip>}
          </span>
          <strong>{formatMoney(line.amount, line.currency)}</strong>
        </div>
      ))}
      <div className="flex items-center justify-between py-[9px]">
        <strong>
          {totalLabel} ({currency.toUpperCase()})
        </strong>
        <strong className="font-display text-xl">{formatMoney(total, currency)}</strong>
      </div>
    </div>
  );
}

export function PropertyMiniHeader({
  title,
  subtitle,
  wellnessHost,
  ekycRequired,
}: {
  title: string;
  subtitle: string;
  wellnessHost?: boolean;
  ekycRequired?: boolean;
}) {
  return (
    <div className="flex items-center gap-3 border-b border-shell pb-3.5">
      <img
        alt=""
        className="block size-[72px] shrink-0 rounded-field object-cover"
        src={getStayImage(title.length).src}
      />
      <div>
        <div className="font-display text-[17px] font-medium">{title}</div>
        <div className="text-[12.5px] text-gray-600">{subtitle}</div>
        {(wellnessHost || ekycRequired) && (
          <div className="mt-1 flex flex-wrap gap-1.5">
            {wellnessHost && (
              <span className="rounded-pill bg-mint-tint px-2 py-[3px] text-[10px] font-bold text-mint-text">◆ WELLNESS HOST</span>
            )}
            {ekycRequired && (
              <span className="rounded-pill bg-info-tint px-2 py-[3px] text-[10px] font-bold text-info-text">eKYC REQUIRED</span>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export const DEGRESSIVE_NOTE =
  "Degressive service fee: 12% under $1,000 · 10% to $5,000 · 8% above. Refundable vs non-refundable split shown on every quote.";

export function BookingScaffold({
  stepper,
  children,
  aside,
}: {
  stepper: ReactNode;
  children: ReactNode;
  aside: ReactNode;
}) {
  return (
    <div className="flex min-h-screen flex-col font-sans text-[15px] leading-[1.55] text-ink">
      <main className="mx-auto flex w-full max-w-[1160px] flex-1 flex-col gap-[22px] px-[clamp(16px,3vw,28px)] pb-16 pt-9">
        {stepper}
        <div className="flex flex-wrap items-start gap-[22px]">
          <section className="flex min-w-0 flex-[1.6_1_420px] flex-col gap-[18px]">{children}</section>
          <aside className="sticky top-24 flex flex-[0_1_360px] flex-col gap-3.5 self-start rounded-card border border-sand-border bg-cream p-[22px]">
            {aside}
          </aside>
        </div>
      </main>
      <footer className="mt-auto flex justify-center bg-footer px-6 py-5">
        <span className="text-[13px] text-on-dark-muted">
          nestystay.net ·{" "}
          <a className="text-on-dark-muted hover:text-on-dark-body" href="https://wa.me/17542482435">
            754-248-2435
          </a>
        </span>
      </footer>
    </div>
  );
}
