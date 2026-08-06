import { motion } from "framer-motion";
import { BadgeCheck, Headphones, ReceiptText, ShieldCheck } from "lucide-react";

const badges = [
  { title: "Verified hosts", icon: BadgeCheck },
  { title: "Safe booking", icon: ShieldCheck },
  { title: "Clear pricing", icon: ReceiptText },
  { title: "Support when yuh need it", icon: Headphones },
];

export default function TrustSection() {
  return (
    <section className="trust-section section-pad" id="trust">
      <div className="trust-backdrop" aria-hidden="true">
        <span />
        <span />
        <span />
      </div>
      <div className="trust-copy">
        <div className="section-tag">
          <span />
          Travel light
        </div>
        <h2>Good stays. Good people. No funny business.</h2>
        <p>
          Nesty Stay is built on trust, so guests can relax and hosts can do
          good business.
        </p>
      </div>
      <div className="trust-badges">
        {badges.map(({ title, icon: Icon }, index) => (
          <motion.div
            className="trust-badge"
            key={title}
            initial={{ opacity: 0, scale: 0.9 }}
            whileInView={{ opacity: 1, scale: 1 }}
            viewport={{ once: true }}
            transition={{ duration: 0.55, delay: index * 0.08 }}
          >
            <span>
              <Icon size={23} />
            </span>
            <strong>{title}</strong>
          </motion.div>
        ))}
      </div>
    </section>
  );
}
