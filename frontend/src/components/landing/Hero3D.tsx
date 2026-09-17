import { useEffect, useRef } from "react";
import { usePrefersReducedMotion } from "../../hooks/usePrefersReducedMotion";
import SearchBar from "./SearchBar";
import "./LandingHero.css";

export default function Hero3D() {
  const reduceMotion = usePrefersReducedMotion();
  const sectionRef = useRef<HTMLElement>(null);
  const backdropRef = useRef<HTMLDivElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const section = sectionRef.current;
    const backdrop = backdropRef.current;
    const content = contentRef.current;
    if (!section || !backdrop || !content) return;

    const reset = () => {
      backdrop.style.transform = "";
      backdrop.style.opacity = "";
      content.style.transform = "";
      content.style.opacity = "";
      backdrop.style.willChange = "auto";
      content.style.willChange = "auto";
    };
    if (reduceMotion) {
      reset();
      return;
    }

    let frame = 0;
    const update = () => {
      frame = 0;
      const progress = Math.min(1, Math.max(0, (window.scrollY - section.offsetTop) / Math.max(1, section.offsetHeight)));
      backdrop.style.transform = `perspective(1200px) rotateY(${-8 * progress}deg) scale(${1 - 0.05 * progress}) translate3d(${-4 * progress}%, 0, 0)`;
      backdrop.style.opacity = String(1 - 0.22 * Math.min(1, progress / 0.6));
      content.style.transform = `translate3d(0, ${-6 * progress}%, 0)`;
      content.style.opacity = String(1 - 0.15 * Math.min(1, progress / 0.7));
    };
    const onScroll = () => {
      if (!frame) frame = window.requestAnimationFrame(update);
    };
    backdrop.style.willChange = "transform, opacity";
    content.style.willChange = "transform, opacity";
    update();
    window.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("resize", onScroll, { passive: true });
    return () => {
      window.removeEventListener("scroll", onScroll);
      window.removeEventListener("resize", onScroll);
      if (frame) window.cancelAnimationFrame(frame);
      reset();
    };
  }, [reduceMotion]);

  return (
    <section className="reference-hero" id="top" aria-labelledby="hero-title" ref={sectionRef}>
      <div
        className="reference-hero__backdrop"
        aria-hidden="true"
        ref={backdropRef}
      >
        <picture>
          <source srcSet="/assets/reference/landing-hero-editorial-realistic-sm.webp 960w, /assets/reference/landing-hero-editorial-realistic.webp 1672w" sizes="100vw" type="image/webp" />
          <img
            src="/assets/reference/landing-hero-editorial-realistic.png"
            alt=""
            className="reference-hero__photo"
            draggable={false}
            fetchPriority="high"
            decoding="async"
            width={1672}
            height={941}
          />
        </picture>
      </div>

      <div
        className="reference-hero__content"
        ref={contentRef}
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
      </div>

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
