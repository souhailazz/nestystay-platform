import { type CSSProperties, useEffect, useRef } from "react";
import { gsap } from "gsap";
import { ScrollTrigger } from "gsap/ScrollTrigger";
import { Check, CloudSun, KeyRound, Waves } from "lucide-react";

gsap.registerPlugin(ScrollTrigger);

const moments = [
  {
    step: "01",
    kicker: "Morning breeze",
    title: "Find the right stay.",
    copy: "Beachside room, hillside villa, or Kingston hideaway - search by location, mood, and host quality.",
    icon: CloudSun,
  },
  {
    step: "02",
    kicker: "Golden hour",
    title: "Book with clarity.",
    copy: "Clear details, fair prices, and secure checkout in a few easy taps.",
    icon: Waves,
  },
  {
    step: "03",
    kicker: "Afterglow",
    title: "Reach and settle in.",
    copy: "Hosts keep the details ready, check-in stays smooth, and the stay feels personal.",
    icon: KeyRound,
  },
];

export default function ScrollStory() {
  const sectionRef = useRef<HTMLElement>(null);
  const sceneRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const media = gsap.matchMedia();
    let timeline: gsap.core.Timeline | undefined;

    const motionQuery = window.matchMedia(
      "(min-width: 861px) and (prefers-reduced-motion: no-preference)",
    );
    const connection = (
      navigator as Navigator & {
        connection?: { effectiveType?: string; saveData?: boolean };
      }
    ).connection;
    const constrainedConnection =
      connection?.saveData === true ||
      connection?.effectiveType === "slow-2g" ||
      connection?.effectiveType === "2g";
    const useStaticLayout = !motionQuery.matches || constrainedConnection;
    const scene = sceneRef.current;
    if (scene && useStaticLayout) {
      scene.classList.add("story-scene--static");
    }

    media.add("(min-width: 861px) and (prefers-reduced-motion: no-preference)", () => {
      const section = sectionRef.current;
      const scene = sceneRef.current;
      if (!section || !scene || constrainedConnection) return;

      const panels = gsap.utils.toArray<HTMLElement>(".story-panel", section);
      timeline = gsap.timeline({
        scrollTrigger: {
          trigger: section,
          start: "top top",
          end: () => {
            const isTablet = window.innerWidth < 1200;
            const distance = isTablet
              ? Math.max(1200, Math.round(window.innerHeight * 1.35))
              : Math.max(1600, Math.round(window.innerHeight * 1.9));
            return `+=${distance}`;
          },
          pin: true,
          scrub: 1.1,
          anticipatePin: 1,
          invalidateOnRefresh: true,
          onEnter: () => scene.classList.add("story-scene--active"),
          onEnterBack: () => scene.classList.add("story-scene--active"),
          onLeave: () => scene.classList.remove("story-scene--active"),
          onLeaveBack: () => scene.classList.remove("story-scene--active"),
        },
      });

      timeline
        .to(scene, {
          "--sunset-mix": 1,
          "--photo-scale": () => (window.innerWidth < 1200 ? 1.02 : 1.035),
          "--photo-y": () => (window.innerWidth < 1200 ? "-1.5%" : "-3%"),
          duration: 2.4,
          ease: "none",
        }, 0)
        .to(panels[0], { opacity: 0, y: -50, duration: 0.4, ease: "power1.out" }, 0.5)
        .fromTo(
          panels[1],
          { opacity: 0, y: 70 },
          { opacity: 1, y: 0, duration: 0.45, ease: "power1.out" },
          0.72,
        )
        .to(panels[1], { opacity: 0, y: -50, duration: 0.4, ease: "power1.out" }, 1.48)
        .fromTo(
          panels[2],
          { opacity: 0, y: 70 },
          { opacity: 1, y: 0, duration: 0.45, ease: "power1.out" },
          1.7,
        );
    });

    return () => {
      // ScrollTrigger pins the scene by inserting a spacer into the document.
      // Explicitly kill the timeline before reverting matchMedia so the spacer
      // and all inline transforms are removed when navigating away from home.
      timeline?.scrollTrigger?.kill();
      timeline?.kill();
      scene?.classList.remove("story-scene--active", "story-scene--static");
      media.revert();
    };
  }, []);

  return (
    <section className="story-section" id="experience" ref={sectionRef}>
      <div
        className="story-scene"
        ref={sceneRef}
        style={{
          "--sunset-mix": 0,
          "--photo-scale": 1,
          "--photo-y": "0%",
        } as CSSProperties}
      >
        <div className="story-photo-stage" aria-hidden="true">
          <picture className="story-photo story-photo--day">
            <source
              type="image/avif"
              srcSet="/assets/landing/jamaica-coast-day-sm.avif 960w, /assets/landing/jamaica-coast-day.avif 1672w"
              sizes="100vw"
            />
            <source
              type="image/webp"
              srcSet="/assets/landing/jamaica-coast-day-sm.webp 960w, /assets/landing/jamaica-coast-day.webp 1672w"
              sizes="100vw"
            />
            <img
              src="/assets/landing/jamaica-coast-day.png"
              alt=""
              loading="lazy"
              decoding="async"
              fetchPriority="low"
              width="1672"
              height="941"
            />
          </picture>
          <picture className="story-photo story-photo--sunset">
            <source
              type="image/avif"
              srcSet="/assets/landing/jamaica-coast-sunset-sm.avif 960w, /assets/landing/jamaica-coast-sunset.avif 1672w"
              sizes="100vw"
            />
            <source
              type="image/webp"
              srcSet="/assets/landing/jamaica-coast-sunset-sm.webp 960w, /assets/landing/jamaica-coast-sunset.webp 1672w"
              sizes="100vw"
            />
            <img
              src="/assets/landing/jamaica-coast-sunset.png"
              alt=""
              loading="lazy"
              decoding="async"
              fetchPriority="low"
              width="1672"
              height="941"
            />
          </picture>
          <div className="story-photo-shade" />
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
