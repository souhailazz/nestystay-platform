import type { AnchorHTMLAttributes, MouseEvent } from "react";

export function navigate(path: string) {
  window.history.pushState({}, "", path);
  window.dispatchEvent(new PopStateEvent("popstate"));
  window.scrollTo({ top: 0, behavior: "smooth" });
}

export function AppLink({ href = "/", onClick, ...props }: AnchorHTMLAttributes<HTMLAnchorElement>) {
  const handleClick = (event: MouseEvent<HTMLAnchorElement>) => {
    onClick?.(event);
    if (
      event.defaultPrevented ||
      event.metaKey ||
      event.ctrlKey ||
      event.shiftKey ||
      event.altKey ||
      props.target ||
      href.startsWith("#") ||
      href.startsWith("http")
    ) {
      return;
    }

    event.preventDefault();
    navigate(href);
  };

  return <a href={href} onClick={handleClick} {...props} />;
}
