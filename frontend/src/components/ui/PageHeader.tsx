import { motion } from "framer-motion";
import type { ReactNode } from "react";
import { Fragment } from "react";

/**
 * DS v2 page header — Sora eyebrow (600 12px, +0.28em, 34px leading dash),
 * Fraunces 400 clamp(30px,3.4vw,40px) title with ONE italic accent word in
 * Deep Hover (#0E4A45). Wrap the accent word in *asterisks* to choose it
 * ("Manage your *yard*"); otherwise the last word is accented.
 */
function renderAccentTitle(title: string) {
  const accentClassName = "font-display italic text-deep-hover";
  const marked = title.match(/^(.*?)\*([^*]+)\*(.*)$/);
  if (marked) {
    return (
      <Fragment>
        {marked[1]}
        <em className={accentClassName}>{marked[2]}</em>
        {marked[3]}
      </Fragment>
    );
  }
  const words = title.trim().split(/\s+/);
  const accent = words.pop() ?? "";
  return (
    <Fragment>
      {words.length > 0 ? `${words.join(" ")} ` : ""}
      <em className={accentClassName}>{accent}</em>
    </Fragment>
  );
}

export function PageHeader({
  eyebrow,
  title,
  copy,
  actions,
}: {
  eyebrow: string;
  title: string;
  copy?: string;
  actions?: ReactNode;
}) {
  return (
    <header className="flex flex-wrap items-end justify-between gap-6">
      <div className="max-w-2xl">
        <motion.span
          animate={{ opacity: 1, y: 0 }}
          className="mb-3 inline-flex items-center gap-3 font-sans text-xs font-semibold uppercase tracking-[0.28em] text-sand-500"
          initial={{ opacity: 0, y: 16 }}
          transition={{ duration: 0.6 }}
        >
          <span aria-hidden="true" className="inline-block h-px w-[34px] bg-sand-500" />
          {eyebrow}
        </motion.span>
        <motion.h1
          animate={{ opacity: 1, y: 0 }}
          className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal leading-[1.12] text-ink"
          initial={{ opacity: 0, y: 22 }}
          transition={{ duration: 0.7, delay: 0.1 }}
        >
          {renderAccentTitle(title)}
        </motion.h1>
        {copy && (
          <motion.p
            animate={{ opacity: 1, y: 0 }}
            className="mb-0 mt-3 font-sans text-[15px] leading-[1.55] text-gray-600"
            initial={{ opacity: 0, y: 18 }}
            transition={{ duration: 0.7, delay: 0.2 }}
          >
            {copy}
          </motion.p>
        )}
      </div>
      {actions && (
        <motion.div
          animate={{ opacity: 1, y: 0 }}
          className="flex flex-wrap items-center gap-3"
          initial={{ opacity: 0, y: 16 }}
          transition={{ duration: 0.6, delay: 0.3 }}
        >
          {actions}
        </motion.div>
      )}
    </header>
  );
}
