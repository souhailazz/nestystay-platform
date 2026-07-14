import { useEffect, useRef } from "react";
import { gsap } from "gsap";
import { ScrollTrigger } from "gsap/ScrollTrigger";
import { Check, CloudSun, KeyRound, Waves } from "lucide-react";

gsap.registerPlugin(ScrollTrigger);

const moments = [
  {
    step: "01",
    kicker: "Morning breeze",
    title: "Find the right likkle corner.",
    copy: "Beachside room, hillside villa, or Kingston hideaway - search by the vibe yuh want.",
    icon: CloudSun,
  },
  {
    step: "02",
    kicker: "Golden hour",
    title: "Book it, no long talk.",
    copy: "Clear details, fair prices, and secure checkout in a few easy taps.",
    icon: Waves,
  },
  {
    step: "03",
    kicker: "Afterglow",
    title: "Reach and settle in.",
    copy: "Hosts keep things ready, check-in is smooth, and the place feels like yard.",
    icon: KeyRound,
  },
];

export default function ScrollStory() {
  const sectionRef = useRef<HTMLElement>(null);
  const sceneRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const media = gsap.matchMedia();

    media.add("(min-width: 900px) and (prefers-reduced-motion: no-preference)", () => {
      const section = sectionRef.current;
      const scene = sceneRef.current;
      if (!section || !scene) return;

      const panels = gsap.utils.toArray<HTMLElement>(".story-panel", section);
      const timeline = gsap.timeline({
        scrollTrigger: {
          trigger: section,
          start: "top top",
          end: "+=2400",
          pin: true,
          scrub: 1,
          anticipatePin: 1,
        },
      });

      timeline
        .to(scene, { "--story-shift": "50%", "--sky-mix": "52%", duration: 1 })
        .to(".story-sun", { x: 130, y: 140, scale: 1.15, duration: 1 }, "<")
        .to(".story-hammock", { rotate: 4, y: 10, duration: 1 }, "<")
        .to(panels[0], { opacity: 0, y: -50, duration: 0.35 }, 0.65)
        .fromTo(
          panels[1],
          { opacity: 0, y: 70 },
          { opacity: 1, y: 0, duration: 0.35 },
          0.72,
        )
        .to(scene, { "--story-shift": "100%", "--sky-mix": "100%", duration: 1 }, 1)
        .to(".story-sun", { x: 260, y: 250, opacity: 0.6, duration: 1 }, 1)
        .to(".story-stars", { opacity: 1, duration: 0.8 }, 1.1)
        .to(".story-hammock", { rotate: -3, y: -2, duration: 1 }, 1)
        .to(panels[1], { opacity: 0, y: -50, duration: 0.35 }, 1.65)
        .fromTo(
          panels[2],
          { opacity: 0, y: 70 },
          { opacity: 1, y: 0, duration: 0.35 },
          1.72,
        );
    });

    return () => media.revert();
  }, []);

  return (
    <section className="story-section" id="experience" ref={sectionRef}>
      <div className="story-scene" ref={sceneRef}>
        <div className="story-stars" aria-hidden="true">
          {Array.from({ length: 18 }, (_, index) => (
            <i key={index} />
          ))}
        </div>
        <div className="story-sun" aria-hidden="true" />
        <div className="story-cloud story-cloud--one" aria-hidden="true" />
        <div className="story-cloud story-cloud--two" aria-hidden="true" />

        <div className="story-landscape" aria-hidden="true">
          <div className="story-mountain story-mountain--back" />
          <div className="story-mountain story-mountain--front" />
          <div className="story-water">
            <span />
            <span />
            <span />
          </div>
          <div className="story-palm story-palm--left">
            <i />
          </div>
          <div className="story-palm story-palm--right">
            <i />
          </div>
          <div className="story-hammock" />
        </div>

        <div className="story-copy">
          <div className="section-tag section-tag--light">
            <span />
            The Nesty vibe
          </div>
          <div className="story-panels">
            {moments.map(({ step, kicker, title, copy, icon: Icon }, index) => (
              <article
                className={`story-panel story-panel--${index + 1}`}
                key={step}
              >
                <div className="story-panel__meta">
                  <span>{step}</span>
                  <Icon size={18} />
                  {kicker}
                </div>
                <h2>{title}</h2>
                <p>{copy}</p>
                <div className="story-check">
                  <Check size={14} /> Easy from search to check-in
                </div>
              </article>
            ))}
          </div>
        </div>

        <div className="story-progress" aria-hidden="true">
          <span />
          <i>01</i>
          <i>03</i>
        </div>
      </div>
    </section>
  );
}
