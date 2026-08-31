import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { ArrowLeft, Bell, ChevronRight, Home, Search } from "lucide-react";
import { AppLink } from "../AppLink";
import { EmblemRoundel } from "./PublicShell";
import { loadSession, type AuthSession } from "../../lib/auth";
import { cx } from "../../lib/ui";
import type { FeedbackTone } from "../../lib/feedback";

type NavItem = {
  label: string;
  href: string;
  routes?: string[];
  roles?: AuthSession["roles"];
  description?: string;
};

const travelerItems: NavItem[] = [
  { label: "My trips", href: "/guest-dashboard", routes: ["guest-dashboard"], roles: ["Guest", "Owner"] },
  { label: "Suggestions", href: "/traveler/suggestions", routes: ["trav-suggestions"], roles: ["Guest", "Owner"] },
  { label: "Collections", href: "/traveler/favorites", routes: ["trav-favorites", "no-favorites"], roles: ["Guest", "Owner"] },
  { label: "Pending reviews", href: "/traveler/reviews/pending", roles: ["Guest", "Owner"] },
  { label: "Invoices", href: "/traveler/invoices", roles: ["Guest", "Owner"] },
  { label: "Messages", href: "/messages", routes: ["messages", "document-message"], roles: ["Guest", "Host", "Owner", "Officer", "ServiceProvider", "LocalBusiness", "PropertyManager"] },
  { label: "Notifications", href: "/traveler/notifications", routes: ["trav-notifications"], roles: ["Guest", "Host", "Owner", "Officer", "ServiceProvider", "LocalBusiness", "PropertyManager"] },
  { label: "Settings", href: "/profile", routes: ["profile"], roles: ["Guest", "Host", "Owner", "Officer", "ServiceProvider", "LocalBusiness", "PropertyManager"] },
];

const workspaceItems: NavItem[] = [
  { label: "Host", href: "/host-dashboard", routes: ["host-dashboard", "host-spec", "host-profile", "host-reports"], roles: ["Host"], description: "Portfolio, pricing, reviews, and host tools" },
  { label: "Properties", href: "/host/properties", routes: ["property-management", "host-property-edit"], roles: ["Host"], description: "Create, edit, publish, and archive listings" },
  { label: "Reservations", href: "/bookings", routes: ["bookings", "no-reservations"], roles: ["Host", "Guest"], description: "Review booking status and payments" },
  { label: "Calendar", href: "/calendar", routes: ["calendar"], roles: ["Host"], description: "Availability and scheduled stays" },
  { label: "Wellness", href: "/host/wellness", routes: ["host-wellness", "officer-wellness", "officer-directory", "wellness-booking"], roles: ["Host", "Officer"], description: "Visits, reports, and officer assignments" },
  { label: "Property manager", href: "/pm/dashboard", routes: ["pm-gates", "pm-dashboard", "pm-invoices", "pm-maintenance", "pm-governance", "pm-documents", "pm-utilities", "pm-verification", "pm-reports", "pm-insurance"], roles: ["PropertyManager"], description: "Owners, units, finances, and maintenance" },
  { label: "Owner portal", href: "/owner/dashboard", routes: ["owner-dashboard"], roles: ["Owner"], description: "Your assigned units and statements" },
  { label: "Directories", href: "/directory/trades", routes: ["business-directory", "provider-dashboard", "directory-spec"], roles: ["Guest", "Host", "Officer", "ServiceProvider", "LocalBusiness"], description: "Find verified local providers" },
  { label: "Admin", href: "/admin", routes: ["admin", "admin-kpis", "admin-reports", "officer-id-reset", "admin-ops"], roles: ["Admin"], description: "Operations, audits, and system controls" },
  { label: "Activity & audit", href: "/admin/reports", routes: ["admin-reports"], roles: ["Admin"], description: "Review important changes and system history" },
];

const roleLabels: Record<AuthSession["roles"][number], string> = {
  Guest: "Guest workspace",
  Host: "Host workspace",
  Officer: "Officer workspace",
  ServiceProvider: "Provider workspace",
  LocalBusiness: "Business workspace",
  Admin: "Admin workspace",
  PropertyManager: "Property manager workspace",
  Owner: "Owner workspace",
};

