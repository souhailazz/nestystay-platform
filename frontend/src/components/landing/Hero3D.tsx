import { motion, useReducedMotion, useScroll, useTransform } from "framer-motion";
import { useRef } from "react";
import SearchBar from "./SearchBar";
import "./LandingHero.css";

export default function Hero3D() {
  const reduceMotion = useReducedMotion();
  const sectionRef = useRef<HTMLElement>(null);
  const { scrollYProgress } = useScroll({
    target: sectionRef,
    offset: ["start start", "end start"],
  });

  const photoRotateY = useTransform(scrollYProgress, [0, 1], [0, -8]);
  const photoScale = useTransform(scrollYProgress, [0, 1], [1, 0.95]);
  const photoTranslateX = useTransform(scrollYProgress, [0, 1], ["0%", "-4%"]);
  const photoOpacity = useTransform(scrollYProgress, [0, 0.6], [1, 0.78]);

  const contentTranslateY = useTransform(scrollYProgress, [0, 1], ["0%", "-6%"]);
  const contentOpacity = useTransform(scrollYProgress, [0, 0.7], [1, 0.85]);

  return (
    <section className="reference-hero" id="top" aria-labelledby="hero-title" ref={sectionRef}>
      <motion.div
        className="reference-hero__backdrop"
        aria-hidden="true"
        style={reduceMotion ? undefined : {
          rotateY: photoRotateY,
          scale: photoScale,
          x: photoTranslateX,
          opacity: photoOpacity,
          transformOrigin: "left center",
          transformPerspective: 1200,
        }}
      >
        <picture>
          <source srcSet="/assets/reference/landing-hero-editorial-realistic.webp" type="image/webp" />
          <img
            src="/assets/reference/landing-hero-editorial-realistic.png"
            alt=""
            className="reference-hero__photo"
            draggable={false}
            fetchPriority="high"
            width={1672}
            height={941}
          />
        </picture>
      </motion.div>

      <motion.div
        className="reference-hero__content"
        style={reduceMotion ? undefined : {
          y: contentTranslateY,
          opacity: contentOpacity,
        }}
      >
        <div className="reference-hero__rule" aria-hidden="true" />
        <h1 id="hero-title">
          More than
          <br />
          a place to stay.
          <em>It&apos;s yaad.</em>
        </h1>
        <p>Find yuh perfect stay. Verified properties, authentic experiences, the real Jamaica.</p>

        <SearchBar />
      </motion.div>

      <div className="reference-hero__left-copy" aria-hidden="true">
        <span>PEOPLE</span>
        <span>PLACES</span>
        <span>CULTURE</span>
        <span>GOOD VIBES</span>
        <div className="reference-hero__jamaica">
          <i />
          <span>JAMAICA</span>
          <span>ALWAYS A GOOD IDEA</span>
        </div>
      </div>

      <p className="sr-only">People, places, culture, good vibes. Jamaica is always a good idea.</p>
    </section>
  );
}
