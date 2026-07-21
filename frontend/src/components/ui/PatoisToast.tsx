import { BadgeCheck } from "lucide-react";
import { cx } from "../../lib/ui";

export function PatoisToast({ className }: { className?: string }) {
  return (
    <aside className={cx("patois-toast", className)} role="status" aria-live="polite">
      <BadgeCheck size={24} />
      <div>
        <strong>Yuh Gud?</strong>
        <span>Are you OK? Welcome back to NestyStay.</span>
      </div>
    </aside>
  );
}
