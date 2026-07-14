import { motion, useMotionValue, useSpring, useTransform } from "framer-motion";
import {
  BadgeCheck,
  CalendarSync,
  ChartNoAxesCombined,
  CreditCard,
  MousePointerClick,
  Star,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import type { MouseEvent } from "react";

const features: {
  title: string;
  copy: string;
  icon: LucideIcon;
  tone: string;
}[] = [
  {
    title: "Verified stays",
    copy: "Real homes and hosts checked so yuh can book with confidence.",
    icon: BadgeCheck,
    tone: "mint",
  },
  {
    title: "Easy booking",
    copy: "Search, compare, and lock in the stay without the hassle.",
    icon: MousePointerClick,
    tone: "sun",
  },
  {
    title: "Secure payments",
    copy: "Pay safe, see the terms clear, and keep everything in one place.",
    icon: CreditCard,
    tone: "coral",
  },
  {
    title: "Host dashboard",
    copy: "Bookings, earnings, and messages in one calm spot.",
    icon: ChartNoAxesCombined,
    tone: "sky",
  },
  {
    title: "Calendar sync",
    copy: "Keep every listing and date lined up, no mix-up.",
    icon: CalendarSync,
    tone: "lime",
  },
  {
    title: "Guest reviews",
    copy: "Real talk from people who actually stayed.",
    icon: Star,
    tone: "sand",
  },
];

function FeatureCard({
  feature,
  index,
}: {
  feature: (typeof features)[number];
  index: number;
}) {
  const rotateXValue = useMotionValue(0);
  const rotateYValue = useMotionValue(0);
  const rotateX = useSpring(rotateXValue, { stiffness: 180, damping: 20 });
  const rotateY = useSpring(rotateYValue, { stiffness: 180, damping: 20 });
  const shineX = useTransform(rotateY, [-8, 8], ["20%", "80%"]);

  const onMove = (event: MouseEvent<HTMLElement>) => {
    const rect = event.currentTarget.getBoundingClientRect();
    const px = (event.clientX - rect.left) / rect.width;
    const py = (event.clientY - rect.top) / rect.height;
    rotateYValue.set((px - 0.5) * 12);
    rotateXValue.set((0.5 - py) * 12);
  };

  const reset = () => {
    rotateXValue.set(0);
    rotateYValue.set(0);
  };

  return (
    <motion.article
      className={`feature-card feature-card--${feature.tone}`}
      initial={{ opacity: 0, y: 45 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-10%" }}
      transition={{ duration: 0.65, delay: index * 0.06 }}
      style={{ rotateX, rotateY, transformPerspective: 900 }}
      onMouseMove={onMove}
      onMouseLeave={reset}
    >
      <motion.span className="card-shine" style={{ left: shineX }} />
      <span className="feature-number">0{index + 1}</span>
      <span className="feature-icon">
        <feature.icon size={24} strokeWidth={1.8} />
      </span>
      <div>
        <h3>{feature.title}</h3>
        <p>{feature.copy}</p>
      </div>
    </motion.article>
  );
}

export default function FeatureCards() {
  return (
    <section className="feature-section section-pad">
      <div className="section-heading">
        <div>
          <div className="section-tag">
            <span />
            Easy living, island style
          </div>
          <h2>All the details sorted.</h2>
        </div>
        <p>
          Less runaround. More good vibes. Nesty Stay brings the whole trip into
          one simple flow.
        </p>
      </div>
      <div className="feature-grid">
        {features.map((feature, index) => (
          <FeatureCard key={feature.title} feature={feature} index={index} />
        ))}
      </div>
    </section>
  );
}
