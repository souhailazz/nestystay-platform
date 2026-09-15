import { usePatois } from "../../lib/patois";
import { cx } from "../../lib/ui";

/**
 * DS v2 loading state — structured skeletons (image rect + text lines) with a
 * warm shimmer (`ns-shimmer`), "Tek Time" heading paired with its English
 * translation. Never a bare spinner; fixed blocks avoid layout shift.
 */
function Skeleton({ className }: { className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={cx(
        "block rounded-[10px] bg-shell bg-no-repeat [background-image:linear-gradient(90deg,rgba(251,247,236,0)_0%,rgba(251,247,236,0.9)_50%,rgba(251,247,236,0)_100%)] [background-size:400px_100%] animate-[ns-shimmer_1.4s_ease-in-out_infinite] motion-reduce:animate-none",
        className,
      )}
    />
  );
}

export function LoadingState({ label = "Loading Nesty Stay data" }: { label?: string }) {
  const { showPatois } = usePatois();
  return (
    <div
      aria-busy="true"
      className="grid gap-6 rounded-card border border-sand-border bg-cream p-6 shadow-card"
      role="status"
    >
      <div className="grid justify-items-center gap-1 text-center">
        {showPatois ? (
          <>
            <h3 className="m-0 font-display text-xl font-medium italic leading-snug text-deep-hover">Tek Time</h3>
            <p className="m-0 font-sans text-[13px] leading-relaxed text-gray-600">
              Take your time — we&apos;re loading the latest NestyStay data.
            </p>
          </>
        ) : (
          <h3 className="m-0 font-display text-xl font-medium leading-snug text-ink">{label}…</h3>
        )}
        <span className="sr-only">{label}</span>
      </div>
      <div aria-hidden="true" className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        {[0, 1, 2].map((item) => (
          <div className="grid gap-2.5" key={item}>
            <Skeleton className="h-28 rounded-card" />
            <Skeleton className="h-3.5 w-4/5" />
            <Skeleton className="h-3.5 w-3/5" />
          </div>
        ))}
      </div>
    </div>
  );
}
