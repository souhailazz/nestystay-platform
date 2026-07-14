import type { ReactNode } from "react";

export function EmptyState({ title, copy, action }: { title: string; copy?: string; action?: ReactNode }) {
  return (
    <div className="ui-state ui-state--empty">
      <h3>{title}</h3>
      {copy && <p>{copy}</p>}
      {action}
    </div>
  );
}
