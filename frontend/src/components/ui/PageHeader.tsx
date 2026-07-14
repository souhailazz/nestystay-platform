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
      <div>
        <span className="product-eyebrow">{eyebrow}</span>
        <h1>{title}</h1>
        {copy && <p>{copy}</p>}
      </div>
      {actions && <div className="product-hero__actions">{actions}</div>}
    </section>
  );
}
