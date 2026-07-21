import { motion } from "framer-motion";
import type { ReactNode } from "react";

export function PageHeader({
  eyebrow,
  title,
  copy,
  actions,
}: {
  eyebrow: string;
  title: string;
  copy?: string;
  actions?: ReactNode;
}) {
  return (
    <section className="product-hero">
      <div className="page-hero-grid" aria-hidden="true" />
      <div className="page-hero-orb page-hero-orb--1" aria-hidden="true" />
      <div className="page-hero-orb page-hero-orb--2" aria-hidden="true" />
      <div className="page-hero-orb page-hero-orb--3" aria-hidden="true" />

      <div>
        <motion.span
          className="product-eyebrow"
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6 }}
        >
          {eyebrow}
        </motion.span>
        <motion.h1
          initial={{ opacity: 0, y: 22 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.7, delay: 0.1 }}
        >
          {title}
        </motion.h1>
        {copy && (
          <motion.p
            initial={{ opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.2 }}
          >
            {copy}
          </motion.p>
        )}
      </div>
      {actions && (
        <motion.div
          className="product-hero__actions"
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6, delay: 0.3 }}
        >
          {actions}
        </motion.div>
      )}
    </section>
  );
}
