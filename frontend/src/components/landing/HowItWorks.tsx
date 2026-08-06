import { motion } from "framer-motion";
import { ArrowDown, CalendarCheck2, Search, Sparkles } from "lucide-react";

const steps = [
  {
    number: "01",
    title: "Search",
    copy: "Tell us the parish, the dates, and the kind of vibe yuh want.",
    icon: Search,
  },
  {
    number: "02",
    title: "Book it",
    copy: "Check the details, pay safe, and get confirmation right away.",
    icon: CalendarCheck2,
  },
  {
    number: "03",
    title: "Settle in",
    copy: "Drop yuh bags, take it easy, and enjoy the place.",
    icon: Sparkles,
  },
];

export default function HowItWorks() {
  return (
    <section className="how-section section-pad" id="how-it-works">
      <div className="how-heading">
        <div className="section-tag">
          <span />
          Three easy steps
        </div>
        <h2>From maybe to booked.</h2>
        <p>
          Your next good stay in Jamaica is close. No long talk - just pick the
          place and go.
        </p>
      </div>

      <div className="steps">
        {steps.map(({ number, title, copy, icon: Icon }, index) => (
          <motion.article
            className="step"
            key={number}
            initial={{ opacity: 0, y: 50 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-12%" }}
            transition={{ duration: 0.7, delay: index * 0.12 }}
          >
            <span className="step-number">{number}</span>
            <motion.div
              className="step-icon"
              whileInView={{ rotateY: [0, 180, 360] }}
              viewport={{ once: true }}
              transition={{ duration: 1, delay: 0.25 + index * 0.12 }}
            >
              <Icon size={28} />
              <span />
            </motion.div>
            <h3>{title}</h3>
            <p>{copy}</p>
            {index < steps.length - 1 && (
              <div className="step-connector" aria-hidden="true">
                <span />
                <ArrowDown size={15} />
              </div>
            )}
          </motion.article>
        ))}
      </div>
    </section>
  );
}
