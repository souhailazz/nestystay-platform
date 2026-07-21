import { type HTMLAttributes, type MouseEvent, useRef } from "react";
import { motion, useMotionValue, useSpring, useTransform } from "framer-motion";
import { cx } from "../../lib/ui";

export function Card({ className, children, ...props }: HTMLAttributes<HTMLElement>) {
  const ref = useRef<HTMLElement>(null);
  const rotateXValue = useMotionValue(0);
  const rotateYValue = useMotionValue(0);
  const rotateX = useSpring(rotateXValue, { stiffness: 200, damping: 22 });
  const rotateY = useSpring(rotateYValue, { stiffness: 200, damping: 22 });
  const shineX = useTransform(rotateY, [-6, 6], ["15%", "85%"]);

  const onMove = (event: MouseEvent<HTMLElement>) => {
    const el = ref.current;
    if (!el) return;
    const rect = el.getBoundingClientRect();
    const px = (event.clientX - rect.left) / rect.width;
    const py = (event.clientY - rect.top) / rect.height;
    rotateYValue.set((px - 0.5) * 8);
    rotateXValue.set((0.5 - py) * 8);
  };

  const onLeave = () => {
    rotateXValue.set(0);
    rotateYValue.set(0);
  };

  return (
    <motion.article
      ref={ref}
      className={cx("ui-card", className)}
      style={{ rotateX, rotateY, transformPerspective: 1000 }}
      onMouseMove={onMove}
      onMouseLeave={onLeave}
      {...(props as Record<string, unknown>)}
    >
      <motion.span className="card-shine" style={{ left: shineX }} />
      {children}
    </motion.article>
  );
}
