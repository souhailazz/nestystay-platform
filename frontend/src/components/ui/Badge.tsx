import type { HTMLAttributes } from "react";
import { cx } from "../../lib/ui";

type Tone = "green" | "sun" | "cream" | "coral" | "ink" | "blue" | "slate" | "mint";

export function Badge({ className, ...props }: HTMLAttributes<HTMLSpanElement> & { tone?: Tone }) {
  const { tone = "green", ...rest } = props;
  return <span className={cx("ui-badge", `ui-badge--${tone}`, className)} {...rest} />;
}
