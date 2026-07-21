import { useRef } from "react";
import { motion, useReducedMotion, useScroll, useTransform } from "framer-motion";
import { BadgeCheck, CalendarDays, CreditCard, KeyRound, MapPin, ShieldCheck, Star } from "lucide-react";
import { AppLink } from "../AppLink";
import { buttonClassName } from "../ui/Button";

const steps = [
  {
    icon: MapPin,
    label: "Discover",
    title: "Pick a verified Caribbean stay.",
    copy: "Search across Jamaica-style villas, hillside studios, and beach retreats with host trust signals visible from the start.",
  },
  {
    icon: ShieldCheck,
    label: "Verify",
    title: "Confirm the guest and the host.",
    copy: "Badge rules, identity checks, and booking holds make the path from interest to approval clear.",
  },
  {
    icon: CreditCard,
    label: "Book",
    title: "Authorize payment, then capture safely.",
    copy: "The booking stays pending until approval, then payment capture and trip status stay connected in the dashboard.",
  },
];

export default function LandingScroll3D() {
  const sectionRef = useRef<HTMLElement>(null);
  const reduceMotion = useReducedMotion();
  const { scrollYProgress } = useScroll({
    target: sectionRef,
    offset: ["start end", "end start"],
  });

  const sceneY = useTransform(scrollYProgress, [0, 0.5, 1], reduceMotion ? [0, 0, 0] : [70, -18, -82]);
  const sceneRotateX = useTransform(scrollYProgress, [0, 0.45, 1], reduceMotion ? [0, 0, 0] : [12, 0, -8]);
  const sceneRotateY = useTransform(scrollYProgress, [0, 0.5, 1], reduceMotion ? [0, 0, 0] : [-18, 5, 18]);
  const photoRotate = useTransform(scrollYProgress, [0, 1], reduceMotion ? [0, 0] : [-4, 4]);
  const trustY = useTransform(scrollYProgress, [0, 0.6, 1], reduceMotion ? [0, 0, 0] : [90, 12, -40]);
  const bookingY = useTransform(scrollYProgress, [0, 0.45, 1], reduceMotion ? [0, 0, 0] : [120, 0, -70]);
  const progressScale = useTransform(scrollYProgress, [0.14, 0.86], [0, 1]);

  return (
    <section className="reference-scroll3d" id="experience" ref={sectionRef}>
      <div className="reference-scroll3d__copy">
        <small>Live booking motion</small>
        <h2>Watch the trip flow come together.</h2>
        <p>
          A premium booking path that lifts search, verification, and checkout into one calm Caribbean journey.
        </p>
      </div>

      <div className="reference-scroll3d__sticky" aria-label="Animated booking preview">
        <motion.div className="reference-scroll3d__progress" style={{ scaleX: progressScale }} />
        <motion.div
          className="reference-scroll3d__stage"
          style={{
            rotateX: sceneRotateX,
            rotateY: sceneRotateY,
            y: sceneY,
          }}
        >
          <div className="reference-scroll3d__base" aria-hidden="true" />

          <motion.div className="reference-scroll3d__photo" style={{ rotateZ: photoRotate }}>
            <img src="/assets/reference/property-1.jpg" alt="Oceanfront Jamaican villa preview" />
            <span>
              <Star size={14} fill="currentColor" /> 4.96 verified stay
            </span>
          </motion.div>

          <motion.div className="reference-scroll3d__trust-card" style={{ y: trustY }}>
            <BadgeCheck size={22} />
            <div>
              <strong>Trusted host</strong>
              <span>Badge active, eKYC ready</span>
            </div>
          </motion.div>

          <motion.div className="reference-scroll3d__booking-card" style={{ y: bookingY }}>
            <div className="reference-scroll3d__booking-head">
              <span>
                <CalendarDays size={18} />
                Booking hold
              </span>
              <b>$850</b>
            </div>
            <div className="reference-scroll3d__booking-grid">
              <span>Montego Bay</span>
              <span>4 nights</span>
              <span>Manual capture</span>
              <span>Approved flow</span>
            </div>
            <AppLink className={buttonClassName("sun")} href="/explore">
              Explore stays
            </AppLink>
          </motion.div>

          <div className="reference-scroll3d__key-card">
            <KeyRound size={20} />
            <span>Secure check-in details unlocked after approval</span>
          </div>
        </motion.div>
      </div>

      <div className="reference-scroll3d__steps">
        {steps.map(({ icon: Icon, label, title, copy }, index) => (
          <motion.article
            key={label}
            initial={{ opacity: 0, y: 24 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-80px" }}
            transition={{ duration: 0.45, delay: index * 0.1 }}
          >
            <span>
              <Icon size={18} />
              {label}
            </span>
            <h3>{title}</h3>
            <p>{copy}</p>
          </motion.article>
        ))}
      </div>
    </section>
  );
}
