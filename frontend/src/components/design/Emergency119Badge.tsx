import type { ReactNode } from "react";

function SirenIcon({ size, withWaves }: { size: number; withWaves: boolean }) {
  return (
    <svg width={size} height={size} viewBox="0 0 30 30" fill="none" aria-hidden="true">
      <path d="M15 5 L27 25.5 H3 Z" stroke="#ffffff" strokeWidth="2" strokeLinejoin="round" />
      <path d="M15 12.5 V18.5" stroke="#ffffff" strokeWidth="2" strokeLinecap="round" />
      <circle cx="15" cy="22" r="1.3" fill="#ffffff" />
      {withWaves && (
        <path
          d="M6.5 4.5 Q4 7 4 10.5 M23.5 4.5 Q26 7 26 10.5"
          stroke="#ffffff"
          strokeWidth="1.6"
          strokeLinecap="round"
          opacity="0.7"
        />
      )}
    </svg>
  );
}

/**
 * Jamaica Emergency 119 badge (Design System v2, DS-06).
 * Emergency Red #DC2626, SVG siren icon — no emoji in production UI.
 * `inline` = compact bar for property pages; `card` = dominant dark card
 * for trust sections ("119" in yellow Fraunces dominates).
 */
export function Emergency119Badge({
  variant = "inline",
  chip,
  note,
  className = "",
}: {
  variant?: "inline" | "card";
  /** Red pill chip line, card variant only (e.g. "Shown on every property page"). */
  chip?: ReactNode;
  /** Muted explainer paragraph, card variant only. */
  note?: ReactNode;
  className?: string;
}) {
  if (variant === "inline") {
    return (
      <div className={`ns-119-inline ${className}`}>
        <SirenIcon size={22} withWaves={false} />
        Jamaica Emergency: 119
      </div>
    );
  }

  return (
    <div className={`ns-119-card ${className}`}>
      <span className="ns-119-card__icon">
        <SirenIcon size={32} withWaves />
      </span>
      <div>
        <div className="ns-119-card__title">
          Jamaica Emergency: <span className="ns-119-card__number">119</span>
        </div>
        <div className="ns-119-card__services">POLICE · FIRE · AMBULANCE — ISLAND-WIDE</div>
      </div>
      {chip && <span className="ns-119-card__chip">{chip}</span>}
      {note && <p className="ns-119-card__note">{note}</p>}
    </div>
  );
}
