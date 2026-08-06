import { useEffect, useState, type ReactNode } from "react";
import {
  AlertTriangle,
  BadgeCheck,
  BarChart3,
  Bell,
  Bookmark,
  BriefcaseBusiness,
  Building2,
  CalendarDays,
  Check,
  ChevronRight,
  CreditCard,
  Download,
  FileText,
  Heart,
  Home,
  KeyRound,
  Lock,
  Map,
  MessageSquare,
  Pencil,
  ReceiptText,
  RefreshCw,
  Search,
  ShieldAlert,
  ShieldCheck,
  SlidersHorizontal,
  TimerReset,
  UserCheck,
  Wrench,
  X,
  type LucideIcon,
} from "lucide-react";
import { AppLink, navigate } from "../components/AppLink";
import { Badge } from "../components/ui/Badge";
import { Button, buttonClassName } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { EmptyState } from "../components/ui/EmptyState";
import { Field, InlineLabel, Input, Select, Textarea } from "../components/ui/Input";
import { PageHeader } from "../components/ui/PageHeader";
import { PatoisToast } from "../components/ui/PatoisToast";
import { EmblemRoundel, TierBadge, deepPatternBackground } from "../components/layout/PublicShell";
import { StatusChip } from "../components/ui/StatusChip";
import { useProperties } from "../hooks/useProperties";
import { formatMoney } from "../lib/api";
import { usePatois } from "../lib/patois";
import { getStayImage } from "../lib/stayImages";
import { cx } from "../lib/ui";

type SpecMetric = {
  icon: LucideIcon;
  label: string;
  value: string;
};

type SpecAction = {
  label: string;
  href?: string;
  variant?: "sun" | "outline" | "dark" | "ghost";
};

function ScreenShell({
  id,
  eyebrow,
  title,
  copy,
  actions,
  children,
  className,
}: {
  id: string;
  eyebrow: string;
  title: string;
  copy: string;
  actions?: ReactNode;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className={["product-page spec-page", className].filter(Boolean).join(" ")}>
      <PageHeader
        eyebrow={`${id} / ${eyebrow}`}
        title={title}
        copy={copy}
        actions={actions}
      />
      {children}
    </div>
  );
}

/* DS v2 honesty rule — any card whose figures have no backing API wears this
   chip so mock numbers never look real. */
export function SampleDataChip({ className }: { className?: string }) {
  return (
    <span
      className={cx(
        "inline-flex shrink-0 items-center rounded-pill bg-amber-tint px-2.5 py-1 font-sans text-[10px] font-bold tracking-[0.08em] text-amber-text",
        className,
      )}
    >
      SAMPLE DATA
    </span>
  );
}

export function DesignSystemReferencePage() {
  const tokens = [
    ["Deep green", "#061f1d"],
    ["Palm", "#0f5a45"],
    ["Sun", "#ffd228"],
    ["Cream", "#f7f3e8"],
    ["Coral", "#e57b54"],
    ["Blue", "#3267a8"],
  ];

  return (
    <ScreenShell
      id="DS-V2"
      eyebrow="Design system"
      title="Reusable product tokens and components."
      copy="Reference screen for the live React implementation: colors, states, cards, controls, tables, badges, and page rhythm."
    >
      <section className="product-section">
        <div className="spec-card-grid spec-card-grid--three">
          {tokens.map(([label, value]) => (
            <Card className="spec-card" key={label}>
              <span
                aria-hidden="true"
                className="spec-card__icon"
                style={{ background: value, color: value === "#ffd228" || value === "#f7f3e8" ? "#061f1d" : "#fff" }}
              >
                Aa
              </span>
              <div>
                <Badge tone={value === "#ffd228" ? "sun" : "slate"}>{value}</Badge>
                <h3>{label}</h3>
                <p>Bound to the shared frontend theme and used across public, traveler, host, and admin flows.</p>
              </div>
            </Card>
          ))}
        </div>
        <Card className="settings-card">
          <h3>Control samples</h3>
          <div className="button-row">
            <Button variant="sun"><Check size={16} /> Primary action</Button>
            <Button variant="outline"><SlidersHorizontal size={16} /> Filter</Button>
            <Button variant="ghost"><Download size={16} /> Export</Button>
          </div>
          <div className="form-grid form-grid--two">
            <Field label="Destination"><Input defaultValue="Montego Bay" /></Field>
            <Field label="Mode"><Select defaultValue="Guest"><option>Guest</option><option>Host</option><option>Admin</option></Select></Field>
          </div>
        </Card>
      </section>
    </ScreenShell>
  );
}

/* ERR-LOAD (DS v2) — structured "Tek Time" skeleton that mirrors the real
   layout: never a bare spinner, layout never shifts on load. */
export function LoadingStatePage() {
  const shimmer = "animate-pulse rounded-lg bg-shell";
  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="ERR-LOAD">
      <div className="flex items-baseline gap-2.5">
        <em className="font-display text-[22px] italic text-deep">Tek Time</em>
        <span className="text-[13px] text-sand-500">Loading your trips…</span>
      </div>
      <div className={cx(shimmer, "h-[38px] w-[260px] rounded-[10px]")} />
      <div className="grid max-w-[760px] grid-cols-[repeat(auto-fit,minmax(200px,1fr))] gap-3.5">
        {[0, 1, 2].map((index) => (
          <div className="flex flex-col gap-2.5 rounded-[18px] border border-sand-border bg-cream p-[18px]" key={index}>
            <div className={cx(shimmer, "h-2.5 w-3/5")} />
            <div className={cx(shimmer, "h-[30px] w-2/5")} />
          </div>
        ))}
      </div>
      <div className="flex max-w-[760px] flex-col gap-3.5">
        {[0, 1].map((index) => (
          <div className="flex items-center gap-3.5 rounded-card border border-sand-border bg-cream p-[18px]" key={index}>
            <div className={cx(shimmer, "h-[76px] w-24 shrink-0 rounded-field")} />
            <div className="flex flex-1 flex-col gap-2">
              <div className={cx(shimmer, "h-4 w-1/2")} />
              <div className={cx(shimmer, "h-3 w-1/3")} />
              <div className={cx(shimmer, "h-3 w-2/3")} />
            </div>
          </div>
        ))}
      </div>
      <div className="mt-4 text-[13px] text-sand-500">
        nestystay.net ·{" "}
        <a className="text-sand-500 hover:text-ink" href="https://wa.me/17542482435">
          754-248-2435
        </a>
      </div>
    </div>
  );
}

function MetricStrip({ items }: { items: SpecMetric[] }) {
  return (
    <div className="metric-grid spec-metric-grid">
      {items.map((item) => {
        const Icon = item.icon;
        return (
          <Card className="metric-card" key={item.label}>
            <span>
              <Icon size={21} />
            </span>
            <small>{item.label}</small>
            <strong>{item.value}</strong>
          </Card>
        );
      })}
    </div>
  );
}

function SpecCard({
  icon: Icon,
  title,
  copy,
  meta,
  action,
}: {
  icon: LucideIcon;
  title: string;
  copy: string;
  meta?: string;
  action?: SpecAction;
}) {
  return (
    <Card className="spec-card">
      <span className="spec-card__icon">
        <Icon size={20} />
      </span>
      <div>
        {meta && <Badge tone="slate">{meta}</Badge>}
        <h3>{title}</h3>
        <p>{copy}</p>
      </div>
      {action?.href ? (
        <AppLink className={buttonClassName(action.variant ?? "outline")} href={action.href}>
          {action.label} <ChevronRight size={16} />
        </AppLink>
      ) : action ? (
        <Button variant={action.variant ?? "outline"}>
          {action.label} <ChevronRight size={16} />
        </Button>
      ) : null}
    </Card>
  );
}

