import { lazy, Suspense, useEffect, useRef, useState, type ComponentType } from "react";
import Hero3D from "../../components/landing/Hero3D";

const ScrollStory = lazy(() => import("../../components/landing/ScrollStory"));
const FeatureCards = lazy(() => import("../../components/landing/FeatureCards"));
const PropertyShowcase = lazy(() => import("../../components/landing/PropertyShowcase"));
const HowItWorks = lazy(() => import("../../components/landing/HowItWorks"));
const TrustSection = lazy(() => import("../../components/landing/TrustSection"));
const FinalCTA = lazy(() => import("../../components/landing/FinalCTA"));

function DeferredLandingSection({
  section: Section,
  minHeight,
}: {
  section: ComponentType;
  minHeight: string;
}) {
  const [ready, setReady] = useState(false);
  const placeholderRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const node = placeholderRef.current;
    if (!node) return;
    if (!("IntersectionObserver" in window)) {
      setReady(true);
      return;
    }
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (!entry?.isIntersecting) return;
        setReady(true);
        observer.disconnect();
      },
      { rootMargin: "240px 0px" },
    );
    observer.observe(node);
    return () => observer.disconnect();
  }, []);

  return (
    <div ref={placeholderRef} style={{ minHeight }}>
      {ready ? (
        <Suspense fallback={<div aria-hidden="true" style={{ minHeight }} />}>
          <Section />
        </Suspense>
      ) : null}
    </div>
  );
}

export function LandingHome() {
  return (
    <>
      <Hero3D />
      <DeferredLandingSection section={ScrollStory} minHeight="100svh" />
      <DeferredLandingSection section={FeatureCards} minHeight="28rem" />
      <DeferredLandingSection section={PropertyShowcase} minHeight="30rem" />
      <DeferredLandingSection section={HowItWorks} minHeight="24rem" />
      <DeferredLandingSection section={TrustSection} minHeight="24rem" />
      <DeferredLandingSection section={FinalCTA} minHeight="30rem" />
    </>
  );
}
