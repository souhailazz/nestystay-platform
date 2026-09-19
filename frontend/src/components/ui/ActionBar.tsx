import type { ReactNode } from "react";
import { cx } from "../../lib/ui";

/**
 * Shared action hierarchy for every workspace screen.
 * Keep the primary action first, use outline for reversible secondary actions,
 * and reserve destructive for irreversible changes.
 */
export function ActionBar({ children, className, sticky = false, labelledBy }: {
  children: ReactNode;
  className?: string;
  sticky?: boolean;
  labelledBy?: string;
}) {
  return (
    <div
      aria-labelledby={labelledBy}
      className={cx("action-bar button-row", sticky && "action-bar--sticky", className)}
      role="group"
    >
      {children}
    </div>
  );
}
