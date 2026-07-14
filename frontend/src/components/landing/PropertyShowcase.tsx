import { motion, useMotionValue, useSpring } from "framer-motion";
import { ArrowUpRight, Heart, MapPin } from "lucide-react";
import type { MouseEvent } from "react";
import { AppLink } from "../AppLink";
import { useProperties } from "../../hooks/useProperties";
import { formatMoney, type PropertyListing } from "../../lib/api";
import { getStayImage } from "../../lib/stayImages";

function PropertyArt({ index, title }: { index: number; title: string }) {
  const image = getStayImage(index);
  return (
    <div className="property-art property-art--image">
      <img className="generated-stay-image" src={image.src} alt={`${title}: ${image.alt}`} loading="lazy" />
    </div>
  );
}

function PropertyCard({
  property,
  index,
}: {
  property: PropertyListing;
  index: number;
}) {
  const x = useMotionValue(0);
  const y = useMotionValue(0);
  const rotateX = useSpring(y, { stiffness: 150, damping: 18 });
  const rotateY = useSpring(x, { stiffness: 150, damping: 18 });

  const handleMove = (event: MouseEvent<HTMLElement>) => {
    const rect = event.currentTarget.getBoundingClientRect();
    const px = (event.clientX - rect.left) / rect.width - 0.5;
    const py = (event.clientY - rect.top) / rect.height - 0.5;
    x.set(px * 9);
    y.set(py * -9);
  };

  return (
    <motion.article
      className="property-card"
      initial={{ opacity: 0, y: 60 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-8%" }}
      transition={{ duration: 0.75, delay: index * 0.1 }}
      style={{ rotateX, rotateY, transformPerspective: 1200 }}
      onMouseMove={handleMove}
      onMouseLeave={() => {
        x.set(0);
        y.set(0);
      }}
    >
      <div className="property-visual">
        <PropertyArt index={index} title={property.title} />
        <span className="property-tag">{property.badgeLevel} host</span>
        <button type="button" className="heart-button" aria-label={`Save ${property.title}`}>
          <Heart size={18} />
        </button>
      </div>
      <div className="property-info">
        <div className="property-rating">{property.guestVerificationEnabled ? "Verified" : "Fast"}</div>
        <h3>{property.title}</h3>
        <p>
          <MapPin size={14} /> {property.location}, {property.country}
        </p>
        <div className="property-bottom">
          <span>
            <strong>{formatMoney(property.nightlyRate, property.currency)}</strong> / night
          </span>
          <AppLink className="property-arrow" href={`/properties/${property.id}`} aria-label={`View ${property.title}`}>
            <ArrowUpRight size={18} />
          </AppLink>
        </div>
      </div>
    </motion.article>
  );
}

export default function PropertyShowcase() {
  const { properties, isLoading, error } = useProperties();
  const visibleProperties = properties.slice(0, 3);

  return (
    <section className="property-section section-pad" id="stays">
      <div className="property-orb property-orb--one" aria-hidden="true" />
      <div className="property-orb property-orb--two" aria-hidden="true" />
      <div className="section-heading section-heading--light">
        <div>
          <div className="section-tag section-tag--light">
            <span />
            Handpicked Jamaican stays
          </div>
          <h2>Places with real vibes.</h2>
        </div>
        <div className="heading-side">
          <p>
            A live collection of homes from the backend property API, styled
            with the same Nesty Stay 3D card language.
          </p>
          <AppLink href="/explore">
            View all stays <ArrowUpRight size={17} />
          </AppLink>
        </div>
      </div>
      {isLoading && <div className="property-preview-state">Loading API stays...</div>}
      {error && <div className="property-preview-state">Property API unavailable: {error}</div>}
      <div className="property-grid">
        {visibleProperties.map((property, index) => (
          <PropertyCard key={property.id} property={property} index={index} />
        ))}
      </div>
    </section>
  );
}
