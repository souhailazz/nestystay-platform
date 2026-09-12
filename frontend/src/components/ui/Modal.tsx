import { AnimatePresence, motion } from "framer-motion";
import { X } from "lucide-react";
import { useEffect, useId, useRef, type ReactNode } from "react";

/**
 * DS v2 modal — cream surface, radius 22px, deep modal shadow, overlay
 * rgba(6,43,43,0.45), Fraunces 500 24px title, 44px round close button.
 */
export function Modal({
  open,
  title,
  children,
  onClose,
  variant = "modal",
  closeOnOverlayClick = false,
}: {
  open: boolean;
  title: string;
  children: ReactNode;
  onClose: () => void;
  variant?: "modal" | "sheet" | "fullscreen";
  closeOnOverlayClick?: boolean;
}) {
  const dialogRef = useRef<HTMLElement>(null);
  const onCloseRef = useRef(onClose);
  const titleId = useId();
  onCloseRef.current = onClose;

  useEffect(() => {
    if (!open) return;

    const previouslyFocused = document.activeElement as HTMLElement | null;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    const dialog = dialogRef.current;
    const focusableSelector =
      'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';
    const firstFocusable = dialog?.querySelector<HTMLElement>(focusableSelector);
    (firstFocusable ?? dialog)?.focus();

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        onCloseRef.current();
        return;
      }

      if (event.key !== "Tab" || !dialog) return;
      const controls = [...dialog.querySelectorAll<HTMLElement>(focusableSelector)].filter(
        (element) => element.offsetParent !== null,
      );
      if (controls.length === 0) {
        event.preventDefault();
        dialog.focus();
        return;
      }

      const first = controls[0];
      const last = controls[controls.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      document.body.style.overflow = previousOverflow;
      previouslyFocused?.focus();
    };
  }, [open]);

  const overlayClass = variant === "sheet" ? "items-end p-0 sm:items-center sm:p-6" : "items-center overflow-y-auto p-6";
  const surfaceClass = variant === "sheet"
    ? "max-h-[min(82dvh,720px)] w-full rounded-t-[22px] rounded-b-none pb-[calc(1.75rem+env(safe-area-inset-bottom))] sm:max-h-[calc(100dvh-48px)] sm:rounded-[22px] sm:pb-7"
    : variant === "fullscreen"
      ? "min-h-[100dvh] w-full rounded-none p-5 sm:min-h-0 sm:w-[min(960px,100%)] sm:rounded-[22px] sm:p-7"
      : "max-h-[calc(100dvh-48px)] w-[min(720px,100%)] rounded-[22px] p-7";

  return (
    <AnimatePresence>
      {open && (
        <motion.div
          className={`fixed inset-0 z-[200] grid overflow-y-auto bg-[rgba(6,43,43,0.45)] ${overlayClass}`}
          onMouseDown={(event) => {
            if (closeOnOverlayClick && event.target === event.currentTarget) onCloseRef.current();
          }}
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
        >
          <motion.section
            aria-labelledby={titleId}
            aria-modal="true"
            className={`${surfaceClass} overflow-y-auto bg-cream shadow-modal`}
            initial={{ opacity: 0, y: 28, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 16, scale: 0.98 }}
            ref={dialogRef}
            role="dialog"
            tabIndex={-1}
          >
            <header className="mb-5 flex items-center justify-between gap-4">
              <h2 className="m-0 font-display text-2xl font-medium leading-tight text-ink" id={titleId}>{title}</h2>
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
