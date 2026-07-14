import { motion } from "framer-motion";
import { ArrowRight, Instagram, Linkedin } from "lucide-react";
import { AppLink } from "../AppLink";

export default function FinalCTA() {
  return (
    <footer className="final-section">
      <div className="final-grid" aria-hidden="true" />
      <div className="final-orb final-orb--one" aria-hidden="true" />
      <div className="final-orb final-orb--two" aria-hidden="true" />

      <motion.div
        className="final-mark"
        initial={{ opacity: 0, scale: 0.82, rotate: -5 }}
        whileInView={{ opacity: 1, scale: 1, rotate: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.8 }}
      >
        <svg viewBox="560 150 930 700" role="img" aria-label="Nesty Stay logo">
          <image
            href="/assets/nesty/Nesty-Stay.png"
            width="2048"
            height="1280"
            preserveAspectRatio="xMidYMid meet"
          />
        </svg>
      </motion.div>

      <motion.div
        className="final-copy"
        initial={{ opacity: 0, y: 35 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.75, delay: 0.1 }}
      >
        <span className="final-kicker">Somewhere nice waiting fi yuh</span>
        <h2>Ready fi your next stay?</h2>
        <p>Find a place that feels right before yuh even reach.</p>
        <AppLink href="/explore" className="button button--sun button--large">
          Explore Stays <ArrowRight size={19} />
        </AppLink>
      </motion.div>

      <div className="footer-bar">
        <AppLink className="brand-lockup" href="/">
          <span>NESTY STAY</span>
        </AppLink>
        <p>© 2026 Nesty Stay. Made for slower mornings and good vibes.</p>
        <div className="socials">
          <a href="#top" aria-label="Instagram">
            <Instagram size={17} />
          </a>
          <a href="#top" aria-label="LinkedIn">
            <Linkedin size={17} />
          </a>
        </div>
      </div>
    </footer>
  );
}
