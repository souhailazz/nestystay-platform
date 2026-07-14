import type { AnchorHTMLAttributes, ButtonHTMLAttributes, ReactNode } from "react";
import { cx } from "../../lib/ui";

type Variant = "sun" | "glass" | "dark" | "outline" | "ghost";

export function buttonClassName(variant: Variant = "sun", className?: string) {
  return cx("ui-button", `ui-button--${variant}`, className);
}

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: Variant;
};

export function Button({ className, variant = "sun", type = "button", ...props }: ButtonProps) {
  return <button className={buttonClassName(variant, className)} type={type} {...props} />;
}

type ButtonLinkProps = AnchorHTMLAttributes<HTMLAnchorElement> & {
  children: ReactNode;
  variant?: Variant;
};

export function ButtonLink({ className, variant = "sun", ...props }: ButtonLinkProps) {
  return <a className={buttonClassName(variant, className)} {...props} />;
}
