import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

/**
 * Guardian test for the Jamaican Patois lexicon.
 *
 * Every approved patois phrase is locked to the source file that renders it
 * through the PatoisPhrase channel (src/lib/patois.tsx). If a component is
 * renamed or moved, update the PATH here — never the phrase, and never delete
 * an entry. Never invent patois: new phrases must come from the approved
 * lexicon (Design System v2, DS-07).
 */
const srcDir = dirname(fileURLToPath(import.meta.url));

const lexicon: Array<{ phrase: string; file: string }> = [
  // Landing (PUB-01) — hero search bar
  { phrase: "Weh Yuh Deh?", file: "components/landing/pub01/GoldenHourHero.tsx" },
  // Shared UI
  { phrase: "Tek Time", file: "components/ui/LoadingState.tsx" },
  { phrase: "Yuh Gud?", file: "components/ui/PatoisToast.tsx" },
  // Booking flow
  { phrase: "Tek Time Pick Yuh Dates", file: "features/booking/BookingModal.tsx" },
  { phrase: "Nuh Fret, Tek Time", file: "features/booking/BookingPendingPage.tsx" },
  { phrase: "Check Everything Good Good", file: "features/booking/BookingReviewPage.tsx" },
  { phrase: "Pay Safe & Secure", file: "features/booking/BookingCheckoutPage.tsx" },
  { phrase: "Everything Gud Good!", file: "features/booking/BookingSuccessPage.tsx" },
  { phrase: "Nuh Fret, Zero Charges", file: "features/booking/BookingRejectedPage.tsx" },
  { phrase: "Nuh Stress, Try Again", file: "features/booking/BookingFailurePage.tsx" },
  { phrase: "Suh It Guh", file: "features/booking/BookingCancelledPage.tsx" },
];

describe("patois lexicon", () => {
  it.each(lexicon)("locks $phrase to $file", ({ phrase, file }) => {
    const source = readFileSync(resolve(srcDir, file), "utf8");
    expect(source, `${file} must render the locked phrase "${phrase}"`).toContain(phrase);
    // PatoisBlock is the styled Design System v2 wrapper around PatoisPhrase.
    expect(
      /PatoisPhrase|PatoisBlock/.test(source),
      `${file} must render patois through the PatoisPhrase channel`,
    ).toBe(true);
  });
});
