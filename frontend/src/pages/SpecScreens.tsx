import { useEffect, useState, type ReactNode } from "react";
import {
  AlertTriangle,
  CalendarDays,
  Check,
  Download,
  Heart,
  KeyRound,
  Lock,
  Search,
  ShieldAlert,
  SlidersHorizontal,
  type LucideIcon,
} from "lucide-react";
import { AppLink, navigate } from "../components/AppLink";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { EmptyState } from "../components/ui/EmptyState";
import { Field, Input, Select } from "../components/ui/Input";
import { PageHeader } from "../components/ui/PageHeader";
import { PatoisToast } from "../components/ui/PatoisToast";
import { EmblemRoundel, deepPatternBackground } from "../components/layout/PublicShell";
import { StatusChip } from "../components/ui/StatusChip";
import { useProperties } from "../hooks/useProperties";
import { api, formatMoney } from "../lib/api";
import type { AuthController } from "../hooks/useAuth";
import { usePatois } from "../lib/patois";
import { getStayImage } from "../lib/stayImages";
import { cx } from "../lib/ui";
import { LEGAL_DETAILS } from "../lib/legal";

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
    ["Coral", "#b93a32"],
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
        <a className="text-sand-500 hover:text-ink" href={LEGAL_DETAILS.supportTel}>
          {LEGAL_DETAILS.supportPhone}
        </a>
      </div>
    </div>
  );
}

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
          <a className="text-on-dark-faint hover:text-on-dark-body" href={LEGAL_DETAILS.supportTel}>
            {LEGAL_DETAILS.supportPhone}
          </a>
        </span>
      </footer>
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
          <a className="text-on-dark-faint hover:text-on-dark-body" href={LEGAL_DETAILS.supportTel}>
            {LEGAL_DETAILS.supportPhone}
          </a>
        </div>
      </footer>
    </div>
  );
}

