import { ArrowRight, CalendarDays, Heart, MapPin, Search, ShieldCheck, Sparkles, Star, UsersRound } from "lucide-react";
import { AppLink } from "../AppLink";
import { buttonClassName } from "../ui/Button";
import LandingScroll3D from "./LandingScroll3D";

const trustItems = [
  {
    icon: <ShieldCheck size={25} />,
    title: "Verified Hosts",
    copy: "Every host follows a multi-step trust path before guests commit to a stay.",
  },
  {
    icon: <Sparkles size={25} />,
    title: "Identity Verification",
    copy: "Guest and host verification flows keep booking decisions clear and accountable.",
  },
  {
    icon: <CalendarDays size={25} />,
    title: "Secure Bookings",
    copy: "Date holds, booking status, and payment capture rules are handled through the platform.",
  },
  {
    icon: <UsersRound size={25} />,
    title: "Trusted Community",
    copy: "Badges, founding benefits, and wellness tools support a safer Caribbean stay network.",
  },
];

const featuredStays = [
  {
    image: "/assets/reference/property-1.jpg",
    title: "Azure Horizon Villa",
    location: "Montego Bay, Jamaica",
    rating: "4.96",
    price: "$850",
  },
  {
    image: "/assets/reference/property-2.jpg",
    title: "The Obsidian Point",
    location: "Ocho Rios, Jamaica",
    rating: "5.0",
    price: "$1,250",
  },
  {
    image: "/assets/reference/property-3.jpg",
    title: "Emerald Peak Villa",
    location: "Negril, Jamaica",
    rating: "4.92",
    price: "$620",
  },
];

export default function ReferenceLanding() {
  return (
    <div className="reference-landing">
      <section className="reference-hero">
        <img src="/assets/reference/landing-hero.jpg" alt="Caribbean villa with a pool" />
        <div className="reference-hero__shade" />
        <div className="reference-hero__copy">
          <h1>
            Trusted Caribbean Stays
            <br />
            Start Here
          </h1>
          <p>
            Book verified vacation rentals across Jamaica and the Caribbean with confidence. Experience premium
            living with local trust.
          </p>
          <div className="reference-search" role="search" aria-label="Search stays">
            <div>
              <small>
                <MapPin size={13} /> Location
              </small>
              <span>Where are you going?</span>
            </div>
            <div>
              <small>
                <CalendarDays size={13} /> Dates
              </small>
              <span>Add dates</span>
            </div>
            <div>
              <small>
                <UsersRound size={13} /> Guests
              </small>
              <span>Add guests</span>
            </div>
            <AppLink aria-label="Search stays" href="/explore">
              <Search size={21} />
            </AppLink>
          </div>
        </div>
      </section>

      <LandingScroll3D />

      <section className="reference-trust">
        <div className="reference-heading">
          <h2>Your Safety is Our Priority</h2>
          <p>
            We have built a platform focused on security, professionalism, and the high-trust engineering required
            for premium travel.
          </p>
        </div>
        <div className="reference-trust-grid">
          {trustItems.map((item, index) => (
            <article key={item.title}>
              <span className={index === 0 || index === 3 ? "reference-trust-icon" : "reference-trust-icon is-deep"}>
                {item.icon}
              </span>
              <h3>{item.title}</h3>
              <p>{item.copy}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="reference-featured">
        <div className="reference-featured__head">
          <div>
            <small>Curated excellence</small>
            <h2>Featured Caribbean Retreats</h2>
          </div>
          <AppLink href="/explore">View all destinations</AppLink>
        </div>
        <div className="reference-stay-grid">
          {featuredStays.map((stay) => (
            <article key={stay.title} className="reference-stay-card">
              <div className="reference-stay-card__image">
                <img src={stay.image} alt={stay.title} />
                <span>
                  <Star size={13} fill="currentColor" /> {stay.rating}
                </span>
                <button type="button" aria-label={`Save ${stay.title}`}>
                  <Heart size={19} />
                </button>
              </div>
              <div className="reference-stay-card__body">
                <div>
                  <h3>{stay.title}</h3>
                  <b>Verified</b>
                </div>
                <p>{stay.location}</p>
                <p>4 Beds · 4.5 Baths</p>
                <footer>
                  <strong>{stay.price}</strong> / night
                  <AppLink href="/explore">Details</AppLink>
                </footer>
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className="reference-cta">
        <div className="reference-cta__card">
          <div>
            <h2>
              Ready to list your Caribbean
              <br />
              property?
            </h2>
            <p>
              Join our network of premium hosts and start earning with the region&apos;s most trusted booking
              platform.
            </p>
            <div className="reference-cta__actions">
              <AppLink className={buttonClassName("sun")} href="/host/properties">
                Start Hosting <ArrowRight size={16} />
              </AppLink>
              <AppLink className={buttonClassName("outline")} href="/explore">
                Learn More
              </AppLink>
            </div>
          </div>
          <div className="reference-cta__orb" aria-hidden="true">
            <ShieldCheck size={64} />
          </div>
        </div>
      </section>

      <footer className="reference-footer">
        <div>
          <b>NestyStay</b>
          <p>
            Professionalizing Caribbean vacation rentals
            <br />
            through technology and trust.
          </p>
        </div>
        <nav aria-label="Footer">
          <AppLink href="/explore">Stays</AppLink>
          <AppLink href="/host-dashboard">Hosts</AppLink>
          <AppLink href="/guest-dashboard">Guests</AppLink>
          <AppLink href="/admin">Admin</AppLink>
        </nav>
        <small>© 2026 NestyStay Inc. All rights reserved.</small>
      </footer>
    </div>
  );
}
