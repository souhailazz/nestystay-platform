import { LoaderCircle } from "lucide-react";

export function LoadingSpinner({ label = "Loading" }: { label?: string }) {
  return (
    <span aria-label={label} className="inline-flex items-center" role="status">
      <LoaderCircle aria-hidden="true" className="animate-spin" size={17} />
      <span className="sr-only">{label}</span>
    </span>
  );
}
