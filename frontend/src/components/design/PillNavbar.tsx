import { useEffect, useState } from "react";
import { AppLink } from "../AppLink";
import { Emblem } from "./Emblem";
import { PillLink } from "./PillButton";

const navLinks = [
  ["Explore", "/explore"],
  ["Host", "/host-dashboard"],
  ["Wellness", "/host/wellness"],
] as const;

/**
 * Floating pill navbar (Design System v2, public shell).
 * Sticky, deep-green pill; compacts once the page is scrolled.
 */
export function PillNavbar() {
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 40);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <div className="ns-nav-wrap">
      <nav className={`ns-nav ${scrolled ? "ns-nav--scrolled" : ""}`} aria-label="Main navigation">
        <AppLink className="ns-nav__brand" href="/" aria-label="NestyStay home">
          <Emblem size={44} />
          <span className="ns-nav__wordmark">NESTY STAY</span>
        </AppLink>
        <div className="ns-nav__links">
          {navLinks.map(([label, href]) => (
            <AppLink key={href} className="ns-navlink" href={href}>
              {label}
            </AppLink>
          ))}
          <PillLink variant="sun" href="/login" arrow className="ns-nav__cta">
            Sign in
          </PillLink>
        </div>
      </nav>
    </div>
  );
}
