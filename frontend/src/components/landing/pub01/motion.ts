import { useEffect, type RefObject } from "react";

/**
 * PUB-01 motion runtime — strict progressive enhancement.
 *
 * Adds the `ns-js` class to the landing root (unlocking CSS reveal states),
 * drives IntersectionObserver reveals for `.ns-rv` elements, and runs the
 * subtle translateY parallax for `[data-depth]` elements.
 *
 * Without JS or under prefers-reduced-motion the page renders complete and
 * static: hidden initial states only exist under `.ns-js` in CSS, and this
 * hook exits before touching anything when reduced motion is requested.
 * Parallax is additionally disabled at mobile widths (≤700px).
 */
export function useLandingMotion(rootRef: RefObject<HTMLElement | null>) {
  useEffect(() => {
    const root = rootRef.current;
    if (!root) return;
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    root.classList.add("ns-js");

    // ---- scroll reveals ----
    const io = new IntersectionObserver(
      (entries) =>
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add("ns-on");
            io.unobserve(entry.target);
          }
        }),
      { threshold: 0.15 },
    );

    const observeAll = () => {
      root.querySelectorAll<HTMLElement>(".ns-rv").forEach((el) => {
        if (el.classList.contains("ns-on")) return;
        const rect = el.getBoundingClientRect();
        if (rect.top < window.innerHeight * 0.9 && rect.bottom > 0) {
          el.classList.add("ns-on");
          return;
        }
        io.observe(el);
      });
    };

    observeAll();
    const rafId = requestAnimationFrame(observeAll);
    const t1 = window.setTimeout(observeAll, 600);
    const t2 = window.setTimeout(observeAll, 2000);
    window.addEventListener("load", observeAll);

    // Safety net: reveal anything at or above the viewport threshold on scroll
    const onScroll = () => {
      root.querySelectorAll<HTMLElement>(".ns-rv:not(.ns-on)").forEach((el) => {
        if (el.getBoundingClientRect().top < window.innerHeight * 0.88) {
          el.classList.add("ns-on");
        }
      });
    };
    window.addEventListener("scroll", onScroll, { passive: true });
    onScroll();

    // ---- parallax: single rAF loop, translateY only, additive to layout ----
    type ParallaxItem = { el: HTMLElement; base: string; top: number; h: number; depth: number };
    const px: { items: ParallaxItem[]; raf: number } = { items: [], raf: 0 };

    const measure = () => {
      if (window.innerWidth <= 700) {
        px.items.forEach((item) => {
          item.el.style.transform = item.base;
        });
        px.items = [];
        return;
      }
      px.items = Array.from(root.querySelectorAll<HTMLElement>("[data-depth]")).map((el) => {
        if (el.dataset.pbase === undefined) el.dataset.pbase = el.style.transform || "";
        const prev = el.style.transform;
        el.style.transform = el.dataset.pbase;
        const rect = el.getBoundingClientRect();
        el.style.transform = prev;
        return {
          el,
          base: el.dataset.pbase,
          top: rect.top + window.scrollY,
          h: rect.height,
          depth: Number.parseFloat(el.dataset.depth ?? "") || 0.5,
        };
      });
    };

    const frame = () => {
      px.raf = 0;
      const vh = window.innerHeight;
      const sy = window.scrollY;
      for (const item of px.items) {
        const rel = Math.max(-1, Math.min(1, (item.top + item.h / 2 - sy - vh / 2) / (vh / 2 + item.h / 2)));
        const ty = rel * 60 * (1 - item.depth);
        item.el.style.transform = `${item.base ? `${item.base} ` : ""}translateY(${ty.toFixed(1)}px)`;
      }
    };

    const tick = () => {
      if (!px.raf) px.raf = requestAnimationFrame(frame);
    };
    const remeasure = () => {
      measure();
      tick();
    };

    measure();
    frame();
    window.addEventListener("scroll", tick, { passive: true });
    window.addEventListener("resize", remeasure);
    const t3 = window.setTimeout(remeasure, 800);
    window.addEventListener("load", remeasure);

    return () => {
      root.classList.remove("ns-js");
      io.disconnect();
      cancelAnimationFrame(rafId);
      window.clearTimeout(t1);
      window.clearTimeout(t2);
      window.clearTimeout(t3);
      window.removeEventListener("load", observeAll);
      window.removeEventListener("scroll", onScroll);
      window.removeEventListener("scroll", tick);
      window.removeEventListener("resize", remeasure);
      window.removeEventListener("load", remeasure);
      if (px.raf) cancelAnimationFrame(px.raf);
      px.items.forEach((item) => {
        item.el.style.transform = item.base;
      });
    };
  }, [rootRef]);
}