const shortcutMap: Record<string, Array<[string, string]>> = {
  Guest: [["Find a stay", "/explore"], ["My trips", "/guest-dashboard"], ["Messages", "/messages"]],
  Host: [["Add property", "/host/properties/new"], ["Reservations", "/bookings"], ["Wellness", "/host/wellness"]],
  Officer: [["My assignments", "/officer/wellness"], ["Wellness directory", "/host/wellness/directory"], ["Messages", "/messages"]],
  ServiceProvider: [["Provider profile", "/directory/provider"], ["Directories", "/directory/trades"], ["Messages", "/messages"]],
  LocalBusiness: [["Provider profile", "/directory/provider"], ["Directories", "/directory/businesses"], ["Messages", "/messages"]],
  Admin: [["Admin console", "/admin"], ["Activity & audit", "/admin/reports"], ["Wellness operations", "/admin/ops/wellness"]],
  PropertyManager: [["Dashboard", "/pm/dashboard"], ["Maintenance", "/pm/maintenance"], ["Gate access", "/pm/gates"]],
  Owner: [["Owner portal", "/owner/dashboard"], ["Invoices", "/traveler/invoices"], ["Messages", "/messages"]],
};

function isActive(item: NavItem, routeName: string, pathname: string) {
  if (pathname === item.href) return true;
  if (routeName === "traveler-spec") return false;
  return Boolean(item.routes?.includes(routeName));
}

function SidebarLink({ item, routeName, pathname, onNavigate }: { item: NavItem; routeName: string; pathname: string; onNavigate?: () => void }) {
  const active = isActive(item, routeName, pathname);
  return (
    <AppLink
      aria-current={active ? "page" : undefined}
      className={cx(
        "flex min-h-11 shrink-0 items-center whitespace-nowrap rounded-nav px-3 font-sans text-[13px] font-semibold transition-colors",
        active ? "bg-yellow/10 text-yellow" : "text-on-dark-nav hover:bg-on-dark-heading/5 hover:text-on-dark-heading",
      )}
      href={item.href}
      onClick={onNavigate}
      title={item.description}
    >
      {item.label}
    </AppLink>
  );
}

function prettyRoute(pathname: string) {
  const segment = pathname.split("/").filter(Boolean).at(-1) ?? "home";
  return segment.replace(/[-_]+/g, " ").replace(/\b\w/g, (letter) => letter.toUpperCase());
}

function Breadcrumbs({ pathname, routeName }: { pathname: string; routeName: string }) {
  const active = [...travelerItems, ...workspaceItems].find((item) => isActive(item, routeName, pathname));
  const current = active?.label ?? prettyRoute(pathname);
  return (
    <nav aria-label="Breadcrumb" className="mb-4 flex flex-wrap items-center gap-1.5 text-xs text-sand-600">
      <AppLink className="inline-flex min-h-8 items-center gap-1 rounded-pill px-2 font-semibold text-deep-hover hover:bg-shell" href="/">
        <Home aria-hidden="true" size={13} /> Home
      </AppLink>
      <ChevronRight aria-hidden="true" size={13} />
      <span aria-current="page" className="font-semibold text-ink">{current}</span>
    </nav>
  );
}

