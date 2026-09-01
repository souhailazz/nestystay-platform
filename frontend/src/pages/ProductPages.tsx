import { useEffect, useMemo, useRef, useState, type FormEvent, type ReactNode } from "react";
import { motion } from "framer-motion";
import {
  ArrowRight,
  BadgeCheck,
  BedDouble,
  Bell,
  CalendarDays,
  CalendarRange,
  CalendarClock,
  Check,
  CircleAlert,
  CreditCard,
  Download,
  Eye,
  FileCheck2,
  Gauge,
  Heart,
  Home,
  KeyRound,
  LayoutDashboard,
  ListChecks,
  Lock,
  Map,
  MapPin,
  MessageSquare,
  Paperclip,
  Plus,
  ReceiptText,
  RotateCcw,
  Search,
  Settings,
  ShieldCheck,
  Sparkles,
  Star,
  SlidersHorizontal,
  TimerReset,
  ToggleLeft,
  UserRound,
  WifiOff,
  X,
} from "lucide-react";
import { AppLink, navigate } from "../components/AppLink";
import { BookingModal } from "../components/booking/BookingModal";
import { Badge } from "../components/ui/Badge";
import { Button, buttonClassName } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { EmptyState } from "../components/ui/EmptyState";
import { ErrorState } from "../components/ui/ErrorState";
import { Field, InlineLabel, Input, Select, Textarea } from "../components/ui/Input";
import { LoadingState } from "../components/ui/LoadingState";
import { PatoisToast } from "../components/ui/PatoisToast";
import { StatusChip } from "../components/ui/StatusChip";
import { TierBadge } from "../components/layout/PublicShell";
import { usePatois } from "../lib/patois";
import { PageHeader } from "../components/ui/PageHeader";
import { useBookings } from "../hooks/useBookings";
import type { AuthController } from "../hooks/useAuth";
import { AdminPermissions, hasAdminPermission } from "../lib/adminPermissions";
import { useProperties, useProperty } from "../hooks/useProperties";
import { getStayImage } from "../lib/stayImages";
import { TravelerStateContainer } from "../features/traveler/TravelerStateContainer";
import { HostStateContainer } from "../features/host/HostStateContainer";
import { PublicStateContainer } from "../features/public/PublicStateContainer";
import { AuthStateContainer } from "../features/auth/AuthStateContainer";
import {
  api,
  formatMoney,
  type BadgeAssignment,
  type BadgeDefinition,
  type BadgeEligibility,
  type BadgeFeatureAccess,
  type BadgeLevel,
  type BadgeRenewal,
  type Booking,
  type BookingQuote,
  type Campaign,
  type CommissionQuote,
  type CreatePropertyRequest,
  type FoundingBenefit,
  type FoundingTier,
  type FoundingTransferEvaluation,
  type GoogleSignInRequest,
  type PhaseTwoPricebookItem,
  type ProfilePhotoUpload,
  type PropertyPhotoUpload,
  type PropertyListing,
  type SocialAuthConfig,
  type UserProfile,
  type WellnessAdminDashboard,
  type WellnessOfficer,
  type WellnessQuote,
  type WellnessReport,
  type WellnessReportPhotoUpload,
  type WellnessVisit,
} from "../lib/api";

function todayPlus(days: number) {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return date.toISOString().slice(0, 10);
}

