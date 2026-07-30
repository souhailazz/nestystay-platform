import { useCallback, useEffect, useState } from "react";
import { AppLink } from "../../AppLink";
import { PillLink } from "../../design/PillButton";
import { PatoisBlock } from "../../design/PatoisBlock";
import { api, formatMoney, type PropertyListing } from "../../../lib/api";
import { showcaseImage } from "./fallbackImages";

function tierChip(badgeLevel: string) {
  const level = badgeLevel.toLowerCase();
  if (level.includes("wellness")) return { className: "ns-tier-chip--wellness", label: "✦ WELLNESS HOST" };
  if (level.includes("trusted")) return { className: "ns-tier-chip--trusted", label: "★ TRUSTED" };
  if (level.includes("verified")) return { className: "ns-tier-chip--verified", label: "✓ VERIFIED" };
  return { className: "ns-tier-chip--free", label: "FREE HOST" };
}

function ShowcaseCard({
  property,
  index,
  size,
}: {
  property: PropertyListing;
  index: number;
  size: "large" | "narrow";
}) {
  const image = showcaseImage(index, property.imageUrl, property.title);
  const chip = tierChip(property.badgeLevel);

  return (
    // anim: reveal on viewport entry, stagger 100ms; photo zoom 1.03 on hover
    <article
      className={`ns-photo-card ns-photo-card--${size} ns-rv ns-zoom`}
      style={{ "--d": `${index * 0.1}s` } as React.CSSProperties}
    >
      <img
        className="ns-photo-card__img ns-zoomimg"
        src={image.src}
        alt={image.alt}
        data-depth={size === "large" ? "0.9" : undefined}
      />
      <div className="ns-photo-card__scrim" />
      <span className={`ns-tier-chip ${chip.className}`}>{chip.label}</span>
      <div className="ns-photo-card__info">
        <div>
          <div className="ns-photo-card__name">{property.title}</div>
          <div className="ns-photo-card__loc">
            {property.location} · {property.country}
          </div>
          {size === "narrow" && (
            <div className="ns-photo-card__price">
              <strong>{formatMoney(property.nightlyRate, property.currency)}</strong> <span>/ night</span>
            </div>
          )}
        </div>
        {size === "large" ? (
          <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
            <span className="ns-photo-card__price" style={{ fontSize: 17, marginTop: 0 }}>
              <strong>{formatMoney(property.nightlyRate, property.currency)}</strong> <span>/ night</span>
            </span>
            <AppLink
              className="ns-photo-card__open"
              href={`/properties/${property.id}`}
              aria-label={`Open ${property.title}`}
            >
              →
            </AppLink>
          </div>
        ) : (
          <AppLink
            className="ns-photo-card__open"
            href={`/properties/${property.id}`}
            aria-label={`Open ${property.title}`}
          >
            →
          </AppLink>
        )}
      </div>
    </article>
  );
}

/**
 * 02 · BROAD DAYLIGHT — property showcase fed by the real properties API
 * (same call the Explore page uses), with Tek Time skeletons, error + retry,
 * and empty states. The fourth property becomes the card overlapping the
 * HIGH NOON boundary.
 */
export function DaylightShowcase() {
  const [properties, setProperties] = useState<PropertyListing[]>([]);
  const [status, setStatus] = useState<"loading" | "ready" | "error">("loading");

  const load = useCallback(() => {
    let active = true;
    setStatus("loading");
    api
      .getProperties()
      .then((list) => {
        if (!active) return;
        setProperties(list.filter((property) => !property.isArchived));
        setStatus("ready");
      })
      .catch(() => {
        if (active) setStatus("error");
      });
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => load(), [load]);

  const featured = properties.slice(0, 3);
  const overlap = properties[3];
  const overlapImage = overlap ? showcaseImage(3, overlap.imageUrl, overlap.title) : null;
  const overlapChip = overlap ? tierChip(overlap.badgeLevel) : null;

  return (
    <section className="ns-day">
      <div className="ns-day__inner">
        <div className="ns-eyebrow">02 · BROAD DAYLIGHT</div>
        <div className="ns-day__head">
          <h2 className="ns-display ns-day__title" data-depth="0.92">
            Four parishes,
            <br />
            one <em>yard</em> at a time.
          </h2>
          <PillLink variant="ghost" href="/explore" arrow>
            All stays
          </PillLink>
        </div>

        {status === "loading" && (
          <div className="ns-day__state" aria-busy="true">
            <PatoisBlock
              phrase="Tek Time"
              translation="Take your time - we're loading your island experience."
              tone="light"
              size={22}
            />
            <div className="ns-day__grid">
              <div className="ns-skel-card ns-photo-card--large" />
              <div className="ns-skel-card ns-photo-card--narrow" />
              <div className="ns-skel-card ns-photo-card--narrow" />
            </div>
          </div>
        )}

        {status === "error" && (
          <div className="ns-day__statecopy">
            <div className="ns-day__error">
              <strong>Couldn't load stays right now.</strong>
              <br />
              Check your connection and try again.
            </div>
            <button type="button" className="ns-btn ns-btn--outline-light" onClick={load}>
              ↻ Try again
            </button>
          </div>
        )}

        {status === "ready" && featured.length === 0 && (
          <div className="ns-day__statecopy">
            <p style={{ margin: 0 }}>No stays published yet — the island is getting ready.</p>
            <PillLink variant="primary-light" href="/explore" arrow>
              Explore NestyStay
            </PillLink>
          </div>
        )}

        {status === "ready" && featured.length > 0 && (
          <div className="ns-day__grid">
            {featured.map((property, index) => (
              <ShowcaseCard
                key={property.id}
                property={property}
                index={index}
                size={index === 0 ? "large" : "narrow"}
              />
            ))}
          </div>
        )}

        {/* overlapping card into HIGH NOON */}
        {overlap && overlapImage && overlapChip && (
          <div className="ns-day__overlap">
            <article className="ns-overlap-card">
              <img src={overlapImage.src} alt={overlapImage.alt} />
              <div className="ns-overlap-card__body">
                <span className={`ns-tier-chip ${overlapChip.className}`}>{overlapChip.label}</span>
                <div className="ns-overlap-card__name">{overlap.title}</div>
                <div className="ns-overlap-card__loc">
                  {overlap.location} · {overlap.country}
                </div>
                <div className="ns-overlap-card__price">
                  <strong>{formatMoney(overlap.nightlyRate, overlap.currency)}</strong> <span>/ night</span>
                  <AppLink href={`/properties/${overlap.id}`}>View →</AppLink>
                </div>
              </div>
            </article>
          </div>
        )}
      </div>
    </section>
  );
}
