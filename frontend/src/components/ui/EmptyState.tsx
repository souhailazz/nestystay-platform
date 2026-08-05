import { Compass } from "lucide-react";
import type { ReactNode } from "react";

/**
 * DS v2 empty state — line-art icon, Fraunces 500 20px title,
 * 13px #5A6B63 body, pill CTA (pass a <Button>/<ButtonLink> as `action`).
 */
export function EmptyState({
  title,
  copy,
  action,
  icon,
}: {
  title: string;
  copy?: string;
  action?: ReactNode;
  icon?: ReactNode;
}) {
  return (
    <div className="grid place-items-center gap-3 rounded-card border border-dashed border-sand-border bg-cream px-8 py-12 text-center">
      <span className="grid size-14 place-items-center rounded-full bg-shell text-deep-hover">
        {icon ?? <Compass aria-hidden="true" size={26} strokeWidth={1.5} />}
      </span>
      <h3 className="m-0 font-display text-xl font-medium leading-snug text-ink">{title}</h3>
      {copy && <p className="m-0 max-w-sm font-sans text-[13px] leading-relaxed text-gray-600">{copy}</p>}
      {action}
    </div>
  );
}