/* Reusable scroll-reveal wrapper for product sections */
function AnimatedSection({ children, className, delay = 0 }: { children: ReactNode; className?: string; delay?: number }) {
  return (
    <motion.div
      className={className}
      initial={{ opacity: 0, y: 36 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-80px" }}
      transition={{ duration: 0.6, delay, ease: [0.22, 1, 0.36, 1] }}
    >
      {children}
    </motion.div>
  );
}

/* DS v2 — StatusBadge now delegates to the contractual StatusChip tones. */
function StatusBadge({ value }: { value: string }) {
  return <StatusChip value={value} />;
}

function MiniPropertyArt({ index = 0, title = "Jamaican stay" }: { index?: number; title?: string }) {
  const image = getStayImage(index);
  return (
    <div className="property-art property-art--image">
      <img className="generated-stay-image" src={image.src} alt={`${title}: ${image.alt}`} loading="lazy" />
    </div>
  );
}

function ProductCard({
  property,
  index,
  onBook,
}: {
  property: PropertyListing;
  index: number;
  onBook: (property: PropertyListing) => void;
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 30 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-60px" }}
      transition={{ duration: 0.5, delay: index * 0.1 }}
    >
    <Card className="stay-result-card">
      <div className="stay-result-card__visual">
        <MiniPropertyArt index={index} title={property.title} />
        <span className="property-tag">{property.badgeLevel} host</span>
        <button type="button" className="heart-button" aria-label={`Save ${property.title}`}>
          <Heart size={18} />
        </button>
      </div>
      <div className="stay-result-card__body">
        <div className="stay-result-card__meta">
          <StatusBadge value={property.guestVerificationEnabled ? "Verified stay" : "Fast booking"} />
          <span>
            <Star size={13} fill="currentColor" /> 4.9
          </span>
        </div>
        <h3>{property.title}</h3>
        <p>
          <MapPin size={14} /> {property.location}, {property.country}
        </p>
        <div className="highlight-list">
          {property.highlights.slice(0, 3).map((highlight) => (
            <span key={highlight}>{highlight}</span>
          ))}
        </div>
        <div className="stay-result-card__footer">
          <strong>{formatMoney(property.nightlyRate, property.currency)} / night</strong>
          <div className="button-row">
            <AppLink className={buttonClassName("outline")} href={`/properties/${property.id}`}>
              Details
            </AppLink>
            <Button onClick={() => onBook(property)}>
              Book <ArrowRight size={16} />
            </Button>
          </div>
        </div>
      </div>
    </Card>
    </motion.div>
  );
}

function RequireAuth({ auth, title }: { auth: AuthController; title: string }) {
  return (
    <section className="product-section product-section--center">
      <EmptyState
        title={title}
        copy="Create a session through the backend auth API before opening this workspace."
        action={
          <AppLink className={buttonClassName("sun")} href="/login">
            Login or register <ArrowRight size={16} />
          </AppLink>
        }
      />
      {auth.pendingChallenge && (
        <p className="micro-note">A 2FA challenge is already open for {auth.pendingChallenge.email}.</p>
      )}
    </section>
  );
}

export function ExplorePage({ auth }: { auth: AuthController }) {
  return <PublicStateContainer view="search" session={auth.session} />;
}

export function PropertyDetailsPage({
  auth,
  propertyId,
}: {
  auth: AuthController;
  propertyId?: string;
}) {
  return <PublicStateContainer view="detail" propertyId={propertyId} session={auth.session} />;
}

export function AuthPage({ auth, mode = "login" }: { auth: AuthController; mode?: "login" | "register" }) {
  return <AuthStateContainer mode={mode} auth={auth} />;
}

async function signInWithGoogle(signIn: (profile: GoogleSignInRequest) => Promise<unknown>, role: "Guest" | "Host") {
  const googleClientId = import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined;
  if (!googleClientId) {
    throw new Error("Google sign-in is unavailable until OAuth is configured.");
  }

  const credential = await requestGoogleCredential(googleClientId);
  return signIn({ credential: credential.raw, role });
}

function requestGoogleCredential(clientId: string) {
  return new Promise<{ email: string; name: string; sub: string; picture?: string; raw: string }>((resolve, reject) => {
    const scriptId = "google-identity-services";
    const existing = document.getElementById(scriptId);
    const loadScript = existing
      ? Promise.resolve()
      : new Promise<void>((scriptResolve, scriptReject) => {
          const script = document.createElement("script");
          script.id = scriptId;
          script.src = "https://accounts.google.com/gsi/client";
          script.async = true;
          script.defer = true;
          script.onload = () => scriptResolve();
          script.onerror = () => scriptReject(new Error("Google sign-in could not load."));
          document.head.appendChild(script);
        });

    loadScript
      .then(() => {
        const google = window.google;
        if (!google?.accounts?.id) {
          reject(new Error("Google sign-in is unavailable in this browser."));
          return;
        }

        google.accounts.id.initialize({
          client_id: clientId,
          callback: (response: { credential?: string }) => {
            if (!response.credential) {
              reject(new Error("Google did not return a credential."));
              return;
            }
            resolve(decodeGoogleCredential(response.credential));
          },
        });
        google.accounts.id.prompt((notification: { isNotDisplayed?: () => boolean; isSkippedMoment?: () => boolean }) => {
          if (notification.isNotDisplayed?.() || notification.isSkippedMoment?.()) {
            reject(new Error("Google sign-in prompt was dismissed."));
          }
        });
      })
      .catch(reject);
  });
}

function decodeGoogleCredential(raw: string) {
  const payload = raw.split(".")[1];
  if (!payload) throw new Error("Google credential is malformed.");
  const base64 = payload.replace(/-/g, "+").replace(/_/g, "/");
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
  const decoded = JSON.parse(window.atob(padded)) as {
    email?: string;
    name?: string;
    sub?: string;
    picture?: string;
  };
  if (!decoded.email || !decoded.sub) throw new Error("Google credential is missing account details.");
  return {
    email: decoded.email,
    name: decoded.name ?? decoded.email.split("@")[0],
    sub: decoded.sub,
    picture: decoded.picture,
    raw,
  };
}

declare global {
  interface Window {
    google?: {
      accounts?: {
        id?: {
          initialize: (options: { client_id: string; callback: (response: { credential?: string }) => void }) => void;
          prompt: (callback?: (notification: { isNotDisplayed?: () => boolean; isSkippedMoment?: () => boolean }) => void) => void;
        };
      };
    };
  }
}

function MetricCard({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof Gauge;
  label: string;
  value: string;
}) {
  return (
    <div className="flex flex-col gap-2 rounded-card border border-sand-border bg-cream p-[18px] font-sans text-ink">
      <span className="text-deep-hover">
        <Icon size={19} />
      </span>
      <small className="text-[10.5px] font-semibold uppercase tracking-[0.14em] text-sand-500">{label}</small>
      <strong className="font-display text-[24px] font-medium leading-none">{value}</strong>
    </div>
  );
}

export function GuestDashboardPage({ auth }: { auth: AuthController }) {
  if (!auth.session) return <RequireAuth auth={auth} title="Guest dashboard needs an active session." />;
  return <TravelerStateContainer view="dashboard" auth={auth} />;
}

function GuestDashboardContent({ auth }: { auth: AuthController }) {
  const { bookings, isLoading, error, reload } = useBookings(auth.session?.accessToken);
  const [showLoginToast, setShowLoginToast] = useState(false);
  const approved = bookings.filter((booking) => booking.status === "APPROVED").length;
  const pending = bookings.filter((booking) => booking.status === "PENDING").length;
  const spend = bookings.reduce((sum, booking) => sum + booking.totalAmount, 0);

  useEffect(() => {
    if (window.sessionStorage.getItem("nesty-login-toast") !== "1") return;
    window.sessionStorage.removeItem("nesty-login-toast");
    setShowLoginToast(true);
    const timer = window.setTimeout(() => setShowLoginToast(false), 3000);
    return () => window.clearTimeout(timer);
  }, []);

  return (
    <div className="product-page">
      {showLoginToast && <PatoisToast />}
      <PageHeader
        eyebrow="Guest dashboard"
        title={`Welcome back, ${auth.session?.displayName}.`}
        copy="Bookings are filtered through the backend booking API by the current user id."
        actions={
          <AppLink className={buttonClassName("sun")} href="/explore">
            Explore stays <ArrowRight size={17} />
          </AppLink>
        }
      />
      <AnimatedSection>
      <section className="product-section">
        <div className="metric-grid">
          <MetricCard icon={BedDouble} label="Bookings" value={String(bookings.length)} />
          <MetricCard icon={ShieldCheck} label="Approved" value={String(approved)} />
          <MetricCard icon={CalendarDays} label="Pending" value={String(pending)} />
          <MetricCard icon={ReceiptText} label="Trip value" value={formatMoney(spend || 0)} />
        </div>
        <BookingList bookings={bookings} error={error} isLoading={isLoading} onReload={reload} />
      </section>
      </AnimatedSection>
    </div>
  );
}

export function HostDashboardPage({ auth }: { auth: AuthController }) {
  if (!auth.session) return <RequireAuth auth={auth} title="Host dashboard needs an active session." />;
  return <HostStateContainer view="analytics" auth={auth} />;
}

function HostDashboardContent({ auth }: { auth: AuthController }) {
  const propertiesState = useProperties(auth.session?.accessToken);
  const bookingsState = useBookings(auth.session?.accessToken);
  const hostProperties = propertiesState.properties.filter(
    (property) => property.hostUserId === auth.session?.userId,
  );
  const hostBookings = bookingsState.bookings.filter((booking) => booking.hostUserId === auth.session?.userId);
  const revenue = hostBookings.reduce((sum, booking) => sum + booking.staySubtotal, 0);

  return (
    <div className="product-page">
      <PageHeader
        eyebrow="Host dashboard"
        title="Operate your stays from one calm view."
        copy="Host metrics are computed from live property and booking API records tied to your user id."
        actions={
          <AppLink className={buttonClassName("sun")} href="/host/properties/new">
            <Plus size={17} /> Add property
          </AppLink>
        }
      />
      <section className="product-section">
        <div className="metric-grid">
          <MetricCard icon={Home} label="Your properties" value={String(hostProperties.length)} />
          <MetricCard icon={ListChecks} label="Bookings" value={String(hostBookings.length)} />
          <MetricCard icon={CreditCard} label="Stay subtotal" value={formatMoney(revenue || 0)} />
          <MetricCard icon={BadgeCheck} label="API source" value="/api" />
        </div>
        {propertiesState.isLoading || bookingsState.isLoading ? <LoadingState /> : null}
        {propertiesState.error && <ErrorState message={propertiesState.error} onRetry={propertiesState.reload} />}
        {!propertiesState.isLoading && hostProperties.length === 0 && (
          <EmptyState
            title="No properties for this host yet."
            copy="Create your first property and it will persist through the backend property endpoint."
            action={
              <AppLink className={buttonClassName("sun")} href="/host/properties/new">
                Create property
              </AppLink>
            }
          />
        )}
        {hostProperties.length > 0 && (
          <div className="management-grid">
            {hostProperties.map((property, index) => (
              <ProductCard key={property.id} property={property} index={index} onBook={() => undefined} />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

function defaultWellnessDateTime() {
  const date = new Date();
  date.setDate(date.getDate() + 1);
  date.setHours(10, 0, 0, 0);
  return date.toISOString().slice(0, 16);
}

function toApiDateTime(value: string) {
  return new Date(value).toISOString();
}

/* HOST-WELL visit rows — officers are badge ID ONLY (client contract rule 4). */
function downloadWellnessReport(visit: WellnessVisit, report: WellnessReport) {
  const body = [
    "NestyStay wellness visit report",
    `Visit: ${visit.id}`,
    `Scheduled: ${new Date(visit.scheduledAt).toLocaleString()}`,
    `Service: ${visit.visitType}`,
    `Officer: ${visit.officerBadgeNumber ?? "Unassigned"}`,
    `Status: ${report.reportStatus}`,
    `Submitted: ${new Date(report.submittedAt).toLocaleString()}`,
    `Photos: ${report.photos.length}`,
    "",
    report.notes,
  ].join("\n");
  const blob = new Blob([body], { type: "text/plain;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = `nestywellness-${visit.id.slice(0, 8)}-report.txt`;
  anchor.click();
  URL.revokeObjectURL(url);
}

function WellnessVisitList({
  visits,
  reports = {},
  onCancel,
}: {
  visits: WellnessVisit[];
  reports?: Record<string, WellnessReport | null>;
  onCancel?: (visit: WellnessVisit) => void;
}) {
  if (visits.length === 0) {
    return <EmptyState title="No wellness visits yet." copy="Requested visits will appear here after the backend saves them." />;
  }

  return (
    <div className="flex flex-col gap-3">
      {visits.map((visit) => (
        <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]" key={visit.id}>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="flex items-center gap-3">
              <span className="inline-flex size-11 shrink-0 items-center justify-center rounded-full bg-deep text-[10px] font-extrabold text-yellow">
                JCF
              </span>
              <div>
                <div className="font-mono text-[13px] font-bold">{visit.officerBadgeNumber ?? "Officer not assigned"}</div>
                <div className="text-[12.5px] text-gray-600">
                  {visit.visitType.replace(/([A-Z])/g, " $1").trim()} · {new Date(visit.scheduledAt).toLocaleString()}
                </div>
              </div>
            </div>
            <div className="flex flex-wrap gap-1.5">
              <StatusChip label="Visit" value={visit.visitStatus} />
              <StatusChip label="Pay" value={visit.paymentStatus} />
            </div>
          </div>
          <div className="text-[11.5px] text-sand-500">
            Badge ID only — never a name, photo, or direct contact. All communication through NestyStay.
          </div>
          {reports[visit.id] && (
            <div className="flex flex-wrap items-center justify-between gap-3 rounded-field bg-success-tint px-4 py-3 text-[12.5px] text-success-text">
              <span>Report submitted · {reports[visit.id]!.photos.length} verified photo{reports[visit.id]!.photos.length === 1 ? "" : "s"} · {reports[visit.id]!.notes}</span>
              <button className="inline-flex min-h-9 items-center gap-1.5 rounded-pill border border-success-text/30 px-3 text-xs font-semibold" onClick={() => downloadWellnessReport(visit, reports[visit.id]!)} type="button">
                <Download size={14} /> Print / save report
              </button>
            </div>
          )}
          {onCancel && !["Completed", "Cancelled"].includes(visit.visitStatus) && (
            <div className="flex flex-wrap items-center justify-between gap-3 border-t border-shell pt-3">
              <span className="text-xs text-gray-600">Need a different time? Cancel and request a new slot.</span>
              <Button onClick={() => onCancel(visit)} variant="ghost"><X size={15} /> Cancel visit</Button>
            </div>
          )}
        </div>
      ))}
    </div>
  );
}

export function HostWellnessPage({ auth }: { auth: AuthController }) {
  if (!auth.session) return <RequireAuth auth={auth} title="Host wellness needs an active host session." />;
  return <HostWellnessContent auth={auth} />;
}

function HostWellnessContent({ auth }: { auth: AuthController }) {
  const { properties, isLoading, error, reload } = useProperties(auth.session?.accessToken);
  const hostProperties = properties.filter((property) => property.hostUserId === auth.session?.userId);
  const [propertyId, setPropertyId] = useState("");
  const [visitType, setVisitType] = useState("StandardWellnessCheck");
  const [scheduledAt, setScheduledAt] = useState(defaultWellnessDateTime);
  const [parish, setParish] = useState("St. Ann");
  const [area, setArea] = useState("Ocho Rios");
  const [quote, setQuote] = useState<WellnessQuote | null>(null);
  const [visits, setVisits] = useState<WellnessVisit[]>([]);
  const [reports, setReports] = useState<Record<string, WellnessReport | null>>({});
  const [subscription, setSubscription] = useState<import("../lib/api").WellnessSubscription | null>(null);
  const [availableOfficers, setAvailableOfficers] = useState<WellnessOfficer[]>([]);
  const [bookingStep, setBookingStep] = useState(1);
  const [showPlanComparison, setShowPlanComparison] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const selectedProperty = hostProperties.find((property) => property.id === propertyId);

  useEffect(() => {
    if (!propertyId && hostProperties[0]) {
      setPropertyId(hostProperties[0].id);
    }
  }, [hostProperties, propertyId]);

  useEffect(() => {
    void api.getWellnessVisits({ hostUserId: auth.session?.userId }, auth.session?.accessToken).then(setVisits).catch(() => undefined);
    if (auth.session) void api.getWellnessSubscription(auth.session.accessToken).then(setSubscription).catch(() => undefined);
  }, [auth.session?.userId]);

  useEffect(() => {
    if (!auth.session || visits.length === 0) return;
    const completed = visits.filter((visit) => visit.visitStatus === "Completed" || visit.reportStatus === "Submitted");
    if (completed.length === 0) return;
    void Promise.all(completed.map(async (visit) => [visit.id, await api.getWellnessReport(visit.id, auth.session!.accessToken)] as const))
      .then((entries) => setReports(Object.fromEntries(entries)))
      .catch(() => undefined);
  }, [auth.session?.accessToken, visits]);

  useEffect(() => {
    if (!auth.session || !parish.trim() || !scheduledAt) return;
    const timer = window.setTimeout(() => {
      void api.getAvailableWellnessOfficers(auth.session!.accessToken, parish.trim(), toApiDateTime(scheduledAt))
        .then(setAvailableOfficers)
        .catch(() => setAvailableOfficers([]));
    }, 350);
    return () => window.clearTimeout(timer);
  }, [auth.session?.accessToken, parish, scheduledAt]);

  async function runWellnessAction(action: () => Promise<string>) {
    setActionError(null);
    setNotice(null);
    try {
      const message = await action();
      setNotice(message);
      const refreshed = await api.getWellnessVisits({ hostUserId: auth.session?.userId }, auth.session?.accessToken);
      setVisits(refreshed);
      if (auth.session) setSubscription(await api.getWellnessSubscription(auth.session.accessToken));
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : "Wellness action failed.");
    }
  }

  function cancelVisit(visit: WellnessVisit) {
    if (!window.confirm("Cancel this wellness visit? The payment will be refunded or released according to its current status.")) return;
    void runWellnessAction(async () => {
      await api.cancelWellnessVisit(visit.id, auth.session!.accessToken, "Cancelled by host from wellness dashboard.");
      return "Visit cancelled. You can request a new time below.";
    });
  }

  function buildRequest() {
    if (!auth.session || !propertyId) throw new Error("Choose a host property.");
    return {
      hostUserId: auth.session.userId,
      propertyId,
      visitType,
      scheduledAt: toApiDateTime(scheduledAt),
      parish,
      area,
    };
  }

  const wellField =
    "min-h-12 w-full rounded-field border-[1.5px] border-sand-input bg-white px-4 font-sans text-[14.5px] text-ink outline-none transition-[border-color,box-shadow] focus:border-deep-hover focus:shadow-[0_0_0_3px_rgba(14,74,69,0.12)]";
  const wellLabel = "font-sans text-[13px] font-semibold text-ink";

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="HOST-WELL">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
          Wellness <em className="italic text-deep-hover">visits</em>
        </h1>
        <div className="flex flex-wrap gap-2">
          <AppLink
            className="inline-flex min-h-[46px] items-center rounded-pill border-[1.5px] border-sand-input px-5 text-[13.5px] font-semibold text-ink transition-colors hover:border-deep"
            href="/host/wellness/directory"
          >
            Police directory
          </AppLink>
          <AppLink
            className="inline-flex min-h-[46px] items-center gap-2 rounded-pill bg-deep px-[22px] text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover"
            href="/host/wellness/book"
          >
            Request a visit
          </AppLink>
        </div>
      </div>

      {/* Rule 3 companion: emergency number always visible on wellness surfaces */}
      <div className="flex items-center gap-3 self-start rounded-field bg-emergency px-[18px] py-3 text-sm font-bold text-white">
        <svg aria-hidden="true" fill="none" height="20" viewBox="0 0 30 30" width="20">
          <path d="M15 5 L27 25.5 H3 Z" stroke="#ffffff" strokeLinejoin="round" strokeWidth="2" />
          <path d="M15 12.5 V18.5" stroke="#ffffff" strokeLinecap="round" strokeWidth="2" />
          <circle cx="15" cy="22" fill="#ffffff" r="1.3" />
        </svg>
        Jamaica Emergency: 119
      </div>

      <section className="rounded-card border border-sand-border bg-cream p-[22px]" aria-labelledby="wellness-plan-title">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <div className="flex items-center gap-2"><CreditCard size={18} className="text-deep-hover" /><h2 className="m-0 font-display text-xl font-medium" id="wellness-plan-title">Wellness subscription</h2></div>
            <p className="mt-1 text-[13px] text-gray-600">Simple monthly coverage with one visit included. Payment runs through the configured local adapter in milestone mode.</p>
          </div>
          <StatusChip value={subscription?.status ?? "Not started"} />
        </div>
        <div className="mt-4 grid gap-3 sm:grid-cols-3">
          <Card><div className="text-xs text-sand-500">Plan</div><strong>$19 / month</strong><div className="text-xs text-gray-600">1 included visit</div></Card>
          <Card><div className="text-xs text-sand-500">Billing period</div><strong>{subscription ? new Date(subscription.currentPeriodEnd).toLocaleDateString() : "Starts after checkout"}</strong><div className="text-xs text-gray-600">Next renewal date</div></Card>
          <Card><div className="text-xs text-sand-500">Usage</div><strong>{subscription?.remainingVisits ?? 0} remaining</strong><div className="text-xs text-gray-600">{subscription?.paymentReference ? `Ref ${subscription.paymentReference.slice(0, 12)}` : "No payment yet"}</div></Card>
        </div>
        <div className="mt-4 flex flex-wrap gap-2">
          {subscription?.status === "Active" ? <Button variant="outline" onClick={() => void runWellnessAction(async () => { await api.cancelWellnessSubscription(auth.session!.accessToken); return "Wellness subscription cancelled. Your current period remains visible for audit."; })}>Cancel subscription</Button> : subscription ? <Button variant="outline" onClick={() => void runWellnessAction(async () => { await api.renewWellnessSubscription(auth.session!.accessToken); return "Wellness subscription renewed."; })}>Renew $19 plan</Button> : <Button variant="dark" onClick={() => void runWellnessAction(async () => { await api.startWellnessSubscription(auth.session!.accessToken); return "Wellness subscription started."; })}>Start $19 plan</Button>}
          <Button variant="ghost" onClick={() => setShowPlanComparison((current) => !current)}>{showPlanComparison ? "Hide plan comparison" : "Compare plans"}</Button>
        </div>
        {showPlanComparison && <div className="mt-4 grid gap-3 md:grid-cols-2" data-testid="wellness-plan-comparison">
          <Card><div className="font-semibold">Pay as you go</div><div className="mt-1 text-sm text-gray-600">$25–$50 per visit · no recurring charge · best for occasional checks.</div></Card>
          <Card className="border-deep"><div className="flex items-center gap-2 font-semibold">Monthly wellness <BadgeCheck size={16} className="text-deep-hover" /></div><div className="mt-1 text-sm text-gray-600">$19/month · one included visit · renewal and cancellation controls above.</div></Card>
        </div>}
      </section>

      {quote && quote.eligible && (
        <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
          <div className="text-[13px] font-semibold">Visit quote — {visitType.replace(/([A-Z])/g, " $1").trim()}</div>
          <div className="flex flex-col text-[13.5px]">
            <div className="flex justify-between border-b border-shell py-[7px]">
              <span>
                Visit price <span className="text-[11px] text-coral-text">($25–$50 per contract visit)</span>
              </span>
              <strong>{formatMoney(quote.price, quote.currency)}</strong>
            </div>
            <div className="flex justify-between border-b border-shell py-[7px]">
              <span>Officer payout (included)</span>
              <span className="text-gray-600">{formatMoney(quote.officerPayoutAmount, quote.currency)}</span>
            </div>
            <div className="flex justify-between py-[9px]">
              <strong>Charged on completion</strong>
              <strong className="font-display text-lg">{formatMoney(quote.price, quote.currency)}</strong>
            </div>
          </div>
          <div className="text-xs text-sand-500">Emergencies: call {quote.emergencyNumber} directly.</div>
        </div>
      )}
      {quote && !quote.eligible && (
        <div className="rounded-field bg-coral-tint px-4 py-3 text-[13px] text-coral-text" role="alert">
          {quote.missingRequirements.join(" ")}
        </div>
      )}

      <form
        className="flex flex-col gap-4 rounded-card border border-sand-border bg-cream p-[22px]"
        onSubmit={(event) => {
          event.preventDefault();
          void runWellnessAction(async () => {
            const result = await api.quoteWellnessVisit(buildRequest());
            setQuote(result);
            return result.eligible ? "Wellness quote is eligible." : "Wellness is locked for this property.";
          });
        }}
      >
        <div className="flex flex-wrap items-center justify-between gap-3"><div className="font-display text-xl font-medium">Request a certified visit</div><div className="text-xs font-semibold text-sand-500">Step {bookingStep} of 3</div></div>
        <div className="flex items-center gap-2" aria-label="Booking progress">
          {["Service", "Schedule", "Confirm"].map((label, index) => <div className="flex flex-1 items-center gap-2" key={label}><span className={`inline-flex size-7 items-center justify-center rounded-full text-xs font-bold ${bookingStep > index ? "bg-deep text-white" : "bg-shell text-sand-500"}`}>{bookingStep > index + 1 ? <Check size={14} /> : index + 1}</span><span className="text-xs font-semibold">{label}</span>{index < 2 && <span className="h-px flex-1 bg-shell" />}</div>)}
        </div>
        <div className="grid gap-3 md:grid-cols-3" data-testid="wellness-service-comparison">
          {[["StandardWellnessCheck", "Standard check", "$50", "60 min"], ["InPersonGuestIdCheck", "Guest ID check", "$40", "45 min"], ["DriveByPatrol", "Drive-by patrol", "$25", "30 min"]].map(([value, label, price, duration]) => <button className={`rounded-card border p-4 text-left transition-colors ${visitType === value ? "border-deep bg-mint-tint" : "border-sand-border bg-white hover:border-deep"}`} key={value} onClick={() => { setVisitType(value); setBookingStep(1); }} type="button"><div className="flex items-center justify-between gap-2"><strong>{label}</strong>{visitType === value && <Check size={16} className="text-deep-hover" />}</div><div className="mt-2 text-sm text-gray-600">{duration} · {price}</div></button>)}
        </div>
        <div className="grid gap-3 sm:grid-cols-2">
          <label className="flex flex-col gap-1.5">
            <span className={wellLabel}>Property</span>
            <select className={wellField} onChange={(event) => setPropertyId(event.target.value)} value={propertyId}>
              {hostProperties.map((property) => (
                <option key={property.id} value={property.id}>
                  {property.title}
                </option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-1.5">
            <span className={wellLabel}>Visit type</span>
            <select className={wellField} onChange={(event) => setVisitType(event.target.value)} value={visitType}>
              <option value="StandardWellnessCheck">Standard wellness check</option>
              <option value="InPersonGuestIdCheck">In-person guest ID check</option>
              <option value="DriveByPatrol">Drive-by patrol</option>
            </select>
          </label>
          <label className="flex flex-col gap-1.5">
            <span className={wellLabel}>Scheduled time</span>
            <input className={wellField} min={defaultWellnessDateTime()} onChange={(event) => { setScheduledAt(event.target.value); setBookingStep(2); }} type="datetime-local" value={scheduledAt} />
          </label>
          <label className="flex flex-col gap-1.5">
            <span className={wellLabel}>Parish</span>
            <input className={wellField} onChange={(event) => setParish(event.target.value)} value={parish} />
          </label>
          <label className="flex flex-col gap-1.5 sm:col-span-2">
            <span className={wellLabel}>Area</span>
            <input className={wellField} onChange={(event) => setArea(event.target.value)} value={area} />
          </label>
        </div>
        <div className="flex flex-wrap items-center gap-3 rounded-field bg-shell px-4 py-3 text-[13px]">
          <MapPin size={16} className="text-deep-hover" /><span>{availableOfficers.length} verified officer{availableOfficers.length === 1 ? "" : "s"} available for this parish/time.</span><span className="text-xs text-sand-500">Assignment is completed by NestyStay operations after confirmation.</span>
        </div>
        <div className="flex flex-wrap gap-2.5">
          <button
            className="inline-flex min-h-[46px] cursor-pointer items-center rounded-pill border-[1.5px] border-sand-input bg-transparent px-5 font-sans text-[13.5px] font-semibold text-ink transition-colors hover:border-deep"
            type="submit"
          >
            Get quote
          </button>
          <button
            className="inline-flex min-h-[46px] cursor-pointer items-center gap-2 rounded-pill border-none bg-deep px-[22px] font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover"
            onClick={() =>
              void runWellnessAction(async () => {
                setBookingStep(3);
                const created = await api.createWellnessVisit(buildRequest(), auth.session!.accessToken);
                setQuote(null);
                return `${created.visitType} requested. Payment is ${created.paymentStatus}.`;
              })
            }
            type="button"
          >
            Request visit
          </button>
        </div>
        {selectedProperty && selectedProperty.badgeLevel !== "Wellness" && (
          <div className="rounded-field bg-mint-tint px-4 py-3 text-[13px] text-mint-text">
            Wellness visits require a ◆ Wellness badge or the unlocked Wellness visits feature.
          </div>
        )}
        {notice && (
          <div className="rounded-field bg-success-tint px-4 py-3 text-[13px] text-success-text" role="status">
            {notice}
          </div>
        )}
        {actionError && <ErrorState message={actionError} />}
      </form>

      <section className="flex flex-col gap-3">
        <h2 className="m-0 font-display text-xl font-medium">Visit status</h2>
        {isLoading && <LoadingState />}
        {error && <ErrorState message={error} onRetry={reload} />}
        <div className="grid gap-3 sm:grid-cols-3">
          <Card><div className="text-xs text-sand-500">Upcoming</div><strong>{visits.filter((visit) => ["Requested", "Scheduled"].includes(visit.visitStatus)).length}</strong><div className="text-xs text-gray-600">Requested or scheduled</div></Card>
          <Card><div className="text-xs text-sand-500">Reports</div><strong>{Object.values(reports).filter(Boolean).length}</strong><div className="text-xs text-gray-600">Host-visible submissions</div></Card>
          <Card><div className="text-xs text-sand-500">Calendar</div><strong>{visits.length ? new Date(visits[0].scheduledAt).toLocaleDateString() : "—"}</strong><div className="text-xs text-gray-600">Next saved visit</div></Card>
        </div>
        <WellnessVisitList onCancel={cancelVisit} visits={visits} reports={reports} />
      </section>
    </div>
  );
}

type WellnessReportPhotoUploadStatus = "queued" | "uploading" | "uploaded" | "failed" | "cancelled";

type WellnessReportPhotoUploadItem = {
  id: string;
  visitId: string;
  file: File;
  progress: number;
  status: WellnessReportPhotoUploadStatus;
  upload?: WellnessReportPhotoUpload;
  error?: string;
  caption?: string;
};

const maximumWellnessReportPhotoBytes = 10 * 1024 * 1024;

export function OfficerWellnessPage({ auth }: { auth: AuthController }) {
  const [badgeNumber, setBadgeNumber] = useState("NST-OFC-2026");
  const [parish, setParish] = useState("St. Ann");
  const [coverageArea, setCoverageArea] = useState("Ocho Rios");
  const [isActiveOffDuty, setIsActiveOffDuty] = useState(true);
  const [isRetired, setIsRetired] = useState(false);
  const [onboardingStep, setOnboardingStep] = useState(1);
  const [documents, setDocuments] = useState({ governmentId: false, policeBadge: false, proofOfAddress: false });
  const [availabilityDays, setAvailabilityDays] = useState<string[]>(["Mon", "Wed", "Fri"]);
  const [availabilityStart, setAvailabilityStart] = useState("09:00");
  const [availabilityEnd, setAvailabilityEnd] = useState("17:00");
  const [privacyConsent, setPrivacyConsent] = useState(false);
  const [draftSavedAt, setDraftSavedAt] = useState<string | null>(null);
  const [isOffline, setIsOffline] = useState(() => typeof navigator !== "undefined" && !navigator.onLine);
  const [visitId, setVisitId] = useState("");
  const [notes, setNotes] = useState("Completed wellness visit. Verified photo evidence attached.");
  const [visits, setVisits] = useState<WellnessVisit[]>([]);
  const [officer, setOfficer] = useState<WellnessOfficer | null>(null);
  const [reportUploads, setReportUploads] = useState<WellnessReportPhotoUploadItem[]>([]);
  const [notice, setNotice] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const reportUploadControllers = useRef<Record<string, AbortController>>({});
  const assignedVisits = visits.filter((visit) => visit.officerBadgeNumber === badgeNumber.trim().toUpperCase());
  const uploadedReportPhotoIds = reportUploads
    .filter((upload) => upload.visitId === visitId.trim() && upload.status === "uploaded" && upload.upload?.scanStatus === "Clean")
    .map((upload) => upload.upload!.id);

  const onboardingDraftKey = `nestyStay.wellnessOfficerDraft.${auth.session?.userId ?? "anonymous"}`;

  useEffect(() => {
    try {
      const saved = window.localStorage.getItem(onboardingDraftKey);
      if (!saved) return;
      const draft = JSON.parse(saved) as Partial<{
        badgeNumber: string; parish: string; coverageArea: string; isActiveOffDuty: boolean; isRetired: boolean;
        documents: typeof documents; availabilityDays: string[]; availabilityStart: string; availabilityEnd: string; privacyConsent: boolean; step: number; savedAt: string;
      }>;
      if (draft.badgeNumber) setBadgeNumber(draft.badgeNumber);
      if (draft.parish) setParish(draft.parish);
      if (draft.coverageArea) setCoverageArea(draft.coverageArea);
      if (typeof draft.isActiveOffDuty === "boolean") setIsActiveOffDuty(draft.isActiveOffDuty);
      if (typeof draft.isRetired === "boolean") setIsRetired(draft.isRetired);
      if (draft.documents) setDocuments(draft.documents);
      if (draft.availabilityDays) setAvailabilityDays(draft.availabilityDays);
      if (draft.availabilityStart) setAvailabilityStart(draft.availabilityStart);
      if (draft.availabilityEnd) setAvailabilityEnd(draft.availabilityEnd);
      if (typeof draft.privacyConsent === "boolean") setPrivacyConsent(draft.privacyConsent);
      if (draft.step) setOnboardingStep(Math.min(3, Math.max(1, draft.step)));
      if (draft.savedAt) setDraftSavedAt(draft.savedAt);
    } catch {
      window.localStorage.removeItem(onboardingDraftKey);
    }
  }, [onboardingDraftKey]);

  useEffect(() => {
    if (!auth.session) return;
    const savedAt = new Date().toISOString();
    window.localStorage.setItem(onboardingDraftKey, JSON.stringify({ badgeNumber, parish, coverageArea, isActiveOffDuty, isRetired, documents, availabilityDays, availabilityStart, availabilityEnd, privacyConsent, step: onboardingStep, savedAt }));
    setDraftSavedAt(savedAt);
  }, [auth.session?.userId, onboardingDraftKey, badgeNumber, parish, coverageArea, isActiveOffDuty, isRetired, documents, availabilityDays, availabilityStart, availabilityEnd, privacyConsent, onboardingStep]);

  useEffect(() => {
    const onOnline = () => setIsOffline(false);
    const onOffline = () => setIsOffline(true);
    window.addEventListener("online", onOnline);
    window.addEventListener("offline", onOffline);
    return () => { window.removeEventListener("online", onOnline); window.removeEventListener("offline", onOffline); };
  }, []);

  useEffect(() => {
    const key = `nestyStay.wellnessReportDraft.${auth.session?.userId ?? "anonymous"}`;
    try {
      const saved = JSON.parse(window.localStorage.getItem(key) ?? "null") as { visitId?: string; notes?: string } | null;
      if (saved?.visitId && !visitId) setVisitId(saved.visitId);
      if (saved?.notes && notes === "Completed wellness visit. Verified photo evidence attached.") setNotes(saved.notes);
    } catch { /* ignore malformed local draft */ }
  }, [auth.session?.userId]);

  useEffect(() => {
    const key = `nestyStay.wellnessReportDraft.${auth.session?.userId ?? "anonymous"}`;
    window.localStorage.setItem(key, JSON.stringify({ visitId, notes, savedAt: new Date().toISOString() }));
  }, [auth.session?.userId, visitId, notes]);

  useEffect(() => {
    if (!auth.session) return;
    void api.getWellnessVisits({}, auth.session.accessToken).then(setVisits).catch(() => undefined);
  }, [auth.session]);

  useEffect(() => () => {
    Object.values(reportUploadControllers.current).forEach((controller) => controller.abort());
  }, []);

  async function runOfficerAction(action: () => Promise<string>) {
    setActionError(null);
    setNotice(null);
    try {
      const message = await action();
      setNotice(message);
      const refreshed = await api.getWellnessVisits({}, auth.session?.accessToken);
      setVisits(refreshed);
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : "Officer wellness action failed.");
    }
  }

  function updateReportUpload(id: string, patch: Partial<WellnessReportPhotoUploadItem>) {
    setReportUploads((items) => items.map((item) => item.id === id ? { ...item, ...patch } : item));
  }

  async function uploadOfficerReportPhoto(id: string, targetVisitId: string, file: File) {
    if (file.size > maximumWellnessReportPhotoBytes) {
      updateReportUpload(id, { status: "failed", error: "Wellness report photos must be 10 MB or smaller." });
      return;
    }

    const controller = new AbortController();
    reportUploadControllers.current[id] = controller;

    try {
      const uploadFile = await compressWellnessReportPhoto(file);
      const contentType = resolveWellnessReportPhotoContentType(uploadFile);
      const prepared = await api.prepareWellnessReportPhotoUpload(targetVisitId, auth.session?.accessToken ?? "", {
        officerBadgeNumber: badgeNumber,
        fileName: uploadFile.name,
        contentType,
        sizeBytes: uploadFile.size,
      });
      updateReportUpload(id, { upload: prepared, progress: 5, status: "uploading", error: undefined });
      const uploaded = await api.uploadWellnessReportPhotoContent(targetVisitId, prepared.id, badgeNumber, auth.session?.accessToken ?? "", uploadFile, {
        signal: controller.signal,
        onProgress: (progress) => updateReportUpload(id, { progress, status: "uploading" }),
      });
      updateReportUpload(id, { upload: uploaded, progress: 100, status: "uploaded", error: undefined });
    } catch (caught) {
      updateReportUpload(id, {
        status: controller.signal.aborted ? "cancelled" : "failed",
        error: caught instanceof Error ? caught.message : "Wellness report photo upload failed.",
      });
    } finally {
      delete reportUploadControllers.current[id];
    }
  }

  function addOfficerReportPhotos(files: FileList | null) {
    if (!files?.length) return;
    const targetVisitId = visitId.trim();
    if (!targetVisitId) {
      setActionError("Enter a visit ID before attaching report photos.");
      return;
    }

    setActionError(null);
    Array.from(files).forEach((file) => {
      const id = createLocalUploadId();
      const isTooLarge = file.size > maximumWellnessReportPhotoBytes;
      setReportUploads((items) => [...items, {
        id,
        visitId: targetVisitId,
        file,
        progress: 0,
        status: isTooLarge ? "failed" : "queued",
        error: isTooLarge ? "Wellness report photos must be 10 MB or smaller." : undefined,
      }]);
      if (!isTooLarge) {
        void uploadOfficerReportPhoto(id, targetVisitId, file);
      }
    });
  }

  function cancelReportPhotoUpload(id: string) {
    reportUploadControllers.current[id]?.abort();
    updateReportUpload(id, { status: "cancelled", error: "Wellness report photo upload cancelled." });
  }

  function retryReportPhotoUpload(item: WellnessReportPhotoUploadItem) {
    updateReportUpload(item.id, { upload: undefined, progress: 0, status: "queued", error: undefined });
    void uploadOfficerReportPhoto(item.id, item.visitId, item.file);
  }

  function removeReportPhotoUpload(id: string) {
    reportUploadControllers.current[id]?.abort();
    setReportUploads((items) => items.filter((item) => item.id !== id));
  }

  /* OFC-01 + OFC-02 (DS v2) — onboarding + visit report share this route.
     Contractual: officers are badge IDs only (NST-OFC-XXXX); names never
     render anywhere. All API logic unchanged. */
  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="OFC-01">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        Officer <em className="italic text-deep-hover">wellness</em>
      </h1>

      <div className="flex items-start gap-3.5 rounded-card bg-deep p-[22px]">
        <span className="inline-flex size-11 shrink-0 items-center justify-center rounded-full bg-yellow/15 text-yellow">
          <ShieldCheck size={20} />
        </span>
        <div>
          <div className="text-[14.5px] font-bold text-on-dark-heading">
            Your NestyStay ID resets every January 1. Your privacy is protected by platform policy.
          </div>
          <div className="mt-1 text-[12.5px] text-on-dark-muted">
            You appear to hosts as a badge ID only — NST-OFC-XXXX. Your name is NEVER shown, anywhere.
          </div>
        </div>
      </div>

      <div className="grid items-start gap-4 lg:grid-cols-2">
        <form
          className="flex flex-col gap-3.5 rounded-card border border-sand-border bg-cream p-[22px]"
          onSubmit={(event) => {
            event.preventDefault();
            void runOfficerAction(async () => {
              const result = await api.onboardWellnessOfficer({
                userId: auth.session?.userId,
                badgeNumber,
                parish,
                coverageArea,
                isActiveOffDuty,
                isRetired,
                verificationMetadata: JSON.stringify({ documents, availability: { days: availabilityDays, start: availabilityStart, end: availabilityEnd }, privacyConsent, submittedAt: new Date().toISOString() }),
              });
              setOfficer(result);
              window.localStorage.removeItem(onboardingDraftKey);
              return `Officer ${result.badgeNumber} onboarding is ${result.onboardingStatus}.`;
            });
          }}
        >
          <div className="flex flex-wrap items-center justify-between gap-2"><h2 className="m-0 font-display text-xl font-medium">Officer onboarding</h2><span className="text-xs font-semibold text-sand-500">Step {onboardingStep} of 3</span></div>
          <div className="flex items-center gap-2" aria-label="Officer onboarding progress">
            {["Identity", "Documents", "Availability"].map((label, index) => <div className="flex flex-1 items-center gap-1.5" key={label}><span className={`inline-flex size-7 items-center justify-center rounded-full text-xs font-bold ${onboardingStep > index ? "bg-deep text-white" : "bg-shell text-sand-500"}`}>{onboardingStep > index + 1 ? <Check size={14} /> : index + 1}</span><span className="hidden text-xs font-semibold sm:inline">{label}</span>{index < 2 && <span className="h-px flex-1 bg-shell" />}</div>)}
          </div>
          {onboardingStep === 1 && <>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field label="JCF badge number"><Input required value={badgeNumber} onChange={(event) => setBadgeNumber(event.target.value)} /></Field>
              <Field label="Parish"><Input required value={parish} onChange={(event) => setParish(event.target.value)} /></Field>
            </div>
            <Field label="Coverage zone"><Input required placeholder="e.g. Negril — West End to Sheffield" value={coverageArea} onChange={(event) => setCoverageArea(event.target.value)} /></Field>
            <div className="flex flex-col gap-1">
              <label className="flex min-h-11 cursor-pointer items-center gap-3"><input checked={isActiveOffDuty} className="size-5 accent-deep-hover" onChange={(event) => setIsActiveOffDuty(event.target.checked)} type="checkbox" /><span className="text-sm">Active off-duty JCF officer</span></label>
              <label className="flex min-h-11 cursor-pointer items-center gap-3"><input checked={isRetired} className="size-5 accent-deep-hover" onChange={(event) => setIsRetired(event.target.checked)} type="checkbox" /><span className="text-sm">Retired</span><span className="text-xs font-semibold text-coral-text">— retired officers are automatically rejected</span></label>
            </div>
            <div className="rounded-field bg-mint-tint px-4 py-3 text-[12.5px] text-mint-text"><Map size={15} className="mr-1 inline" /> Coverage preview: <strong>{parish || "Select a parish"}</strong> · {coverageArea || "Add a coverage area"}. Location data is used for assignment matching only.</div>
          </>}
          {onboardingStep === 2 && <div className="flex flex-col gap-3"><div className="text-sm text-gray-600">Upload or confirm the documents an administrator will review. Files are not exposed to hosts.</div>{([ ["governmentId", "Government-issued ID"], ["policeBadge", "JCF badge evidence"], ["proofOfAddress", "Proof of address" ]] as const).map(([key, label]) => <label className="flex min-h-12 cursor-pointer items-center justify-between gap-3 rounded-field border border-sand-border bg-white px-4" key={key}><span className="flex items-center gap-2 text-sm font-semibold"><FileCheck2 size={16} className={documents[key] ? "text-success-text" : "text-sand-500"} />{label}</span><input aria-label={label} checked={documents[key]} className="size-5 accent-deep-hover" onChange={(event) => setDocuments((current) => ({ ...current, [key]: event.target.checked }))} type="checkbox" /></label>)}<div className="rounded-field bg-amber-tint px-4 py-3 text-[12.5px] text-amber-text"><Eye size={15} className="mr-1 inline" /> Admin review uses a side-by-side checklist. Sensitive documents remain role-restricted.</div></div>}
          {onboardingStep === 3 && <div className="flex flex-col gap-3"><div className="text-sm text-gray-600">Set the days and hours you can accept assignments. Times are stored as your local availability window.</div><div className="flex flex-wrap gap-2">{["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map((day) => <label className={`inline-flex min-h-10 cursor-pointer items-center gap-2 rounded-pill border px-3 text-xs font-semibold ${availabilityDays.includes(day) ? "border-deep bg-mint-tint" : "border-sand-input bg-white"}`} key={day}><input checked={availabilityDays.includes(day)} className="sr-only" onChange={() => setAvailabilityDays((current) => current.includes(day) ? current.filter((item) => item !== day) : [...current, day])} type="checkbox" />{availabilityDays.includes(day) && <Check size={13} />}{day}</label>)}</div><div className="grid gap-3 sm:grid-cols-2"><Field label="Available from"><Input type="time" value={availabilityStart} onChange={(event) => setAvailabilityStart(event.target.value)} /></Field><Field label="Available until"><Input type="time" value={availabilityEnd} onChange={(event) => setAvailabilityEnd(event.target.value)} /></Field></div><label className="flex items-start gap-3 rounded-field border border-sand-border bg-white p-3"><input checked={privacyConsent} className="mt-1 size-5 accent-deep-hover" onChange={(event) => setPrivacyConsent(event.target.checked)} type="checkbox" /><span className="text-[12.5px]">I consent to NestyStay securely processing my officer information for verification, assignment, payouts, and audit retention. Hosts see badge ID only.</span></label></div>}
          <div className="flex flex-wrap items-center justify-between gap-2"><span className="text-xs text-sand-500">{draftSavedAt ? `Draft saved ${new Date(draftSavedAt).toLocaleTimeString()}` : "Draft saves automatically"}</span><div className="flex gap-2">{onboardingStep > 1 && <Button onClick={() => setOnboardingStep((step) => step - 1)} type="button" variant="ghost">Back</Button>}{onboardingStep < 3 ? <Button onClick={() => setOnboardingStep((step) => step + 1)} type="button" variant="outline">Continue <ArrowRight size={16} /></Button> : <Button disabled={!privacyConsent || availabilityDays.length === 0 || !documents.governmentId || !documents.policeBadge || !documents.proofOfAddress} type="submit" variant="dark"><BadgeCheck size={17} /> Submit for verification</Button>}</div></div>
          {officer && (
            <div className="flex flex-wrap items-center gap-2 rounded-field bg-shell px-4 py-3 text-[13px]">
              <span className="font-mono font-bold">{officer.badgeNumber}</span>
              <StatusChip value={officer.verificationStatus} />
              <span className="text-gray-600">free badges {officer.freeBadges.join(", ") || "pending"}</span>
            </div>
          )}
          <div className="flex flex-wrap items-center gap-1.5">
            <span className="text-xs font-semibold text-sand-500">Application statuses:</span>
            <StatusChip value="Pending" />
            <StatusChip value="Verified" />
            <StatusChip value="Rejected" />
            <StatusChip value="Suspended" />
          </div>
        </form>

        <form className="flex flex-col gap-3.5 rounded-card border border-sand-border bg-cream p-[22px]">
          <h2 className="m-0 font-display text-xl font-medium">Evidence report</h2>
          <div className="grid gap-3 sm:grid-cols-2">
            <Field label="Visit ID">
              <Input value={visitId} onChange={(event) => setVisitId(event.target.value)} />
            </Field>
            <Field label="Officer badge">
              <Input value={badgeNumber} onChange={(event) => setBadgeNumber(event.target.value)} />
            </Field>
          </div>
          <Field label="Notes">
            <Textarea placeholder="Perimeter secure, pool gate latched, no signs of entry…" value={notes} onChange={(event) => setNotes(event.target.value)} />
          </Field>
          <div className="rounded-field bg-amber-tint px-4 py-3 text-[12.5px] text-amber-text">
            Enforced by the API: only the assigned officer can file, and never before the scheduled time.
          </div>
          <div className={`flex items-center gap-2 rounded-field px-4 py-3 text-[12.5px] ${isOffline ? "bg-amber-tint text-amber-text" : "bg-shell text-gray-600"}`} role="status">
            {isOffline ? <WifiOff size={15} /> : <Check size={15} />} {isOffline ? "Offline draft mode: your visit ID and notes are saved locally and will be ready when you reconnect." : "Draft autosaved locally. Uploads resume only when you are online."}
          </div>
          <div className="wellness-upload-panel">
            <label className={buttonClassName("outline", "property-photo-picker")}>
              <Paperclip size={16} /> Report photos
              <input accept="image/jpeg,image/png,image/webp" multiple onChange={(event) => { addOfficerReportPhotos(event.currentTarget.files); event.currentTarget.value = ""; }} type="file" />
            </label>
            <WellnessReportUploadList
              uploads={reportUploads.filter((upload) => upload.visitId === visitId.trim())}
              onCaptionChange={(id, caption) => updateReportUpload(id, { caption })}
              onCancel={cancelReportPhotoUpload}
              onRemove={removeReportPhotoUpload}
              onRetry={retryReportPhotoUpload}
            />
          </div>
          <Button
            type="button"
            variant="dark"
            disabled={uploadedReportPhotoIds.length === 0}
            onClick={() =>
              void runOfficerAction(async () => {
                if (!auth.session?.accessToken) throw new Error("Sign in with an approved Officer account before submitting a report.");
                const result = await api.submitWellnessReport(visitId, auth.session?.accessToken ?? "", {
                  officerBadgeNumber: badgeNumber,
                  notes: [notes, ...reportUploads.filter((upload) => upload.visitId === visitId.trim() && upload.caption?.trim()).map((upload) => `${upload.file.name}: ${upload.caption!.trim()}`)].join("\nPhoto notes: "),
                  photos: uploadedReportPhotoIds,
                });
                return `Report submitted. Visit is ${result.visitStatus}; payout is ${result.paymentStatus}.`;
              })
            }
          >
            <ReceiptText size={17} /> Submit report
          </Button>
          {notice && (
            <div className="rounded-field bg-success-tint px-4 py-3 text-[13px] font-semibold text-success-text">{notice}</div>
          )}
          {actionError && <ErrorState message={actionError} />}
        </form>
      </div>

      <div className="flex flex-col gap-3" id="OFC-02">
        <div className="flex flex-wrap items-center justify-between gap-2"><h2 className="m-0 font-display text-xl font-medium">Your assigned visits</h2><span className="inline-flex items-center gap-1.5 rounded-pill bg-shell px-3 py-1.5 text-xs font-semibold"><CalendarClock size={14} /> {assignedVisits.length} active assignment{assignedVisits.length === 1 ? "" : "s"}</span></div>
        {assignedVisits.length === 0 ? (
          <div className="text-[13px] text-gray-600">
            No visits are assigned to <span className="font-mono font-bold">{badgeNumber.trim().toUpperCase()}</span> yet.
          </div>
        ) : (
          <div className="flex flex-col gap-3">{assignedVisits.map((visit) => <Card className="border border-sand-border" key={visit.id}><div className="flex flex-wrap items-start justify-between gap-3"><div><div className="font-semibold">{new Date(visit.scheduledAt).toLocaleDateString(undefined, { weekday: "short", month: "short", day: "numeric" })} · {new Date(visit.scheduledAt).toLocaleTimeString(undefined, { hour: "numeric", minute: "2-digit" })}</div><div className="mt-1 text-xs text-gray-600">{visit.visitType} · {visit.parish} / {visit.area}</div></div><StatusChip value={visit.visitStatus} /></div><div className="mt-3 flex flex-wrap items-center gap-2 text-xs text-sand-500"><TimerReset size={14} /> {visit.timeline.slice(-2).join(" · ") || "Assignment created"}</div></Card>)}</div>
        )}
      </div>

      <section className="grid gap-4 lg:grid-cols-3" aria-label="Officer privacy and support">
        <Card><div className="flex items-center gap-2 font-semibold"><ShieldCheck size={17} className="text-deep-hover" /> Privacy controls</div><p className="mt-2 text-[12.5px] text-gray-600">Your name, photo, phone, and home address are never shown to hosts. Only badge ID, parish coverage, and assignment status are shared.</p><div className="mt-3 flex items-center gap-2 text-xs font-semibold text-success-text"><Check size={14} /> Host visibility restricted</div></Card>
        <Card><div className="flex items-center gap-2 font-semibold"><Bell size={17} className="text-deep-hover" /> Notification preferences</div><p className="mt-2 text-[12.5px] text-gray-600">Assignment and report events are recorded in the secure activity timeline. Email/SMS delivery depends on the configured notification provider.</p><div className="mt-3 flex items-center gap-2 text-xs font-semibold text-amber-text"><CircleAlert size={14} /> Provider delivery status shown by admin</div></Card>
        <Card><div className="flex items-center gap-2 font-semibold"><Lock size={17} className="text-deep-hover" /> Data retention</div><p className="mt-2 text-[12.5px] text-gray-600">Verification and report records are retained for audit and safety purposes, then handled under the platform retention policy.</p><div className="mt-3 text-xs font-semibold text-sand-500">Consent history captured with onboarding</div></Card>
      </section>
    </div>
  );
}

function WellnessReportUploadList({
  uploads,
  onCaptionChange,
  onCancel,
  onRemove,
  onRetry,
}: {
  uploads: WellnessReportPhotoUploadItem[];
  onCaptionChange?: (id: string, caption: string) => void;
  onCancel: (id: string) => void;
  onRemove: (id: string) => void;
  onRetry: (item: WellnessReportPhotoUploadItem) => void;
}) {
  if (uploads.length === 0) {
    return null;
  }

  return (
    <div className="property-upload-list wellness-upload-list">
      {uploads.map((upload) => (
        <div className="property-upload-item" key={upload.id}>
          <div className="flex min-w-0 flex-1 flex-col gap-1"><span className="truncate">{upload.file.name}</span>{onCaptionChange && <input aria-label={`Caption ${upload.file.name}`} className="min-h-8 rounded border border-sand-input bg-white px-2 text-xs" onChange={(event) => onCaptionChange(upload.id, event.target.value)} placeholder="Optional caption" value={upload.caption ?? ""} />}</div>
          <small>{upload.status === "uploading" ? `${upload.progress}%` : upload.error ?? upload.upload?.scanStatus ?? upload.status}</small>
          <div className="property-upload-progress"><span style={{ width: `${upload.status === "uploaded" ? 100 : upload.progress}%` }} /></div>
          {(upload.status === "uploading" || upload.status === "queued") && <Button onClick={() => onCancel(upload.id)} title="Cancel upload" variant="ghost"><X size={15} /></Button>}
          {(upload.status === "failed" || upload.status === "cancelled") && <Button onClick={() => onRetry(upload)} title="Retry upload" variant="ghost"><RotateCcw size={15} /></Button>}
          {upload.status !== "uploading" && <Button onClick={() => onRemove(upload.id)} title="Remove photo" variant="ghost"><X size={15} /></Button>}
        </div>
      ))}
    </div>
  );
}

type PropertyPhotoUploadStatus = "queued" | "uploading" | "uploaded" | "failed" | "cancelled";

type PropertyPhotoUploadItem = {
  id: string;
  propertyId: string;
  file: File;
  progress: number;
  status: PropertyPhotoUploadStatus;
  upload?: PropertyPhotoUpload;
  error?: string;
};

const maximumPropertyPhotoBytes = 10 * 1024 * 1024;

export function PropertyManagementPage({ auth }: { auth: AuthController }) {
  if (!auth.session) return <RequireAuth auth={auth} title="Property management needs a host session." />;
  return <PropertyManagementContent auth={auth} />;
}

function PropertyManagementContent({ auth }: { auth: AuthController }) {
  const { properties, isLoading, error, reload } = useProperties(auth.session?.accessToken);
  const [form, setForm] = useState({
    title: "New Tropical Studio",
    location: "Port Antonio, Portland",
    country: "Jamaica",
    nightlyRate: "155",
    currency: "USD",
    badgeLevel: "Free",
    cancellationPolicy: "Flexible",
    guestVerificationEnabled: false,
    insuraGuestEnabled: true,
    highlights: "Ocean breeze, Workspace, Fast Wi-Fi",
  });
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [created, setCreated] = useState<PropertyListing | null>(null);
  const [photoUploads, setPhotoUploads] = useState<PropertyPhotoUploadItem[]>([]);
  const photoUploadControllers = useRef<Record<string, AbortController>>({});
  const hostProperties = properties.filter((property) => property.hostUserId === auth.session?.userId);

  useEffect(() => () => {
    Object.values(photoUploadControllers.current).forEach((controller) => controller.abort());
  }, []);

  function update<K extends keyof typeof form>(key: K, value: (typeof form)[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function updatePhotoUpload(id: string, patch: Partial<PropertyPhotoUploadItem>) {
    setPhotoUploads((items) => items.map((item) => item.id === id ? { ...item, ...patch } : item));
  }

  async function uploadPropertyPhoto(id: string, propertyId: string, file: File) {
    if (!auth.session) return;
    if (file.size > maximumPropertyPhotoBytes) {
      updatePhotoUpload(id, { status: "failed", error: "Property photos must be 10 MB or smaller." });
      return;
    }

    const controller = new AbortController();
    photoUploadControllers.current[id] = controller;

    try {
      const contentType = resolvePropertyPhotoContentType(file);
      const prepared = await api.preparePropertyPhotoUpload(propertyId, auth.session.accessToken, {
        fileName: file.name,
        contentType,
        sizeBytes: file.size,
      });
      updatePhotoUpload(id, { upload: prepared, progress: 5, status: "uploading", error: undefined });
      const uploaded = await api.uploadPropertyPhotoContent(propertyId, prepared.id, auth.session.accessToken, file, {
        signal: controller.signal,
        onProgress: (progress) => updatePhotoUpload(id, { progress, status: "uploading" }),
      });
      updatePhotoUpload(id, { upload: uploaded, progress: 100, status: "uploaded", error: undefined });
    } catch (caught) {
      updatePhotoUpload(id, {
        status: controller.signal.aborted ? "cancelled" : "failed",
        error: caught instanceof Error ? caught.message : "Property photo upload failed.",
      });
    } finally {
      delete photoUploadControllers.current[id];
    }
  }

  function addPropertyPhotos(propertyId: string, files: FileList | null) {
    if (!files?.length) return;
    Array.from(files).forEach((file) => {
      const id = createLocalUploadId();
      const isTooLarge = file.size > maximumPropertyPhotoBytes;
      setPhotoUploads((items) => [...items, {
        id,
        propertyId,
        file,
        progress: 0,
        status: isTooLarge ? "failed" : "queued",
        error: isTooLarge ? "Property photos must be 10 MB or smaller." : undefined,
      }]);
      if (!isTooLarge) {
        void uploadPropertyPhoto(id, propertyId, file);
      }
    });
  }

  function cancelPropertyPhotoUpload(id: string) {
    photoUploadControllers.current[id]?.abort();
    updatePhotoUpload(id, { status: "cancelled", error: "Property photo upload cancelled." });
  }

  function retryPropertyPhotoUpload(item: PropertyPhotoUploadItem) {
    updatePhotoUpload(item.id, { upload: undefined, progress: 0, status: "queued", error: undefined });
    void uploadPropertyPhoto(item.id, item.propertyId, item.file);
  }

  function removePropertyPhotoUpload(id: string) {
    photoUploadControllers.current[id]?.abort();
    setPhotoUploads((items) => items.filter((item) => item.id !== id));
  }

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    if (!auth.session) return;
    setSubmitError(null);
    setCreated(null);

    const payload: CreatePropertyRequest = {
      hostUserId: auth.session.userId,
      hostName: auth.session.displayName,
      hostEmail: auth.session.email,
      title: form.title,
      location: form.location,
      country: form.country,
      nightlyRate: Number(form.nightlyRate),
      currency: form.currency,
      badgeLevel:
        form.guestVerificationEnabled && form.badgeLevel === "Free" ? "Verified" : form.badgeLevel,
      guestVerificationEnabled: form.guestVerificationEnabled,
      insuraGuestEnabled: form.insuraGuestEnabled,
      cancellationPolicy: form.cancellationPolicy,
      highlights: form.highlights
        .split(",")
        .map((item) => item.trim())
        .filter(Boolean),
    };

    try {
      const property = await api.createProperty(payload, auth.session.accessToken);
      setCreated(property);
      await reload();
    } catch (caught) {
      setSubmitError(caught instanceof Error ? caught.message : "Property could not be created.");
    }
  }

  const wizardField =
    "min-h-12 w-full rounded-field border-[1.5px] border-sand-input bg-white px-4 font-sans text-[14.5px] text-ink outline-none transition-[border-color,box-shadow] focus:border-deep-hover focus:shadow-[0_0_0_3px_rgba(14,74,69,0.12)]";
  const wizardLabel = "font-sans text-[13px] font-semibold text-ink";

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="HOST-05">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
        New <em className="italic text-deep-hover">property</em>
      </h1>

      {/* Wizard context — the form below covers every backend field; step 8 is the verification rule */}
      <div className="flex flex-wrap gap-1.5">
        {["Basics", "Location", "Pricing", "Highlights", "Cancellation", "Insurance"].map((step) => (
          <span
            className="inline-flex min-h-11 items-center rounded-pill border border-sand-input bg-cream px-3.5 text-[12.5px] font-semibold text-success-text"
            key={step}
          >
            ✓ {step}
          </span>
        ))}
        <span className="inline-flex min-h-11 items-center gap-1.5 rounded-pill bg-deep px-4 text-[12.5px] font-bold text-on-dark-heading">
          <span className="inline-flex size-[18px] items-center justify-center rounded-full bg-yellow text-[10px] font-bold text-deep">8</span>
          Verification
        </span>
      </div>

      <form className="flex flex-col gap-5" onSubmit={handleCreate}>
        <div className="flex flex-col gap-4 rounded-card border border-sand-border bg-cream p-[22px]">
          <div className="font-display text-xl font-medium">Listing details</div>
          <div className="grid gap-3 sm:grid-cols-2">
            <label className="flex flex-col gap-1.5">
              <span className={wizardLabel}>Title</span>
              <input className={wizardField} onChange={(event) => update("title", event.target.value)} value={form.title} />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={wizardLabel}>Location</span>
              <input className={wizardField} onChange={(event) => update("location", event.target.value)} value={form.location} />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={wizardLabel}>Country</span>
              <input className={wizardField} onChange={(event) => update("country", event.target.value)} value={form.country} />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={wizardLabel}>Nightly rate (must be &gt; 0)</span>
              <input className={wizardField} min="1" onChange={(event) => update("nightlyRate", event.target.value)} type="number" value={form.nightlyRate} />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={wizardLabel}>Currency (3-letter)</span>
              <input className={wizardField} maxLength={3} onChange={(event) => update("currency", event.target.value.toUpperCase())} value={form.currency} />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={wizardLabel}>Badge</span>
              <select className={wizardField} onChange={(event) => update("badgeLevel", event.target.value)} value={form.badgeLevel}>
                <option value="Free">Free</option>
                <option value="Verified">Verified</option>
                <option value="Trusted">Trusted</option>
                <option value="Wellness">Wellness</option>
              </select>
            </label>
            <label className="flex flex-col gap-1.5">
              <span className={wizardLabel}>Cancellation policy</span>
              <select className={wizardField} onChange={(event) => update("cancellationPolicy", event.target.value)} value={form.cancellationPolicy}>
                <option value="Flexible">Flexible</option>
                <option value="Moderate">Moderate</option>
                <option value="Strict">Strict</option>
              </select>
            </label>
            <label className="flex flex-col gap-1.5 sm:col-span-2">
              <span className={wizardLabel}>Highlights (comma-separated, shown as chips)</span>
              <textarea className={`${wizardField} min-h-[80px] resize-y py-3`} onChange={(event) => update("highlights", event.target.value)} value={form.highlights} />
            </label>
          </div>
        </div>

        {/* Step 8 — client contract: verification is NEVER automatic */}
        <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
          <div className="font-display text-xl font-medium">Step 8 — Verification &amp; availability</div>
          <div className="rounded-[16px] border-[1.5px] border-sand-border bg-white px-5 py-[18px]">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <span className="text-[14.5px] font-semibold">Traveler identity verification (eKYC)</span>
              <button
                aria-checked={form.guestVerificationEnabled}
                aria-label="Traveler identity verification (eKYC)"
                className={`relative block h-9 w-16 shrink-0 cursor-pointer rounded-pill border-none transition-colors ${form.guestVerificationEnabled ? "bg-deep-hover" : "bg-sand-input"}`}
                onClick={() => update("guestVerificationEnabled", !form.guestVerificationEnabled)}
                role="switch"
                type="button"
              >
                <span className={`absolute top-1 block size-7 rounded-full bg-white transition-all ${form.guestVerificationEnabled ? "right-1" : "left-1"}`} />
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
          <label className="flex min-h-11 cursor-pointer items-center gap-3 text-sm">
            <input
              checked={form.insuraGuestEnabled}
              className="size-5 accent-deep-hover"
              onChange={(event) => update("insuraGuestEnabled", event.target.checked)}
              type="checkbox"
            />
            InsuraGuest coverage for this property
          </label>
          <div className="flex flex-wrap items-center justify-end gap-2.5 border-t border-shell pt-3.5">
            <button
              className="inline-flex min-h-[46px] cursor-pointer items-center gap-2 rounded-pill border-none bg-deep px-[22px] font-sans text-[13.5px] font-semibold text-on-dark-heading transition-colors hover:bg-deep-hover"
              type="submit"
            >
              Publish property
            </button>
          </div>
          {created && (
            <div className="rounded-field bg-success-tint px-4 py-3 text-[13px] text-success-text" role="status">
              {created.title} is live in the property API.
            </div>
          )}
          {submitError && <ErrorState message={submitError} />}
        </div>
      </form>

      <section className="flex flex-col gap-3">
          <h2 className="m-0 font-display text-xl font-medium">Your live listings</h2>
          {isLoading && <LoadingState />}
          {error && <ErrorState message={error} onRetry={reload} />}
          {!isLoading && hostProperties.length === 0 && (
            <EmptyState title="No host listings yet." copy="Your saved properties will appear here." />
          )}
          {hostProperties.map((property) => {
            const uploadsForProperty = photoUploads.filter((upload) => upload.propertyId === property.id);
            return (
              <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]" key={property.id}>
                <div className="flex flex-wrap items-center gap-3.5">
                  <div className="min-w-[200px] flex-1">
                    <div className="font-display text-[17px] font-medium">{property.title}</div>
                    <div className="text-[12.5px] text-gray-600">{property.location}</div>
                  </div>
                  <TierBadge level={property.badgeLevel} />
                  <label className="inline-flex min-h-[46px] cursor-pointer items-center gap-2 rounded-pill border-[1.5px] border-sand-input px-5 text-[13.5px] font-semibold text-ink transition-colors hover:border-deep">
                    <Paperclip size={15} /> Photos
                    <input accept="image/jpeg,image/png,image/webp" className="hidden" multiple onChange={(event) => { addPropertyPhotos(property.id, event.currentTarget.files); event.currentTarget.value = ""; }} type="file" />
                  </label>
                </div>
                {uploadsForProperty.length > 0 && (
                  <div className="flex flex-col gap-2">
                    {uploadsForProperty.map((upload) => (
                      <div className="flex flex-wrap items-center gap-2.5 rounded-field border border-sand-border bg-white px-3.5 py-2.5 text-[13px]" key={upload.id}>
                        <span className="flex-1 truncate font-semibold">{upload.file.name}</span>
                        <small className="text-gray-600">{upload.status === "uploading" ? `${upload.progress}%` : upload.error ?? upload.upload?.scanStatus ?? upload.status}</small>
                        <div className="h-1.5 w-24 overflow-hidden rounded-pill bg-shell">
                          <span className="block h-full rounded-pill bg-deep-hover" style={{ width: `${upload.status === "uploaded" ? 100 : upload.progress}%` }} />
                        </div>
                        {(upload.status === "uploading" || upload.status === "queued") && <Button onClick={() => cancelPropertyPhotoUpload(upload.id)} title="Cancel upload" variant="ghost"><X size={15} /></Button>}
                        {(upload.status === "failed" || upload.status === "cancelled") && <Button onClick={() => retryPropertyPhotoUpload(upload)} title="Retry upload" variant="ghost"><RotateCcw size={15} /></Button>}
                        {upload.status !== "uploading" && <Button onClick={() => removePropertyPhotoUpload(upload.id)} title="Remove photo" variant="ghost"><X size={15} /></Button>}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            );
          })}
      </section>
    </div>
  );
}

function createLocalUploadId() {
  return globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2)}`;
}

function resolvePropertyPhotoContentType(file: File) {
  if (file.type) return file.type;
  const name = file.name.toLowerCase();
  if (name.endsWith(".png")) return "image/png";
  if (name.endsWith(".webp")) return "image/webp";
  if (name.endsWith(".jpg") || name.endsWith(".jpeg")) return "image/jpeg";
  return "application/octet-stream";
}

function resolveWellnessReportPhotoContentType(file: File) {
  return resolvePropertyPhotoContentType(file);
}

async function compressWellnessReportPhoto(file: File): Promise<File> {
  if (file.size <= 2 * 1024 * 1024 || !file.type.startsWith("image/")) return file;
  try {
    const bitmap = await createImageBitmap(file);
    const scale = Math.min(1, 1600 / Math.max(bitmap.width, bitmap.height));
    const canvas = document.createElement("canvas");
    canvas.width = Math.max(1, Math.round(bitmap.width * scale));
    canvas.height = Math.max(1, Math.round(bitmap.height * scale));
    canvas.getContext("2d")?.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
    bitmap.close();
    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, "image/jpeg", 0.82));
    return blob ? new File([blob], file.name.replace(/\.(png|webp)$/i, ".jpg"), { type: "image/jpeg", lastModified: Date.now() }) : file;
  } catch {
    return file;
  }
}

export function CalendarPage({ auth }: { auth: AuthController }) {
  const propertiesState = useProperties();
  const bookingsState = useBookings(auth.session?.accessToken);
  const [propertyId, setPropertyId] = useState("");
  const [checkIn, setCheckIn] = useState(todayPlus(9));
  const [checkOut, setCheckOut] = useState(todayPlus(13));
  const [quote, setQuote] = useState<BookingQuote | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!propertyId && propertiesState.properties[0]) {
      setPropertyId(propertiesState.properties[0].id);
    }
  }, [propertiesState.properties, propertyId]);

  const selectedBookings = bookingsState.bookings.filter((booking) => booking.propertyId === propertyId);

  async function checkAvailability() {
    setError(null);
    setQuote(null);
    try {
      setQuote(await api.quoteBooking({ propertyId, checkIn, checkOut }));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Availability could not be checked.");
    }
  }

  return (
    <div className="product-page">
      <PageHeader
        eyebrow="Calendar"
        title="Availability and date holds."
        copy="The quote endpoint checks blocking bookings and pending verification holds."
      />
      <section className="product-section calendar-layout">
        <div className="calendar-controls">
          <Field label="Property">
            <Select value={propertyId} onChange={(event) => setPropertyId(event.target.value)}>
              {propertiesState.properties.map((property) => (
                <option key={property.id} value={property.id}>
                  {property.title}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Check-in">
            <Input type="date" value={checkIn} onChange={(event) => setCheckIn(event.target.value)} />
          </Field>
          <Field label="Check-out">
            <Input type="date" value={checkOut} onChange={(event) => setCheckOut(event.target.value)} />
          </Field>
          <Button disabled={!propertyId} onClick={checkAvailability}>
            Check dates
          </Button>
        </div>
        {propertiesState.isLoading || bookingsState.isLoading ? <LoadingState /> : null}
        {propertiesState.error && <ErrorState message={propertiesState.error} />}
        {bookingsState.error && <ErrorState message={bookingsState.error} />}
        {error && <ErrorState message={error} />}
        {quote && (
          <div className="notice-panel">
            {quote.datesAvailable ? "Dates are available." : "Dates are not available."} Total quote:{" "}
            {formatMoney(quote.totalAmount, quote.currency)}.
          </div>
        )}
        <div className="calendar-board">
          {selectedBookings.length === 0 ? (
            <EmptyState title="No persisted bookings for this property." />
          ) : (
            selectedBookings.map((booking) => (
              <Card className="calendar-booking" key={booking.id}>
                <CalendarRange size={19} />
                <div>
                  <strong>{booking.checkIn} to {booking.checkOut}</strong>
                  <span>{booking.propertyTitle}</span>
                </div>
                <StatusBadge value={booking.status} />
              </Card>
            ))
          )}
        </div>
      </section>
    </div>
  );
}

function BookingList({
  bookings,
  isLoading,
  error,
  onReload,
}: {
  bookings: Booking[];
  isLoading: boolean;
  error: string | null;
  onReload: () => void;
}) {
  if (isLoading) return <LoadingState label="Loading bookings from the API" />;
  if (error) return <ErrorState message={error} onRetry={onReload} />;
  if (bookings.length === 0) return <EmptyState title="No bookings yet." copy="Bookings created through the popup will appear here." />;

  return (
    <div className="booking-list">
      {bookings.map((booking) => (
        <Card className="booking-row" key={booking.id}>
          <div>
            <strong>{booking.propertyTitle ?? booking.propertyId}</strong>
            <span>{booking.checkIn} to {booking.checkOut}</span>
          </div>
          <StatusBadge value={booking.status} />
          <StatusBadge value={booking.verificationStatus} />
          <StatusBadge value={booking.paymentStatus} />
          <strong>{formatMoney(booking.totalAmount, booking.currency)}</strong>
        </Card>
      ))}
    </div>
  );
}

export function BookingManagementPage({ auth }: { auth: AuthController }) {
  const { bookings, isLoading, error, reload } = useBookings(auth.session?.accessToken);
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionNotice, setActionNotice] = useState<string | null>(null);

  async function resolveVerification(booking: Booking, passed: boolean) {
    if (!booking.ekycTransactionId) return;
    setActionError(null);
    setActionNotice(null);
    try {
      if (!auth.session) throw new Error("A signed admin session is required.");
      await api.resolveVerification(booking.id, passed, booking.ekycTransactionId, auth.session.accessToken);
      setActionNotice(`Verification ${passed ? "approved" : "rejected"} for ${booking.propertyTitle}.`);
      await reload();
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : "Verification could not be resolved.");
    }
  }

  async function capturePayment(booking: Booking) {
    setActionError(null);
    setActionNotice(null);
    try {
      if (!auth.session) throw new Error("A signed host or admin session is required.");
      await api.capturePayment(booking.id, auth.session.accessToken);
      setActionNotice(`Payment captured for ${booking.propertyTitle}.`);
      await reload();
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : "Payment could not be captured.");
    }
  }

  return (
    <div className="product-page">
      <PageHeader
        eyebrow="Bookings"
        title="Manage verification and payment."
        copy="This page uses booking list, verification resolution, and manual capture endpoints."
      />
      <section className="product-section">
        {actionNotice && <div className="notice-panel">{actionNotice}</div>}
        {actionError && <ErrorState message={actionError} />}
        {isLoading && <LoadingState />}
        {error && <ErrorState message={error} onRetry={reload} />}
        {!isLoading && !error && bookings.length === 0 && <EmptyState title="No bookings in the API yet." />}
        <div className="booking-admin-list">
          {bookings.map((booking) => (
            <Card className="booking-admin-card" key={booking.id}>
              <div className="booking-admin-card__head">
                <div>
                  <strong>{booking.propertyTitle ?? booking.id}</strong>
                  <span>{booking.checkIn} to {booking.checkOut}</span>
                </div>
                <StatusBadge value={booking.status} />
              </div>
              <div className="status-grid">
                <span>Verification <StatusBadge value={booking.verificationStatus} /></span>
                <span>Payment <StatusBadge value={booking.paymentStatus} /></span>
                <span>Total <strong>{formatMoney(booking.totalAmount, booking.currency)}</strong></span>
              </div>
              {booking.requiresGuestVerification && booking.verificationStatus !== "PASSED" && (
                <div className="verification-progress-panel">
                  <strong>Nuh Fret</strong>
                  <span>Do not worry - your identity is being verified.</span>
                  <div className="progress-bar"><span /></div>
                  <small>
                    Date hold visible until{" "}
                    {booking.holdExpiresAt ? new Date(booking.holdExpiresAt).toLocaleString() : "host approval"}
                  </small>
                </div>
              )}
              <div className="button-row">
                {booking.requiresGuestVerification && booking.ekycTransactionId && booking.verificationStatus !== "PASSED" && (
                  <>
                    <Button onClick={() => resolveVerification(booking, true)} variant="outline">
                      Pass eKYC
                    </Button>
                    <Button onClick={() => resolveVerification(booking, false)} variant="ghost">
                      Reject
                    </Button>
                  </>
                )}
                {booking.status === "APPROVED" && booking.paymentStatus !== "CAPTURED" && (
                  <Button onClick={() => capturePayment(booking)}>
                    Capture payment
                  </Button>
                )}
              </div>
            </Card>
          ))}
        </div>
      </section>
    </div>
  );
}

export function PaymentConfirmationPage({ auth, bookingId }: { auth: AuthController; bookingId?: string }) {
  const { bookings, isLoading, error, reload } = useBookings(auth.session?.accessToken);
  const [notice, setNotice] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const booking = bookingId ? bookings.find((item) => item.id === bookingId) : bookings[0];

  async function capture() {
    if (!booking) return;
    setNotice(null);
    setActionError(null);
    try {
      if (!auth.session) throw new Error("A signed host or admin session is required.");
      await api.capturePayment(booking.id, auth.session.accessToken);
      setNotice("Payment capture completed through the backend.");
      await reload();
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : "Payment capture failed.");
    }
  }

  return (
    <div className="product-page">
      <PageHeader
        eyebrow="Payment"
        title="Confirmation and capture."
        copy="Approved bookings can be captured through Stripe manual-capture flow when the backend permits it."
      />
      <section className="product-section product-section--center">
        {isLoading && <LoadingState />}
        {error && <ErrorState message={error} onRetry={reload} />}
        {notice && <div className="notice-panel">{notice}</div>}
        {actionError && <ErrorState message={actionError} />}
        {!isLoading && !booking && <EmptyState title="No booking is ready for payment confirmation." />}
        {booking && (
          <Card className="payment-card">
            <CreditCard size={28} />
            <h2>{booking.propertyTitle}</h2>
            <p>{booking.checkIn} to {booking.checkOut}</p>
            <strong>{formatMoney(booking.totalAmount, booking.currency)}</strong>
            <div className="status-grid">
              <span>Status <StatusBadge value={booking.status} /></span>
              <span>Payment <StatusBadge value={booking.paymentStatus} /></span>
            </div>
            <Button disabled={booking.status !== "APPROVED" || booking.paymentStatus === "CAPTURED"} onClick={capture}>
              Capture payment
            </Button>
          </Card>
        )}
      </section>
    </div>
  );
}

type ProfilePhotoUploadStatus = "queued" | "uploading" | "uploaded" | "failed" | "cancelled";

type ProfilePhotoUploadItem = {
  id: string;
  file: File;
  progress: number;
  status: ProfilePhotoUploadStatus;
  upload?: ProfilePhotoUpload;
  error?: string;
};

const maximumProfilePhotoBytes = 10 * 1024 * 1024;

export function ProfileSettingsPage({ auth }: { auth: AuthController }) {
  // TRAV-12: the toggle drives the REAL patois setting (usePatois → localStorage →
  // PatoisToast and all patois copy platform-wide).
  const { showPatois, setShowPatois } = usePatois();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [profilePhotoUrl, setProfilePhotoUrl] = useState("");
  const [profileUploads, setProfileUploads] = useState<ProfilePhotoUploadItem[]>([]);
  const [profileNotice, setProfileNotice] = useState<string | null>(null);
  const [profileError, setProfileError] = useState<string | null>(null);
  const [isProfileLoading, setIsProfileLoading] = useState(false);
  const profileUploadControllers = useRef<Record<string, AbortController>>({});
  const session = auth.session;

  useEffect(() => () => {
    Object.values(profileUploadControllers.current).forEach((controller) => controller.abort());
  }, []);

  useEffect(() => {
    let cancelled = false;
    if (!session) {
      setProfile(null);
      setProfilePhotoUrl("");
      return;
    }

    setIsProfileLoading(true);
    setProfileError(null);
    api.getProfile(session.accessToken)
      .then((result) => {
        if (!cancelled) setProfile(result);
      })
      .catch((caught) => {
        if (!cancelled) setProfileError(caught instanceof Error ? caught.message : "Profile could not be loaded.");
      })
      .finally(() => {
        if (!cancelled) setIsProfileLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [session]);

  useEffect(() => {
    let cancelled = false;
    setProfilePhotoUrl("");
    if (!session || !profile?.photo) {
      return () => {
        cancelled = true;
      };
    }

    api.getProfilePhotoDownload(session.accessToken, profile.photo.id)
      .then((download) => {
        if (!cancelled) setProfilePhotoUrl(download.url);
      })
      .catch(() => {
        if (!cancelled) setProfilePhotoUrl("");
      });
    return () => {
      cancelled = true;
    };
  }, [session, profile?.photo?.id]);

  if (!session) return <RequireAuth auth={auth} title="Profile settings need an active session." />;

  function updateProfileUpload(id: string, patch: Partial<ProfilePhotoUploadItem>) {
    setProfileUploads((items) => items.map((item) => item.id === id ? { ...item, ...patch } : item));
  }

  async function refreshProfile() {
    if (!session) return;
    setProfile(await api.getProfile(session.accessToken));
  }

  async function uploadProfilePhoto(id: string, file: File) {
    if (!session) return;
    setProfileNotice(null);
    setProfileError(null);
    if (file.size > maximumProfilePhotoBytes) {
      updateProfileUpload(id, { status: "failed", error: "Profile photos must be 10 MB or smaller." });
      return;
    }

    const controller = new AbortController();
    profileUploadControllers.current[id] = controller;

    try {
      const prepared = await api.prepareProfilePhotoUpload(session.accessToken, {
        fileName: file.name,
        contentType: resolvePropertyPhotoContentType(file),
        sizeBytes: file.size,
      });
      updateProfileUpload(id, { upload: prepared, progress: 5, status: "uploading", error: undefined });
      const uploaded = await api.uploadProfilePhotoContent(session.accessToken, prepared.id, file, {
        signal: controller.signal,
        onProgress: (progress) => updateProfileUpload(id, { progress, status: "uploading" }),
      });
      updateProfileUpload(id, { upload: uploaded, progress: 100, status: "uploaded", error: undefined });
      setProfileNotice(`${uploaded.fileName} uploaded and verified.`);
      await refreshProfile();
    } catch (caught) {
      updateProfileUpload(id, {
        status: controller.signal.aborted ? "cancelled" : "failed",
        error: caught instanceof Error ? caught.message : "Profile photo upload failed.",
      });
    } finally {
      delete profileUploadControllers.current[id];
    }
  }

  function addProfilePhoto(files: FileList | null) {
    const file = files?.[0];
    if (!file) return;
    const id = createLocalUploadId();
    const isTooLarge = file.size > maximumProfilePhotoBytes;
    setProfileUploads((items) => [...items, {
      id,
      file,
      progress: 0,
      status: isTooLarge ? "failed" : "queued",
      error: isTooLarge ? "Profile photos must be 10 MB or smaller." : undefined,
    }]);
    if (!isTooLarge) {
      void uploadProfilePhoto(id, file);
    }
  }

  function cancelProfilePhotoUpload(id: string) {
    profileUploadControllers.current[id]?.abort();
    updateProfileUpload(id, { status: "cancelled", error: "Profile photo upload cancelled." });
  }

  function retryProfilePhotoUpload(item: ProfilePhotoUploadItem) {
    updateProfileUpload(item.id, { upload: undefined, progress: 0, status: "queued", error: undefined });
    void uploadProfilePhoto(item.id, item.file);
  }

  function removeProfilePhotoUpload(id: string) {
    profileUploadControllers.current[id]?.abort();
    setProfileUploads((items) => items.filter((item) => item.id !== id));
  }

  const initials = (profile?.displayName ?? session.displayName)
    .split(/\s+/)
    .map((part) => part[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();

  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="TRAV-12">
      <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">Settings</h1>

      {/* Account card */}
      <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="flex flex-wrap items-center gap-4">
          <span className="relative inline-flex size-16 shrink-0 items-center justify-center overflow-hidden rounded-full bg-deep font-display text-2xl text-yellow">
            {profilePhotoUrl ? (
              <img alt="" className="absolute inset-0 h-full w-full object-cover" onError={() => setProfilePhotoUrl("")} src={profilePhotoUrl} />
            ) : (
              initials
            )}
          </span>
          <div className="min-w-[200px] flex-1">
            <div className="font-display text-xl font-medium">{profile?.displayName ?? session.displayName}</div>
            <div className="text-[13px] text-gray-600">
              {profile?.email ?? session.email} · {(profile?.roles ?? session.roles).join(" · ").toLowerCase()} account
            </div>
            <div className="mt-0.5 text-xs text-sand-500">
              Session expires {new Date(session.expiresAt).toLocaleString()} — signing out everywhere resets it.
            </div>
          </div>
          <label className="profile-photo-picker inline-flex min-h-[46px] cursor-pointer items-center gap-2 rounded-pill border-[1.5px] border-sand-input px-5 text-[13.5px] font-semibold text-ink transition-colors hover:border-deep">
            <Paperclip size={15} /> Profile photo
            <input accept="image/jpeg,image/png,image/webp" className="hidden" onChange={(event) => { addProfilePhoto(event.currentTarget.files); event.currentTarget.value = ""; }} type="file" />
          </label>
          <button
            className="inline-flex min-h-[46px] cursor-pointer items-center rounded-pill border-[1.5px] border-sand-input bg-transparent px-5 font-sans text-[13.5px] font-semibold text-ink transition-colors hover:border-deep"
            onClick={() => {
              void auth.logout().finally(() => navigate("/logout"));
            }}
            type="button"
          >
            Sign out
          </button>
        </div>
        {isProfileLoading && <LoadingState label="Loading profile" />}
        {profile?.photo && (
          <div className="rounded-field bg-shell px-4 py-2.5 text-[13px] text-gray-600">
            {profile.photo.fileName} · {profile.photo.scanStatus}
          </div>
        )}
        {profileUploads.length > 0 && (
          <div className="flex flex-col gap-2">
            {profileUploads.map((upload) => (
              <div className="flex flex-wrap items-center gap-2.5 rounded-field border border-sand-border bg-white px-3.5 py-2.5 text-[13px]" key={upload.id}>
                <span className="flex-1 truncate font-semibold">{upload.file.name}</span>
                <small className="text-gray-600">{upload.status === "uploading" ? `${upload.progress}%` : upload.error ?? upload.upload?.scanStatus ?? upload.status}</small>
                <div className="h-1.5 w-24 overflow-hidden rounded-pill bg-shell">
                  <span className="block h-full rounded-pill bg-deep-hover" style={{ width: `${upload.status === "uploaded" ? 100 : upload.progress}%` }} />
                </div>
                {(upload.status === "uploading" || upload.status === "queued") && <Button onClick={() => cancelProfilePhotoUpload(upload.id)} title="Cancel upload" variant="ghost"><X size={15} /></Button>}
                {(upload.status === "failed" || upload.status === "cancelled") && <Button onClick={() => retryProfilePhotoUpload(upload)} title="Retry upload" variant="ghost"><RotateCcw size={15} /></Button>}
                {upload.status !== "uploading" && <Button onClick={() => removeProfilePhotoUpload(upload.id)} title="Remove photo" variant="ghost"><X size={15} /></Button>}
              </div>
            ))}
          </div>
        )}
        {profileNotice && <div className="rounded-field bg-success-tint px-4 py-2.5 text-[13px] text-success-text" role="status">{profileNotice}</div>}
        {profileError && <ErrorState message={profileError} />}
      </div>

      {/* Prominent patois toggle card — never buried. Drives the real setting. */}
      <div className="flex items-start gap-[18px] rounded-card bg-deep p-[26px]">
        <button
          aria-checked={showPatois}
          aria-label="Jamaican Patois greetings"
          className={`relative mt-1 block h-9 w-16 shrink-0 cursor-pointer rounded-pill border-none transition-colors ${showPatois ? "bg-yellow" : "bg-on-dark-heading/20"}`}
          onClick={() => setShowPatois(!showPatois)}
          role="switch"
          type="button"
        >
          <span
            className={`absolute top-1 block size-7 rounded-full transition-all ${showPatois ? "right-1 bg-deep" : "left-1 bg-on-dark-heading"}`}
          />
        </button>
        <div className="flex-1">
          <div className="flex flex-wrap items-center gap-2.5">
            <span className="font-display text-[21px] font-medium text-on-dark-heading">Jamaican Patois greetings</span>
            <span className={`rounded-pill px-2.5 py-1 text-[10.5px] font-bold tracking-[0.06em] ${showPatois ? "bg-yellow/15 text-yellow" : "bg-on-dark-heading/10 text-on-dark-muted"}`}>
              {showPatois ? "ON — DEFAULT" : "OFF"}
            </span>
          </div>
          <div className="mt-1 text-[13.5px] text-on-dark-muted">Show the island personality across your NestyStay experience.</div>
          <div className="mt-2 text-xs text-on-dark-faint">
            When off, greetings switch to plain English — <em className="font-display italic text-yellow">Yuh Gud?</em>{" "}
            becomes &quot;Welcome back, {session.displayName.split(" ")[0]}!&quot;
          </div>
        </div>
      </div>

      {/* Account rows */}
      <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="text-[13px] font-semibold">Account</div>
        <div className="flex flex-col">
          {(
            [
              ["Email & password", "Change your login details", "/auth/forgot-password"],
              ["Payment methods", "Manage saved cards", "/traveler/payment-methods"],
              ["Notification preferences", "Email + push", "/traveler/notifications"],
              ["Identity verification", "Manage verified documents", "/traveler/identity"],
            ] as const
          ).map(([label, sub, href]) => (
            <AppLink
              className="flex min-h-11 items-center justify-between gap-2.5 border-b border-shell py-[13px] last:border-b-0"
              href={href}
              key={label}
            >
              <span>
                <span className="block text-sm font-semibold text-ink">{label}</span>
                <span className="block text-[12.5px] text-sand-500">{sub}</span>
              </span>
              <span className="text-deep-hover">→</span>
            </AppLink>
          ))}
        </div>
      </div>

      {/* Session security */}
      <div className="flex flex-col gap-3 rounded-card border border-sand-border bg-cream p-[22px]">
        <div className="text-[13px] font-semibold">Session security</div>
        <div className="font-mono text-xs text-gray-600">{session.userId}</div>
        <div className="flex flex-wrap items-center gap-2.5 text-[13px]">
          <span className="flex items-center gap-1.5">2FA <StatusChip value="Verified" /></span>
          <span className="flex items-center gap-1.5">Access <StatusChip value="Active" /></span>
          <span>
            Expires <strong>{new Date(session.expiresAt).toLocaleString()}</strong>
          </span>
        </div>
      </div>
    </div>
  );
}

export function AdminPage({ auth }: { auth: AuthController }) {
  const [data, setData] = useState<{
    health?: string;
    modules?: number;
    portals?: number;
    vendors?: number;
    tables?: number;
    jobs?: number;
    rules?: number;
    seedPricebook?: number;
    pricebook?: PhaseTwoPricebookItem[];
    badges?: BadgeDefinition[];
    assignments?: BadgeAssignment[];
    renewals?: BadgeRenewal[];
    campaigns?: Campaign[];
    properties?: PropertyListing[];
    wellness?: WellnessAdminDashboard;
    wellnessOfficers?: WellnessOfficer[];
    errors: string[];
  }>({ errors: [] });
  const [isLoading, setIsLoading] = useState(true);
  const adminToken = auth.session?.accessToken ?? "";
  const canManageOfficers = hasAdminPermission(auth.session, AdminPermissions.officerManagement);
  const canViewFinancials = hasAdminPermission(auth.session, AdminPermissions.financialReporting);
  const canConfigureSystem = hasAdminPermission(auth.session, AdminPermissions.systemConfiguration);
  const [selectedPricebookKey, setSelectedPricebookKey] = useState("");
  const [pricebookAmount, setPricebookAmount] = useState("0");
  const [pricebookActive, setPricebookActive] = useState(true);
  const [subjectType, setSubjectType] = useState("Host");
  const [subjectId, setSubjectId] = useState("");
  const [foundingPropertyId, setFoundingPropertyId] = useState("");
  const [badgeLevel, setBadgeLevel] = useState<BadgeLevel>("Verified");
  const [campaignKey, setCampaignKey] = useState("");
  const [ekycPassed, setEkycPassed] = useState(true);
  const [wellnessActive, setWellnessActive] = useState(false);
  const [propertyAddress, setPropertyAddress] = useState("123 Ocean Avenue");
  const [completedBookings, setCompletedBookings] = useState("3");
  const [campaignForm, setCampaignForm] = useState({
    key: `launch-${Date.now().toString(36)}`,
    name: "Launch Campaign",
    campaignType: "Discount",
    overrideAmount: "49",
    appliesTo: "Verified",
    opensAt: new Date().toISOString().slice(0, 16),
    closesAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().slice(0, 16),
    isActive: true,
  });
  const [foundingTier, setFoundingTier] = useState<FoundingTier>("Silver");
  const [foundingEligible, setFoundingEligible] = useState(true);
  const [transferForm, setTransferForm] = useState({
    previousOwnerVerified: true,
    previousOwnerTrusted: true,
    hasPropertyId: true,
    hasCurrentTaxReceipt: true,
  });
  const [commissionValue, setCommissionValue] = useState("1200");
  const [commissionNights, setCommissionNights] = useState("3");
  const [eligibility, setEligibility] = useState<BadgeEligibility | null>(null);
  const [featureAccess, setFeatureAccess] = useState<BadgeFeatureAccess | null>(null);
  const [foundingBenefit, setFoundingBenefit] = useState<FoundingBenefit | null>(null);
  const [transferEvaluation, setTransferEvaluation] = useState<FoundingTransferEvaluation | null>(null);
  const [commissionQuote, setCommissionQuote] = useState<CommissionQuote | null>(null);
  const [selectedWellnessOfficerId, setSelectedWellnessOfficerId] = useState("");
  const [selectedWellnessVisitId, setSelectedWellnessVisitId] = useState("");
  const [wellnessReportNotes, setWellnessReportNotes] = useState("Admin completion with verified wellness report photo evidence.");
  const [wellnessOfficerSearch, setWellnessOfficerSearch] = useState("");
  const [wellnessOfficerStatus, setWellnessOfficerStatus] = useState("All");
  const [wellnessParishFilter, setWellnessParishFilter] = useState("All");
  const [selectedWellnessOfficerIds, setSelectedWellnessOfficerIds] = useState<string[]>([]);
  const [reviewTemplate, setReviewTemplate] = useState("Approved from admin dashboard.");
  const [adminReportUploads, setAdminReportUploads] = useState<WellnessReportPhotoUploadItem[]>([]);
  const [notice, setNotice] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const adminReportUploadControllers = useRef<Record<string, AbortController>>({});

  async function loadAdminData(cancelled?: () => boolean) {
    setIsLoading(true);
    const results = await Promise.allSettled([
      api.health(),
      api.getPlatformModules(),
      api.getPlatformPortals(),
      api.getPlatformVendors(),
      api.getBackendTables(),
      api.getBackendJobs(),
      api.getBackendRules(),
      api.getBackendSeedPricebook(),
      api.getBadgePricebook(),
      api.getBadgeDefinitions(),
      api.getBadgeAssignments(adminToken),
      api.getBadgeRenewals(adminToken),
      api.getCampaigns(),
      api.getProperties(),
      api.getWellnessAdminDashboard(adminToken),
      api.getWellnessOfficers(adminToken),
    ]);
    if (cancelled?.()) return;
    const errors = results
      .filter((result): result is PromiseRejectedResult => result.status === "rejected")
      .map((result) => (result.reason instanceof Error ? result.reason.message : "Admin API request failed."));
    const pricebook = results[8].status === "fulfilled" ? results[8].value : [];
    const properties = results[13].status === "fulfilled" ? results[13].value : [];
    const wellness = results[14].status === "fulfilled" ? results[14].value : undefined;
    const wellnessOfficers = results[15].status === "fulfilled" ? results[15].value : [];
    setData({
      health: results[0].status === "fulfilled" ? results[0].value.status : undefined,
      modules: results[1].status === "fulfilled" ? results[1].value.length : undefined,
      portals: results[2].status === "fulfilled" ? results[2].value.length : undefined,
      vendors: results[3].status === "fulfilled" ? results[3].value.length : undefined,
      tables: results[4].status === "fulfilled" ? results[4].value.length : undefined,
      jobs: results[5].status === "fulfilled" ? results[5].value.length : undefined,
      rules: results[6].status === "fulfilled" ? results[6].value.length : undefined,
      seedPricebook: results[7].status === "fulfilled" ? results[7].value.length : undefined,
      pricebook,
      badges: results[9].status === "fulfilled" ? results[9].value : [],
      assignments: results[10].status === "fulfilled" ? results[10].value : [],
      renewals: results[11].status === "fulfilled" ? results[11].value : [],
      campaigns: results[12].status === "fulfilled" ? results[12].value : [],
      properties,
      wellness,
      wellnessOfficers,
      errors,
    });

    const nextPricebookItem = pricebook.find((item) => item.key === selectedPricebookKey) ?? pricebook[0];
    if (nextPricebookItem && !selectedPricebookKey) {
      setSelectedPricebookKey(nextPricebookItem.key);
      setPricebookAmount(String(nextPricebookItem.amount));
      setPricebookActive(nextPricebookItem.isActive);
    }
    if (properties[0] && !subjectId) {
      setSubjectId(properties[0].hostUserId);
    }
    if (properties[0] && !foundingPropertyId) {
      setFoundingPropertyId(properties[0].id);
    }
    if (wellnessOfficers[0] && !selectedWellnessOfficerId) {
      setSelectedWellnessOfficerId(wellnessOfficers[0].id);
    }
    if (wellness?.recentVisits[0] && !selectedWellnessVisitId) {
      setSelectedWellnessVisitId(wellness.recentVisits[0].id);
    }
    setIsLoading(false);
  }

  useEffect(() => {
    let cancelled = false;
    void loadAdminData(() => cancelled);
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => () => {
    Object.values(adminReportUploadControllers.current).forEach((controller) => controller.abort());
  }, []);

  const selectedPricebookItem = data.pricebook?.find((item) => item.key === selectedPricebookKey);
  const selectedAssignment = data.assignments?.find((assignment) => assignment.subjectId === subjectId) ?? data.assignments?.[0];
  const selectedCampaignKey = campaignKey || data.campaigns?.[0]?.key || campaignForm.key;
  const selectedWellnessVisit = data.wellness?.recentVisits.find((item) => item.id === selectedWellnessVisitId);
  const selectedWellnessOfficer = data.wellnessOfficers?.find((item) => item.id === selectedWellnessOfficerId);
  const filteredWellnessOfficers = (data.wellnessOfficers ?? []).filter((officer) => {
    const matchesSearch = !wellnessOfficerSearch.trim() || `${officer.badgeNumber} ${officer.parish} ${officer.coverageArea}`.toLowerCase().includes(wellnessOfficerSearch.trim().toLowerCase());
    const matchesStatus = wellnessOfficerStatus === "All" || officer.verificationStatus === wellnessOfficerStatus || officer.onboardingStatus === wellnessOfficerStatus;
    const matchesParish = wellnessParishFilter === "All" || officer.parish === wellnessParishFilter;
    return matchesSearch && matchesStatus && matchesParish;
  });
  const wellnessParishes = Array.from(new Set((data.wellnessOfficers ?? []).map((officer) => officer.parish))).sort();
  const pendingReports = (data.wellness?.recentVisits ?? []).filter((visit) => visit.reportStatus !== "Submitted" && visit.visitStatus !== "Cancelled");
  const wellnessSlaWarnings = (data.wellness?.recentVisits ?? []).filter((visit) => visit.visitStatus === "Requested" && Date.now() - new Date(visit.createdAt).getTime() > 24 * 60 * 60 * 1000);
  const adminReportUploadsForVisit = adminReportUploads.filter((upload) => upload.visitId === selectedWellnessVisitId);
  const uploadedAdminReportPhotoIds = adminReportUploadsForVisit
    .filter((upload) => upload.status === "uploaded" && upload.upload?.scanStatus === "Clean")
    .map((upload) => upload.upload!.id);

  function buildBadgeRequest() {
    return {
      subjectType,
      subjectId,
      level: badgeLevel,
      campaignKey: campaignKey || null,
      hostVerificationPassed: ekycPassed,
      completedApprovedBookings: Number(completedBookings || 0),
      hasPropertyAddress: propertyAddress.trim().length > 0,
      hasWellnessSubscription: wellnessActive,
      paymentSucceeded: true,
    };
  }

  function toDateTimeOffset(value: string) {
    return value.endsWith("Z") ? value : `${value}:00Z`;
  }

  async function runAction(action: () => Promise<string>) {
    setNotice(null);
    setActionError(null);
    try {
      const message = await action();
      setNotice(message);
      await loadAdminData();
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : "Admin action failed.");
    }
  }

  async function runBulkOfficerAction(action: (officerId: string) => Promise<WellnessOfficer>, verb: string) {
    if (!selectedWellnessOfficerIds.length) return;
    if (!window.confirm(`${verb} ${selectedWellnessOfficerIds.length} officer${selectedWellnessOfficerIds.length === 1 ? "" : "s"}?`)) return;
    await runAction(async () => {
      const results = await Promise.all(selectedWellnessOfficerIds.map((id) => action(id)));
      setSelectedWellnessOfficerIds([]);
      return `${results.length} officer${results.length === 1 ? "" : "s"} ${verb.toLowerCase()}. Notification events queued for each change.`;
    });
  }

  function updateAdminReportUpload(id: string, patch: Partial<WellnessReportPhotoUploadItem>) {
    setAdminReportUploads((items) => items.map((item) => item.id === id ? { ...item, ...patch } : item));
  }

  async function uploadAdminReportPhoto(id: string, targetVisitId: string, file: File) {
    if (file.size > maximumWellnessReportPhotoBytes) {
      updateAdminReportUpload(id, { status: "failed", error: "Wellness report photos must be 10 MB or smaller." });
      return;
    }

    const controller = new AbortController();
    adminReportUploadControllers.current[id] = controller;

    try {
      const uploadFile = await compressWellnessReportPhoto(file);
      const contentType = resolveWellnessReportPhotoContentType(uploadFile);
      const officerBadgeNumber = selectedWellnessVisit?.officerBadgeNumber ?? selectedWellnessOfficer?.badgeNumber ?? "ADMIN";
      const prepared = await api.prepareAdminWellnessReportPhotoUpload(targetVisitId, adminToken, {
        officerBadgeNumber,
        fileName: uploadFile.name,
        contentType,
        sizeBytes: uploadFile.size,
      });
      updateAdminReportUpload(id, { upload: prepared, progress: 5, status: "uploading", error: undefined });
      const uploaded = await api.uploadAdminWellnessReportPhotoContent(targetVisitId, prepared.id, adminToken, uploadFile, {
        signal: controller.signal,
        onProgress: (progress) => updateAdminReportUpload(id, { progress, status: "uploading" }),
      });
      updateAdminReportUpload(id, { upload: uploaded, progress: 100, status: "uploaded", error: undefined });
    } catch (caught) {
      updateAdminReportUpload(id, {
        status: controller.signal.aborted ? "cancelled" : "failed",
        error: caught instanceof Error ? caught.message : "Wellness report photo upload failed.",
      });
    } finally {
      delete adminReportUploadControllers.current[id];
    }
  }

  function addAdminReportPhotos(files: FileList | null) {
    if (!files?.length) return;
    const targetVisitId = selectedWellnessVisitId.trim();
    if (!targetVisitId) {
      setActionError("Select a wellness visit before attaching report photos.");
      return;
    }

    setActionError(null);
    Array.from(files).forEach((file) => {
      const id = createLocalUploadId();
      const isTooLarge = file.size > maximumWellnessReportPhotoBytes;
      setAdminReportUploads((items) => [...items, {
        id,
        visitId: targetVisitId,
        file,
        progress: 0,
        status: isTooLarge ? "failed" : "queued",
        error: isTooLarge ? "Wellness report photos must be 10 MB or smaller." : undefined,
      }]);
      if (!isTooLarge) {
        void uploadAdminReportPhoto(id, targetVisitId, file);
      }
    });
  }

  function cancelAdminReportPhotoUpload(id: string) {
    adminReportUploadControllers.current[id]?.abort();
    updateAdminReportUpload(id, { status: "cancelled", error: "Wellness report photo upload cancelled." });
  }

  function retryAdminReportPhotoUpload(item: WellnessReportPhotoUploadItem) {
    updateAdminReportUpload(item.id, { upload: undefined, progress: 0, status: "queued", error: undefined });
    void uploadAdminReportPhoto(item.id, item.visitId, item.file);
  }

  function removeAdminReportPhotoUpload(id: string) {
    adminReportUploadControllers.current[id]?.abort();
    setAdminReportUploads((items) => items.filter((item) => item.id !== id));
  }

  function onPricebookKeyChange(key: string) {
    setSelectedPricebookKey(key);
    const item = data.pricebook?.find((entry) => entry.key === key);
    if (item) {
      setPricebookAmount(String(item.amount));
      setPricebookActive(item.isActive);
    }
  }

  /* ADM-01 (DS v2) — bearer-token auth surface; all metrics and actions run
     against the live admin APIs. */
  return (
    <div className="flex flex-col gap-5 font-sans text-ink" id="ADM-01">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="m-0 font-display text-[clamp(30px,3.4vw,40px)] font-normal tracking-[-0.01em]">
          Platform <em className="italic text-deep-hover">operations</em>
        </h1>
        <div className="flex items-center gap-2 rounded-pill border-[1.5px] border-sand-input bg-cream py-1 pl-[18px] pr-1.5">
          <span className="text-xs font-semibold text-sand-500">Bearer token</span>
          <span className="font-mono text-[13px] tracking-widest">••••••••</span>
          <StatusChip value={adminToken ? "Authenticated" : "Missing token"} />
        </div>
      </div>
      <section className="product-section">
        {isLoading && <LoadingState label="Checking backend admin endpoints" />}
        {data.errors.map((message) => (
          <ErrorState key={message} message={message} />
        ))}
        {actionError && <ErrorState message={actionError} />}
        {notice && <div className="notice-panel">{notice}</div>}
        <div className="metric-grid">
          <MetricCard icon={Gauge} label="API health" value={data.health ?? "unknown"} />
          <MetricCard icon={LayoutDashboard} label="Modules" value={String(data.modules ?? 0)} />
          <MetricCard icon={KeyRound} label="Portals" value={String(data.portals ?? 0)} />
          <MetricCard icon={Sparkles} label="Vendors" value={String(data.vendors ?? 0)} />
          <MetricCard icon={Settings} label="Schema tables" value={String(data.tables ?? 0)} />
          <MetricCard icon={ListChecks} label="Jobs" value={String(data.jobs ?? 0)} />
          <MetricCard icon={ShieldCheck} label="Rules" value={String(data.rules ?? 0)} />
          <MetricCard icon={ReceiptText} label="Seed prices" value={String(data.seedPricebook ?? 0)} />
          <MetricCard icon={BadgeCheck} label="Pricebook" value={String(data.pricebook?.length ?? 0)} />
          <MetricCard icon={Star} label="Campaigns" value={String(data.campaigns?.length ?? 0)} />
          <MetricCard icon={ShieldCheck} label="Wellness visits" value={String(data.wellness?.requestedVisits ?? 0)} />
          <MetricCard icon={CreditCard} label="Wellness payouts" value={String(data.wellness?.pendingPayouts ?? 0)} />
        </div>
        <AppLink className="flex flex-col gap-1.5 rounded-card bg-deep p-[22px] transition-colors hover:bg-deep-hover" href="/admin/officer-id-reset">
          <span className="text-[11px] font-bold tracking-[0.16em] text-on-dark-faint">OFFICER ID RESET</span>
          <span className="font-display text-[22px] font-medium text-on-dark-heading">
            Next: Jan 1 · {String(data.wellnessOfficers?.length || 0)} officers enrolled
          </span>
          <span className="text-[13px] font-bold text-yellow">View schedule →</span>
        </AppLink>
      </section>

      <section className="product-section management-layout wellness-workflow">
        <form className="management-form management-form--wellness">
          <div className="flex flex-wrap items-center justify-between gap-2"><h2 className="section-subtitle">Wellness operations</h2><span className="inline-flex items-center gap-1.5 rounded-pill bg-shell px-3 py-1.5 text-xs font-semibold"><SlidersHorizontal size={14} /> Live API queue</span></div>
          <div className="grid gap-3 sm:grid-cols-3">
            <Field label="Search officers"><Input placeholder="Badge, parish, area" value={wellnessOfficerSearch} onChange={(event) => setWellnessOfficerSearch(event.target.value)} /></Field>
            <Field label="Status"><Select value={wellnessOfficerStatus} onChange={(event) => setWellnessOfficerStatus(event.target.value)}><option>All</option><option>Pending</option><option>Verified</option><option>Rejected</option><option>Suspended</option></Select></Field>
            <Field label="Parish"><Select value={wellnessParishFilter} onChange={(event) => setWellnessParishFilter(event.target.value)}><option>All</option>{wellnessParishes.map((parishName) => <option key={parishName}>{parishName}</option>)}</Select></Field>
          </div>
          <div className="form-grid form-grid--two">
            <Field label="Officer">
              <Select value={selectedWellnessOfficerId} onChange={(event) => setSelectedWellnessOfficerId(event.target.value)}>
                {filteredWellnessOfficers.map((officer) => (
                  <option key={officer.id} value={officer.id}>
                    {officer.badgeNumber} · {officer.verificationStatus}
                  </option>
                ))}
              </Select>
            </Field>
            <Field label="Visit">
              <Select value={selectedWellnessVisitId} onChange={(event) => setSelectedWellnessVisitId(event.target.value)}>
                {(data.wellness?.recentVisits ?? []).map((visit) => (
                  <option key={visit.id} value={visit.id}>
                    {visit.visitType} · {visit.visitStatus}
                  </option>
                ))}
              </Select>
            </Field>
            <Field label="Report notes" className="form-grid__full">
              <Textarea value={wellnessReportNotes} onChange={(event) => setWellnessReportNotes(event.target.value)} />
            </Field>
            <Field label="Review response template" className="form-grid__full"><Select value={reviewTemplate} onChange={(event) => setReviewTemplate(event.target.value)}><option>Approved from admin dashboard.</option><option>Documents verified — approved.</option><option>Additional evidence required before approval.</option><option>Coverage or eligibility did not meet requirements.</option></Select></Field>
          </div>
          <div className="wellness-upload-panel">
            <label className={buttonClassName("outline", "property-photo-picker")}>
              <Paperclip size={16} /> Report photos
              <input accept="image/jpeg,image/png,image/webp" multiple onChange={(event) => { addAdminReportPhotos(event.currentTarget.files); event.currentTarget.value = ""; }} type="file" />
            </label>
            <WellnessReportUploadList
              uploads={adminReportUploadsForVisit}
              onCaptionChange={(id, caption) => updateAdminReportUpload(id, { caption })}
              onCancel={cancelAdminReportPhotoUpload}
              onRemove={removeAdminReportPhotoUpload}
              onRetry={retryAdminReportPhotoUpload}
            />
          </div>
          <div className="button-row">
            <Button
              type="button"
              variant="outline"
              disabled={!selectedWellnessOfficerId || !canManageOfficers}
              onClick={() =>
                void runAction(async () => {
                  const officer = await api.approveWellnessOfficer(selectedWellnessOfficerId, adminToken, reviewTemplate);
                  return `${officer.badgeNumber} approved.`;
                })
              }
            >
              Approve
            </Button>
            <Button
              type="button"
              variant="ghost"
              disabled={!selectedWellnessOfficerId || !canManageOfficers}
              onClick={() =>
                void runAction(async () => {
                  const officer = await api.rejectWellnessOfficer(selectedWellnessOfficerId, adminToken, reviewTemplate);
                  return `${officer.badgeNumber} rejected.`;
                })
              }
            >
              Reject
            </Button>
            <Button
              type="button"
              variant="outline"
              disabled={!selectedWellnessVisitId || !selectedWellnessOfficerId || !canManageOfficers}
              onClick={() =>
                void runAction(async () => {
                  const visit = await api.assignWellnessOfficer(selectedWellnessVisitId, selectedWellnessOfficerId, adminToken);
                  return `Visit ${visit.id.slice(0, 8)} assigned to ${visit.officerBadgeNumber}.`;
                })
              }
            >
              Assign
            </Button>
            <Button variant="dark"
              type="button"
              disabled={!selectedWellnessVisitId || uploadedAdminReportPhotoIds.length === 0 || !canManageOfficers}
              onClick={() =>
                void runAction(async () => {
                  await api.completeWellnessVisit(selectedWellnessVisitId, adminToken, {
                    officerBadgeNumber: selectedWellnessVisit?.officerBadgeNumber ?? selectedWellnessOfficer?.badgeNumber ?? "ADMIN",
                    notes: [wellnessReportNotes, ...adminReportUploadsForVisit.filter((upload) => upload.caption?.trim()).map((upload) => `${upload.file.name}: ${upload.caption!.trim()}`)].join("\nPhoto notes: "),
                    photos: uploadedAdminReportPhotoIds,
                  });
                  return "Visit completed with verified report photo.";
                })
              }
            >
              Complete
            </Button>
            <Button
              type="button"
              variant="ghost"
              disabled={!selectedWellnessVisitId || !canManageOfficers}
              onClick={() =>
                void runAction(async () => {
                  await api.cancelWellnessVisit(selectedWellnessVisitId, adminToken, "Cancelled from admin dashboard.");
                  return "Visit cancelled.";
                })
              }
            >
              Cancel
            </Button>
            <Button variant="dark"
              type="button"
              disabled={!selectedWellnessVisitId || !canViewFinancials}
              onClick={() =>
                void runAction(async () => {
                  const payout = await api.markWellnessPayoutPaid(
                    selectedWellnessVisitId,
                    adminToken,
                    `local-admin-${Date.now()}`,
                    "Paid in local milestone mode.",
                  );
                  return `Payout ${payout.status.toLowerCase()} for ${formatMoney(payout.officerAmount, payout.currency)}.`;
                })
              }
            >
              Pay payout
            </Button>
          </div>
          <div className="notice-panel">
            Pending officers {data.wellness?.pendingOfficers ?? 0} · verified officers {data.wellness?.verifiedOfficers ?? 0} · pending payouts{" "}
            {formatMoney(data.wellness?.pendingPayoutAmount ?? 0)}
          </div>
          <div className="flex flex-wrap gap-2 border-t border-shell pt-3">
            <Button disabled={!canManageOfficers || selectedWellnessOfficerIds.length === 0} onClick={() => void runBulkOfficerAction((id) => api.approveWellnessOfficer(id, adminToken, reviewTemplate), "Approved")} type="button" variant="outline">Bulk approve ({selectedWellnessOfficerIds.length})</Button>
            <Button disabled={!canManageOfficers || selectedWellnessOfficerIds.length === 0} onClick={() => void runBulkOfficerAction((id) => api.rejectWellnessOfficer(id, adminToken, reviewTemplate), "Rejected")} type="button" variant="ghost">Bulk reject</Button>
            <Button disabled={!canManageOfficers || selectedWellnessOfficerIds.length === 0} onClick={() => void runBulkOfficerAction((id) => api.suspendWellnessOfficer(id, adminToken, "Suspended from wellness queue."), "Suspended")} type="button" variant="ghost">Bulk suspend</Button>
          </div>
        </form>

        <div>
          <div className="flex flex-wrap items-center justify-between gap-2"><h2 className="section-subtitle">Wellness queue</h2><span className="text-xs text-sand-500">{filteredWellnessOfficers.length} shown</span></div>
          <div className="mb-3 grid gap-2 sm:grid-cols-3"><Card><div className="text-xs text-sand-500">Open reports</div><strong>{pendingReports.length}</strong><div className="text-xs text-gray-600">Visits awaiting report</div></Card><Card><div className="text-xs text-sand-500">SLA warnings</div><strong>{wellnessSlaWarnings.length}</strong><div className="text-xs text-gray-600">Requested over 24h</div></Card><Card><div className="text-xs text-sand-500">Payout exceptions</div><strong>{(data.wellness?.payouts ?? []).filter((payout) => payout.status !== "Paid").length}</strong><div className="text-xs text-gray-600">Pending / review</div></Card></div>
          <div className="compact-list">
            {filteredWellnessOfficers.slice(0, 12).map((officer) => (
              <Card className="compact-list__item" key={officer.id}>
                <input aria-label={`Select ${officer.badgeNumber}`} checked={selectedWellnessOfficerIds.includes(officer.id)} className="size-4 accent-deep-hover" onChange={() => setSelectedWellnessOfficerIds((current) => current.includes(officer.id) ? current.filter((id) => id !== officer.id) : [...current, officer.id])} type="checkbox" />
                <BadgeCheck size={20} />
                <div>
                  <strong>{officer.badgeNumber}</strong>
                  <span>{officer.parish} · {officer.coverageArea} · {officer.availabilityStatus}</span>
                </div>
                <StatusBadge value={officer.verificationStatus} />
              </Card>
            ))}
            {(data.wellness?.recentVisits ?? []).slice(0, 6).map((visit) => (
              <Card className="compact-list__item" key={visit.id}>
                <CalendarDays size={20} />
                <div>
                  <strong>{visit.visitType}</strong>
                  <span>{visit.officerBadgeNumber ?? "Unassigned"} · {formatMoney(visit.price, visit.currency)}</span>
                </div>
                <StatusBadge value={visit.visitStatus} />
              </Card>
            ))}
          </div>
        </div>
      </section>

      <section className="grid gap-4 lg:grid-cols-2" aria-label="Wellness review and payouts">
        <Card>
          <div className="flex items-center justify-between gap-3"><h2 className="section-subtitle m-0">Side-by-side application review</h2><Eye size={18} className="text-deep-hover" /></div>
          {selectedWellnessOfficer ? <div className="mt-4 grid gap-3 sm:grid-cols-2"><div className="rounded-field border border-sand-border bg-white p-4"><div className="text-xs font-bold uppercase tracking-wide text-sand-500">Applicant</div><div className="mt-2 font-mono font-bold">{selectedWellnessOfficer.badgeNumber}</div><div className="mt-1 text-sm">{selectedWellnessOfficer.parish} · {selectedWellnessOfficer.coverageArea}</div><div className="mt-2 flex flex-wrap gap-1.5"><StatusChip value={selectedWellnessOfficer.verificationStatus} /><StatusChip value={selectedWellnessOfficer.availabilityStatus} /></div></div><div className="rounded-field border border-sand-border bg-shell p-4"><div className="text-xs font-bold uppercase tracking-wide text-sand-500">Review checklist</div><div className="mt-2 flex flex-col gap-2 text-sm"><span className="flex items-center gap-2"><Check size={15} className="text-success-text" /> Off-duty eligibility declared</span><span className="flex items-center gap-2"><Check size={15} className="text-success-text" /> Coverage zone recorded</span><span className="flex items-center gap-2"><Lock size={15} className="text-deep-hover" /> Identity documents role-restricted</span></div></div></div> : <EmptyState title="Select an officer" copy="Choose an officer from the live queue to review their application." />}
          <div className="mt-4 rounded-field bg-amber-tint px-4 py-3 text-[12.5px] text-amber-text"><MessageSquare size={15} className="mr-1 inline" /> Approval/rejection actions queue notification events and persist an audit reason.</div>
        </Card>
        <Card>
          <div className="flex items-center justify-between gap-3"><h2 className="section-subtitle m-0">Commission & payout desk</h2><CreditCard size={18} className="text-deep-hover" /></div>
          <div className="mt-3 grid gap-2 sm:grid-cols-3"><div className="rounded-field bg-shell p-3"><div className="text-xs text-sand-500">Pending</div><strong>{formatMoney(data.wellness?.pendingPayoutAmount ?? 0)}</strong></div><div className="rounded-field bg-shell p-3"><div className="text-xs text-sand-500">Paid</div><strong>{formatMoney((data.wellness?.payouts ?? []).filter((payout) => payout.status === "Paid").reduce((sum, payout) => sum + payout.officerAmount, 0))}</strong></div><div className="rounded-field bg-shell p-3"><div className="text-xs text-sand-500">Schedule</div><strong>On completion</strong></div></div>
          <div className="mt-3 flex flex-col gap-2">{(data.wellness?.payouts ?? []).slice(0, 5).map((payout) => <div className="flex flex-wrap items-center justify-between gap-2 rounded-field border border-sand-border bg-white px-3 py-2 text-sm" key={payout.id}><span className="font-mono">{payout.visitId.slice(0, 8)}</span><span>{formatMoney(payout.officerAmount, payout.currency)}</span><StatusChip value={payout.status} /></div>)}{(data.wellness?.payouts ?? []).length === 0 && <div className="text-sm text-gray-600">No payout records yet.</div>}</div>
          <div className="mt-3 flex items-center gap-2 text-xs text-sand-500"><CircleAlert size={14} /> Bank-account verification and disputes require the configured payment provider.</div>
        </Card>
      </section>

      <section className="product-section management-layout">
        <form
          className="management-form"
          onSubmit={(event) => {
            event.preventDefault();
            void runAction(async () => {
              if (!selectedPricebookKey) throw new Error("Select a pricebook item.");
              const updated = await api.updateBadgePricebookItem(
                selectedPricebookKey,
                { amount: Number(pricebookAmount), isActive: pricebookActive },
                adminToken,
              );
              return `${updated.label} pricebook item saved.`;
            });
          }}
        >
          <h2 className="section-subtitle">Pricebook</h2>
          <div className="form-grid form-grid--two">
            <Field label="Item">
              <Select value={selectedPricebookKey} onChange={(event) => onPricebookKeyChange(event.target.value)}>
                {(data.pricebook ?? []).map((item) => (
                  <option key={item.key} value={item.key}>
                    {item.label}
                  </option>
                ))}
              </Select>
            </Field>
            <Field label="Amount">
              <Input
                min="0"
                step="0.01"
                type="number"
                value={pricebookAmount}
                onChange={(event) => setPricebookAmount(event.target.value)}
              />
            </Field>
          </div>
          <div className="toggle-row">
            <InlineLabel>
              <input
                checked={pricebookActive}
                type="checkbox"
                onChange={(event) => setPricebookActive(event.target.checked)}
              />
              Active
            </InlineLabel>
          </div>
          <Button variant="dark" disabled={!canConfigureSystem} type="submit">
            <ReceiptText size={17} /> Save price
          </Button>
          {selectedPricebookItem && (
            <div className="notice-panel">
              {selectedPricebookItem.key} · {formatMoney(selectedPricebookItem.amount, selectedPricebookItem.currency)} ·{" "}
              {selectedPricebookItem.cadence}
            </div>
          )}
        </form>

        <div>
          <h2 className="section-subtitle">Badge catalog</h2>
          <div className="compact-list">
            {(data.badges ?? []).map((badge) => (
              <Card className="compact-list__item" key={badge.id}>
                <BadgeCheck size={20} />
                <div>
                  <strong>{badge.level}</strong>
                  <span>{badge.appliesTo} · {formatMoney(badge.annualPrice, badge.currency)}</span>
                </div>
                <StatusBadge value={badge.unlocks.length ? `${badge.unlocks.length} unlocks` : "Base"} />
              </Card>
            ))}
          </div>
        </div>
      </section>

      <section className="product-section management-layout">
        <form className="management-form">
          <h2 className="section-subtitle">Badge operations</h2>
          <div className="form-grid form-grid--two">
            <Field label="Subject type">
              <Select value={subjectType} onChange={(event) => setSubjectType(event.target.value)}>
                <option value="Property">Property</option>
                <option value="Host">Host</option>
                <option value="Guest">Guest</option>
              </Select>
            </Field>
            <Field label="Subject id">
              <Input value={subjectId} onChange={(event) => setSubjectId(event.target.value)} />
            </Field>
            <Field label="Badge level">
              <Select value={badgeLevel} onChange={(event) => setBadgeLevel(event.target.value as BadgeLevel)}>
                <option value="Free">Free</option>
                <option value="Verified">Verified</option>
                <option value="Trusted">Trusted</option>
                <option value="Wellness">Wellness</option>
              </Select>
            </Field>
            <Field label="Campaign">
              <Input value={campaignKey} onChange={(event) => setCampaignKey(event.target.value)} />
            </Field>
            <Field label="Property address" className="form-grid__full">
              <Input value={propertyAddress} onChange={(event) => setPropertyAddress(event.target.value)} />
            </Field>
            <Field label="Approved bookings">
              <Input
                min="0"
                type="number"
                value={completedBookings}
                onChange={(event) => setCompletedBookings(event.target.value)}
              />
            </Field>
          </div>
          <div className="toggle-row">
            <InlineLabel>
              <input checked={ekycPassed} type="checkbox" onChange={(event) => setEkycPassed(event.target.checked)} />
              eKYC passed
            </InlineLabel>
            <InlineLabel>
              <input
                checked={wellnessActive}
                type="checkbox"
                onChange={(event) => setWellnessActive(event.target.checked)}
              />
              Wellness active
            </InlineLabel>
          </div>
          <div className="button-row">
            <Button
              type="button"
              variant="outline"
              onClick={() =>
                void runAction(async () => {
                  const result = await api.getBadgeEligibility(buildBadgeRequest(), adminToken);
                  setEligibility(result);
                  return result.eligible ? `${result.level} is eligible.` : `${result.level} is not eligible.`;
                })
              }
            >
              <ShieldCheck size={17} /> Check
            </Button>
            <Button variant="dark"
              type="button"
              onClick={() =>
                void runAction(async () => {
                  const result = await api.purchaseBadge(buildBadgeRequest(), adminToken);
                  return `${result.level} badge purchased.`;
                })
              }
            >
              <CreditCard size={17} /> Purchase
            </Button>
            <Button
              type="button"
              variant="ghost"
              onClick={() =>
                void runAction(async () => {
                  const result = await api.getBadgeFeatureAccess(subjectType, subjectId, adminToken);
                  setFeatureAccess(result);
                  return `${result.activeLevel} feature access loaded.`;
                })
              }
            >
              <ListChecks size={17} /> Features
            </Button>
          </div>
          {eligibility && (
            <div className="notice-panel">
              {eligibility.level}: {eligibility.eligible ? "eligible" : eligibility.missingRequirements.join(", ")}
            </div>
          )}
          {featureAccess && (
            <div className="notice-panel">
              Enabled {featureAccess.unlockedFeatures.length} · Locked {featureAccess.lockedFeatures.length}
            </div>
          )}
        </form>

        <div>
          <h2 className="section-subtitle">Assignments and renewals</h2>
          <div className="compact-list">
            {(data.assignments ?? []).map((assignment) => (
              <Card className="compact-list__item" key={assignment.id}>
                <Star size={20} />
                <div>
                  <strong>{assignment.level}</strong>
                  <span>{assignment.subjectType} · {formatMoney(assignment.amountCharged, assignment.currency)}</span>
                </div>
                <StatusBadge value={assignment.status} />
              </Card>
            ))}
          </div>
          <div className="button-row">
            <Button
              type="button"
              variant="outline"
              disabled={!selectedAssignment || !canConfigureSystem}
              onClick={() =>
                void runAction(async () => {
                  if (!selectedAssignment) throw new Error("No assignment is available.");
                  await api.expireBadgeAssignment(selectedAssignment.id, adminToken);
                  return "Assignment expired.";
                })
              }
            >
              Expire
            </Button>
            <Button
              type="button"
              variant="ghost"
              disabled={!selectedAssignment || !canConfigureSystem}
              onClick={() =>
                void runAction(async () => {
                  if (!selectedAssignment) throw new Error("No assignment is available.");
                  await api.suspendBadgeAssignment(selectedAssignment.id, adminToken);
                  return "Assignment suspended.";
                })
              }
            >
              Suspend
            </Button>
            <Button variant="dark"
              type="button"
              disabled={!selectedAssignment}
              onClick={() =>
                void runAction(async () => {
                  if (!selectedAssignment) throw new Error("No assignment is available.");
                  await api.payBadgeRenewal(selectedAssignment.id, adminToken);
                  return "Renewal paid.";
                })
              }
            >
              Pay renewal
            </Button>
          </div>
        </div>
      </section>

      <section className="product-section management-layout">
        <form
          className="management-form"
          onSubmit={(event) => {
            event.preventDefault();
            void runAction(async () => {
              const campaign = await api.createCampaign(
                {
                  key: campaignForm.key,
                  name: campaignForm.name,
                  campaignType: campaignForm.campaignType,
                  overrideAmount: Number(campaignForm.overrideAmount),
                  appliesTo: campaignForm.appliesTo,
                  opensAt: toDateTimeOffset(campaignForm.opensAt),
                  closesAt: toDateTimeOffset(campaignForm.closesAt),
                  isActive: campaignForm.isActive,
                },
                adminToken,
              );
              setCampaignKey(campaign.key);
              return `${campaign.name} campaign saved.`;
            });
          }}
        >
          <h2 className="section-subtitle">Campaigns</h2>
          <div className="form-grid form-grid--two">
            <Field label="Key">
              <Input
                value={campaignForm.key}
                onChange={(event) => setCampaignForm((current) => ({ ...current, key: event.target.value }))}
              />
            </Field>
            <Field label="Name">
              <Input
                value={campaignForm.name}
                onChange={(event) => setCampaignForm((current) => ({ ...current, name: event.target.value }))}
              />
            </Field>
            <Field label="Type">
              <Input
                value={campaignForm.campaignType}
                onChange={(event) => setCampaignForm((current) => ({ ...current, campaignType: event.target.value }))}
              />
            </Field>
            <Field label="Applies to">
              <Select
                value={campaignForm.appliesTo}
                onChange={(event) => setCampaignForm((current) => ({ ...current, appliesTo: event.target.value }))}
              >
                <option value="Verified">Verified</option>
                <option value="Trusted">Trusted</option>
                <option value="Wellness">Wellness</option>
              </Select>
            </Field>
            <Field label="Override amount">
              <Input
                min="0"
                step="0.01"
                type="number"
                value={campaignForm.overrideAmount}
                onChange={(event) =>
                  setCampaignForm((current) => ({ ...current, overrideAmount: event.target.value }))
                }
              />
            </Field>
            <Field label="Opens">
              <Input
                type="datetime-local"
                value={campaignForm.opensAt}
                onChange={(event) => setCampaignForm((current) => ({ ...current, opensAt: event.target.value }))}
              />
            </Field>
            <Field label="Closes" className="form-grid__full">
              <Input
                type="datetime-local"
                value={campaignForm.closesAt}
                onChange={(event) => setCampaignForm((current) => ({ ...current, closesAt: event.target.value }))}
              />
            </Field>
          </div>
          <div className="toggle-row">
            <InlineLabel>
              <input
                checked={campaignForm.isActive}
                type="checkbox"
                onChange={(event) => setCampaignForm((current) => ({ ...current, isActive: event.target.checked }))}
              />
              Active
            </InlineLabel>
          </div>
          <div className="button-row">
            <Button variant="dark" disabled={!canConfigureSystem} type="submit">
              <Plus size={17} /> Save campaign
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() =>
                void runAction(async () => {
                  const enrollment = await api.enrollCampaign(selectedCampaignKey, subjectType, subjectId, adminToken);
                  return `${enrollment.campaignKey} enrollment saved.`;
                })
              }
            >
              Enroll
            </Button>
          </div>
        </form>

        <div>
          <h2 className="section-subtitle">Live campaigns</h2>
          <div className="compact-list">
            {(data.campaigns ?? []).map((campaign) => (
              <Card className="compact-list__item" key={campaign.id}>
                <Sparkles size={20} />
                <div>
                  <strong>{campaign.name}</strong>
                  <span>{campaign.key} · {formatMoney(campaign.overrideAmount ?? 0)}</span>
                </div>
                <StatusBadge value={campaign.isActive ? "Active" : "Inactive"} />
              </Card>
            ))}
          </div>
        </div>
      </section>

      <section className="product-section management-layout">
        <form className="management-form">
          <h2 className="section-subtitle">Founding benefits</h2>
          <div className="form-grid form-grid--two">
            <Field label="Property id">
              <Input value={foundingPropertyId} onChange={(event) => setFoundingPropertyId(event.target.value)} />
            </Field>
            <Field label="Tier">
              <Select value={foundingTier} onChange={(event) => setFoundingTier(event.target.value as FoundingTier)}>
                <option value="Standard">Standard</option>
                <option value="Silver">Silver</option>
                <option value="Gold">Gold</option>
                <option value="Platinum">Platinum</option>
              </Select>
            </Field>
            <Field label="Booking value">
              <Input
                min="0"
                step="0.01"
                type="number"
                value={commissionValue}
                onChange={(event) => setCommissionValue(event.target.value)}
              />
            </Field>
            <Field label="Nights">
              <Input
                min="1"
                type="number"
                value={commissionNights}
                onChange={(event) => setCommissionNights(event.target.value)}
              />
            </Field>
          </div>
          <div className="toggle-row">
            <InlineLabel>
              <input
                checked={foundingEligible}
                type="checkbox"
                onChange={(event) => setFoundingEligible(event.target.checked)}
              />
              Eligible
            </InlineLabel>
            {Object.entries(transferForm).map(([key, value]) => (
              <InlineLabel key={key}>
                <input
                  checked={value}
                  type="checkbox"
                  onChange={(event) => setTransferForm((current) => ({ ...current, [key]: event.target.checked }))}
                />
                {key.replace(/[A-Z]/g, " $&")}
              </InlineLabel>
            ))}
          </div>
          <div className="button-row">
            <Button variant="dark"
              type="button"
              disabled={!canConfigureSystem}
              onClick={() =>
                void runAction(async () => {
                  const result = await api.upsertFoundingBenefit(
                    { propertyId: foundingPropertyId, tier: foundingTier, isEligible: foundingEligible },
                    adminToken,
                  );
                  setFoundingBenefit(result);
                  return `${result.tier} founding benefit saved.`;
                })
              }
            >
              <Home size={17} /> Save benefit
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() =>
                void runAction(async () => {
                  const result = await api.getFoundingBenefit(foundingPropertyId, adminToken);
                  setFoundingBenefit(result);
                  return `${result.tier} founding benefit loaded.`;
                })
              }
            >
              Load benefit
            </Button>
            <Button
              type="button"
              variant="ghost"
              onClick={() =>
                void runAction(async () => {
                  const result = await api.evaluateFoundingTransfer(transferForm);
                  setTransferEvaluation(result);
                  return result.canTransfer ? "Transfer approved." : "Transfer blocked.";
                })
              }
            >
              Transfer
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() =>
                void runAction(async () => {
                  const result = await api.quoteCommission({
                    bookingValue: Number(commissionValue),
                    nights: Number(commissionNights || 1),
                    tier: foundingTier,
                  });
                  setCommissionQuote(result);
                  return "Commission quote calculated.";
                })
              }
            >
              Quote
            </Button>
          </div>
          {foundingBenefit && (
            <div className="notice-panel">
              {foundingBenefit.tier}: guest {formatMoney(foundingBenefit.guestFlatFee)} · host{" "}
              {foundingBenefit.hostCommissionPercent}%
            </div>
          )}
          {transferEvaluation && (
            <div className="notice-panel">
              {transferEvaluation.canTransfer ? "Transferable" : transferEvaluation.missingRequirements.join(", ")}
            </div>
          )}
          {commissionQuote && (
            <div className="notice-panel">
              Platform total {formatMoney(commissionQuote.nestyStayRevenue)} · guest fee{" "}
              {formatMoney(commissionQuote.guestFeeAmount)}
            </div>
          )}
        </form>

        <div>
          <h2 className="section-subtitle">Seed properties</h2>
          <div className="compact-list">
            {(data.properties ?? []).slice(0, 6).map((property) => (
              <Card className="compact-list__item" key={property.id}>
                <Home size={20} />
                <div>
                  <strong>{property.title}</strong>
                  <span>{property.id}</span>
                </div>
                <StatusBadge value={property.badgeLevel} />
              </Card>
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}
