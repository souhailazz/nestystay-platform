import { useState, type FormEvent } from "react";
import { navigate } from "../../AppLink";
import { PillLink } from "../../design/PillButton";
import { PatoisBlock } from "../../design/PatoisBlock";
import { heroFallbackImages } from "./fallbackImages";

/**
 * 01 · GOLDEN HOUR — hero. Line-by-line title reveal, floating featured card
 * with micro-badges, and the "Weh Yuh Deh?" search bar submitting to
 * /explore?query=…
 */
export function GoldenHourHero() {
  const [query, setQuery] = useState("");

  const onSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    navigate(`/explore?query=${encodeURIComponent(query.trim())}`);
  };

  return (
    <header className="ns-hero">
      <img
        className="ns-hero__bg"
        src={heroFallbackImages.valley.src}
        alt={heroFallbackImages.valley.alt}
        data-depth="0.3"
      />
      <div className="ns-hero__scrim" />
      <div className="ns-hero__inner">
        <div className="ns-hero__copy">
          <div className="ns-eyebrow">01 · GOLDEN HOUR</div>
          <h1 className="ns-display ns-hero__title">
            {/* anim: line-by-line reveal on load, 0.8s ease-out, stagger 150ms */}
            <span className="ns-line" style={{ "--d": "0s" } as React.CSSProperties}>
              Touch down.
            </span>
            <br />
            <span className="ns-line" style={{ "--d": "0.15s" } as React.CSSProperties}>
              Slow down.
            </span>
            <br />
            <span className="ns-line" style={{ "--d": "0.3s" } as React.CSSProperties}>
              Stay <em>golden.</em>
            </span>
          </h1>
          <p className="ns-hero__lede">
            Jamaica's own trusted stays platform — verified hosts, wellness security visits, and doors that open
            like they know you.
          </p>
          <form className="ns-hero__search" onSubmit={onSearch} role="search">
            <PatoisBlock phrase="Weh Yuh Deh?" translation="Where are you going?" tone="dark" size={22} />
            <div className="ns-hero__searchbar">
              <input
                type="text"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder="Search parishes, towns, stays…"
                aria-label="Where are you going?"
              />
              <button type="submit" aria-label="Search stays">
                <span className="ns-arrow" aria-hidden="true">
                  →
                </span>
              </button>
            </div>
          </form>
          <div className="ns-hero__ctas">
            <PillLink variant="sun" href="/explore" arrow>
              Explore stays
            </PillLink>
            <PillLink variant="outline" href="/register">
              Become a host
            </PillLink>
          </div>
          <div className="ns-hero__proof">
            {/* decorative avatar strip — the caption carries the meaning */}
            <div className="ns-hero__avatars" aria-hidden="true">
              {heroFallbackImages.travelers.map((src) => (
                <img key={src} src={src} alt="" />
              ))}
            </div>
            <div className="ns-hero__rating">
              <span className="ns-stars">★★★★★</span> &nbsp;Loved by 2,000+ island travelers
            </div>
          </div>
        </div>

        {/* floating featured card */}
        <div className="ns-hero__cardwrap" data-depth="0.85">
          {/* anim: float 6s ease-in-out infinite, translateY ±8px */}
          <div className="ns-hero__card ns-breathe">
            <div className="ns-hero__cardimg">
              <img src={heroFallbackImages.featured.src} alt={heroFallbackImages.featured.alt} />
              <span className="ns-hero__cardtag">FEATURED STAY</span>
            </div>
            <div className="ns-hero__cardbody">
              <div>
                <div className="ns-hero__cardname">Cliffside Retreat</div>
                <div className="ns-hero__cardloc">Negril · Westmoreland parish</div>
              </div>
              <div className="ns-hero__cardrating">★ 5.0</div>
            </div>
          </div>
          {/* anim: float, phase +0.9s */}
          <span className="ns-hero__chip-dates ns-b1" data-depth="0.8">
            <span className="ns-chip">Dec 12 – 18</span>
          </span>
          {/* anim: float, phase +1.8s */}
          <span className="ns-hero__chip-verified ns-b2" data-depth="0.9">
            <span className="ns-chip">✓ Verified host</span>
          </span>
        </div>
      </div>
      <div className="ns-hero__scrollcue" aria-hidden="true">
        <span>SCROLL</span>
        <span />
      </div>
    </header>
  );
}
