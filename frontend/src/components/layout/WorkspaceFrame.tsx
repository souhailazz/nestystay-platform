import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { ArrowLeft, Bell, ChevronRight, Home, Search } from "lucide-react";
import { AppLink } from "../AppLink";
import { EmblemRoundel } from "./PublicShell";
import { loadSession, type AuthSession } from "../../lib/auth";
import { cx } from "../../lib/ui";
import type { FeedbackTone } from "../../lib/feedback";
import { getScreenDefinition, navigationForRole, type NavigationItem } from "../../app/routeManifest";
import { requestConfirmation } from "../../lib/confirmation";

const roleLabels: Record<AuthSession["roles"][number], string> = {
  Guest: "Guest workspace", Host: "Host workspace", Officer: "Officer workspace", ServiceProvider: "Provider workspace",
  LocalBusiness: "Business workspace", Admin: "Admin workspace", PropertyManager: "Property manager workspace", Owner: "Owner workspace",
};

const roleQuickActions: Partial<Record<AuthSession["roles"][number], readonly [string, string][]>> = {
  Guest: [["Find a stay", "/explore"], ["View trips", "/guest-dashboard"], ["Saved stays", "/traveler/favorites"]],
  Host: [["Add property", "/host/properties/new"], ["Review reservations", "/bookings"], ["View reports", "/host/reports"]],
  PropertyManager: [["Open portfolio", "/pm/dashboard"], ["Review invoices", "/pm/invoices"], ["View reports", "/pm/reports"]],
  Owner: [["Open owner portal", "/owner/dashboard"], ["Open settings", "/profile"], ["Find a stay", "/explore"]],
  Officer: [["Open assignments", "/officer/wellness"], ["Open directory", "/host/wellness/directory"], ["Open settings", "/profile"]],
  ServiceProvider: [["Open provider dashboard", "/directory/provider"], ["Open directory", "/directory/trades"], ["Open settings", "/profile"]],
  LocalBusiness: [["Open business profile", "/directory/provider"], ["Open directory", "/directory/businesses"], ["Open settings", "/profile"]],
  Admin: [["Open operations", "/admin"], ["Review queues", "/admin/ops/disputes"], ["Open audit", "/admin/reports"]],
};

function isActive(item: NavigationItem, screenId: string, pathname: string) {
  return pathname === item.href || screenId === item.screenId;
}

function SidebarLink({ item, screenId, pathname, onNavigate }: { item: NavigationItem; screenId: string; pathname: string; onNavigate?: () => void }) {
  const active = isActive(item, screenId, pathname);
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
      <span className="workspace-sidebar-link-label">{item.label}</span>
    </AppLink>
  );
}

function prettyRoute(pathname: string) {
  const segment = pathname.split("/").filter(Boolean).at(-1) ?? "home";
  return segment.replace(/[-_]+/g, " ").replace(/\b\w/g, (letter) => letter.toUpperCase());
}

