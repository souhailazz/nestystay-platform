import { Emblem } from "./Emblem";

/**
 * Standard footer (Design System v2) — deep footer green, emblem + wordmark,
 * nestystay.net · WhatsApp line.
 */
export function SiteFooter() {
  return (
    <footer className="ns-footer">
      <div className="ns-footer__inner">
        <div className="ns-footer__brand">
          <Emblem size={64} />
          <span className="ns-footer__wordmark">NESTY STAY</span>
        </div>
        <div className="ns-footer__meta">
          nestystay.net · <a href="https://wa.me/17542482435">754-248-2435</a>
        </div>
      </div>
    </footer>
  );
}
