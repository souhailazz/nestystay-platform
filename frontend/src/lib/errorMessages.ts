export type AppErrorKind = "auth" | "validation" | "not-found" | "conflict" | "network" | "server" | "unknown";

const safeMessages: Record<AppErrorKind, string> = {
  auth: "Your session has expired. Sign in again to continue.",
  validation: "Check the highlighted fields and try again.",
  "not-found": "We couldn’t find that record.",
  conflict: "That change is no longer available. Refresh and try again.",
  network: "NestyStay is having trouble connecting. Check your connection and try again.",
  server: "NestyStay couldn’t complete that request. Try again shortly.",
  unknown: "Something went wrong. Try again shortly.",
};

export function classifyError(error: unknown): AppErrorKind {
  const status = typeof error === "object" && error !== null && "status" in error ? Number((error as { status?: unknown }).status) : 0;
  if (status === 401 || status === 403) return "auth";
  if (status === 404) return "not-found";
  if (status === 409) return "conflict";
  if (status >= 500) return "server";
  if (error instanceof TypeError || (error instanceof Error && /network|fetch|connection|timeout/i.test(error.message))) return "network";
  if (status === 400 || status === 422) return "validation";
  return "unknown";
}

export function userSafeErrorMessage(error: unknown, fallback?: string) {
  if (typeof error === "string" && error.length > 0 && !/exception|stack trace|sqlstate|at\s+\w+\s+\(/i.test(error)) return error;
  return fallback ?? safeMessages[classifyError(error)];
}
