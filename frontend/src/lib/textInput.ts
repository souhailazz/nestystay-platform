export type TextInputRequest = {
  title?: string;
  message?: string;
  label?: string;
  initialValue?: string;
  placeholder?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  type?: "text" | "datetime-local";
  required?: boolean;
};

/** Open the shared in-app text input dialog and resolve with null on cancel. */
export function requestTextInput(request: TextInputRequest): Promise<string | null> {
  if (typeof window === "undefined") return Promise.resolve(null);

  return new Promise((resolve) => {
    window.dispatchEvent(new CustomEvent("nesty:text-input", { detail: { request, resolve } }));
  });
}