function DataTable({
  columns,
  rows,
}: {
  columns: string[];
  rows: Array<Array<ReactNode>>;
}) {
  return (
    <div className="spec-table-wrap">
      <table className="spec-table">
        <thead>
          <tr>
            {columns.map((column) => (
              <th key={column}>{column}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, rowIndex) => (
            <tr key={rowIndex}>
              {row.map((cell, cellIndex) => (
                <td key={cellIndex}>{cell}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function StayThumbs({ start = 0 }: { start?: number }) {
  return (
    <div className="spec-collage" aria-hidden="true">
      {[0, 1, 2].map((offset) => {
        const image = getStayImage(start + offset);
        return <img key={image.src} src={image.src} alt="" loading="lazy" />;
      })}
    </div>
  );
}

function VerificationToggle({
  enabled,
  onChange,
}: {
  enabled: boolean;
  onChange: (value: boolean) => void;
}) {
  return (
    <div className="verification-toggle-card">
      <div>
        <strong>Enable guest identity verification for this property</strong>
        <p>NEVER AUTOMATIC - host enables per property.</p>
      </div>
      <InlineLabel className="switch-label">
        <input checked={enabled} type="checkbox" onChange={(event) => onChange(event.target.checked)} />
        <span className="switch-track" aria-hidden="true" />
        <span>{enabled ? "Enabled" : "Off"}</span>
      </InlineLabel>
      <div className="verification-pricing">
        <span>$0.14 per booking</span>
        <span>$1.26 / 10-pack</span>
        <span>$2.99 / month</span>
        <span>$29.99 / year</span>
      </div>
    </div>
  );
}

const staticProperties = [
  ["Azure Cove Villa", "Montego Bay", "$450", "Trusted"],
  ["Kingston Business Stay", "Kingston", "$320", "Verified"],
  ["Portland Rainforest House", "Port Antonio", "$280", "Wellness"],
  ["Negril Beach Cottage", "Negril", "$390", "Trusted"],
];

const jamaicaPins = [
  { x: 20, y: 46, price: "$320", parish: "Westmoreland" },
  { x: 30, y: 34, price: "$450", parish: "St. James" },
  { x: 44, y: 44, price: "$280", parish: "Trelawny" },
  { x: 55, y: 36, price: "$510", parish: "St. Ann" },
  { x: 66, y: 52, price: "$260", parish: "St. Mary" },
  { x: 73, y: 44, price: "$370", parish: "Portland" },
  { x: 58, y: 68, price: "$295", parish: "St. Catherine" },
  { x: 70, y: 74, price: "$340", parish: "Kingston" },
];

/** AUTH-POST — "Yuh Gud?" toast over a condensed traveler dashboard backdrop. */
export function AuthPostLoginToastPage() {
  const sidebarLinks = [
    ["My trips", "/guest-dashboard", true],
    ["Collections", "/traveler/favorites", false],
    ["Invoices", "/traveler/invoices", false],
    ["Settings", "/profile", false],
  ] as const;

  return (
    <div className="relative min-h-screen font-sans text-[15px] leading-[1.55] text-ink" id="AUTH-POST">
      {/* Dashboard backdrop (condensed TRAV-01) */}
      <div className="grid min-h-screen md:grid-cols-[220px_1fr]">
        <aside className="flex flex-col gap-1 bg-deep p-5 px-3.5">
          <AppLink className="flex items-center gap-2 px-2 pb-3.5" href="/">
            <EmblemRoundel size={32} />
            <span className="text-[11px] font-bold tracking-[0.14em] text-sand">NESTY STAY</span>
          </AppLink>
          {sidebarLinks.map(([label, href, active]) => (
            <AppLink
              className={cx(
                "flex min-h-11 items-center rounded-nav px-3 text-[13px] font-semibold transition-colors",
                active ? "bg-yellow/10 text-yellow" : "text-on-dark-nav hover:bg-on-dark-heading/5 hover:text-on-dark-heading",
              )}
              href={href}
              key={label}
            >
              {label}
            </AppLink>
          ))}
        </aside>
        <main className="flex flex-col gap-5 px-10 py-9">
          <h1 className="m-0 font-display text-[38px] font-normal">
            Your <em className="italic text-deep-hover">trips</em>
          </h1>
          <div className="grid max-w-[760px] grid-cols-[repeat(auto-fit,minmax(200px,1fr))] gap-3.5">
            {(
              [
                ["UPCOMING", "2"],
                ["SAVED STAYS", "14"],
                ["PENDING REVIEWS", "2"],
              ] as const
            ).map(([label, value]) => (
              <div className="rounded-[18px] border border-sand-border bg-cream p-[18px]" key={label}>
                <div className="text-[11px] font-semibold tracking-[0.16em] text-sand-500">{label}</div>
                <div className="font-display text-[32px] font-medium">{value}</div>
              </div>
            ))}
          </div>
          <div className="text-[13px] text-sand-500">
            Full dashboard:{" "}
            <AppLink className="font-semibold text-deep-hover hover:text-deep" href="/guest-dashboard">
              TRAV-01
            </AppLink>
          </div>
        </main>
      </div>
      {/* Login success toast — slide-in 200ms ease-out; auto-dismiss stays off in this spec frame */}
      <PatoisToast
        autoDismiss={false}
        className="fixed right-5 top-5 z-50 max-w-[380px]"
        phrase="Yuh Gud?"
        translation="Are you OK? — Welcome back!"
      />
    </div>
  );
}

/** AUTH-LOGOUT — "Likkle More" full-viewport Deep screen with English pairing. */
export function LogoutScreenPage() {
  const { showPatois } = usePatois();
  return (
    <div
      className="flex min-h-screen flex-col font-sans text-[15px] leading-[1.55] text-on-dark-heading"
      id="AUTH-LOGOUT"
      style={deepPatternBackground}
    >
      <main className="flex flex-1 flex-col items-center justify-center gap-[26px] px-6 pb-12 pt-[72px] text-center">
        <div className="flex items-center gap-3.5">
          <EmblemRoundel size={56} />
          <span className="text-[19px] font-bold tracking-[0.22em] text-sand">NESTY STAY</span>
        </div>
        <div className="flex flex-col items-center gap-2.5">
          {showPatois ? (
            <>
              <div className="font-display text-[clamp(44px,8vw,84px)] italic leading-[1.05] text-yellow [text-wrap:pretty]">
                Likkle More
              </div>
              <div className="max-w-[480px] text-[clamp(15px,1.6vw,19px)] text-on-dark-body">
                See you later! — You have been signed out safely.
              </div>
            </>
          ) : (
            <>
              <div className="font-display text-[clamp(34px,5vw,56px)] leading-[1.05] text-on-dark-heading [text-wrap:pretty]">
                See you later!
              </div>
              <div className="max-w-[480px] text-[clamp(15px,1.6vw,19px)] text-on-dark-body">
                You have been signed out safely.
              </div>
            </>
          )}
        </div>
        <div className="flex flex-wrap justify-center gap-3">
          <AppLink
            className="group inline-flex min-h-[50px] items-center gap-2.5 rounded-pill bg-yellow px-7 text-[15px] font-bold text-deep transition-colors hover:bg-yellow-press"
            href="/login"
          >
            Log back in{" "}
            <span aria-hidden="true" className="inline-block transition-transform duration-200 group-hover:translate-x-1">
              →
            </span>
          </AppLink>
          <AppLink
            className="inline-flex min-h-[50px] items-center rounded-pill border-[1.5px] border-on-dark-heading/55 px-7 text-[15px] font-semibold text-on-dark-heading transition-colors hover:border-on-dark-heading hover:bg-on-dark-heading/10"
            href="/explore"
          >
            Browse as guest
          </AppLink>
        </div>
      </main>
      <footer className="flex justify-center p-6">
        <span className="text-[13px] text-on-dark-faint">
          nestystay.net ·{" "}
          <a className="text-on-dark-faint hover:text-on-dark-body" href="https://wa.me/17542482435">
            754-248-2435
          </a>
        </span>
      </footer>
    </div>
  );
}

/** PUB-MAP — full-height map view with its own topbar, list panel and mobile drawer. */
export function MapSearchPage() {
  const { properties } = useProperties();
  const [activeId, setActiveId] = useState(0);
  const [chip, setChip] = useState("all");
  const cards = properties.length
    ? properties.slice(0, 5).map((property) => ({
        title: property.title,
        location: property.location,
        price: formatMoney(property.nightlyRate, property.currency),
        badge: property.badgeLevel,
      }))
    : staticProperties.map(([title, location, price, badge]) => ({ title, location, price, badge }));

  const chips = [
    ["all", "All badges"],
    ["verified", "✓ Verified"],
    ["trusted", "★ Trusted"],
    ["wellness", "✦ Wellness"],
  ] as const;
  const visible = cards.filter((c) => chip === "all" || c.badge.toLowerCase().includes(chip));

  const miniCard = (card: (typeof cards)[number], index: number, extra?: string) => (
    <button
      className={cx(
        "flex cursor-pointer items-center gap-3 rounded-field border bg-cream p-2.5 text-left font-sans transition-shadow hover:shadow-[0_4px_14px_rgba(6,43,43,0.1)]",
        index === activeId ? "border-[1.5px] border-deep-hover" : "border-sand-border",
        extra,
      )}
      key={card.title}
      onClick={() => setActiveId(index)}
      type="button"
    >
      <img alt="" className="block h-[76px] w-24 shrink-0 rounded-[12px] object-cover" src={getStayImage(index).src} />
      <span>
        <span className="block font-display text-[15px] font-semibold text-ink">{card.title}</span>
        <span className="mt-px block text-[12.5px] text-gray-600">{card.location}</span>
        <span className="mt-1 flex flex-wrap items-center gap-2 text-sm text-ink">
          <strong>{card.price}</strong> / night <TierBadge className="!px-2.5 !py-[3px] !text-[9.5px]" level={card.badge} />
        </span>
      </span>
    </button>
  );

  return (
    <div className="font-sans text-[15px] leading-[1.55] text-ink">
      {/* TOPBAR */}
      <div className="sticky top-0 z-40 border-b border-sand-border bg-sand/95">
        <div className="flex flex-wrap items-center gap-3 px-5 py-2.5">
          <AppLink
            className="inline-flex min-h-11 items-center gap-2 rounded-field border-[1.5px] border-deep-hover bg-cream px-[18px] text-sm font-semibold text-deep-hover transition-colors hover:bg-shell"
            href="/explore"
          >
            ← Back to list view
          </AppLink>
          <span className="flex items-center gap-2.5 text-[13.5px] font-bold tracking-[0.14em] text-deep">
            <EmblemRoundel size={40} />
            NESTY STAY
          </span>
          <div className="ml-auto hidden flex-wrap items-center gap-2 md:flex">
            {chips.map(([value, label]) => (
              <button
                className={cx(
                  "min-h-11 cursor-pointer rounded-pill px-[18px] font-sans text-[13.5px] font-semibold transition-colors",
                  chip === value
                    ? "border border-deep bg-deep text-white"
                    : "border-[1.5px] border-sand-input bg-cream text-gray-600 hover:border-deep-hover hover:text-deep-hover",
                )}
                key={value}
                onClick={() => setChip(value)}
                type="button"
              >
                {label}
              </button>
            ))}
          </div>
        </div>
      </div>

      <div className="flex h-[calc(100vh-65px)] min-h-[520px]">
        {/* LIST PANEL */}
        <aside className="hidden w-[372px] shrink-0 flex-col gap-3 overflow-y-auto border-r border-sand-border bg-sand p-[18px] md:flex">
          <div className="text-xs font-semibold uppercase tracking-[0.1em] text-sand-500">
            {visible.length} stays on the map
          </div>
          {visible.map((card, index) => miniCard(card, index))}
        </aside>

        {/* MAP */}
        <div aria-label="Map of Jamaica with priced stays by parish" className="relative min-w-0 flex-1 bg-[#D8E9E4]" role="img">
          <svg className="absolute inset-0 block h-full w-full" preserveAspectRatio="none" viewBox="0 0 100 100">
            {/* Simplified Jamaica silhouette (illustrative geometry, sized to the pin field) */}
            <path
              d="M 8 52 Q 12 38 24 32 Q 36 25 48 26 Q 60 26 70 30 Q 82 34 90 42 Q 94 48 92 54 Q 88 62 78 66 Q 70 78 58 76 Q 46 80 34 74 Q 20 68 12 60 Q 8 56 8 52 Z"
              fill="#F3F4EC"
              stroke="#0E4A45"
              strokeWidth="0.25"
              vectorEffect="non-scaling-stroke"
            />
          </svg>
          {(
            [
              ["HANOVER", 14, 44], ["WESTMORELAND", 18, 56], ["ST. JAMES", 27, 40], ["TRELAWNY", 37, 42],
              ["ST. ANN", 52, 40], ["ST. MARY", 64, 42], ["PORTLAND", 80, 48], ["ST. THOMAS", 76, 62],
              ["ST. ANDREW", 67, 60], ["ST. CATHERINE", 58, 62], ["CLARENDON", 49, 64], ["MANCHESTER", 41, 62],
              ["ST. ELIZABETH", 29, 62],
            ] as const
          ).map(([name, x, y]) => (
            <span
              className="pointer-events-none absolute -translate-x-1/2 -translate-y-1/2 text-[9.5px] font-semibold tracking-[0.12em] text-deep/30"
              key={name}
              style={{ left: `${x}%`, top: `${y}%` }}
            >
              {name}
            </span>
          ))}
          {jamaicaPins.map((pin, index) => (
            <button
              className={cx(
                "absolute -translate-x-1/2 -translate-y-1/2 cursor-pointer rounded-pill px-3.5 py-1.5 font-sans text-[13.5px] font-bold shadow-[0_2px_6px_rgba(6,43,43,0.22)] transition-transform hover:scale-105",
                index === activeId ? "border border-deep bg-deep text-white" : "border border-sand-input bg-white text-deep",
              )}
              key={pin.parish}
              onClick={() => setActiveId(index)}
              style={{ left: `${pin.x}%`, top: `${pin.y}%` }}
              title={pin.parish}
              type="button"
            >
              {pin.price}
            </button>
          ))}
          <div className="absolute bottom-3.5 right-4 rounded-lg bg-white/90 px-2.5 py-[5px] text-[11px] text-gray-600">
            Static map view — pins are illustrative positions
          </div>
        </div>
      </div>

      {/* MOBILE DRAWER */}
      <div className="fixed inset-x-0 bottom-0 z-[45] flex flex-col gap-3 rounded-t-[18px] border-t border-sand-border bg-sand px-3.5 pb-4 pt-2.5 shadow-[0_-8px_24px_rgba(6,43,43,0.14)] md:hidden">
        <div className="mx-auto h-[5px] w-11 shrink-0 rounded-pill bg-sand-input" />
        <div className="flex gap-3 overflow-x-auto pb-0.5">
          {visible.slice(0, 3).map((card, index) => miniCard(card, index, "shrink-0 basis-[280px]"))}
        </div>
      </div>
    </div>
  );
}

const LAUNCH_OFFSET_MS = ((14 * 24 + 6) * 60 + 32) * 60_000 + 9_000; // 14d 06h 32m 09s

function CountdownCell({ value, label }: { value: string; label: string }) {
  return (
    <div className="flex min-w-[clamp(72px,9vw,96px)] flex-col gap-0.5 rounded-field border border-[#1D4A46] bg-white/5 px-2.5 pb-[13px] pt-4">
      <span className="font-display text-[clamp(30px,4vw,42px)] font-normal leading-none">{value}</span>
      <span className="text-[10.5px] font-semibold tracking-[0.14em] text-on-dark-faint">{label}</span>
    </div>
  );
}

/** PUB-SOON — full-viewport Deep countdown page with patois headline + English pairing. */
export function ComingSoonPage() {
  const { showPatois } = usePatois();
  const [target] = useState(() => Date.now() + LAUNCH_OFFSET_MS);
  const [now, setNow] = useState(() => Date.now());
  const [notified, setNotified] = useState(false);

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, []);

  const remaining = Math.max(0, target - now);
  const pad = (n: number) => String(n).padStart(2, "0");
  const days = Math.floor(remaining / 86_400_000);
  const hours = Math.floor((remaining % 86_400_000) / 3_600_000);
  const minutes = Math.floor((remaining % 3_600_000) / 60_000);
  const seconds = Math.floor((remaining % 60_000) / 1000);

  return (
    <div className="flex min-h-screen flex-col bg-deep font-sans text-[15px] leading-[1.55] text-white">
      <main className="flex flex-1 flex-col items-center justify-center gap-7 px-6 pb-12 pt-[72px] text-center">
        <div className="flex items-center gap-3.5">
          <EmblemRoundel size={56} />
          <span className="text-[19px] font-bold tracking-[0.22em] text-sand">NESTY STAY</span>
        </div>

        {/* patois-block (dark): patois line always paired with its English translation */}
        <div className="flex flex-col items-center gap-2.5">
          {showPatois ? (
            <>
              <div className="font-display text-[clamp(44px,8vw,84px)] italic leading-[1.05] text-yellow [text-wrap:pretty]">
                Wi Soon Come!
              </div>
              <div className="max-w-[480px] text-[clamp(15px,1.6vw,19px)] text-white opacity-90">
                Coming soon — Jamaica&apos;s own trusted stays platform.
              </div>
            </>
          ) : (
            <div className="max-w-[560px] font-display text-[clamp(30px,4.4vw,52px)] leading-[1.1] text-on-dark-heading [text-wrap:pretty]">
              Coming soon — Jamaica&apos;s own trusted stays platform.
            </div>
          )}
        </div>

        <div aria-label="Launch countdown" className="flex flex-wrap justify-center gap-[clamp(8px,1.6vw,14px)]">
          <CountdownCell label="DAYS" value={pad(days)} />
          <CountdownCell label="HRS" value={pad(hours)} />
          <CountdownCell label="MIN" value={pad(minutes)} />
          <CountdownCell label="SEC" value={pad(seconds)} />
        </div>

        <form
          className="flex w-full max-w-[460px] flex-col items-center gap-2.5"
          onSubmit={(event) => {
            event.preventDefault();
            setNotified(true);
          }}
        >
          <div className="flex w-full flex-wrap justify-center gap-2.5">
            <input
              aria-label="Email address"
              className="min-h-12 flex-[1_1_240px] rounded-field border-[1.5px] border-[#1D4A46] bg-white/5 px-4 font-sans text-[15px] text-white outline-none transition-colors placeholder:text-on-dark-faint focus:border-on-dark-faint"
              placeholder="Your email address"
              required
              type="email"
            />
            <button
              className="min-h-12 cursor-pointer rounded-field border-none bg-yellow px-[26px] font-sans text-[15px] font-bold text-deep transition-colors hover:bg-yellow-press"
              type="submit"
            >
              Notify Me
            </button>
          </div>
          <div className="text-xs text-on-dark-faint">
            {notified ? "You're on the list — one launch email, that's all." : "No spam — one launch email, that's all."}
          </div>
        </form>

        <AppLink
          className="inline-flex min-h-11 items-center border-b border-on-dark-nav/40 text-[14.5px] font-semibold text-on-dark-nav transition-colors hover:text-white"
          href="/"
        >
          150 Platinum Founding Member spots available. See details →
        </AppLink>
      </main>

      <footer className="flex justify-center border-t border-[#1D4A46] px-6 py-7">
        <div className="text-[13px] text-on-dark-faint">
          nestystay.net ·{" "}
          <a className="text-on-dark-faint hover:text-on-dark-body" href="https://wa.me/17542482435">
            754-248-2435
          </a>
        </div>
      </footer>
    </div>
  );
}

/** TRAV-COL (DS v2) — collections with 3-thumb collages. No wishlist API yet (spec): local state. */
export function FavoritesCollectionsPage() {
  const [collections, setCollections] = useState([
    { id: "c1", name: "Winter escape", stays: 5 },
    { id: "c2", name: "Kingston work trips", stays: 3 },
    { id: "c3", name: "Anniversary ideas", stays: 6 },
  ]);
  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState("");

  const roundBtn =
    "grid size-[46px] cursor-pointer place-items-center rounded-full border-[1.5px] border-sand-input bg-transparent transition-colors hover:border-deep";

  return (
    <div className="flex flex-col gap-5" id="TRAV-COL">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">Collections</h1>
        <button
          className="inline-flex min-h-[46px] cursor-pointer items-center gap-2 rounded-pill border-none bg-deep px-[22px] font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover"
          onClick={() => setCreating((c) => !c)}
          type="button"
        >
          + Create new collection
        </button>
      </div>

      {creating && (
        <form
          className="flex flex-wrap items-center gap-2.5 rounded-card border border-sand-border bg-cream p-4"
          onSubmit={(event) => {
            event.preventDefault();
            if (!newName.trim()) return;
            setCollections((cols) => [...cols, { id: `c-${cols.length + 1}-${newName.trim()}`, name: newName.trim(), stays: 0 }]);
            setNewName("");
            setCreating(false);
          }}
        >
          <Input
            aria-label="Collection name"
            className="w-auto flex-[1_1_240px]"
            onChange={(e) => setNewName(e.target.value)}
            placeholder="e.g. Honeymoon beach stays"
            value={newName}
          />
          <Button type="submit" variant="dark">
            Create
          </Button>
        </form>
      )}

      <div className="grid grid-cols-[repeat(auto-fill,minmax(280px,1fr))] gap-4">
        {collections.map((col, index) => (
          <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]" key={col.id}>
            <div className="grid h-[150px] grid-cols-[2fr_1fr] grid-rows-2 gap-1.5 overflow-hidden rounded-field">
              <img alt="" className="row-span-2 h-full w-full object-cover" src={getStayImage(index).src} />
              <img alt="" className="h-full w-full object-cover" src={getStayImage(index + 1).src} />
              <img alt="" className="h-full w-full object-cover" src={getStayImage(index + 2).src} />
            </div>
            <div className="flex items-center justify-between gap-2.5">
              <div>
                <div className="font-display text-lg font-medium">{col.name}</div>
                <div className="text-[12.5px] text-gray-600">
                  {col.stays} stay{col.stays === 1 ? "" : "s"}
                </div>
              </div>
              <div className="flex gap-1.5">
                <AppLink
                  className="inline-flex min-h-[46px] items-center rounded-pill border-[1.5px] border-sand-input px-5 text-[13.5px] font-semibold text-ink transition-colors hover:border-deep"
                  href="/explore"
                >
                  View
                </AppLink>
                <button
                  aria-label={`Rename ${col.name}`}
                  className={cx(roundBtn, "text-gray-600")}
                  onClick={() => {
                    const name = window.prompt("Rename collection", col.name);
                    if (name?.trim()) setCollections((cols) => cols.map((c) => (c.id === col.id ? { ...c, name: name.trim() } : c)));
                  }}
                  type="button"
                >
                  <Pencil size={15} />
                </button>
                <button
                  aria-label={`Delete ${col.name}`}
                  className={cx(roundBtn, "text-coral-text")}
                  onClick={() => setCollections((cols) => cols.filter((c) => c.id !== col.id))}
                  type="button"
                >
                  <X size={15} />
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>
      {collections.length === 0 && <EmptyState title="No collections yet" copy="Save stays while exploring to start a collection." />}
    </div>
  );
}

export function InvoicesPage() {
  return (
    <ScreenShell
      id="TRAV-INV"
      eyebrow="Traveler portal"
      title="Invoices."
      copy="Downloadable payment records with year filtering and batch export."
      actions={<Button><Download size={17} /> Download all</Button>}
    >
      <section className="product-section">
        <div className="search-panel spec-filter-bar">
          <Field label="Year">
            <Select defaultValue="2026">
              <option>2026</option>
              <option>2025</option>
            </Select>
          </Field>
          <Button variant="dark">
            <Search size={17} /> Filter
          </Button>
        </div>
        <DataTable
          columns={["Invoice", "Property", "Stay dates", "Amount", "Status", "File"]}
          rows={[
            ["NST-2026-1024", "Coral Reef Sanctuary", "Jul 12-16", "$4,130", <Badge tone="green">Paid</Badge>, <a>Download</a>],
            ["NST-2026-0977", "Kingston Business Stay", "Jun 2-5", "$1,280", <Badge tone="green">Paid</Badge>, <a>Download</a>],
            ["NST-2026-0811", "Blue Mountain Retreat", "Apr 18-20", "$980", <Badge tone="green">Paid</Badge>, <a>Download</a>],
            ["NST-2026-0642", "Negril Beach Cottage", "Feb 7-10", "$1,620", <Badge tone="green">Paid</Badge>, <a>Download</a>],
          ]}
        />
      </section>
    </ScreenShell>
  );
}

export function PendingReviewsPage() {
  const reviewCards = [
    ["Coral Reef Sanctuary", "Jul 12-16", "12 days left to review", false],
    ["Kingston Business Stay", "Jun 2-5", "5 days left to review", false],
    ["Negril Beach Cottage", "Review window closed", "Closed", true],
  ] as const;

  return (
    <ScreenShell
      id="TRAV-PEND"
      eyebrow="Traveler portal"
      title="Pending reviews."
      copy="Review reminders show deadlines, closed windows, and property context."
    >
      <section className="product-section">
        <div className="notice-panel">You have 2 pending reviews.</div>
        <div className="spec-card-grid">
          {reviewCards.map(([title, dates, deadline, closed], index) => (
            <Card className={closed ? "review-card review-card--closed" : "review-card"} key={title}>
              <img src={getStayImage(index).src} alt="" />
              <div>
                <h3>{title}</h3>
                <p>{dates}</p>
                <Badge tone={closed ? "slate" : "sun"}>{deadline}</Badge>
              </div>
              <Button disabled={closed} variant={closed ? "ghost" : "sun"}>
                Write review
              </Button>
            </Card>
          ))}
        </div>
      </section>
    </ScreenShell>
  );
}

/** TRAV-NOTIF (DS v2) — notifications center with filters, unread markers and read controls. */
export function NotificationsCenterPage() {
  type Notif = { id: string; icon: string; iconTone: string; title: string; body: string; category: string; time: string; unread: boolean };
  const [items, setItems] = useState<Notif[]>([
    { id: "n1", icon: "✓", iconTone: "bg-success-tint text-success-text", title: "Booking confirmed", body: "Cliffside Retreat is locked in for Dec 12 – 18.", category: "Bookings", time: "2 min ago", unread: true },
    { id: "n2", icon: "$", iconTone: "bg-info-tint text-info-text", title: "Payment captured", body: "$2,970 charged to Visa ····4242 — receipt NST-2026-0148.", category: "Payments", time: "2 min ago", unread: true },
    { id: "n3", icon: "✉", iconTone: "bg-amber-tint text-amber-text", title: "New message from your host", body: "“Linens and a welcome basket will be ready…”", category: "Messages", time: "1 hr ago", unread: true },
    { id: "n4", icon: "◆", iconTone: "bg-mint-tint text-mint-text", title: "Wellness visit scheduled", body: "Officer NST-OFC-4821 · Dec 13, 10:00.", category: "Bookings", time: "Yesterday", unread: false },
    { id: "n5", icon: "★", iconTone: "bg-shell text-sand-500", title: "Review reminder", body: "12 days left to review Uptown Loft.", category: "Bookings", time: "2 days ago", unread: false },
  ]);
  const [filter, setFilter] = useState("All");
  const unreadCount = items.filter((n) => n.unread).length;
  const visible = items.filter((n) => {
    if (filter === "All") return true;
    if (filter === "Unread") return n.unread;
    return n.category === filter;
  });

  return (
    <div className="flex flex-col gap-5" id="TRAV-NOTIF">
      <div className="flex items-center gap-3">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">Notifications</h1>
        {unreadCount > 0 && (
          <span className="inline-flex h-[26px] min-w-[26px] items-center justify-center rounded-pill bg-coral px-2 text-xs font-bold text-white">
            {unreadCount}
          </span>
        )}
      </div>
      <div className="flex flex-wrap gap-2">
        {["All", "Unread", "Bookings", "Payments", "Messages"].map((tab) => (
          <button
            className={cx(
              "inline-flex min-h-11 cursor-pointer items-center rounded-pill px-5 font-sans text-[13px] font-semibold transition-colors",
              filter === tab
                ? "border-none bg-deep text-on-dark-heading"
                : "border-[1.5px] border-sand-input bg-transparent text-gray-600 hover:border-deep hover:text-ink",
            )}
            key={tab}
            onClick={() => setFilter(tab)}
            type="button"
          >
            {tab}
          </button>
        ))}
      </div>
      <div className="overflow-hidden rounded-card border border-sand-border bg-cream">
        {visible.length === 0 && <div className="p-6 text-center text-[13.5px] text-gray-600">Nothing here — you&apos;re all caught up.</div>}
        {visible.map((n) => (
          <div
            className={cx("flex items-start gap-3.5 border-b border-shell px-5 py-[15px] last:border-b-0", n.unread && "bg-[#FAF6EA]")}
            key={n.id}
          >
            <span className={cx("inline-flex size-[38px] shrink-0 items-center justify-center rounded-full text-[15px] font-bold", n.iconTone)}>
              {n.icon}
            </span>
            <div className="flex-1">
              <div className="flex items-center gap-2">
                <span className={cx("text-sm", n.unread ? "font-bold" : "font-semibold")}>{n.title}</span>
                {n.unread && <span aria-label="Unread" className="inline-block size-2 rounded-full bg-coral" />}
              </div>
              <div className="text-[13px] text-gray-600">{n.body}</div>
            </div>
            <div className="flex flex-col items-end gap-1">
              <span className="whitespace-nowrap text-[11.5px] text-sand-500">{n.time}</span>
              {n.unread && (
                <button
                  className="inline-flex min-h-8 cursor-pointer items-center border-none bg-transparent p-0 text-[11.5px] font-bold text-deep-hover hover:text-deep"
                  onClick={() => setItems((all) => all.map((x) => (x.id === n.id ? { ...x, unread: false } : x)))}
                  type="button"
                >
                  Mark as read
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

/** TRAV-SUGG (DS v2) — preference-matched stays from the live property list with reason tags. */
export function TripSuggestionsPage() {
  const { properties } = useProperties();
  const [filter, setFilter] = useState("All parishes");
  const [saved, setSaved] = useState<Record<string, boolean>>({});

  const reasons = ["◎ You loved beachfront stays", "◎ Similar to your last stay", "◎ Quiet parishes you browse"];
  const filtered = properties.filter((p) => {
    if (filter === "Under $300") return p.nightlyRate < 300;
    if (filter === "Wellness hosts") return p.badgeLevel.toLowerCase().includes("well");
    if (filter === "Beachfront") return p.highlights.some((h) => /beach|ocean|sea/i.test(h));
    return true;
  });

  return (
    <div className="flex flex-col gap-5" id="TRAV-SUGG">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        {filtered.length} stay{filtered.length === 1 ? "" : "s"} match your{" "}
        <em className="italic text-deep-hover">preferences.</em>
      </h1>
      <div className="flex flex-wrap gap-2">
        {["All parishes", "Beachfront", "Under $300", "Wellness hosts"].map((tab) => (
          <button
            className={cx(
              "inline-flex min-h-11 cursor-pointer items-center rounded-pill px-5 font-sans text-[13px] font-semibold transition-colors",
              filter === tab
                ? "border-none bg-deep text-on-dark-heading"
                : "border-[1.5px] border-sand-input bg-transparent text-gray-600 hover:border-deep hover:text-ink",
            )}
            key={tab}
            onClick={() => setFilter(tab)}
            type="button"
          >
            {tab}
          </button>
        ))}
      </div>
      <div className="grid grid-cols-[repeat(auto-fill,minmax(300px,1fr))] gap-4">
        {filtered.map((prop, index) => (
          <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]" key={prop.id}>
            <div className="relative -mx-1.5 -mt-1.5 h-40 overflow-hidden rounded-field">
              <img alt={prop.title} className="h-full w-full object-cover" src={prop.imageUrl ?? getStayImage(index).src} />
              <button
                aria-label={`Save ${prop.title}`}
                aria-pressed={Boolean(saved[prop.id])}
                className="absolute right-2.5 top-2.5 grid size-11 cursor-pointer place-items-center rounded-full border-none bg-deep/60 text-base text-on-dark-heading"
                onClick={() => setSaved((s) => ({ ...s, [prop.id]: !s[prop.id] }))}
                type="button"
              >
                {saved[prop.id] ? "♥" : "♡"}
              </button>
            </div>
            <div className="flex items-baseline justify-between gap-2.5">
              <div className="font-display text-lg font-medium">{prop.title}</div>
              <span className="text-sm">
                <strong>{formatMoney(prop.nightlyRate, prop.currency)}</strong>{" "}
                <span className="text-[11.5px] text-sand-500">/ night</span>
              </span>
            </div>
            <div className="text-[12.5px] text-gray-600">
              {prop.location} · {prop.country}
            </div>
            <div className="flex flex-wrap gap-1.5">
              <span className="rounded-pill bg-shell px-2.5 py-1 text-[10.5px] font-bold uppercase tracking-[0.06em] text-gray-600">
                {reasons[index % reasons.length]}
              </span>
              <span className="rounded-pill bg-success-tint px-2.5 py-1 text-[10.5px] font-bold uppercase tracking-[0.06em] text-success-text">
                Available your dates
              </span>
            </div>
            <div className="flex gap-2">
              <AppLink
                className="inline-flex min-h-[46px] items-center rounded-pill bg-deep px-[22px] text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover"
                href={`/properties/${prop.id}`}
              >
                View
              </AppLink>
              <button
                className="inline-flex min-h-[46px] cursor-pointer items-center rounded-pill border-[1.5px] border-sand-input bg-transparent px-5 font-sans text-[13.5px] font-semibold text-ink transition-colors hover:border-deep"
                onClick={() => setSaved((s) => ({ ...s, [prop.id]: !s[prop.id] }))}
                type="button"
              >
                {saved[prop.id] ? "Saved" : "Save"}
              </button>
            </div>
          </div>
        ))}
      </div>
      {filtered.length === 0 && <EmptyState title="No matches for that filter" copy="Try a different filter to see more stays." />}
    </div>
  );
}

/** HOST-EDIT (DS v2) — section cards with round edit affordances; verification card enforces the NEVER AUTOMATIC rule. */
export function HostPropertyEditPage() {
  const { properties } = useProperties();
  const property = properties[0];
  const [verificationEnabled, setVerificationEnabled] = useState(property?.guestVerificationEnabled ?? false);

  const titleParts = (property?.title ?? "Azure Cove Villa").split(" ");
  const accent = titleParts.pop() ?? "";
  const sections: Array<[string, string, string]> = [
    ["Basics", "Title, description, guest capacity", property?.title ?? "Azure Cove Villa"],
    ["Pricing", "Nightly rate & currency", `${formatMoney(property?.nightlyRate ?? 450, property?.currency ?? "USD")} / night · ${property?.currency ?? "USD"}`],
    ["Highlights", "Shown as chips on the listing", (property?.highlights ?? ["Workspace", "Wi-Fi"]).join(" · ")],
    ["Cancellation policy", "Traveler-facing terms", property?.cancellationPolicy ?? "Flexible"],
  ];

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="HOST-EDIT">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
            {titleParts.join(" ")} <em className="italic text-deep-hover">{accent}</em>
          </h1>
          <div className="mt-2 flex flex-wrap items-center gap-2">
            <StatusChip value={property?.isArchived ? "Draft" : "Published"} />
            {property && <TierBadge className="!px-2.5 !py-1 !text-[10.5px]" level={property.badgeLevel} />}
          </div>
        </div>
        {property && (
          <AppLink
            className="inline-flex min-h-[46px] items-center rounded-pill border-[1.5px] border-sand-input px-5 text-[13.5px] font-semibold text-ink transition-colors hover:border-deep"
            href={`/properties/${property.id}`}
          >
            View public listing ↗
          </AppLink>
        )}
      </div>

      {sections.map(([label, sub, value]) => (
        <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]" key={label}>
          <div className="flex items-start justify-between gap-3">
            <div>
              <div className="text-[14.5px] font-bold">{label}</div>
              <div className="text-xs text-sand-500">{sub}</div>
            </div>
            <button
              aria-label={`Edit ${label}`}
              className="grid size-11 cursor-pointer place-items-center rounded-full border-[1.5px] border-sand-input bg-transparent text-gray-600 transition-colors hover:border-deep hover:text-deep"
              type="button"
            >
              <Pencil size={15} />
            </button>
          </div>
          <div className="text-[13.5px] text-gray-600">{value}</div>
        </div>
      ))}

      {/* Verification — client contract: NEVER AUTOMATIC */}
      <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="text-[14.5px] font-bold">Verification</div>
        <div className="rounded-[16px] border-[1.5px] border-sand-border bg-white px-5 py-[18px]">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <span className="text-[14.5px] font-semibold">Traveler identity verification (eKYC)</span>
            <button
              aria-checked={verificationEnabled}
              aria-label="Traveler identity verification (eKYC)"
              className={cx(
                "relative block h-9 w-16 shrink-0 cursor-pointer rounded-pill border-none transition-colors",
                verificationEnabled ? "bg-deep-hover" : "bg-sand-input",
              )}
              onClick={() => setVerificationEnabled((v) => !v)}
              role="switch"
              type="button"
            >
              <span className={cx("absolute top-1 block size-7 rounded-full bg-white transition-all", verificationEnabled ? "right-1" : "left-1")} />
            </button>
          </div>
          <div className="mt-2.5 inline-flex rounded-pill bg-amber-tint px-2.5 py-[5px] text-[11px] font-bold tracking-[0.08em] text-amber-text">
            NEVER AUTOMATIC — HOST ENABLES PER PROPERTY
          </div>
          <div className="mt-2 text-[12.5px] text-gray-600">
            $0.14 per booking · $1.26 / 10-pack · $2.99 / month · $29.99 / year{" "}
            <span className="text-coral-text">(pricing under client arbitration)</span>
          </div>
        </div>
      </div>
    </div>
  );
}

/** HOST-RPT (DS v2) — reports & tax-ready summary (spec figures until the reports API lands). */
export function HostReportsPage() {
  const [period, setPeriod] = useState("This month");
  const stats = [
    ["REVENUE", "$24,850", "▲ 12%"],
    ["OCCUPANCY", "84%", "▲ 6 pts"],
    ["BOOKINGS", "12", "▲ 3"],
    ["CANCELLATIONS", "1", ""],
    ["AVG SCORE", "4.9", ""],
  ] as const;

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="HOST-RPT">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">Reports</h1>
        <div className="flex flex-wrap gap-2.5">
          <select
            aria-label="Reporting period"
            className="min-h-[46px] rounded-pill border-[1.5px] border-sand-input bg-cream px-[18px] font-sans text-[13.5px] font-semibold text-ink outline-none focus:border-deep-hover"
            onChange={(e) => setPeriod(e.target.value)}
            value={period}
          >
            {["This month", "Last 3 months", "YTD"].map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
          <button className="inline-flex min-h-[46px] cursor-pointer items-center gap-2 rounded-pill border-[1.5px] border-sand-input bg-transparent px-5 font-sans text-[13.5px] font-semibold text-ink transition-colors hover:border-deep" type="button">
            <Download size={15} /> Export CSV
          </button>
          <button className="inline-flex min-h-[46px] cursor-pointer items-center gap-2 rounded-pill border-[1.5px] border-sand-input bg-transparent px-5 font-sans text-[13.5px] font-semibold text-ink transition-colors hover:border-deep" type="button">
            <FileText size={15} /> Export PDF
          </button>
        </div>
      </div>

      <div className="grid grid-cols-[repeat(auto-fit,minmax(180px,1fr))] gap-3.5">
        {stats.map(([label, value, delta]) => (
          <div className="flex flex-col gap-2.5 rounded-card border border-sand-border bg-cream p-[22px]" key={label}>
            <div className="text-[11px] font-semibold tracking-[0.16em] text-sand-500">{label}</div>
            <div className="font-display text-[32px] font-medium leading-none">{value}</div>
            {delta && <div className="text-xs text-success-text">{delta}</div>}
            <SampleDataChip className="self-start" />
          </div>
        ))}
      </div>

      <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="flex flex-wrap items-center justify-between gap-2.5">
          <div className="flex items-center gap-2.5">
            <div className="font-display text-xl font-medium">Tax-ready summary</div>
            <SampleDataChip />
          </div>
          <button className="inline-flex min-h-[46px] cursor-pointer items-center gap-2 rounded-pill border-none bg-deep px-[22px] font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover" type="button">
            <Download size={15} /> Download tax summary
          </button>
        </div>
        <div className="flex flex-col text-[13.5px]">
          <div className="flex justify-between border-b border-shell py-2">
            <span>Gross booking revenue</span>
            <strong>$24,850</strong>
          </div>
          <div className="flex justify-between border-b border-shell py-2">
            <span>Platform fees (separated)</span>
            <strong className="text-coral-text">−$2,236.50</strong>
          </div>
          <div className="flex justify-between py-2.5">
            <strong>Net payout</strong>
            <strong className="font-display text-xl">$22,613.50</strong>
          </div>
        </div>
        <div className="text-xs text-sand-500">Figures follow the platform pricebook; Jamaica compliance formats live in admin reports.</div>
      </div>
    </div>
  );
}

/* Shared PM pill styles (DS v2). */
const pmSelectPill =
  "min-h-[46px] rounded-pill border-[1.5px] border-sand-input bg-cream px-[18px] font-sans text-[13.5px] font-semibold text-ink outline-none focus:border-deep-hover";
const pmOutlinePill =
  "inline-flex min-h-[46px] cursor-pointer items-center gap-2 rounded-pill border-[1.5px] border-sand-input bg-transparent px-5 font-sans text-[13.5px] font-semibold text-ink transition-colors hover:border-deep";
const pmDeepPill =
  "inline-flex min-h-[46px] cursor-pointer items-center gap-2 rounded-pill border-none bg-deep px-[22px] font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover";

/* PM-GATE (DS v2) — gate guard threads. VISION screen (no gates API yet): all
   thread content is sample data, so the cards are chipped. Approve/Deny and
   the note input are local-only until the blueprint API lands. */
export function PropertyManagerGatePage() {
  const [activeThread, setActiveThread] = useState("t1");
  const [note, setNote] = useState("");
  const threads = [
    { id: "t1", gate: "Main gate — Ocho Palms", ago: "2 min", preview: "Visitor: Andre M. · plate 4821 GT" },
    { id: "t2", gate: "Tower B — Harbour View", ago: "48 min", preview: "Delivery — Island Grocers" },
  ];

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="PM-GATE">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
          Gate <em className="italic text-deep-hover">communications</em>
        </h1>
        <select aria-label="Gate filter" className={pmSelectPill} defaultValue="All gates">
          <option>All gates</option>
          <option>Main gate — Ocho Palms</option>
          <option>Tower B — Harbour View</option>
        </select>
      </div>

      <div className="grid min-h-[460px] overflow-hidden rounded-card border border-sand-border bg-cream md:grid-cols-[minmax(230px,300px)_1fr]">
        <div className="border-b border-sand-border md:border-b-0 md:border-r">
          {threads.map((thread) => (
            <button
              className={cx(
                "flex w-full cursor-pointer flex-col gap-0.5 border-b border-shell px-4 py-3.5 text-left font-sans",
                activeThread === thread.id ? "bg-shell" : "bg-transparent hover:bg-shell/60",
              )}
              key={thread.id}
              onClick={() => setActiveThread(thread.id)}
              type="button"
            >
              <span className="flex justify-between gap-2">
                <span className="text-[13px] font-bold text-ink">{thread.gate}</span>
                <span className="text-[11px] text-sand-500">{thread.ago}</span>
              </span>
              <span className="text-xs text-gray-600">{thread.preview}</span>
            </button>
          ))}
        </div>
        <div className="flex flex-col">
          <div className="flex flex-wrap items-center justify-between gap-2.5 border-b border-shell px-5 py-3.5">
            <div className="text-sm font-bold">Main gate — Ocho Palms</div>
            <div className="flex items-center gap-1.5">
              <span className="inline-flex items-center rounded-pill bg-info-tint px-2.5 py-1 text-[10.5px] font-bold tracking-[0.06em] text-info-text">
                GUARD ON DUTY
              </span>
              <SampleDataChip />
            </div>
          </div>
          <div className="flex flex-1 flex-col gap-3 p-5">
            <div className="max-w-[75%] self-start rounded-[16px_16px_16px_4px] bg-shell px-4 py-3 text-[13.5px]">
              <div className="mb-1 text-xs font-bold">GATE GUARD · 14:02</div>
              Visitor <strong>Andre M.</strong> · plate <strong>4821 GT</strong> · reason: &quot;pool maintenance, unit 12&quot;.
              <div className="mt-2 flex h-[90px] w-[150px] items-center justify-center rounded-[10px] bg-sand-border text-[11px] text-sand-500">
                photo attached
              </div>
            </div>
            <div className="flex flex-col items-end gap-1.5 self-end">
              <div className="rounded-[16px_16px_4px_16px] bg-deep px-4 py-3 text-[13.5px] text-on-dark-heading">
                Approved — expected, unit 12 confirmed.
              </div>
              <span className="text-[11px] text-sand-500">14:04 · logged to audit trail</span>
            </div>
          </div>
          <div className="flex flex-wrap gap-2.5 border-t border-shell px-5 py-3.5">
            <button
              className="inline-flex min-h-[46px] cursor-pointer items-center rounded-pill border-none bg-success-tint px-[22px] font-sans text-[13.5px] font-bold text-success-text"
              type="button"
            >
              ✓ Approve
            </button>
            <button
              className="inline-flex min-h-[46px] cursor-pointer items-center rounded-pill border-none bg-coral-tint px-[22px] font-sans text-[13.5px] font-bold text-coral-text"
              type="button"
            >
              ✕ Deny
            </button>
            <input
              className="min-h-[46px] min-w-[160px] flex-1 rounded-pill border-[1.5px] border-sand-input bg-white px-[18px] font-sans text-[13.5px] text-ink outline-none focus:border-deep-hover"
              onChange={(event) => setNote(event.target.value)}
              placeholder="Add a note…"
              type="text"
              value={note}
            />
          </div>
        </div>
      </div>

      <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="flex items-center justify-between gap-2.5">
          <div className="text-[13px] font-semibold">Audit log</div>
          <SampleDataChip />
        </div>
        <div className="font-mono text-[12.5px] leading-relaxed text-gray-600">
          14:04 · APPROVED · Andre M. / 4821 GT · by PM S. Chin
          <br />
          11:32 · DENIED · unlisted visitor · by PM S. Chin
          <br />
          09:15 · APPROVED · Island Grocers delivery · by guard rule
        </div>
      </div>
    </div>
  );
}

/* PM-UTIL (DS v2) — utility bill-back. VISION screen: readings and bill-back
   amounts are sample data (chipped) until the utilities API lands. */
export function PropertyManagerUtilitiesPage() {
  const gridCols = "grid grid-cols-[0.9fr_0.8fr_0.8fr_0.8fr_1fr_0.8fr] items-center gap-3";
  const rows = [
    ["Unit 10", "14,220 kWh", "14,610 kWh", "390 kWh", "METERED", "$97.50"],
    ["Unit 11", "9,804 kWh", "10,020 kWh", "216 kWh", "METERED", "$54.00"],
    ["Unit 12", "—", "—", "—", "EQUAL SHARE", "$62.25"],
    ["Unit 14", "—", "—", "—", "CUSTOM 35%", "$87.15"],
  ] as const;
  const allocationChip = (allocation: string) =>
    allocation === "METERED"
      ? "bg-info-tint text-info-text"
      : allocation.startsWith("CUSTOM")
        ? "bg-amber-tint text-amber-text"
        : "bg-shell text-sand-500";

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="PM-UTIL">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
          Utility <em className="italic text-deep-hover">tracking</em>
        </h1>
        <div className="flex flex-wrap gap-2.5">
          <select aria-label="Billing period" className={pmSelectPill} defaultValue="December 2026">
            <option>December 2026</option>
          </select>
          <button className={pmOutlinePill} type="button">
            + Add meter reading
          </button>
        </div>
      </div>

      <div className="overflow-hidden rounded-card border border-sand-border bg-cream">
        <div className="flex items-center justify-between gap-2.5 border-b border-shell px-5 py-3">
          <span className="text-[13px] font-semibold">Meter readings &amp; allocation</span>
          <SampleDataChip />
        </div>
        <div className={cx(gridCols, "bg-shell px-5 py-3 text-[11px] font-bold tracking-[0.1em] text-sand-500")}>
          <span>UNIT</span>
          <span>PREVIOUS</span>
          <span>CURRENT</span>
          <span>USAGE</span>
          <span>ALLOCATION</span>
          <span>BILL-BACK</span>
        </div>
        {rows.map(([unit, previous, current, usage, allocation, billback], index) => (
          <div
            className={cx(gridCols, "border-b border-shell px-5 py-3 text-[13px]", index % 2 === 1 && "bg-sand")}
            key={unit}
          >
            <strong>{unit}</strong>
            <span className="text-gray-600">{previous}</span>
            <span className="text-gray-600">{current}</span>
            <span>{usage}</span>
            <span>
              <span
                className={cx(
                  "inline-flex items-center rounded-pill px-2.5 py-1 text-[10.5px] font-bold tracking-[0.06em]",
                  allocationChip(allocation),
                )}
              >
                {allocation}
              </span>
            </span>
            <strong>{billback}</strong>
          </div>
        ))}
      </div>

      <div className="flex flex-wrap gap-2.5">
        <button className={pmOutlinePill} type="button">
          Preview bill-back
        </button>
        <button className={pmDeepPill} type="button">
          Generate utility invoices
        </button>
      </div>
    </div>
  );
}

/* PM-VERIFY (DS v2) — tenant/owner verification. VISION screen: members are
   sample data (chipped). Reject requires a mandatory reason (spec rule);
   checklist + reason are local-only until the verification API lands. */
export function PropertyManagerVerificationPage() {
  const [idReceived, setIdReceived] = useState(true);
  const [leaseReceived, setLeaseReceived] = useState(false);
  const [rejectReason, setRejectReason] = useState("");

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="PM-VERIFY">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        Tenant &amp; owner <em className="italic text-deep-hover">verification</em>
      </h1>

      <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <div className="text-[15px] font-bold">Andre Morgan · Unit 12 · Tenant</div>
            <div className="text-[12.5px] text-gray-600">Submitted Dec 8 · lease ends 2027-06-30</div>
          </div>
          <div className="flex items-center gap-1.5">
            <StatusChip label="eKYC" value="Pending" />
            <SampleDataChip />
          </div>
        </div>
        <div className="flex flex-col gap-2 text-[13.5px]">
          <label className="flex min-h-11 cursor-pointer items-center gap-3">
            <input
              checked={idReceived}
              className="size-5 accent-deep-hover"
              onChange={(event) => setIdReceived(event.target.checked)}
              type="checkbox"
            />
            <span>Government ID received</span>
          </label>
          <label className="flex min-h-11 cursor-pointer items-center gap-3">
            <input
              checked={leaseReceived}
              className="size-5 accent-deep-hover"
              onChange={(event) => setLeaseReceived(event.target.checked)}
              type="checkbox"
            />
            <span>Proof of lease received</span>
          </label>
        </div>
        <div className="flex flex-wrap gap-2.5">
          <button className={pmDeepPill} type="button">
            Trigger eKYC ↗
          </button>
          <button
            className="inline-flex min-h-[46px] cursor-pointer items-center rounded-pill border-none bg-success-tint px-[22px] font-sans text-[13.5px] font-bold text-success-text"
            type="button"
          >
            ✓ Approve
          </button>
          <button
            className="inline-flex min-h-[46px] cursor-pointer items-center rounded-pill border-none bg-coral-tint px-[22px] font-sans text-[13.5px] font-bold text-coral-text"
            type="button"
          >
            ✕ Reject…
          </button>
        </div>
        <div className="rounded-field border-[1.5px] border-sand-border bg-white px-4 py-3.5">
          <div className="mb-1.5 text-xs font-bold">Reject requires a reason (mandatory)</div>
          <input
            className="box-border min-h-[46px] w-full rounded-nav border-[1.5px] border-sand-input px-3.5 font-sans text-[13.5px] text-ink outline-none focus:border-deep-hover"
            onChange={(event) => setRejectReason(event.target.value)}
            placeholder="e.g. Lease document illegible — please re-upload"
            type="text"
            value={rejectReason}
          />
        </div>
      </div>

      <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="flex items-center justify-between gap-2.5">
          <div className="text-[13px] font-semibold">Verified members</div>
          <SampleDataChip />
        </div>
        {(
          [
            ["✓ Sasha Chin · Unit 10 · Owner", "Verified Nov 2"],
            ["✓ Robert Palmer · Unit 14 · Owner", "Verified Oct 19"],
          ] as const
        ).map(([member, when]) => (
          <div className="flex items-center justify-between gap-2.5 border-b border-shell py-2" key={member}>
            <span className="text-[13.5px] font-semibold text-success-text">{member}</span>
            <span className="text-xs text-sand-500">{when}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

/* PM-RPT (DS v2) — portfolio reports. VISION screen: every figure is sample
   data (each card chipped) until the portfolio reports API lands. */
export function PropertyManagerReportsPage() {
  const [period, setPeriod] = useState("YTD 2026");
  const stats = [
    ["PORTFOLIO REVENUE", "$248,300"],
    ["AVG OCCUPANCY", "71%"],
    ["MAINTENANCE SPEND", "$18,450"],
    ["UTILITIES BILLED BACK", "$9,320"],
  ] as const;
  const owners = [
    ["Sasha Chin", "4", "$112,400", "−$11,240", "$101,160"],
    ["Robert Palmer", "2", "$76,100", "−$7,610", "$68,490"],
    ["Ocho Palms Ltd.", "3", "$59,800", "−$5,980", "$53,820"],
  ] as const;
  const ownerCols = "grid grid-cols-[1.3fr_0.6fr_0.9fr_0.9fr_0.9fr] items-center gap-3";

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="PM-RPT">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
          Portfolio <em className="italic text-deep-hover">reports</em>
        </h1>
        <div className="flex flex-wrap gap-2.5">
          <select
            aria-label="Reporting period"
            className={pmSelectPill}
            onChange={(event) => setPeriod(event.target.value)}
            value={period}
          >
            <option>YTD 2026</option>
            <option>Q4 2026</option>
          </select>
          <button className={pmOutlinePill} type="button">
            Export CSV
          </button>
          <button className={pmOutlinePill} type="button">
            Export PDF
          </button>
          <button className={pmOutlinePill} type="button">
            Tax export
          </button>
        </div>
      </div>

      <div className="grid grid-cols-[repeat(auto-fit,minmax(190px,1fr))] gap-3.5">
        {stats.map(([label, value]) => (
          <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]" key={label}>
            <div className="flex items-start justify-between gap-2">
              <div className="text-[11px] font-semibold tracking-[0.16em] text-sand-500">{label}</div>
              <SampleDataChip />
            </div>
            <div className="font-display text-[30px] font-medium leading-none">{value}</div>
          </div>
        ))}
      </div>

      <div className="overflow-hidden rounded-card border border-sand-border bg-cream">
        <div className="flex items-center justify-between gap-2.5 border-b border-shell px-5 py-3">
          <span className="text-[13px] font-semibold">Owner statements</span>
          <SampleDataChip />
        </div>
        <div className={cx(ownerCols, "bg-shell px-5 py-3 text-[11px] font-bold tracking-[0.1em] text-sand-500")}>
          <span>OWNER</span>
          <span>UNITS</span>
          <span>REVENUE</span>
          <span>PM FEES</span>
          <span>NET PAYOUT</span>
        </div>
        {owners.map(([owner, units, revenue, fees, net], index) => (
          <div
            className={cx(ownerCols, "border-b border-shell px-5 py-3 text-[13px]", index % 2 === 1 && "bg-sand")}
            key={owner}
          >
            <strong>{owner}</strong>
            <span>{units}</span>
            <span>{revenue}</span>
            <span className="text-coral-text">{fees}</span>
            <strong>{net}</strong>
          </div>
        ))}
      </div>
    </div>
  );
}

/* PM-INS (DS v2) — "Property Protection — Powered by InsuraGuest". VISION
   screen: the $50/$69/$99 tiers are the design-spec figures; no insurance API
   is wired yet, so every priced card is chipped. */
export function InsuraGuestPage() {
  const plans = [
    {
      name: "Basic",
      price: "$50",
      cap: "$25,000 coverage cap",
      features: ["✓ Damage protection", "✓ Guest injury liability"],
      current: false,
    },
    {
      name: "Standard",
      price: "$69",
      cap: "$50,000 coverage cap",
      features: ["✓ Everything in Basic", "✓ Theft coverage", "✓ Loss-of-income (7 days)"],
      current: true,
    },
    {
      name: "Premium",
      price: "$99",
      cap: "$100,000 coverage cap",
      features: ["✓ Everything in Standard", "✓ Full loss-of-income", "✓ Legal assistance"],
      current: false,
    },
  ];

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="PM-INS">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        Property Protection <em className="italic text-deep-hover">— Powered by InsuraGuest</em>
      </h1>

      <div className="flex flex-wrap items-center justify-between gap-2.5 rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="text-sm">
          <strong>Active plan:</strong> Standard — renews Feb 1, 2027
        </div>
        <div className="flex items-center gap-1.5">
          <StatusChip value="Active" />
          <SampleDataChip />
        </div>
      </div>

      <div className="grid grid-cols-[repeat(auto-fit,minmax(250px,1fr))] gap-4">
        {plans.map((plan) => (
          <div
            className={cx(
              "flex flex-col gap-2.5 rounded-card p-6",
              plan.current ? "bg-deep" : "border border-sand-border bg-cream",
            )}
            key={plan.name}
          >
            <div className="flex items-center justify-between gap-2">
              <span className={cx("font-display text-xl font-medium", plan.current && "text-on-dark-heading")}>
                {plan.name}
              </span>
              <div className="flex items-center gap-1.5">
                {plan.current && (
                  <span className="inline-flex items-center rounded-pill bg-yellow/15 px-2.5 py-1 text-[10.5px] font-bold tracking-[0.06em] text-yellow">
                    CURRENT
                  </span>
                )}
                <SampleDataChip />
              </div>
            </div>
            <div className={cx("font-display text-[32px] font-medium", plan.current && "text-on-dark-heading")}>
              {plan.price}
              <span className={cx("font-sans text-[13px]", plan.current ? "text-on-dark-muted" : "text-sand-500")}> / month</span>
            </div>
            <div className={cx("text-[12.5px] font-bold", plan.current ? "text-on-dark-muted" : "text-gray-600")}>{plan.cap}</div>
            <div className={cx("flex flex-col gap-[5px] text-[12.5px]", plan.current ? "text-on-dark-muted" : "text-gray-600")}>
              {plan.features.map((feature) => (
                <span key={feature}>{feature}</span>
              ))}
            </div>
            {plan.current ? (
              <button
                className="mt-auto inline-flex min-h-[46px] cursor-pointer items-center justify-center rounded-pill border-[1.5px] border-on-dark-heading/40 bg-transparent font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:border-on-dark-heading"
                type="button"
              >
                Manage plan
              </button>
            ) : (
              <button
                className="mt-auto inline-flex min-h-[46px] cursor-pointer items-center justify-center rounded-pill border-none bg-deep font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover"
                type="button"
              >
                Subscribe
              </button>
            )}
          </div>
        ))}
      </div>

      <div className="rounded-field bg-shell px-4 py-3 text-xs text-sand-500">
        Integration subject to provider agreement — screen reflects final UI.
      </div>
    </div>
  );
}

/* OFC-DIR (DS v2) — police directory, WELLNESS HOSTS ONLY. Contractual: badge
   IDs only (NST-OFC-XXXX), zero names/photos/contacts. Rows are sample data
   until the officers API lands. */
export function PoliceDirectoryPage() {
  const officers = [
    { badge: "NST-OFC-4821", parish: "Westmoreland", availability: "AVAILABLE" },
    { badge: "NST-OFC-2210", parish: "St. Elizabeth", availability: "AVAILABLE" },
    { badge: "NST-OFC-3377", parish: "Westmoreland", availability: "ON DUTY" },
    { badge: "NST-OFC-1904", parish: "St. Andrew", availability: "UNAVAILABLE" },
  ];
  const availabilityChip = (availability: string) =>
    availability === "AVAILABLE"
      ? "bg-success-tint text-success-text"
      : availability === "ON DUTY"
        ? "bg-info-tint text-info-text"
        : "bg-shell text-sand-500";
  const rowCols = "grid grid-cols-[1fr_1fr_1fr_1fr] items-center gap-3";

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="OFC-DIR">
      <div className="flex flex-wrap items-center gap-2.5">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
          Police <em className="italic text-deep-hover">directory</em>
        </h1>
        <span className="inline-flex items-center rounded-pill bg-mint-tint px-2.5 py-1 text-[10.5px] font-bold tracking-[0.06em] text-mint-text">
          WELLNESS ACCESS REQUIRED
        </span>
      </div>

      <div className="rounded-nav bg-deep px-5 py-3.5 text-[13px] text-on-dark-body">
        This directory is restricted. All communications are through NestyStay platform only.
      </div>

      <div className="overflow-hidden rounded-card border border-sand-border bg-cream">
        <div className="flex items-center justify-between gap-2.5 border-b border-shell px-5 py-3">
          <span className="text-[13px] font-semibold">Officer availability</span>
          <SampleDataChip />
        </div>
        <div className={cx(rowCols, "bg-shell px-5 py-3 text-[11px] font-bold tracking-[0.1em] text-sand-500")}>
          <span>BADGE ID</span>
          <span>PARISH</span>
          <span>AVAILABILITY</span>
          <span />
        </div>
        {officers.map((officer, index) => (
          <div
            className={cx(rowCols, "border-b border-shell px-5 py-3 text-[13px]", index % 2 === 1 && "bg-sand")}
            key={officer.badge}
          >
            <span className="flex items-center gap-2.5">
              <span className="inline-flex size-11 shrink-0 items-center justify-center rounded-full bg-deep text-[10px] font-extrabold text-yellow">
                JCF
              </span>
              <span className="font-mono text-[12.5px] font-bold">{officer.badge}</span>
            </span>
            <span>{officer.parish}</span>
            <span>
              <span
                className={cx(
                  "inline-flex items-center rounded-pill px-2.5 py-1 text-[10.5px] font-bold tracking-[0.06em]",
                  availabilityChip(officer.availability),
                )}
              >
                {officer.availability}
              </span>
            </span>
            <span className="text-right">
              {officer.availability === "AVAILABLE" ? (
                <AppLink
                  className="inline-flex min-h-[46px] items-center gap-2 rounded-pill bg-deep px-[22px] font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover"
                  href="/host/wellness/book"
                >
                  Request wellness visit
                </AppLink>
              ) : (
                <span className="text-xs text-sand-500">—</span>
              )}
            </span>
          </div>
        ))}
      </div>

      <div className="text-xs text-sand-500">
        No names, no photos, no direct contact — badge IDs only (rule 1). Traveler-facing copy says &quot;private
        security&quot;, never &quot;police&quot; (§9.4).
      </div>
    </div>
  );
}

/* OFC-BOOK (DS v2) — wellness visit booking. Property list is live (useProperties);
   visit-type pricing shows the design-spec backend values, flagged under client
   arbitration §9.1 and chipped as sample until the pricing API lands. Officers
   are badge IDs only. */
export function WellnessBookingPage() {
  const { properties } = useProperties();
  const [pending, setPending] = useState(false);
  const [visitType, setVisitType] = useState("standard");

  useEffect(() => {
    if (!pending) return;
    const timer = window.setTimeout(() => setPending(false), 6000);
    return () => window.clearTimeout(timer);
  }, [pending]);

  const visitTypes = [
    { id: "standard", name: "Standard wellness check", detail: "60 min walk-through, notes + photos", price: "$85" },
    { id: "idcheck", name: "In-person ID check", detail: "75 min, verifies guest identity on arrival", price: "$125" },
    { id: "driveby", name: "Drive-by check", detail: "45 min exterior pass", price: "$65" },
  ];
  const fieldClass =
    "min-h-12 rounded-field border-[1.5px] border-sand-input bg-white px-4 font-sans text-[14.5px] text-ink outline-none focus:border-deep-hover";

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="OFC-BOOK">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        Book a wellness <em className="italic text-deep-hover">visit</em>
      </h1>

      <form
        className="flex flex-col gap-3.5 rounded-card border border-sand-border bg-cream p-[22px]"
        onSubmit={(event) => {
          event.preventDefault();
          setPending(true);
        }}
      >
        <label className="flex max-w-[380px] flex-col gap-1.5">
          <span className="text-[13px] font-semibold">Property</span>
          <select className={fieldClass}>
            {properties.map((property) => (
              <option key={property.id}>{property.title}</option>
            ))}
            {properties.length === 0 && <option>Loading properties…</option>}
          </select>
        </label>

        <div className="flex flex-wrap items-center gap-2 text-[13px] font-semibold">
          Visit type{" "}
          <span className="text-xs font-semibold text-coral-text">
            (naming &amp; pricing under client arbitration §9.1 — backend values shown)
          </span>
          <SampleDataChip />
        </div>
        <div className="flex flex-col gap-2.5">
          {visitTypes.map((type) => (
            <label
              className={cx(
                "flex cursor-pointer items-center gap-3.5 rounded-[16px] bg-cream px-[18px] py-3.5",
                visitType === type.id
                  ? "border-2 border-deep-hover shadow-[0_0_0_3px_rgba(14,74,69,0.1)]"
                  : "border-[1.5px] border-sand-border",
              )}
              key={type.id}
            >
              <input
                checked={visitType === type.id}
                className="size-5 accent-deep-hover"
                name="visit-type"
                onChange={() => setVisitType(type.id)}
                type="radio"
              />
              <span className="flex-1">
                <span className="block text-sm font-semibold">{type.name}</span>
                <span className="block text-xs text-gray-600">{type.detail}</span>
              </span>
              <strong className="font-display text-lg">{type.price}</strong>
            </label>
          ))}
        </div>

        <div className="grid max-w-[420px] grid-cols-2 gap-3">
          <label className="flex flex-col gap-1.5">
            <span className="text-[13px] font-semibold">Date</span>
            <input className={fieldClass} defaultValue="2026-12-13" type="date" />
          </label>
          <label className="flex flex-col gap-1.5">
            <span className="text-[13px] font-semibold">Time</span>
            <input className={fieldClass} defaultValue="10:00" type="time" />
          </label>
        </div>

        <div className="flex flex-wrap items-center gap-2.5 text-[13px]">
          <span className="font-semibold">Officer:</span>
          <span className="font-mono text-[12.5px] font-bold">NST-OFC-4821</span>
          <span className="inline-flex items-center rounded-pill bg-success-tint px-2.5 py-1 text-[10.5px] font-bold tracking-[0.06em] text-success-text">
            AVAILABLE DEC 13
          </span>
          <SampleDataChip />
        </div>

        <button
          className="inline-flex min-h-[46px] cursor-pointer items-center gap-2 self-start rounded-pill border-none bg-deep px-[22px] font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover"
          type="submit"
        >
          Request visit
        </button>
      </form>

      <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="text-[13px] font-semibold">After requesting</div>
        {pending ? (
          <div className="flex flex-col gap-2.5">
            <div className="flex flex-wrap items-center gap-2">
              <StatusChip value="Requested" />
              <span className="text-[12.5px] text-gray-600">
                Visit request sent — countdown active while the officer accepts through the platform.
              </span>
            </div>
            <div className="h-1.5 w-full overflow-hidden rounded-pill bg-shell">
              <span className="block h-full w-1/3 animate-pulse rounded-pill bg-amber" />
            </div>
          </div>
        ) : (
          <div className="flex flex-wrap items-center gap-2">
            <StatusChip value="Requested" />
            <span className="text-sand-500">→</span>
            <span className="text-[12.5px] text-gray-600">
              acceptance countdown <strong>23:12</strong> shown
            </span>
            <span className="text-sand-500">→</span>
            <StatusChip value="Scheduled" />
            <span className="text-sand-500">→</span>
            <StatusChip value="Completed" />
          </div>
        )}
        <div className="text-xs text-sand-500">All communication remains mediated by NestyStay.</div>
      </div>
    </div>
  );
}

/* DIR-BIZ (DS v2, spec-URL variant) — local business directory. VISION screen:
   listings are sample data (chipped). Trusted = featured Deep card (spec). The
   live route /directory/businesses renders the API-backed DirectorySpecPage. */
export function BusinessDirectoryPage() {
  const [category, setCategory] = useState("All");
  const categories = ["All", "Restaurants", "Tours", "Transport", "Water Sports", "Photography", "Gift Shops"];
  const businesses = [
    {
      name: "Miss Cherry's Kitchen",
      category: "Restaurant",
      where: "Negril · Westmoreland · 8am – 10pm",
      copy: "Ital stews, fresh-catch grill, and the West End's best view at sunset.",
      trusted: true,
    },
    {
      name: "Blue Hole River Tours",
      category: "Tours",
      where: "Port Antonio · Portland · 9am – 4pm",
      copy: "Guided swims, bamboo rafting and Blue Lagoon runs.",
      trusted: false,
    },
    {
      name: "Reliable Rides JA",
      category: "Transport",
      where: "Montego Bay · St. James · 24 hrs",
      copy: "Airport transfers and island-wide private drivers.",
      trusted: false,
    },
  ];

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="DIR-BIZ">
      <div className="flex flex-wrap items-center gap-2.5">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
          Local <em className="italic text-deep-hover">businesses</em>
        </h1>
        <SampleDataChip />
      </div>

      <div className="flex flex-wrap items-center gap-2">
        {categories.map((item) => (
          <button
            className={cx(
              "inline-flex min-h-11 cursor-pointer items-center rounded-pill px-[18px] font-sans text-[13px] font-semibold transition-colors",
              category === item
                ? "border-none bg-deep text-on-dark-heading"
                : "border-[1.5px] border-sand-input bg-transparent text-gray-600 hover:border-deep hover:text-ink",
            )}
            key={item}
            onClick={() => setCategory(item)}
            type="button"
          >
            {item}
          </button>
        ))}
        <select
          aria-label="Parish filter"
          className="ml-auto min-h-11 rounded-pill border-[1.5px] border-sand-input bg-cream px-4 font-sans text-[13px] font-semibold text-ink outline-none focus:border-deep-hover"
        >
          <option>All parishes</option>
          <option>Westmoreland</option>
          <option>Portland</option>
        </select>
      </div>

      <div className="grid grid-cols-[repeat(auto-fill,minmax(290px,1fr))] gap-4">
        {businesses
          .filter((business) => category === "All" || business.category.startsWith(category.replace(/s$/, "")))
          .map((business) =>
            business.trusted ? (
              <div className="flex flex-col gap-2.5 rounded-card bg-deep p-[22px]" key={business.name}>
                <div className="flex justify-between gap-2.5">
                  <span className="inline-flex items-center rounded-pill bg-yellow/15 px-2.5 py-1 text-[10.5px] font-bold tracking-[0.06em] text-yellow">
                    FEATURED
                  </span>
                  <span className="inline-flex items-center gap-1.5 rounded-pill bg-yellow px-3 py-1 text-[10.5px] font-bold tracking-[0.1em] text-deep">
                    ★ TRUSTED
                  </span>
                </div>
                <div className="font-display text-[19px] font-medium text-on-dark-heading">{business.name}</div>
                <div className="flex flex-wrap items-center gap-1.5">
                  <span className="inline-flex items-center rounded-pill bg-mint-tint px-2.5 py-1 text-[10.5px] font-bold tracking-[0.06em] text-mint-text">
                    {business.category.toUpperCase()}
                  </span>
                  <span className="text-xs text-on-dark-muted">{business.where}</span>
                </div>
                <div className="text-[13px] text-on-dark-muted">{business.copy}</div>
                <button
                  className="mt-auto inline-flex min-h-[46px] cursor-pointer items-center justify-center rounded-pill border-none bg-yellow font-sans text-[13.5px] font-bold text-deep transition-colors hover:bg-yellow-press"
                  type="button"
                >
                  View business
                </button>
              </div>
            ) : (
              <div className="flex flex-col gap-2.5 rounded-card border border-sand-border bg-cream p-[22px]" key={business.name}>
                <div className="font-display text-[19px] font-medium">{business.name}</div>
                <div className="flex flex-wrap items-center gap-1.5">
                  <span className="inline-flex items-center rounded-pill bg-mint-tint px-2.5 py-1 text-[10.5px] font-bold tracking-[0.06em] text-mint-text">
                    {business.category.toUpperCase()}
                  </span>
                  <span className="text-xs text-gray-600">{business.where}</span>
                </div>
                <div className="text-[13px] text-gray-600">{business.copy}</div>
                <div className="mt-auto flex items-center justify-between gap-2.5">
                  <span className="text-[11.5px] text-sand-500">Free listing</span>
                  <button
                    className="inline-flex min-h-[46px] cursor-pointer items-center rounded-pill border-[1.5px] border-sand-input bg-transparent px-5 font-sans text-[13.5px] font-semibold text-ink transition-colors hover:border-deep"
                    type="button"
                  >
                    View business
                  </button>
                </div>
              </div>
            ),
          )}
      </div>
    </div>
  );
}

/* DIR-PROV (DS v2, spec-URL variant) — provider dashboard. VISION screen: all
   figures sample data (chipped). The live route /directory/provider renders the
   API-backed ProviderPortal. */
export function ProviderDashboardPage() {
  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="DIR-PROV">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        Your provider <em className="italic text-deep-hover">profile</em>
      </h1>

      <div className="grid grid-cols-[repeat(auto-fit,minmax(300px,1fr))] gap-4">
        <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
          <div className="flex items-center gap-3.5">
            <span className="inline-flex size-[60px] shrink-0 items-center justify-center rounded-full bg-deep font-display text-[22px] text-yellow">
              DC
            </span>
            <div>
              <div className="font-display text-[19px] font-medium">Delroy Campbell</div>
              <div className="text-[12.5px] text-gray-600">Electrician · Westmoreland + St. James</div>
            </div>
          </div>
          <div className="text-[13px] text-gray-600">
            &quot;20 years wiring villas from Negril to MoBay. Licensed and insured.&quot;
          </div>
          <div className="flex flex-wrap items-center justify-between gap-2.5">
            <TierBadge level="Trusted" />
            <div className="flex items-center gap-1.5">
              <StatusChip value="Renews in 30 days" />
              <SampleDataChip />
            </div>
          </div>
          <div className="text-xs text-sand-500">Trusted Badge: $120/year or $12/month — identical for all user types.</div>
        </div>

        <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
          <div className="flex items-center justify-between gap-2.5">
            <div className="text-[13px] font-semibold">Services &amp; prices</div>
            <SampleDataChip />
          </div>
          {(
            [
              ["Call-out & diagnosis", "$40"],
              ["Panel upgrade", "from $350"],
              ["Pool pump circuit", "from $180"],
            ] as const
          ).map(([service, price]) => (
            <div className="flex justify-between border-b border-shell py-2 text-[13.5px]" key={service}>
              <span>{service}</span>
              <strong>{price}</strong>
            </div>
          ))}
          <button className="inline-flex min-h-11 cursor-pointer items-center self-start border-none bg-transparent p-0 font-sans text-[13px] font-bold text-deep-hover" type="button">
            + Add service
          </button>
        </div>

        <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
          <div className="flex items-center justify-between gap-2.5">
            <div className="text-[13px] font-semibold">Incoming requests</div>
            <SampleDataChip />
          </div>
          <div className="flex items-center justify-between gap-2.5 border-b border-shell py-2">
            <div>
              <div className="text-[13.5px] font-semibold">Cliffside Retreat — Marcia</div>
              <div className="text-xs text-gray-600">Generator transfer switch quote</div>
            </div>
            <span className="inline-flex items-center rounded-pill bg-amber-tint px-2.5 py-1 text-[10.5px] font-bold tracking-[0.06em] text-amber-text">
              NEW
            </span>
          </div>
          <div className="flex items-center justify-between gap-2.5 border-b border-shell py-2">
            <div>
              <div className="text-[13.5px] font-semibold">Ocho Palms PM — S. Chin</div>
              <div className="text-xs text-gray-600">Unit 12 breaker keeps tripping</div>
            </div>
            <span className="inline-flex items-center rounded-pill bg-info-tint px-2.5 py-1 text-[10.5px] font-bold tracking-[0.06em] text-info-text">
              REPLIED
            </span>
          </div>
          <div className="text-xs text-sand-500">All messages stay in the platform inbox.</div>
        </div>

        <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
          <div className="flex items-center justify-between gap-2.5">
            <div className="text-[11px] font-semibold tracking-[0.16em] text-sand-500">EARNINGS — YTD</div>
            <SampleDataChip />
          </div>
          <div className="font-display text-[32px] font-medium leading-none">$7,420</div>
          <div className="text-[12.5px] text-success-text">18 completed jobs via NestyStay</div>
          <div className="mt-1.5 text-[13px] font-semibold">Availability</div>
          <div className="flex flex-wrap gap-1.5">
            {(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"] as const).map((day, index) => (
              <span
                className={cx(
                  "inline-flex size-11 items-center justify-center rounded-nav text-xs font-bold",
                  index < 5 ? "bg-success-tint text-success-text" : "bg-shell text-sand-500",
                )}
                key={day}
              >
                {day}
              </span>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

/* ADM-KPI (DS v2) — analytics. VISION data (chipped) until the KPI API lands. */
export function AdminKpiPage() {
  const [period, setPeriod] = useState("30d");
  const chartCard = "flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]";
  const chartHead = (title: string) => (
    <div className="flex items-baseline justify-between gap-2">
      <div className="flex items-center gap-2">
        <div className="text-[13px] font-semibold">{title}</div>
        <SampleDataChip />
      </div>
      <button className="inline-flex min-h-11 cursor-pointer items-center border-none bg-transparent p-0 text-xs font-bold text-deep-hover" type="button">
        ↓ CSV
      </button>
    </div>
  );
  const track = "h-2 rounded-pill bg-shell";
  const barRow = (label: string, value: string, width: string, color: string) => (
    <div className="flex flex-col gap-1" key={label}>
      <div className="flex justify-between text-[12.5px]">
        <span>{label}</span>
        <strong>{value}</strong>
      </div>
      <div className={track}>
        <div className="h-full rounded-pill" style={{ width, background: color }} />
      </div>
    </div>
  );

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="ADM-KPI">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">Analytics</h1>
        <div className="flex gap-1.5">
          {["7d", "30d", "90d", "YTD"].map((option) => (
            <button
              className={cx(
                "inline-flex min-h-11 cursor-pointer items-center rounded-pill px-[18px] font-sans text-[13px] font-semibold transition-colors",
                period === option
                  ? "border-none bg-deep font-bold text-on-dark-heading"
                  : "border-[1.5px] border-sand-input bg-transparent text-gray-600 hover:border-deep hover:text-ink",
              )}
              key={option}
              onClick={() => setPeriod(option)}
              type="button"
            >
              {option}
            </button>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-[repeat(auto-fit,minmax(320px,1fr))] gap-4">
        <div className={chartCard}>
          {chartHead("New users / week")}
          <div className="flex h-[120px] items-end gap-2">
            {[42, 55, 48, 71, 64, 89].map((height, index) => (
              <div className="flex-1 rounded-t-[6px] bg-deep-hover" key={index} style={{ height: `${height}%` }} />
            ))}
          </div>
          <div className="text-xs text-success-text">▲ 18% vs previous period</div>
        </div>

        <div className={chartCard}>
          {chartHead("Bookings / week")}
          <svg aria-label="Bookings per week line chart" className="h-[120px] w-full" viewBox="0 0 300 120">
            <polyline fill="none" points="0,90 50,74 100,80 150,52 200,60 250,34 300,28" stroke="#0E4A45" strokeLinejoin="round" strokeWidth="3" />
            <polyline fill="rgba(14,74,69,0.08)" points="0,90 50,74 100,80 150,52 200,60 250,34 300,28 300,120 0,120" stroke="none" />
          </svg>
          <div className="text-xs text-success-text">▲ 11% vs previous period</div>
        </div>

        <div className={chartCard}>
          {chartHead("Revenue / month")}
          <div className="flex h-[120px] items-end gap-2">
            {[38, 46, 52, 61, 58, 74].map((height, index) => (
              <div className="flex-1 rounded-t-[6px] bg-[#D9A800]" key={index} style={{ height: `${height}%` }} />
            ))}
          </div>
          <div className="text-xs text-success-text">▲ 9% vs previous period</div>
        </div>

        <div className={chartCard}>
          {chartHead("Verification success")}
          <div className="flex items-center gap-5">
            <svg aria-label="Verification success donut 94%" className="size-[110px]" viewBox="0 0 120 120">
              <circle cx="60" cy="60" fill="none" r="46" stroke="#FBE7E6" strokeWidth="16" />
              <circle cx="60" cy="60" fill="none" r="46" stroke="#1B7A4B" strokeDasharray="271.7 289.03" strokeLinecap="round" strokeWidth="16" transform="rotate(-90 60 60)" />
              <text fill="#12241F" fontFamily="Fraunces,serif" fontSize="24" textAnchor="middle" x="60" y="66">
                94%
              </text>
            </svg>
            <div className="flex flex-col gap-1.5 text-[12.5px] text-gray-600">
              <span>
                <span className="mr-1.5 inline-block size-2.5 rounded-[3px] bg-[#1B7A4B]" />
                Passed — 94%
              </span>
              <span>
                <span className="mr-1.5 inline-block size-2.5 rounded-[3px] bg-[#D64F45]" />
                Failed — 6%
              </span>
            </div>
          </div>
        </div>

        <div className={chartCard}>
          {chartHead("Cancellations by type")}
          {barRow("Traveler-initiated", "62%", "62%", "#D64F45")}
          {barRow("Host-initiated", "24%", "24%", "#D9A800")}
          {barRow("Verification failed", "14%", "14%", "#8A7F5E")}
        </div>

        <div className={chartCard}>
          {chartHead("Host response time by badge")}
          {barRow("◆ Wellness", "41 min", "82%", "#2FB893")}
          {barRow("★ Trusted", "1.2 hrs", "66%", "#062B2B")}
          {barRow("✓ Verified", "3.4 hrs", "38%", "#1B7A4B")}
          {barRow("Free", "9.1 hrs", "14%", "#8A7F5E")}
        </div>
      </div>
    </div>
  );
}

/* ADM-RPT (DS v2) — 6 report types + scheduled email reports (VISION, chipped). */
export function AdminReportsPage() {
  const reports = [
    ["User growth", "Sign-ups, activations, churn by role"],
    ["Bookings", "Volume, value, parish breakdown"],
    ["Revenue & commission", "GMV, platform take, payouts"],
    ["Verification audit", "eKYC pass/fail trail, per property"],
    ["Cancellations & refunds", "Reasons, refundable splits, fees kept"],
    ["Jamaica tax compliance", "GCT-ready ledger, platform fees separated"],
  ] as const;
  const scheduled = [
    ["Revenue & commission", "Weekly · Mondays 07:00 · ops@nestystay.net", "Active"],
    ["Jamaica tax compliance", "Monthly · 1st · finance@nestystay.net", "Active"],
    ["Verification audit", "Weekly · paused", "Paused"],
  ] as const;

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="ADM-RPT">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        Reports &amp; <em className="italic text-deep-hover">compliance</em>
      </h1>

      <div className="grid grid-cols-[repeat(auto-fill,minmax(300px,1fr))] gap-4">
        {reports.map(([title, copy]) => (
          <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]" key={title}>
            <div className="font-display text-lg font-medium">{title}</div>
            <div className="text-[12.5px] text-gray-600">{copy}</div>
            <div className="mt-auto flex gap-2">
              <button className={pmDeepPill} type="button">
                Generate
              </button>
              <button className={pmOutlinePill} type="button">
                PDF
              </button>
              <button className={pmOutlinePill} type="button">
                CSV
              </button>
            </div>
          </div>
        ))}
      </div>

      <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="flex items-center justify-between gap-2.5">
          <div className="text-[13px] font-semibold">Scheduled reports</div>
          <SampleDataChip />
        </div>
        {scheduled.map(([title, cadence, status]) => (
          <div className="flex items-center justify-between gap-2.5 border-b border-shell py-2" key={title}>
            <div>
              <div className="text-[13.5px] font-semibold">{title}</div>
              <div className="text-xs text-sand-500">{cadence}</div>
            </div>
            <StatusChip value={status} />
          </div>
        ))}
        <button className="inline-flex min-h-11 cursor-pointer items-center self-start border-none bg-transparent p-0 font-sans text-[12.5px] font-bold text-deep-hover" type="button">
          + Schedule a report
        </button>
      </div>
    </div>
  );
}

/* ADM-RESET (DS v2) — rule 4: "No Override" + "Zero Trace"; no control may
   suggest linking old→new IDs. Figures are VISION data (chipped). Schedule
   statuses use the contractual StatusChip tones. */
export function OfficerIdResetPage() {
  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="ADM-RESET">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        Annual officer ID <em className="italic text-deep-hover">reset</em>
      </h1>

      <div className="flex flex-col gap-3.5 rounded-card bg-deep p-[26px]">
        <div className="flex flex-wrap items-baseline justify-between gap-3">
          <span className="font-display text-2xl font-medium text-on-dark-heading">Next reset: January 1, 2027</span>
          <span className="flex items-center gap-2">
            <span className="font-display text-[30px] text-yellow">in 155 days</span>
            <SampleDataChip />
          </span>
        </div>
        <div className="h-2 rounded-pill bg-on-dark-heading/10">
          <div className="h-full w-[58%] rounded-pill bg-yellow" />
        </div>
        <div className="text-[13px] text-on-dark-muted">
          1,240 officers enrolled · every NST-OFC-XXXX ID regenerates at 00:00 JST
        </div>
      </div>

      <div className="grid grid-cols-[repeat(auto-fit,minmax(280px,1fr))] gap-4">
        <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
          <div className="flex items-center gap-3">
            <span className="inline-flex size-11 shrink-0 items-center justify-center rounded-nav bg-coral-tint text-coral-text">
              <ShieldAlert size={20} />
            </span>
            <div className="font-display text-[19px] font-medium">No Override</div>
          </div>
          <div className="text-[13px] text-gray-600">
            The reset cannot be postponed, skipped, or applied selectively — for any officer, by any admin, ever.
          </div>
        </div>
        <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
          <div className="flex items-center gap-3">
            <span className="inline-flex size-11 shrink-0 items-center justify-center rounded-nav bg-info-tint text-info-text">
              <KeyRound size={20} />
            </span>
            <div className="font-display text-[19px] font-medium">Zero Trace</div>
          </div>
          <div className="text-[13px] text-gray-600">
            No mapping between old and new IDs is stored or derivable. Historical records keep the ID that was current at
            the time.
          </div>
        </div>
      </div>

      <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="flex items-center justify-between gap-2.5">
          <div className="text-[13px] font-semibold">Reset schedule</div>
          <SampleDataChip />
        </div>
        {(
          [
            ["Jan 1, 2027", "1,240 IDs regenerate", "Scheduled"],
            ["Jan 1, 2026", "1,082 IDs regenerated", "Completed"],
            ["Jan 1, 2025", "914 IDs regenerated", "Completed"],
          ] as const
        ).map(([date, detail, status]) => (
          <div className="flex items-center justify-between gap-2.5 border-b border-shell py-2" key={date}>
            <div>
              <div className="text-[13.5px] font-semibold">{date}</div>
              <div className="text-xs text-sand-500">{detail}</div>
            </div>
            <StatusChip value={status} />
          </div>
        ))}
        <div className="text-xs text-sand-500">By design there is no per-officer view here — the schedule is the only control.</div>
      </div>
    </div>
  );
}

/* ERR-* (DS v2) — centered full-height states: emblem roundel, line icon,
   Fraunces title, muted copy. Footer stays with the app shell. */
function ErrorTemplate({
  id,
  icon: Icon,
  title,
  copy,
  children,
}: {
  id: string;
  icon?: LucideIcon;
  title: ReactNode;
  copy: string;
  children?: ReactNode;
}) {
  return (
    <div className="flex min-h-[72vh] flex-col items-center justify-center gap-4 px-6 py-[72px] text-center font-sans text-ink" id={id}>
      <EmblemRoundel size={48} />
      {Icon && <Icon className="text-sand-500" size={48} strokeWidth={1.6} />}
      <h1 className="m-0 font-display text-[clamp(32px,4vw,44px)] font-normal">{title}</h1>
      <div className="max-w-[420px] text-[14.5px] text-gray-600">{copy}</div>
      {children}
      <div className="mt-6 text-[13px] text-sand-500">
        nestystay.net ·{" "}
        <a className="text-sand-500 hover:text-ink" href="https://wa.me/17542482435">
          754-248-2435
        </a>
      </div>
    </div>
  );
}

const errDeepPill =
  "inline-flex min-h-12 items-center gap-2.5 rounded-pill bg-deep px-[26px] font-sans text-[14.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover";
const errOutlinePill =
  "inline-flex min-h-12 cursor-pointer items-center rounded-pill border-[1.5px] border-sand-input bg-transparent px-[26px] font-sans text-[14.5px] font-semibold text-ink transition-colors hover:border-deep";

export function SignInRequiredPage() {
  return (
    <ErrorTemplate
      copy="This part of NestyStay needs an account. Log in, or keep browsing as a guest."
      icon={Lock}
      id="ERR-401"
      title="Sign in required."
    >
      <div className="mt-1.5 flex flex-wrap justify-center gap-3">
        <AppLink className={errDeepPill} href="/login">
          Log in →
        </AppLink>
        <AppLink className={errOutlinePill} href="/explore">
          Browse as guest
        </AppLink>
      </div>
      <AppLink className="inline-flex min-h-11 items-center text-[13.5px] font-semibold text-deep-hover" href="/register">
        Create account
      </AppLink>
    </ErrorTemplate>
  );
}

/* ERR-403 — security rule: never leak which permission was missing. */
export function AccessRestrictedPage() {
  return (
    <ErrorTemplate
      copy="Your account can't open this page. If you think this is a mistake, reach out to support."
      icon={ShieldAlert}
      id="ERR-403"
      title="Access is restricted."
    >
      <div className="mt-1.5 flex flex-wrap justify-center gap-3">
        <AppLink className={errDeepPill} href="/">
          Return safely
        </AppLink>
      </div>
      <a className="inline-flex min-h-11 items-center text-[13.5px] font-semibold text-deep-hover" href="https://wa.me/17542482435">
        Contact support
      </a>
    </ErrorTemplate>
  );
}

/* ERR-404 — approved patois lexicon, always paired with English. */
export function NotFoundPage() {
  return (
    <ErrorTemplate
      copy="This page has drifted away."
      id="ERR-404"
      title={<em className="font-display italic text-deep" style={{ fontSize: "clamp(38px,6vw,64px)", lineHeight: 1.05 }}>Dis page gone a sea</em>}
    >
      <div className="mt-1.5 flex flex-wrap justify-center gap-3">
        <AppLink className={errDeepPill} href="/explore">
          Return to trusted stays
        </AppLink>
      </div>
      <form
        className="mt-2 flex w-full max-w-[380px] items-center gap-2.5 rounded-pill border-[1.5px] border-sand-input bg-cream px-5"
        onSubmit={(event) => {
          event.preventDefault();
          navigate("/explore");
        }}
      >
        <Search className="shrink-0 text-sand-500" size={16} />
        <input
          className="min-h-[50px] flex-1 border-none bg-transparent font-sans text-[14.5px] text-ink outline-none"
          placeholder="Search stays, parishes…"
          type="text"
        />
      </form>
    </ErrorTemplate>
  );
}

/* ERR-500 — tappable WhatsApp support is required on 5xx. */
export function ServerErrorPage() {
  return (
    <ErrorTemplate
      copy="We're on it. Try again in a moment — your booking and payment data are safe."
      icon={AlertTriangle}
      id="ERR-500"
      title="Something went wrong on our side."
    >
      <div className="mt-1.5 flex flex-wrap justify-center gap-3">
        <button className={errOutlinePill} onClick={() => window.location.reload()} type="button">
          ↻ Try again
        </button>
        <AppLink className={errDeepPill} href="/explore">
          Back to Explore
        </AppLink>
      </div>
      <a
        className="mt-1.5 inline-flex min-h-12 items-center gap-2.5 rounded-pill bg-success-tint px-[22px] text-sm font-bold text-success-text"
        href="https://wa.me/17542482435"
      >
        Urgent? Message us on WhatsApp: 754-248-2435
      </a>
    </ErrorTemplate>
  );
}

/* ERR-NOFAV / ERR-NORES — dashed empty cards; navigation stays visible via the app shell. */
export function NoFavoritesPage() {
  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="ERR-NOFAV">
      <h1 className="m-0 font-display text-4xl font-normal">Collections</h1>
      <div className="flex max-w-[640px] flex-col items-center gap-2.5 rounded-card border border-dashed border-sand-input bg-cream px-6 py-14 text-center">
        <Heart className="text-sand-500" size={44} strokeWidth={1.5} />
        <div className="font-display text-[22px] font-medium">No favorites saved.</div>
        <div className="text-[13.5px] text-gray-600">Save a stay to return to it here.</div>
        <AppLink className={cx(errDeepPill, "mt-1.5")} href="/explore">
          Explore stays
        </AppLink>
      </div>
    </div>
  );
}

export function NoReservationsPage() {
  const [filterActive, setFilterActive] = useState(true);

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="ERR-NORES">
      <h1 className="m-0 font-display text-4xl font-normal">
        Your <em className="italic text-deep-hover">trips</em>
      </h1>
      {filterActive && (
        <div className="flex flex-wrap items-center gap-2">
          <button
            className="inline-flex min-h-11 cursor-pointer items-center gap-1.5 rounded-pill border-none bg-deep px-[18px] font-sans text-[13px] font-semibold text-on-dark-heading"
            onClick={() => setFilterActive(false)}
            type="button"
          >
            Cancelled ✕
          </button>
          <span className="text-[12.5px] text-sand-500">← active filter</span>
        </div>
      )}
      <div className="flex max-w-[640px] flex-col items-center gap-2.5 rounded-card border border-dashed border-sand-input bg-cream px-6 py-14 text-center">
        <CalendarDays className="text-sand-500" size={44} strokeWidth={1.5} />
        <div className="font-display text-[22px] font-medium">No reservations found.</div>
        <div className="text-[13.5px] text-gray-600">
          {filterActive ? "Nothing matches this filter." : "Explore verified stays to plan your first trip."}
        </div>
        <div className="mt-1.5 flex flex-wrap justify-center gap-2.5">
          {filterActive && (
            <button className={errOutlinePill} onClick={() => setFilterActive(false)} type="button">
              Clear filters
            </button>
          )}
          <AppLink className={errDeepPill} href="/explore">
            Explore stays
          </AppLink>
        </div>
      </div>
    </div>
  );
}

/* MSG-DOC (DS v2) — file bubble; secure link expires 24 h after download. */
export function DocumentMessagePage() {
  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="MSG-DOC">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        Secure document <em className="italic text-deep-hover">share</em>
      </h1>
      <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="flex items-center justify-between gap-2.5">
          <div className="text-[13px] font-semibold">File bubble — in thread</div>
          <SampleDataChip />
        </div>
        <div className="flex max-w-[420px] flex-col gap-2.5 self-start rounded-[16px_16px_16px_4px] bg-shell px-4 py-3.5">
          <div className="text-[13px]">Here&apos;s the rental agreement for your stay:</div>
          <div className="flex items-center gap-3 rounded-field border border-sand-border bg-cream px-3.5 py-3">
            <span className="inline-flex size-[42px] shrink-0 items-center justify-center rounded-[10px] bg-[#D64F45] text-[10px] font-extrabold text-white">
              PDF
            </span>
            <div className="min-w-0 flex-1">
              <div className="truncate text-[13.5px] font-bold">Rental-Agreement-Dec.pdf</div>
              <div className="text-[11.5px] text-sand-500">248 KB</div>
            </div>
            <button className="inline-flex min-h-[46px] cursor-pointer items-center gap-2 rounded-pill border-none bg-deep px-[22px] font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover" type="button">
              ↓ Download
            </button>
          </div>
          <div className="text-[11px] text-sand-500">Secure link · expires 24 hours after download</div>
        </div>
        <div className="flex items-center gap-2 text-xs text-sand-500">
          After download: <StatusChip value="Downloaded — link expires in 24 hrs" className="normal-case" />
        </div>
      </div>
    </div>
  );
}
