import { AnimatePresence, motion, useReducedMotion } from "framer-motion";
import { useEffect, useState } from "react";
import { usePatois } from "../../lib/patois";
import { cx } from "../../lib/ui";

/**
 * DS v2 patois toast — Deep card, emblem roundel, Fraunces italic Yellow patois
 * line with its English translation directly below (mandatory pairing, smaller,
 * muted). Slides in 200ms ease-out and auto-dismisses after 3s. When the patois
 * toggle is OFF, shows plain English only. Approved lexicon only — never invent
 * patois: Yuh Gud? / Likkle More / Wi Soon Come! / Nuh Fret / Dis page gone a
 * sea / Tek Time / Manage Your Yard / How wi can help yuh?
 */
export function PatoisToast({
  phrase = "Yuh Gud?",
  translation = "Are you OK? Welcome back to NestyStay.",
  autoDismiss = true,
  onDismiss,
  className,
}: {
  phrase?: string;
  translation?: string;
  autoDismiss?: boolean;
  onDismiss?: () => void;
  className?: string;
}) {
  const { showPatois } = usePatois();
  const reduceMotion = useReducedMotion();
  const [visible, setVisible] = useState(true);

  useEffect(() => {
    if (!autoDismiss) return;
    const timer = window.setTimeout(() => {
      setVisible(false);
      onDismiss?.();
    }, 3000);
    return () => window.clearTimeout(timer);
  }, [autoDismiss, onDismiss]);

  return (
    <AnimatePresence>
      {visible && (
        <motion.aside
          animate={{ x: 0, opacity: 1 }}
          aria-live="polite"
          className={cx("flex items-center gap-3.5 rounded-card bg-deep p-4 pr-5 shadow-navbar", className)}
          exit={{ opacity: 0 }}
          initial={reduceMotion ? false : { x: 24, opacity: 0 }}
          role="status"
          transition={{ duration: 0.2, ease: "easeOut" }}
        >
          <span className="grid size-11 shrink-0 place-items-center overflow-hidden rounded-full bg-cream">
            <img
              alt=""
              aria-hidden="true"
              className="size-9 rounded-full object-contain"
              src="/assets/nestystay-emblem.png"
            />
          </span>
          {showPatois ? (
            <span aria-label={`${phrase} - English: ${translation}`} className="grid gap-0.5">
              <strong className="font-display text-lg font-medium italic leading-tight text-yellow">{phrase}</strong>
              <small className="font-sans text-[13px] leading-snug text-on-dark-muted">{translation}</small>
            </span>
          ) : (
            <span className="font-sans text-[15px] leading-snug text-on-dark-body">{translation}</span>
          )}
        </motion.aside>
      )}
    </AnimatePresence>
  );
}
