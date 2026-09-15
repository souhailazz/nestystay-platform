import type {
  InputHTMLAttributes,
  LabelHTMLAttributes,
  ReactNode,
  SelectHTMLAttributes,
  TextareaHTMLAttributes,
} from "react";
import { cx } from "../../lib/ui";

/**
 * DS v2 fields — white bg, 1.5px #C7BC9C border, radius 14px, min-height 48px.
 * Focus: Deep Hover border + soft ring. Error (aria-invalid): coral border;
 * show the backend message verbatim via Field's `error` prop.
 */
const fieldClassName =
  "w-full min-h-12 rounded-field border-[1.5px] border-sand-input bg-white px-4 font-sans text-[15px] text-ink outline-none transition-[border-color,box-shadow] duration-200 " +
  "placeholder:text-sand-500 " +
  "focus:border-deep-hover focus:shadow-[0_0_0_3px_rgba(14,74,69,0.12)] " +
  "aria-[invalid]:border-coral " +
  "disabled:border-sand-border disabled:bg-shell disabled:text-sand-500";

export function Field({
  label,
  hint,
  error,
  children,
  className,
}: {
  label: string;
  hint?: string;
  error?: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <label className={cx("grid gap-2 font-sans", className)}>
      <span className="text-[13px] font-semibold text-ink">{label}</span>
      {children}
      {hint && !error && <small className="text-xs font-normal text-gray-600">{hint}</small>}
      {error && (
        <small className="text-[13px] font-medium text-coral-text" role="alert">
          {error}
        </small>
      )}
    </label>
  );
}

export function Input({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={cx(fieldClassName, className)} {...props} />;
}

export function Select({ className, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select className={cx(fieldClassName, "pr-9", className)} {...props} />;
}

export function Textarea({ className, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea className={cx(fieldClassName, "min-h-[120px] resize-y py-3", className)} {...props} />;
}

export function InlineLabel({ className, ...props }: LabelHTMLAttributes<HTMLLabelElement>) {
  return (
    <label
      className={cx(
        "inline-flex min-h-11 cursor-pointer items-center gap-2.5 rounded-field border border-sand-border bg-cream px-3.5 font-sans text-[13px] font-semibold text-ink transition-colors hover:border-sand-input",
        "[&_input]:accent-deep-hover",
        className,
      )}
      {...props}
    />
  );
}
