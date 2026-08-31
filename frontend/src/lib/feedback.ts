export type FeedbackTone = "success" | "error" | "info";

export function announceFeedback(message: string, tone: FeedbackTone = "success") {
  window.dispatchEvent(new CustomEvent("nesty:feedback", { detail: { message, tone } }));
}
