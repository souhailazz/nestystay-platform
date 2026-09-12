export type ConfirmationRequest = {
  title?: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
};

export function requestConfirmation(request: ConfirmationRequest): Promise<boolean> {
  if (typeof window === "undefined") return Promise.resolve(false);

  return new Promise((resolve) => {
    window.dispatchEvent(new CustomEvent("nesty:confirm", { detail: { request, resolve } }));
  });
}