function Breadcrumbs({ pathname, screenId }: { pathname: string; screenId: string }) {
  const active = getScreenDefinition(screenId)?.navigation;
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

export function WorkspaceFrame({ routeName, screenId, children }: { routeName: string; screenId: string; children: ReactNode }) {
  const pathname = window.location.pathname;
  const session = loadSession();
  const roles: AuthSession["roles"] = session?.roles?.length ? session.roles : ["Guest"];
  const rolePriority: AuthSession["roles"] = ["Admin", "PropertyManager", "Owner", "Host", "Officer", "ServiceProvider", "LocalBusiness", "Guest"];
  const activeRole = rolePriority.find((role) => roles.includes(role)) ?? "Guest";
  const allVisibleItems = navigationForRole(activeRole);
  const mobileItems = navigationForRole(activeRole, true);
  const moreItems = allVisibleItems.filter((item) => !mobileItems.some((mobileItem) => mobileItem.screenId === item.screenId));
  const [searchTerm, setSearchTerm] = useState("");
  const [feedback, setFeedback] = useState<{ message: string; tone: FeedbackTone } | null>(null);
  const searchRef = useRef<HTMLInputElement>(null);
  const shortcuts = roleQuickActions[activeRole] ?? allVisibleItems.slice(0, 3).map((item) => [item.label, item.href] as [string, string]);
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
    <div className="workspace-layout grid min-h-screen font-sans text-[15px] leading-[1.55] text-ink" data-route-name={routeName}>
      <a className="skip-link" href="#main-content">Skip to main content</a>
      <nav aria-label="Workspace navigation" className="workspace-sidebar flex flex-row items-center gap-[3px] overflow-x-auto bg-deep p-3 lg:flex-col lg:items-stretch lg:overflow-visible lg:p-5 lg:px-3.5">
        <AppLink aria-label="Nesty Stay home" className="flex shrink-0 items-center gap-2 px-2 lg:pb-3.5" href="/">
          <EmblemRoundel size={32} />
          <span className="hidden text-[11px] font-bold tracking-[0.14em] text-sand lg:inline">NESTY STAY</span>
        </AppLink>
        <div className="workspace-search relative mx-1 my-1 min-w-[220px] lg:mx-0 lg:mb-3">
          <Search aria-hidden="true" className="pointer-events-none absolute left-3 top-3.5 text-on-dark-faint" size={16} />
          <input ref={searchRef} aria-label="Search workspace" className="min-h-11 w-full rounded-nav border border-on-dark-faint/20 bg-on-dark-heading/5 pl-9 pr-8 text-[13px] text-white outline-none transition-colors placeholder:text-on-dark-faint focus:border-yellow focus:bg-on-dark-heading/10" onChange={(event) => setSearchTerm(event.target.value)} placeholder="Search workspace (/)" type="search" value={searchTerm} />
          {searchResults.length > 0 && <div aria-label="Workspace search results" className="absolute left-0 right-0 top-12 z-40 grid gap-1 rounded-card border border-sand-border bg-cream p-2 text-ink shadow-card" role="listbox">{searchResults.map((item) => <AppLink className="grid gap-0.5 rounded-field px-3 py-2 text-sm hover:bg-shell" href={item.href} key={item.href} onClick={() => setSearchTerm("")}><strong>{item.label}</strong>{item.description && <small className="text-xs text-sand-600">{item.description}</small>}</AppLink>)}</div>}
          {searchTerm.trim() && searchResults.length === 0 && <div className="absolute left-0 right-0 top-12 z-40 rounded-card border border-sand-border bg-cream p-3 text-xs text-sand-600 shadow-card">No workspace matches. Try another word.</div>}
        </div>
        {allVisibleItems.map((item) => <SidebarLink item={item} key={item.href} pathname={pathname} screenId={screenId} />)}
        <div className="workspace-sidebar-divider mx-3 my-0 h-6 shrink-0 border-l border-on-dark-faint/25 lg:mx-3 lg:mb-1 lg:mt-3 lg:h-auto lg:border-l-0 lg:border-t lg:pt-3 lg:text-[10px] lg:font-bold lg:tracking-[0.18em] lg:text-on-dark-faint"><span className="hidden lg:inline">{roleLabels[activeRole]} · WORKSPACES</span></div>
        <AppLink
          aria-label="Sign out"
          className="mt-auto flex min-h-11 shrink-0 items-center whitespace-nowrap rounded-nav px-3 font-sans text-[13px] font-semibold text-on-dark-muted transition-colors hover:text-on-dark-heading"
          href="/logout"
          onClick={async (event) => {
            if (!(await requestConfirmation({ title: "Sign out?", message: "Sign out of Nesty Stay on this device?" }))) event.preventDefault();
          }}
        >
          Sign out
        </AppLink>
      </nav>
      <div className="flex min-h-screen min-w-0 flex-col">
        <main className="workspace-main w-full max-w-[1120px] flex-1 px-[clamp(20px,3.5vw,44px)] pb-24 pt-6 lg:py-9" id="main-content" tabIndex={-1}>
          <div className="workspace-toolbar mb-5 flex flex-wrap items-center justify-between gap-3">
            <Breadcrumbs pathname={pathname} screenId={screenId} />
            <div className="flex items-center gap-2"><button aria-label="Go back" className="inline-flex min-h-9 items-center gap-1.5 rounded-pill border border-sand-input bg-cream px-3 text-xs font-semibold text-deep-hover transition-colors hover:border-deep-hover" onClick={() => window.history.length > 1 && window.history.back()} type="button"><ArrowLeft aria-hidden="true" size={14} /> Back</button><AppLink aria-label="Open notifications" className="relative inline-flex min-h-9 items-center gap-1.5 rounded-pill border border-sand-input bg-cream px-3 text-xs font-semibold text-deep-hover transition-colors hover:border-deep-hover" href="/traveler/notifications"><Bell aria-hidden="true" size={14} /> Alerts</AppLink></div>
          </div>
          <section aria-label="Quick actions" className="quick-actions mb-6 flex flex-wrap items-center gap-2 rounded-card border border-sand-border bg-cream p-3 shadow-card"><span className="mr-1 text-xs font-semibold uppercase tracking-[0.08em] text-sand-600">Quick actions</span>{shortcuts.map(([label, href]) => <AppLink className="inline-flex min-h-9 items-center rounded-pill border border-sand-input bg-white px-3 text-xs font-semibold text-deep-hover transition-colors hover:border-deep-hover hover:bg-shell" href={href} key={href}>{label}</AppLink>)}</section>
          {feedback && <div aria-live="polite" className={cx("mb-4 flex items-center justify-between gap-3 rounded-field border px-4 py-3 text-sm font-semibold", feedback.tone === "error" ? "border-coral/30 bg-coral-tint text-coral-text" : feedback.tone === "info" ? "border-blue/20 bg-info-tint text-info-text" : "border-green/20 bg-success-tint text-success-text")} role={feedback.tone === "error" ? "alert" : "status"}><span>{feedback.message}</span><button aria-label="Dismiss notification" className="rounded-pill px-2 text-lg leading-none" onClick={() => setFeedback(null)} type="button">×</button></div>}
          {children}
        </main>
        <footer className="flex justify-center bg-footer px-6 py-[18px]"><span className="text-[13px] text-on-dark-muted">nestystay.net · <a className="text-on-dark-muted hover:text-on-dark-body" href="https://wa.me/17542482435">754-248-2435</a></span></footer>
      </div>
      <nav aria-label="Mobile workspace navigation" className="workspace-mobile-nav fixed inset-x-3 bottom-3 z-50 grid grid-cols-5 gap-1 rounded-card border border-sand-border bg-deep/95 p-2 shadow-navbar backdrop-blur md:hidden">{mobileItems.map((item) => { const active = isActive(item, screenId, pathname); const itemKey = `${item.screenId}-${item.href}`; if (item.more) return <button aria-label="Open more workspace destinations" className="grid min-h-12 place-items-center rounded-field px-1 text-center text-[10px] font-semibold text-on-dark-nav hover:bg-on-dark-heading/10" key={itemKey} onClick={() => window.dispatchEvent(new CustomEvent("nesty:workspace-more"))} type="button">{item.label}</button>; return <AppLink aria-current={active ? "page" : undefined} className={cx("grid min-h-12 place-items-center rounded-field px-1 text-center text-[10px] font-semibold", active ? "bg-yellow text-deep" : "text-on-dark-nav hover:bg-on-dark-heading/10")} href={item.href} key={itemKey}>{item.label}</AppLink>; })}</nav>
      <WorkspaceMoreSheet items={moreItems} roleLabel={roleLabels[activeRole]} />
    </div>
  );
}

function WorkspaceMoreSheet({ items, roleLabel }: { items: readonly NavigationItem[]; roleLabel: string }) {
  const [open, setOpen] = useState(false);
  useEffect(() => {
    const onOpen = () => setOpen(true);
    window.addEventListener("nesty:workspace-more", onOpen);
    return () => window.removeEventListener("nesty:workspace-more", onOpen);
  }, []);
  if (!open) return null;
  return <div aria-label={`${roleLabel} destinations`} className="workspace-more-sheet fixed inset-x-3 bottom-20 z-50 rounded-card border border-sand-border bg-deep p-3 shadow-navbar"><div className="mb-2 flex items-center justify-between text-xs font-semibold uppercase tracking-[0.12em] text-on-dark-faint"><span>{roleLabel}</span><button aria-label="Close more destinations" className="rounded-pill px-2 text-lg text-on-dark-nav" onClick={() => setOpen(false)} type="button">×</button></div><div className="grid gap-1">{items.map((item) => <AppLink className="rounded-field px-3 py-2 text-sm font-semibold text-on-dark-nav hover:bg-on-dark-heading/10 hover:text-white" href={item.href} key={item.href} onClick={() => setOpen(false)}>{item.label}</AppLink>)}</div></div>;
}
