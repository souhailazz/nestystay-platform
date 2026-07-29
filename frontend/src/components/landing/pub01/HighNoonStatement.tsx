/**
 * 03 · HIGH NOON — full-yellow statement section. Deep-on-yellow is the one
 * allowed inversion (Design System v2).
 */
export function HighNoonStatement() {
  return (
    <section className="ns-noon">
      <div className="ns-noon__inner">
        <div className="ns-eyebrow">03 · HIGH NOON</div>
        {/* anim: reveal on viewport entry, 0.7s ease-out */}
        <h2 className="ns-display ns-noon__title ns-rv">
          Trust, said in <em>broad daylight.</em>
        </h2>
        <p className="ns-noon__lede">
          Every host vetted, every visit logged, every message on the record. No fine print, no favors — just the
          island holding its own standard.
        </p>
      </div>
    </section>
  );
}