export function WorkspaceFrame({ routeName, children }: { routeName: string; children: ReactNode }) {
  const pathname = window.location.pathname;
  const session = loadSession();
  const roles: AuthSession["roles"] = session?.roles?.length ? session.roles : ["Guest"];
  const isAdmin = roles.includes("Admin");
  const visibleTravelerItems = travelerItems.filter((item) => isAdmin || item.roles?.some((role) => roles.includes(role)));
  const visibleWorkspaceItems = workspaceItems.filter((item) => isAdmin || item.roles?.some((role) => roles.includes(role)));
  const allVisibleItems = [...visibleTravelerItems, ...visibleWorkspaceItems];
  const [searchTerm, setSearchTerm] = useState("");
  const [feedback, setFeedback] = useState<{ message: string; tone: FeedbackTone } | null>(null);
  const searchRef = useRef<HTMLInputElement>(null);
  const activeRole: AuthSession["roles"][number] = roles.find((role) => shortcutMap[role]) ?? "Guest";
  const shortcuts = shortcutMap[activeRole] ?? shortcutMap.Guest;
  const searchResults = useMemo(() => {
    const query = searchTerm.trim().toLowerCase();
    return query ? allVisibleItems.filter((item) => `${item.label} ${item.description ?? ""}`.toLowerCase().includes(query)).slice(0, 6) : [];
  }, [allVisibleItems, searchTerm]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement | null;
      if (event.key === "/" && !["INPUT", "TEXTAREA", "SELECT"].includes(target?.tagName ?? "")) {
        event.preventDefault();
        searchRef.current?.focus();
      }
      if (event.key === "Escape" && document.activeElement === searchRef.current) {
        setSearchTerm("");
        searchRef.current?.blur();
      }
    };
    const onFeedback = (event: Event) => {
      const detail = (event as CustomEvent<{ message?: string; tone?: FeedbackTone }>).detail;
      if (!detail?.message) return;
      setFeedback({ message: detail.message, tone: detail.tone ?? "success" });
      window.setTimeout(() => setFeedback(null), 4200);
    };
    window.addEventListener("keydown", onKeyDown);
    window.addEventListener("nesty:feedback", onFeedback);
    return () => {
      window.removeEventListener("keydown", onKeyDown);
      window.removeEventListener("nesty:feedback", onFeedback);
    };
  }, []);

  return (
    <div className="workspace-layout grid min-h-screen font-sans text-[15px] leading-[1.55] text-ink md:grid-cols-[230px_1fr]">
      <a className="skip-link" href="#main-content">Skip to main content</a>
      <aside aria-label="Workspace navigation" className="workspace-sidebar flex flex-row items-center gap-[3px] overflow-x-auto bg-deep p-3 md:flex-col md:items-stretch md:overflow-visible md:p-5 md:px-3.5">
        <AppLink aria-label="Nesty Stay home" className="flex shrink-0 items-center gap-2 px-2 md:pb-3.5" href="/">
          <EmblemRoundel size={32} />
          <span className="hidden text-[11px] font-bold tracking-[0.14em] text-sand md:inline">NESTY STAY</span>
        </AppLink>
        <div className="workspace-search relative mx-1 my-1 min-w-[220px] md:mx-0 md:mb-3">
          <Search aria-hidden="true" className="pointer-events-none absolute left-3 top-3.5 text-on-dark-faint" size={16} />
          <input ref={searchRef} aria-label="Search workspace" className="min-h-11 w-full rounded-nav border border-on-dark-faint/20 bg-on-dark-heading/5 pl-9 pr-8 text-[13px] text-white outline-none transition-colors placeholder:text-on-dark-faint focus:border-yellow focus:bg-on-dark-heading/10" onChange={(event) => setSearchTerm(event.target.value)} placeholder="Search workspace (/)" type="search" value={searchTerm} />
          {searchResults.length > 0 && <div aria-label="Workspace search results" className="absolute left-0 right-0 top-12 z-40 grid gap-1 rounded-card border border-sand-border bg-cream p-2 text-ink shadow-card" role="listbox">{searchResults.map((item) => <AppLink className="grid gap-0.5 rounded-field px-3 py-2 text-sm hover:bg-shell" href={item.href} key={item.href} onClick={() => setSearchTerm("")}><strong>{item.label}</strong>{item.description && <small className="text-xs text-sand-600">{item.description}</small>}</AppLink>)}</div>}
          {searchTerm.trim() && searchResults.length === 0 && <div className="absolute left-0 right-0 top-12 z-40 rounded-card border border-sand-border bg-cream p-3 text-xs text-sand-600 shadow-card">No workspace matches. Try another word.</div>}
        </div>
        {visibleTravelerItems.map((item) => <SidebarLink item={item} key={item.href} pathname={pathname} routeName={routeName} />)}
        <div className="mx-3 my-0 h-6 shrink-0 border-l border-on-dark-faint/25 md:mx-3 md:mb-1 md:mt-3 md:h-auto md:border-l-0 md:border-t md:pt-3 md:text-[10px] md:font-bold md:tracking-[0.18em] md:text-on-dark-faint"><span className="hidden md:inline">{roleLabels[activeRole]} · WORKSPACES</span></div>
        {visibleWorkspaceItems.map((item) => <SidebarLink item={item} key={item.href} pathname={pathname} routeName={routeName} />)}
        <AppLink aria-label="View notifications" className="flex min-h-11 shrink-0 items-center gap-2 whitespace-nowrap rounded-nav px-3 font-sans text-[13px] font-semibold text-on-dark-nav transition-colors hover:bg-on-dark-heading/5 hover:text-on-dark-heading" href="/traveler/notifications"><Bell aria-hidden="true" size={15} /> Notifications</AppLink>
        <AppLink className="mt-auto flex min-h-11 shrink-0 items-center whitespace-nowrap rounded-nav px-3 font-sans text-[13px] font-semibold text-on-dark-muted transition-colors hover:text-on-dark-heading" href="/logout">Sign out</AppLink>
      </aside>
      <div className="flex min-h-screen min-w-0 flex-col">
        <main className="workspace-main w-full max-w-[1120px] flex-1 px-[clamp(20px,3.5vw,44px)] pb-24 pt-6 md:py-9" id="main-content" tabIndex={-1}>
          <div className="workspace-toolbar mb-5 flex flex-wrap items-center justify-between gap-3">
            <Breadcrumbs pathname={pathname} routeName={routeName} />
            <div className="flex items-center gap-2"><button aria-label="Go back" className="inline-flex min-h-9 items-center gap-1.5 rounded-pill border border-sand-input bg-cream px-3 text-xs font-semibold text-deep-hover transition-colors hover:border-deep-hover" onClick={() => window.history.length > 1 && window.history.back()} type="button"><ArrowLeft aria-hidden="true" size={14} /> Back</button><AppLink aria-label="Open notifications" className="relative inline-flex min-h-9 items-center gap-1.5 rounded-pill border border-sand-input bg-cream px-3 text-xs font-semibold text-deep-hover transition-colors hover:border-deep-hover" href="/traveler/notifications"><Bell aria-hidden="true" size={14} /> Alerts</AppLink></div>
          </div>
          <section aria-label="Quick actions" className="quick-actions mb-6 flex flex-wrap items-center gap-2 rounded-card border border-sand-border bg-cream p-3 shadow-card"><span className="mr-1 text-xs font-semibold uppercase tracking-[0.08em] text-sand-600">Quick actions</span>{shortcuts.map(([label, href]) => <AppLink className="inline-flex min-h-9 items-center rounded-pill border border-sand-input bg-white px-3 text-xs font-semibold text-deep-hover transition-colors hover:border-deep-hover hover:bg-shell" href={href} key={href}>{label}</AppLink>)}</section>
          {feedback && <div aria-live="polite" className={cx("mb-4 flex items-center justify-between gap-3 rounded-field border px-4 py-3 text-sm font-semibold", feedback.tone === "error" ? "border-coral/30 bg-coral-tint text-coral-text" : feedback.tone === "info" ? "border-blue/20 bg-info-tint text-info-text" : "border-green/20 bg-success-tint text-success-text")} role={feedback.tone === "error" ? "alert" : "status"}><span>{feedback.message}</span><button aria-label="Dismiss notification" className="rounded-pill px-2 text-lg leading-none" onClick={() => setFeedback(null)} type="button">×</button></div>}
          {children}
        </main>
        <footer className="flex justify-center bg-footer px-6 py-[18px]"><span className="text-[13px] text-on-dark-muted">nestystay.net · <a className="text-on-dark-muted hover:text-on-dark-body" href="https://wa.me/17542482435">754-248-2435</a></span></footer>
      </div>
      <nav aria-label="Mobile workspace navigation" className="workspace-mobile-nav fixed inset-x-3 bottom-3 z-50 grid grid-cols-5 gap-1 rounded-card border border-sand-border bg-deep/95 p-2 shadow-navbar backdrop-blur md:hidden">{allVisibleItems.slice(0, 5).map((item) => { const active = isActive(item, routeName, pathname); return <AppLink aria-current={active ? "page" : undefined} className={cx("grid min-h-12 place-items-center rounded-field px-1 text-center text-[10px] font-semibold", active ? "bg-yellow text-deep" : "text-on-dark-nav hover:bg-on-dark-heading/10")} href={item.href} key={item.href}>{item.label}</AppLink>; })}</nav>
    </div>
  );
}
