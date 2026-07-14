import type { HTMLAttributes } from "react";
import { cx } from "../../lib/ui";

export function Card({ className, ...props }: HTMLAttributes<HTMLElement>) {
  return <article className={cx("ui-card", className)} {...props} />;
}