/** TRAV-SUGG (DS v2) — explainable recommendations persisted by the API. */
export function TripSuggestionsPage({ auth }: { auth?: AuthController } = {}) {
  const { properties } = useProperties();
  const [filter, setFilter] = useState("All parishes");
  const [saved, setSaved] = useState<Record<string, boolean>>({});
  const [recommendations, setRecommendations] = useState<import("../lib/api").TravelerRecommendation[]>([]);
  const [loading, setLoading] = useState(Boolean(auth?.session));
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const userId = auth?.session?.userId;
  const token = auth?.session?.accessToken ?? "";

  useEffect(() => {
    if (!userId) { setRecommendations([]); setLoading(false); return; }
    const query = filter === "Under $300" ? { maximumNightlyRate: 300 } : filter === "Wellness hosts" ? { badgeLevel: "Wellness" } : undefined;
    let active = true;
    setLoading(true); setError(null);
    void api.getTravelerRecommendations(userId, token, query)
      .then((items) => { if (active) setRecommendations(items); })
      .catch((caught) => { if (active) setError(caught instanceof Error ? caught.message : "Recommendations could not be loaded."); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [filter, token, userId]);

  const visible = userId
    ? recommendations
    : properties.filter((property) => filter === "Under $300" ? property.nightlyRate < 300 : filter === "Wellness hosts" ? property.badgeLevel.toLowerCase().includes("well") : filter === "Beachfront" ? property.highlights.some((highlight) => /beach|ocean|sea/i.test(highlight)) : true).map((property) => ({ propertyId: property.id, propertyTitle: property.title, location: property.location, country: property.country, nightlyRate: property.nightlyRate, currency: property.currency, badgeLevel: property.badgeLevel, highlights: property.highlights, score: 0, reason: "Sign in to save preferences and receive explainable recommendations.", isDismissed: false, generatedAt: new Date().toISOString() }));

  async function dismiss(propertyId: string) {
    if (!userId) { setNotice("Sign in to dismiss and restore recommendations."); return; }
    try { await api.dismissTravelerRecommendation(userId, propertyId, token); setRecommendations((items) => items.filter((item) => item.propertyId !== propertyId)); setNotice("Recommendation dismissed. You can restore it from your preferences later."); }
    catch (caught) { setError(caught instanceof Error ? caught.message : "Recommendation could not be dismissed."); }
  }

  async function savePreference() {
    if (!userId) { setNotice("Sign in to save recommendation preferences."); return; }
    try {
      await api.saveTravelerPreferences(userId, token, { maximumNightlyRate: filter === "Under $300" ? 300 : null, preferredBadgeLevel: filter === "Wellness hosts" ? "Wellness" : null });
      setNotice("Your recommendation preference was saved.");
    } catch (caught) { setError(caught instanceof Error ? caught.message : "Preference could not be saved."); }
  }

  return (
    <div className="flex flex-col gap-5" id="TRAV-SUGG">
      <div className="flex flex-wrap items-start justify-between gap-3"><div><h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">{visible.length} stay{visible.length === 1 ? "" : "s"} match your <em className="italic text-deep-hover">preferences.</em></h1><p className="m-0 mt-2 max-w-[620px] text-sm text-gray-600">Recommendations are ranked from your saved stays, completed trips, and preferences. Every reason is shown so you stay in control.</p></div>{userId && <Button onClick={() => void savePreference()} variant="outline">Save this filter</Button>}</div>
      <div className="flex flex-wrap gap-2">{["All parishes", "Beachfront", "Under $300", "Wellness hosts"].map((tab) => <button className={cx("inline-flex min-h-11 cursor-pointer items-center rounded-pill px-5 font-sans text-[13px] font-semibold transition-colors", filter === tab ? "border-none bg-deep text-on-dark-heading" : "border-[1.5px] border-sand-input bg-transparent text-gray-600 hover:border-deep hover:text-ink")} key={tab} onClick={() => setFilter(tab)} type="button">{tab}</button>)}</div>
      {notice && <div className="rounded-field bg-success-tint px-4 py-3 text-sm text-success-text" role="status">{notice}</div>}
      {error && <div className="rounded-field bg-coral-tint px-4 py-3 text-sm text-coral-text" role="alert">{error}</div>}
      {!userId && <div className="rounded-field bg-amber-tint px-4 py-3 text-sm text-amber-text">Browse sample matches as a guest, or <AppLink className="font-semibold underline" href="/login">sign in</AppLink> to save preferences and dismiss suggestions.</div>}
      {loading ? <div className="rounded-card border border-sand-border bg-cream p-6 text-sm text-sand-600">Loading recommendations from PostgreSQL…</div> : <div className="grid grid-cols-[repeat(auto-fill,minmax(300px,1fr))] gap-4">{visible.map((item, index) => <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]" key={item.propertyId}><div className="relative -mx-1.5 -mt-1.5 h-40 overflow-hidden rounded-field"><img alt={item.propertyTitle} className="h-full w-full object-cover" src={getStayImage(index).src} /></div><div className="flex items-baseline justify-between gap-2.5"><div className="font-display text-lg font-medium">{item.propertyTitle}</div><span className="text-sm"><strong>{formatMoney(item.nightlyRate, item.currency)}</strong> <span className="text-[11.5px] text-sand-500">/ night</span></span></div><div className="text-[12.5px] text-gray-600">{item.location} · {item.country}</div><div className="flex flex-wrap gap-1.5"><span className="rounded-pill bg-shell px-2.5 py-1 text-[10.5px] font-bold tracking-[0.04em] text-gray-600">{item.reason}</span>{item.score > 0 && <span className="rounded-pill bg-success-tint px-2.5 py-1 text-[10.5px] font-bold text-success-text">Match score {item.score}</span>}</div><div className="flex flex-wrap gap-2"><AppLink className="inline-flex min-h-[46px] items-center rounded-pill bg-deep px-[22px] text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover" href={`/properties/${item.propertyId}`}>View</AppLink><button className="inline-flex min-h-[46px] cursor-pointer items-center rounded-pill border-[1.5px] border-sand-input bg-transparent px-5 font-sans text-[13.5px] font-semibold text-ink transition-colors hover:border-deep" onClick={() => void dismiss(item.propertyId)} type="button">Dismiss</button></div></div>)}</div>}
      {!loading && visible.length === 0 && <EmptyState title="No matches for that filter" copy="Try a different filter to see more stays." />}
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
        <a className="text-sand-500 hover:text-ink" href={LEGAL_DETAILS.supportTel}>
          {LEGAL_DETAILS.supportPhone}
        </a>
      </div>
    </div>
  );
}

const errDeepPill =
  "inline-flex min-h-12 items-center gap-2.5 rounded-pill bg-deep px-[26px] font-sans text-[14.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover";
const errOutlinePill =
  "inline-flex min-h-12 cursor-pointer items-center rounded-pill border-[1.5px] border-sand-input bg-transparent px-[26px] font-sans text-[14.5px] font-semibold text-ink transition-colors hover:border-deep";

export function SignInRequiredPage({ returnTo }: { returnTo?: string } = {}) {
  return (
    <ErrorTemplate
      copy="This part of NestyStay needs an account. Log in, or keep browsing as a guest."
      icon={Lock}
      id="ERR-401"
      title="Sign in required."
    >
      <div className="mt-1.5 flex flex-wrap justify-center gap-3">
        <AppLink className={errDeepPill} href={returnTo ? `/login?returnTo=${encodeURIComponent(returnTo)}` : "/login"}>
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
      <a className="inline-flex min-h-11 items-center text-[13.5px] font-semibold text-deep-hover" href={LEGAL_DETAILS.whatsappUrl}>
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
      <div className="mt-1.5 flex flex-wrap justify-center gap-3">
        <a className="inline-flex min-h-12 items-center gap-2.5 rounded-pill bg-success-tint px-[22px] text-sm font-bold text-success-text" href={LEGAL_DETAILS.whatsappUrl} rel="noreferrer" target="_blank">
          Urgent? Message us on WhatsApp
        </a>
        <a className="inline-flex min-h-12 items-center rounded-pill border border-sand-input px-[22px] text-sm font-bold text-deep-hover" href={LEGAL_DETAILS.supportTel}>
          Call {LEGAL_DETAILS.supportPhone}
        </a>
      </div>
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
