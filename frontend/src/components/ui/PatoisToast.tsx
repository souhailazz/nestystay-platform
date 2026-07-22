import { BadgeCheck } from "lucide-react";
import { PatoisPhrase } from "../../lib/patois";
import { cx } from "../../lib/ui";

export function PatoisToast({ className }: { className?: string }) {
  return (
    <aside className={cx("patois-toast", className)} role="status" aria-live="polite">
      <BadgeCheck size={24} />
      <PatoisPhrase phrase="Yuh Gud?" translation="Are you OK? Welcome back to NestyStay." />
    </aside>
  );
}
