import { motion, useReducedMotion, useScroll, useTransform } from "framer-motion";
import {
  ArrowDown,
  ArrowRight,
  CalendarDays,
  MapPin,
  ShieldCheck,
  Sparkles,
  Star,
} from "lucide-react";
import { AppLink } from "../AppLink";
import { getStayImage } from "../../lib/stayImages";

function Palm({ side }: { side: "left" | "right" }) {
  return (
    <div className={`hero-palm hero-palm--${side}`} aria-hidden="true">
      <span className="palm-trunk" />
      <span className="palm-leaf leaf-1" />
      <span className="palm-leaf leaf-2" />
      <span className="palm-leaf leaf-3" />
      <span className="palm-leaf leaf-4" />
      <span className="palm-leaf leaf-5" />
    </div>
  );
}

export default function Hero3D() {
  const reduceMotion = useReducedMotion();
  const heroImage = getStayImage(0);
  const { scrollYProgress } = useScroll();
  const artY = useTransform(scrollYProgress, [0, 0.18], [0, reduceMotion ? 0 : 100]);
  const copyY = useTransform(scrollYProgress, [0, 0.18], [0, reduceMotion ? 0 : 48]);
  const artRotate = useTransform(
    scrollYProgress,
    [0, 0.18],
    [0, reduceMotion ? 0 : -3],
  );

  return (
    <section className="hero" id="top">
      <div className="hero-grid" aria-hidden="true" />
      <div className="hero-sun" aria-hidden="true" />
      <Palm side="left" />
      <Palm side="right" />

      <motion.div className="hero-copy" style={{ y: copyY }}>
        <motion.div
          className="eyebrow"
          initial={{ opacity: 0, y: 14 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7 }}
        >
          <Sparkles size={15} />
          Premium tropical stays by Nesty Stay
        </motion.div>

        <motion.h1
          initial={{ opacity: 0, y: 26 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.85, delay: 0.12 }}
        >
          Find your perfect stay, <em>effortlessly.</em>
        </motion.h1>

        <motion.p
          initial={{ opacity: 0, y: 22 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8, delay: 0.22 }}
        >
          A smooth, modern booking experience for tropical homes, studios, and
          premium stays.
        </motion.p>

        <motion.div
          className="hero-actions"
          initial={{ opacity: 0, y: 22 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8, delay: 0.32 }}
        >
          <AppLink href="/explore" className="button button--sun">
            Explore Stays <ArrowRight size={18} />
          </AppLink>
          <AppLink href="/host/properties" className="button button--glass">
            List Your Property
          </AppLink>
        </motion.div>

        <motion.div
          className="hero-proof"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.9, delay: 0.5 }}
        >
          <span className="avatar-stack" aria-hidden="true">
            <i>JD</i>
            <i>SK</i>
            <i>AM</i>
          </span>
          <span>
            <strong>4.9</strong>
            <span className="proof-stars">
              {[0, 1, 2, 3, 4].map((star) => (
                <Star key={star} size={11} fill="currentColor" />
              ))}
            </span>
            loved by guests near and far
          </span>
        </motion.div>
      </motion.div>

      <motion.div
        className="hero-art"
        style={{ y: artY, rotateZ: artRotate }}
        initial={{ opacity: 0, scale: 0.92, rotateY: -8 }}
        animate={{ opacity: 1, scale: 1, rotateY: 0 }}
        transition={{ duration: 1.15, delay: 0.15, ease: [0.2, 0.8, 0.2, 1] }}
      >
        <div className="orbit orbit--one" aria-hidden="true">
          <span />
        </div>
        <div className="orbit orbit--two" aria-hidden="true">
          <span />
        </div>
        <div className="island-shadow" aria-hidden="true" />
        <div className="island-disc" aria-hidden="true">
          <div className="island-water" />
          <div className="island-sand" />
          <div className="island-green" />
          <div className="tiny-house">
            <span className="house-roof" />
            <span className="house-window" />
          </div>
          <div className="mini-palm mini-palm--one">
            <span />
          </div>
          <div className="mini-palm mini-palm--two">
            <span />
          </div>
        </div>

        <motion.div
          className="stay-card"
          animate={reduceMotion ? undefined : { y: [0, -10, 0], rotateZ: [-2, -1, -2] }}
          transition={{ duration: 5.8, repeat: Infinity, ease: "easeInOut" }}
        >
          <div className="stay-card__visual">
            <img className="generated-stay-image" src={heroImage.src} alt={heroImage.alt} />
          </div>
          <div className="stay-card__body">
            <div>
              <span className="micro-label">Featured stay</span>
              <h3>Seaview Villa</h3>
              <span className="location">
                <MapPin size={13} /> Port Antonio, Jamaica
              </span>
            </div>
            <span className="rating">
              <Star size={13} fill="currentColor" /> 4.98
            </span>
          </div>
        </motion.div>

        <motion.div
          className="floating-chip floating-chip--date"
          animate={reduceMotion ? undefined : { y: [0, 7, 0] }}
          transition={{ duration: 4, repeat: Infinity, ease: "easeInOut" }}
        >
          <CalendarDays size={17} />
          <span>
            <small>Next beach break</small>
            Jun 28 — Jul 03
          </span>
        </motion.div>

        <motion.div
          className="floating-chip floating-chip--safe"
          animate={reduceMotion ? undefined : { y: [0, -6, 0] }}
          transition={{ duration: 4.6, repeat: Infinity, ease: "easeInOut" }}
        >
          <ShieldCheck size={18} />
          <span>
            <small>Booking safe</small>
            Protected
          </span>
        </motion.div>
      </motion.div>

      <a className="scroll-cue" href="#experience">
        <span>Explore the experience</span>
        <ArrowDown size={16} />
      </a>
    </section>
  );
}
