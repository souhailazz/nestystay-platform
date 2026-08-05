import { AnimatePresence, motion } from "framer-motion";
import { X } from "lucide-react";
import type { ReactNode } from "react";

/**
 * DS v2 modal — cream surface, radius 22px, deep modal shadow, overlay
 * rgba(6,43,43,0.45), Fraunces 500 24px title, 44px round close button.
 */
export function Modal({
  open,
  title,
  children,
  onClose,
}: {
  open: boolean;
  title: string;
  children: ReactNode;
  onClose: () => void;
}) {
  return (
    <AnimatePresence>
      {open && (
        <motion.div
          className="fixed inset-0 z-[200] grid place-items-center overflow-y-auto bg-[rgba(6,43,43,0.45)] p-6"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
        >
          <motion.section
            aria-modal="true"
            className="max-h-[calc(100vh-48px)] w-[min(720px,100%)] overflow-y-auto rounded-[22px] bg-cream p-7 shadow-modal"
            initial={{ opacity: 0, y: 28, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 16, scale: 0.98 }}
            role="dialog"
          >
            <header className="mb-5 flex items-center justify-between gap-4">
              <h2 className="m-0 font-display text-2xl font-medium leading-tight text-ink">{title}</h2>
              <button
                aria-label="Close modal"
                className="grid size-11 shrink-0 cursor-pointer place-items-center rounded-pill border border-sand-border bg-transparent text-ink transition-colors duration-200 hover:bg-shell focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-deep-hover/25"
                onClick={onClose}
                type="button"
              >
                <X size={18} />
              </button>
            </header>
            {children}
          </motion.section>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
