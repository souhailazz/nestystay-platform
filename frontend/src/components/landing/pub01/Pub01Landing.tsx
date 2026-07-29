import { useRef } from "react";
import { PillNavbar } from "../../design/PillNavbar";
import { GoldenHourHero } from "./GoldenHourHero";
import { DaylightShowcase } from "./DaylightShowcase";
import { HighNoonStatement } from "./HighNoonStatement";
import { DuskSecurity } from "./DuskSecurity";
import { NightfallScene } from "./NightfallScene";
import { useLandingMotion } from "./motion";

/**
 * PUB-01 landing — "a Jamaican day": golden hour hero → cream daylight →
 * full-yellow statement → dusk security → nightfall scene with fused footer.
 * Owns its floating pill navbar and footer.
 */
export function Pub01Landing() {
  const rootRef = useRef<HTMLDivElement>(null);
  useLandingMotion(rootRef);

  return (
    <div ref={rootRef} className="ns-surface ns-landing" data-testid="pub01-landing">
      <PillNavbar />
      <GoldenHourHero />
      <DaylightShowcase />
      <HighNoonStatement />
      <DuskSecurity />
      <NightfallScene />
    </div>
  );
}
