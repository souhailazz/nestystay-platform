import type { ReactNode } from "react";
import { AppLink } from "../AppLink";
import { EmblemRoundel } from "./PublicShell";
import { cx } from "../../lib/ui";

/* DS v2 workspace shell — Deep sidebar 230px (emblem + wordmark, 44px nav items
   radius 12, active = yellow tint bg + Yellow text), sand canvas, and the
   nestystay.net · 754-248-2435 footer on every page. */

type NavItem = { label: string; href: string; routes?: string[] };

const travelerItems: NavItem[] = [
  { label: "My trips", href: "/guest-dashboard", routes: ["guest-dashboard"] },
  { label: "Suggestions", href: "/traveler/suggestions", routes: ["trav-suggestions"] },
  { label: "Collections", href: "/traveler/favorites", routes: ["trav-favorites", "no-favorites"] },
  { label: "Pending reviews", href: "/traveler/reviews/pending" },
  { label: "Invoices", href: "/traveler/invoices" },
  { label: "Messages", href: "/messages", routes: ["messages", "document-message"] },
  { label: "Notifications", href: "/traveler/notifications", routes: ["trav-notifications"] },
  { label: "Settings", href: "/profile", routes: ["profile"] },
];

const workspaceItems: NavItem[] = [
  { label: "Host", href: "/host-dashboard", routes: ["host-dashboard", "host-spec", "host-profile", "host-reports"] },
  { label: "Properties", href: "/host/properties", routes: ["property-management", "host-property-edit"] },
  { label: "Reservations", href: "/bookings", routes: ["bookings", "no-reservations"] },
  { label: "Calendar", href: "/calendar", routes: ["calendar"] },
  { label: "Wellness", href: "/host/wellness", routes: ["host-wellness", "officer-wellness", "officer-directory", "wellness-booking"] },
  { label: "Property manager", href: "/pm/gates", routes: ["pm-gates", "pm-utilities", "pm-verification", "pm-reports", "pm-insurance"] },
  { label: "Directories", href: "/directory/trades", routes: ["business-directory", "provider-dashboard", "directory-spec"] },
  { label: "Admin", href: "/admin", routes: ["admin", "admin-kpis", "admin-reports", "officer-id-reset", "admin-ops"] },
];

function isActive(item: NavItem, routeName: string, pathname: string) {
  if (pathname === item.href) return true;
  if (routeName === "traveler-spec") return false; // several paths share this route — match by pathname only
  return Boolean(item.routes?.includes(routeName));
}

function SidebarLink({ item, routeName, pathname }: { item: NavItem; routeName: string; pathname: string }) {
  const active = isActive(item, routeName, pathname);
  return (
    <AppLink
      className={cx(
        "flex min-h-11 items-center rounded-nav px-3 font-sans text-[13px] font-semibold transition-colors",
        active ? "bg-yellow/10 text-yellow" : "text-on-dark-nav hover:bg-on-dark-heading/5 hover:text-on-dark-heading",
      )}
      href={item.href}
    >
      {item.label}
    </AppLink>
  );
}

export function WorkspaceFrame({ routeName, children }: { routeName: string; children: ReactNode }) {
  const pathname = window.location.pathname;
  return (
    <div className="grid min-h-screen font-sans text-[15px] leading-[1.55] text-ink md:grid-cols-[230px_1fr]">
      <aside aria-label="Workspace navigation" className="flex flex-col gap-[3px] bg-deep p-5 px-3.5">
        <AppLink className="flex items-center gap-2 px-2 pb-3.5" href="/">
          <EmblemRoundel size={32} />
          <span className="text-[11px] font-bold tracking-[0.14em] text-sand">NESTY STAY</span>
        </AppLink>
        {travelerItems.map((item) => (
          <SidebarLink item={item} key={item.href} pathname={pathname} routeName={routeName} />
        ))}
        <div className="mx-3 mb-1 mt-3 border-t border-on-dark-faint/25 pt-3 text-[10px] font-bold tracking-[0.18em] text-on-dark-faint">
          WORKSPACES
        </div>
        {workspaceItems.map((item) => (
          <SidebarLink item={item} key={item.href} pathname={pathname} routeName={routeName} />
        ))}
        <AppLink
          className="mt-auto flex min-h-11 items-center rounded-nav px-3 font-sans text-[13px] font-semibold text-on-dark-muted transition-colors hover:text-on-dark-heading"
          href="/logout"
        >
          Sign out
        </AppLink>
      </aside>
      <div className="flex min-h-screen min-w-0 flex-col">
        <main className="w-full max-w-[1060px] flex-1 px-[clamp(20px,3.5vw,44px)] py-9">{children}</main>
        <footer className="flex justify-center bg-footer px-6 py-[18px]">
          <span className="text-[13px] text-on-dark-muted">
            nestystay.net ·{" "}
            <a className="text-on-dark-muted hover:text-on-dark-body" href="https://wa.me/17542482435">
              754-248-2435
            </a>
          </span>
        </footer>
      </div>
    </div>
  );
}
