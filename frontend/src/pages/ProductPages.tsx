import { useEffect, useMemo, useRef, useState, type FormEvent, type ReactNode } from "react";
import { motion } from "framer-motion";
import {
  ArrowRight,
  BadgeCheck,
  BedDouble,
  CalendarDays,
  CalendarRange,
  Check,
  CreditCard,
  Gauge,
  Heart,
  Home,
  KeyRound,
  LayoutDashboard,
  ListChecks,
  Lock,
  MapPin,
  Paperclip,
  Plus,
  ReceiptText,
  RotateCcw,
  Search,
  Settings,
  ShieldCheck,
  Sparkles,
  Star,
  TimerReset,
  ToggleLeft,
  UserRound,
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
import { PageHeader } from "../components/ui/PageHeader";
import { useBookings } from "../hooks/useBookings";
import type { AuthController } from "../hooks/useAuth";
import { useProperties, useProperty } from "../hooks/useProperties";
import { getStayImage } from "../lib/stayImages";
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
  type PropertyPhotoUpload,
  type PropertyListing,
  type SocialAuthConfig,
  type WellnessAdminDashboard,
  type WellnessOfficer,
  type WellnessQuote,
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

function statusTone(value: string): "green" | "sun" | "coral" | "ink" | "blue" | "slate" | "mint" {
  const normalized = value.toUpperCase();
  if (
    normalized.includes("APPROVED") ||
    normalized.includes("CAPTURED") ||
    normalized.includes("PASSED") ||
    normalized.includes("COMPLETED") ||
    normalized.includes("SUBMITTED") ||
    normalized.includes("VERIFIED")
  ) {
    return "green";
  }
  if (normalized.includes("REJECTED") || normalized.includes("FAILED") || normalized.includes("CANCELLED")) {
    return "coral";
  }
  if (normalized.includes("PENDING") || normalized.includes("AUTHORIZED") || normalized.includes("REQUESTED")) {
    return "sun";
  }
  if (normalized.includes("SCHEDULED") || normalized.includes("ASSIGNED") || normalized.includes("ACTIVE")) {
    return "blue";
  }
  if (normalized.includes("WELLNESS")) return "mint";
  return "ink";
}

function StatusBadge({ value }: { value: string }) {
  return <Badge tone={statusTone(value)}>{value}</Badge>;
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
  const { properties, isLoading, error, reload } = useProperties();
  const [query, setQuery] = useState("");
  const [badge, setBadge] = useState("all");
  const [bookingProperty, setBookingProperty] = useState<PropertyListing | null>(null);

  const filtered = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    return properties.filter((property) => {
      const matchesQuery =
        !normalizedQuery ||
        [property.title, property.location, property.country, property.hostName]
          .join(" ")
          .toLowerCase()
          .includes(normalizedQuery);
      const matchesBadge = badge === "all" || property.badgeLevel === badge;
      return matchesQuery && matchesBadge;
    });
  }, [badge, properties, query]);

  return (
    <div className="product-page">
      <PageHeader
        eyebrow="Explore stays"
        title="Find your next tropical base."
        copy="Live listings come from the backend properties API and stay in sync with host-created homes."
        actions={
          <AppLink className={buttonClassName("sun")} href="/host/properties">
            <Plus size={17} /> List your property
          </AppLink>
        }
      />

      <AnimatedSection>
      <section className="product-section">
        <div className="search-panel">
          <Field label="Search">
            <Input
              placeholder="Parish, host, or stay name"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
            />
          </Field>
          <Field label="Host badge">
            <Select value={badge} onChange={(event) => setBadge(event.target.value)}>
              <option value="all">All badges</option>
              <option value="Free">Free</option>
              <option value="Verified">Verified</option>
              <option value="Trusted">Trusted</option>
              <option value="Wellness">Wellness</option>
            </Select>
          </Field>
          <Button onClick={reload} variant="dark">
            <Search size={17} /> Refresh
          </Button>
        </div>

        {isLoading && <LoadingState label="Loading properties from the API" />}
        {error && <ErrorState message={error} onRetry={reload} />}
        {!isLoading && !error && filtered.length === 0 && (
          <EmptyState title="No stays match that search." copy="Try a different parish, badge, or host." />
        )}
        <div className="stay-result-grid">
          {filtered.map((property, index) => (
            <ProductCard key={property.id} property={property} index={index} onBook={setBookingProperty} />
          ))}
        </div>
      </section>
      </AnimatedSection>

      <BookingModal
        open={Boolean(bookingProperty)}
        property={bookingProperty}
        session={auth.session}
        onClose={() => setBookingProperty(null)}
      />
    </div>
  );
}

export function PropertyDetailsPage({
  auth,
  propertyId,
}: {
  auth: AuthController;
  propertyId?: string;
}) {
  const { property, isLoading, error } = useProperty(propertyId);
  const [bookingOpen, setBookingOpen] = useState(false);

  return (
    <div className="product-page">
      <PageHeader
        eyebrow="Property details"
        title={property?.title ?? "Stay details"}
        copy={
          property
            ? `${property.location}, ${property.country} hosted by ${property.hostName}.`
            : "Loading a live property record from the API."
        }
        actions={
          property && (
            <Button onClick={() => setBookingOpen(true)}>
              Book this stay <ArrowRight size={17} />
            </Button>
          )
        }
      />

      {property?.country.toLowerCase() === "jamaica" && (
        <section className="product-section product-section--compact">
          <div className="emergency-119-badge">
            <ShieldCheck size={18} />
            Jamaica Emergency: 119
          </div>
        </section>
      )}

      <section className="product-section">
        {isLoading && <LoadingState label="Loading property details" />}
        {error && <ErrorState message={error} />}
        {property && (
          <div className="details-layout">
            <div className="details-visual">
              <MiniPropertyArt index={1} title={property.title} />
            </div>
            <div className="details-copy">
              <div className="details-copy__badges">
                <StatusBadge value={`${property.badgeLevel} host`} />
                <StatusBadge value={property.guestVerificationEnabled ? "eKYC required" : "No eKYC required"} />
                <StatusBadge value={property.insuraGuestEnabled ? "InsuraGuest" : "Standard cover"} />
              </div>
              <h2>{formatMoney(property.nightlyRate, property.currency)} per night</h2>
              <p>
                {property.cancellationPolicy} cancellation. This page is backed by{" "}
                <code>GET /api/properties/{property.id}</code>.
              </p>
              <div className="highlight-list highlight-list--large">
                {property.highlights.map((highlight) => (
                  <span key={highlight}>
                    <Check size={14} /> {highlight}
                  </span>
                ))}
              </div>
              <div className="button-row">
                <Button onClick={() => setBookingOpen(true)}>Open booking flow</Button>
                <AppLink className={buttonClassName("outline")} href="/calendar">
                  Check calendar
                </AppLink>
              </div>
            </div>
          </div>
        )}
      </section>

      <BookingModal
        open={bookingOpen}
        property={property}
        session={auth.session}
        onClose={() => setBookingOpen(false)}
      />
    </div>
  );
}

export function AuthPage({ auth, mode = "login" }: { auth: AuthController; mode?: "login" | "register" }) {
  const [activeMode, setActiveMode] = useState(mode);
  const [email, setEmail] = useState("guest@nestystay.local");
  const [password, setPassword] = useState("NestyStay1");
  const [confirmPassword, setConfirmPassword] = useState("NestyStay1");
  const [displayName, setDisplayName] = useState("Nesty Guest");
  const [phone, setPhone] = useState("");
  const [role, setRole] = useState<"Guest" | "Host">("Guest");
  const [acceptedTerms, setAcceptedTerms] = useState(false);
  const [acceptedPrivacy, setAcceptedPrivacy] = useState(false);
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [socialConfig, setSocialConfig] = useState<SocialAuthConfig | null>(null);
  const googleConfigured = Boolean(import.meta.env.VITE_GOOGLE_CLIENT_ID && socialConfig?.googleEnabled);

  useEffect(() => {
    let cancelled = false;
    api.getSocialAuthConfig()
      .then((config) => {
        if (!cancelled) setSocialConfig(config);
      })
      .catch(() => {
        if (!cancelled) setSocialConfig({ googleEnabled: false, appleEnabled: false, facebookEnabled: false, requiredEnvironmentVariables: [] });
      });

    return () => {
      cancelled = true;
    };
  }, []);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setNotice(null);
    try {
      if (activeMode === "register") {
        await auth.register({
          email,
          password,
          displayName,
          phone,
          confirmPassword,
          acceptedTerms,
          acceptedPrivacy,
          role,
        });
        setCode("");
        setNotice("Registration saved. Enter the verification code from your authenticator app.");
      } else {
        const result = await auth.login(email, password);
        if ("accessToken" in result) {
          window.sessionStorage.setItem("nesty-login-toast", "1");
          navigate("/guest-dashboard");
          return;
        }

        setCode("");
        setNotice("Login challenge opened. Enter the verification code from your authenticator app.");
      }
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Authentication failed.");
    }
  }

  async function handleVerify() {
    setError(null);
    try {
      await auth.verify(code);
      window.sessionStorage.setItem("nesty-login-toast", "1");
      navigate("/guest-dashboard");
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "2FA verification failed.");
    }
  }

  async function handleGoogleSignIn() {
    setError(null);
    setNotice(null);
    try {
      await signInWithGoogle(auth.signInWithGoogle, role);
      window.sessionStorage.setItem("nesty-login-toast", "1");
      navigate("/guest-dashboard");
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Google sign-in failed.");
    }
  }

  return (
    <div className="product-page product-page--auth">
      <section className="auth-shell">
        <aside className="auth-brand-panel">
          <Badge tone="mint">Secure access</Badge>
          <h1>Welcome back to Nesty Stay.</h1>
          <p>Manage trips, listings, wellness visits, and booking approvals from one protected account.</p>
          <div className="auth-proof-grid">
            <span>
              <ShieldCheck size={18} />
              Protected guest sessions
            </span>
            <span>
              <KeyRound size={18} />
              2FA for password sign-in
            </span>
            <span>
              <UserRound size={18} />
              Google account access
            </span>
          </div>
        </aside>

        <div className="auth-card">
          <div className="auth-card__header">
            <div>
              <span className="product-eyebrow">Account</span>
              <h2>{activeMode === "login" ? "Sign in" : "Create your account"}</h2>
              <p>
                {activeMode === "login"
                  ? "Use your email and password, or continue with Google."
                  : "Set up your profile, then confirm access with a short verification step."}
              </p>
            </div>
          </div>

          <Button className="google-auth-button" disabled={auth.isAuthBusy || !googleConfigured} type="button" variant="outline" onClick={handleGoogleSignIn}>
            <span className="google-mark" aria-hidden="true">G</span>
            Continue with Google
          </Button>
          {!googleConfigured && <p className="micro-note">Google sign-in is unavailable until OAuth is configured.</p>}

          <div className="auth-divider"><span>or use email</span></div>

          <form className="auth-form" onSubmit={handleSubmit}>
            <div className="segmented-control auth-mode-toggle">
              <button
                type="button"
                className={activeMode === "login" ? "is-active" : ""}
                onClick={() => setActiveMode("login")}
              >
                Login
              </button>
              <button
                type="button"
                className={activeMode === "register" ? "is-active" : ""}
                onClick={() => setActiveMode("register")}
              >
                Register
              </button>
            </div>

            {activeMode === "register" && (
              <div className="form-grid form-grid--two">
                <Field label="Account type">
                  <Select value={role} onChange={(event) => setRole(event.target.value as "Guest" | "Host")}>
                    <option value="Guest">Traveler</option>
                    <option value="Host">Host</option>
                  </Select>
                </Field>
                <Field label="Display name">
                  <Input value={displayName} onChange={(event) => setDisplayName(event.target.value)} />
                </Field>
                <Field label="Phone">
                  <Input value={phone} onChange={(event) => setPhone(event.target.value)} />
                </Field>
                <Field label="Confirm password">
                  <Input
                    type="password"
                    value={confirmPassword}
                    onChange={(event) => setConfirmPassword(event.target.value)}
                  />
                </Field>
                <label className="inline-check">
                  <input
                    checked={acceptedTerms}
                    onChange={(event) => setAcceptedTerms(event.target.checked)}
                    type="checkbox"
                  />
                  <span>I accept the terms of service.</span>
                </label>
                <label className="inline-check">
                  <input
                    checked={acceptedPrivacy}
                    onChange={(event) => setAcceptedPrivacy(event.target.checked)}
                    type="checkbox"
                  />
                  <span>I accept the privacy policy.</span>
                </label>
              </div>
            )}

            <Field label="Email">
              <Input type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
            </Field>
            <Field label="Password">
              <Input type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
            </Field>
            <Button disabled={auth.isAuthBusy} type="submit">
              <Lock size={17} /> {activeMode === "login" ? "Continue securely" : "Create account"}
            </Button>

            {notice && <div className="notice-panel">{notice}</div>}
            {error && <ErrorState message={error} />}
          </form>

          <div className="auth-challenge-panel">
            <div>
              <Badge tone={auth.pendingChallenge ? "blue" : auth.session ? "green" : "slate"}>
                {auth.pendingChallenge ? "Verification" : auth.session ? "Signed in" : "Next step"}
              </Badge>
            </div>
            {auth.pendingChallenge ? (
              <>
                <h3>Check your verification code</h3>
                <p>{auth.pendingChallenge.email} expires at {new Date(auth.pendingChallenge.expiresAt).toLocaleTimeString()}.</p>
                <Field label="Verification code" hint="Use the code delivered to your configured verification method.">
                  <Input value={code} onChange={(event) => setCode(event.target.value)} />
                </Field>
                <Button disabled={auth.isAuthBusy} onClick={handleVerify}>
                  Verify and enter <ArrowRight size={17} />
                </Button>
              </>
            ) : auth.session ? (
              <>
                <h3>{auth.session.displayName}</h3>
                <p>{auth.session.email}</p>
                <div className="button-row">
                  <AppLink className={buttonClassName("sun")} href="/guest-dashboard">
                    Dashboard
                  </AppLink>
                  <Button
                    onClick={() => {
                      auth.logout();
                      navigate("/logout");
                    }}
                    variant="ghost"
                  >
                    Logout
                  </Button>
                </div>
              </>
            ) : (
              <p>Password sign-in uses 2FA. Social sign-in requires configured OAuth and account role confirmation.</p>
            )}
          </div>
        </div>
      </section>
    </div>
  );
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
    <Card className="metric-card">
      <span>
        <Icon size={21} />
      </span>
      <small>{label}</small>
      <strong>{value}</strong>
    </Card>
  );
}

export function GuestDashboardPage({ auth }: { auth: AuthController }) {
  if (!auth.session) return <RequireAuth auth={auth} title="Guest dashboard needs an active session." />;
  return <GuestDashboardContent auth={auth} />;
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
  return <HostDashboardContent auth={auth} />;
}

function HostDashboardContent({ auth }: { auth: AuthController }) {
  const propertiesState = useProperties();
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
          <AppLink className={buttonClassName("sun")} href="/host/properties">
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
              <AppLink className={buttonClassName("sun")} href="/host/properties">
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

function WellnessVisitList({ visits }: { visits: WellnessVisit[] }) {
  if (visits.length === 0) {
    return <EmptyState title="No wellness visits yet." copy="Requested visits will appear here after the backend saves them." />;
  }

  return (
    <div className="compact-list">
      {visits.map((visit) => (
        <Card className="compact-list__item wellness-visit-item" key={visit.id}>
          <ShieldCheck size={20} />
          <div>
            <strong>{visit.visitType.replace(/([A-Z])/g, " $1").trim()}</strong>
            <span>
              {new Date(visit.scheduledAt).toLocaleString()} · Officer{" "}
              {visit.officerBadgeNumber ?? "not assigned"}
            </span>
          </div>
          <StatusBadge value={`${visit.visitStatus} / ${visit.paymentStatus}`} />
        </Card>
      ))}
    </div>
  );
}

export function HostWellnessPage({ auth }: { auth: AuthController }) {
  if (!auth.session) return <RequireAuth auth={auth} title="Host wellness needs an active host session." />;
  return <HostWellnessContent auth={auth} />;
}

function HostWellnessContent({ auth }: { auth: AuthController }) {
  const { properties, isLoading, error, reload } = useProperties();
  const hostProperties = properties.filter((property) => property.hostUserId === auth.session?.userId);
  const [propertyId, setPropertyId] = useState("");
  const [visitType, setVisitType] = useState("StandardWellnessCheck");
  const [scheduledAt, setScheduledAt] = useState(defaultWellnessDateTime);
  const [parish, setParish] = useState("St. Ann");
  const [area, setArea] = useState("Ocho Rios");
  const [quote, setQuote] = useState<WellnessQuote | null>(null);
  const [visits, setVisits] = useState<WellnessVisit[]>([]);
  const [notice, setNotice] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const selectedProperty = hostProperties.find((property) => property.id === propertyId);

  useEffect(() => {
    if (!propertyId && hostProperties[0]) {
      setPropertyId(hostProperties[0].id);
    }
  }, [hostProperties, propertyId]);

  useEffect(() => {
    void api.getWellnessVisits({ hostUserId: auth.session?.userId }).then(setVisits).catch(() => undefined);
  }, [auth.session?.userId]);

  async function runWellnessAction(action: () => Promise<string>) {
    setActionError(null);
    setNotice(null);
    try {
      const message = await action();
      setNotice(message);
      const refreshed = await api.getWellnessVisits({ hostUserId: auth.session?.userId });
      setVisits(refreshed);
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : "Wellness action failed.");
    }
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

  return (
    <div className="product-page product-page--wellness">
      <PageHeader
        eyebrow="Host wellness"
        title="Schedule platform-managed wellness visits."
        copy="Eligible hosts can request officer wellness visits, track visit status, and keep officer contact mediated inside the platform."
        actions={
          <div className="button-row">
            <AppLink className={buttonClassName("outline")} href="/host/wellness/directory">
              Directory <ArrowRight size={17} />
            </AppLink>
            <AppLink className={buttonClassName("outline")} href="/host/wellness/book">
              Book visit <ArrowRight size={17} />
            </AppLink>
            <AppLink className={buttonClassName("ghost")} href="/officer/wellness">
              Officer view <ArrowRight size={17} />
            </AppLink>
          </div>
        }
      />

      <section className="product-section wellness-command-strip">
        <div className="metric-grid">
          <MetricCard icon={ShieldCheck} label="Visits" value={String(visits.length)} />
          <MetricCard icon={CalendarDays} label="Scheduled" value={String(visits.filter((visit) => visit.visitStatus === "Scheduled").length)} />
          <MetricCard icon={ReceiptText} label="Reports" value={String(visits.filter((visit) => visit.reportStatus === "Submitted").length)} />
          <MetricCard icon={CreditCard} label="Emergency" value="119" />
        </div>
      </section>

      <section className="product-section management-layout wellness-workflow">
        <form
          className="management-form"
          onSubmit={(event) => {
            event.preventDefault();
            void runWellnessAction(async () => {
              const result = await api.quoteWellnessVisit(buildRequest());
              setQuote(result);
              return result.eligible ? "Wellness quote is eligible." : "Wellness is locked for this property.";
            });
          }}
        >
          <h2 className="section-subtitle">Request a certified visit</h2>
          <div className="form-grid form-grid--two">
            <Field label="Property">
              <Select value={propertyId} onChange={(event) => setPropertyId(event.target.value)}>
                {hostProperties.map((property) => (
                  <option key={property.id} value={property.id}>
                    {property.title}
                  </option>
                ))}
              </Select>
            </Field>
            <Field label="Visit type">
              <Select value={visitType} onChange={(event) => setVisitType(event.target.value)}>
                <option value="StandardWellnessCheck">Standard wellness check</option>
                <option value="InPersonGuestIdCheck">In-person guest ID check</option>
                <option value="DriveByPatrol">Drive-by patrol</option>
              </Select>
            </Field>
            <Field label="Scheduled time">
              <Input type="datetime-local" value={scheduledAt} onChange={(event) => setScheduledAt(event.target.value)} />
            </Field>
            <Field label="Parish">
              <Input value={parish} onChange={(event) => setParish(event.target.value)} />
            </Field>
            <Field label="Area" className="form-grid__full">
              <Input value={area} onChange={(event) => setArea(event.target.value)} />
            </Field>
          </div>
          <div className="button-row">
            <Button type="submit" variant="outline">
              <ReceiptText size={17} /> Quote
            </Button>
            <Button
              type="button"
              onClick={() =>
                void runWellnessAction(async () => {
                  const created = await api.createWellnessVisit(buildRequest());
                  setQuote(null);
                  return `${created.visitType} requested. Payment is ${created.paymentStatus}.`;
                })
              }
            >
              <ShieldCheck size={17} /> Request visit
            </Button>
          </div>
          {quote && (
            <div className="notice-panel">
              {quote.eligible
                ? `${formatMoney(quote.price, quote.currency)} visit · ${formatMoney(quote.officerPayoutAmount, quote.currency)} officer payout · call ${quote.emergencyNumber} for emergencies`
                : quote.missingRequirements.join(" ")}
            </div>
          )}
          {selectedProperty && selectedProperty.badgeLevel !== "Wellness" && (
            <div className="notice-panel">
              <Lock size={15} /> Wellness visits require a Wellness badge or unlocked Wellness visits feature.
            </div>
          )}
          {notice && <div className="notice-panel">{notice}</div>}
          {actionError && <ErrorState message={actionError} />}
        </form>

        <div>
          <h2 className="section-subtitle">Visit status</h2>
          {isLoading && <LoadingState />}
          {error && <ErrorState message={error} onRetry={reload} />}
          <WellnessVisitList visits={visits} />
        </div>
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
};

const maximumWellnessReportPhotoBytes = 10 * 1024 * 1024;

export function OfficerWellnessPage() {
  const [badgeNumber, setBadgeNumber] = useState("NST-OFC-2026");
  const [parish, setParish] = useState("St. Ann");
  const [coverageArea, setCoverageArea] = useState("Ocho Rios");
  const [isActiveOffDuty, setIsActiveOffDuty] = useState(true);
  const [isRetired, setIsRetired] = useState(false);
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

  useEffect(() => {
    void api.getWellnessVisits().then(setVisits).catch(() => undefined);
  }, []);

  useEffect(() => () => {
    Object.values(reportUploadControllers.current).forEach((controller) => controller.abort());
  }, []);

  async function runOfficerAction(action: () => Promise<string>) {
    setActionError(null);
    setNotice(null);
    try {
      const message = await action();
      setNotice(message);
      const refreshed = await api.getWellnessVisits();
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
      const contentType = resolveWellnessReportPhotoContentType(file);
      const prepared = await api.prepareWellnessReportPhotoUpload(targetVisitId, {
        officerBadgeNumber: badgeNumber,
        fileName: file.name,
        contentType,
        sizeBytes: file.size,
      });
      updateReportUpload(id, { upload: prepared, progress: 5, status: "uploading", error: undefined });
      const uploaded = await api.uploadWellnessReportPhotoContent(targetVisitId, prepared.id, badgeNumber, file, {
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

  return (
    <div className="product-page product-page--wellness">
      <PageHeader
        eyebrow="Officer wellness"
        title="Manage anonymous officer wellness work."
        copy="Officer-facing flows use badge IDs, visit IDs, report notes, and payout status. Names are never shown to any user."
      />

      <section className="product-section management-layout wellness-workflow">
        <form
          className="management-form"
          onSubmit={(event) => {
            event.preventDefault();
            void runOfficerAction(async () => {
              const result = await api.onboardWellnessOfficer({
                badgeNumber,
                parish,
                coverageArea,
                isActiveOffDuty,
                isRetired,
              });
              setOfficer(result);
              return `Officer ${result.badgeNumber} onboarding is ${result.onboardingStatus}.`;
            });
          }}
        >
          <h2 className="section-subtitle">Officer onboarding</h2>
          <div className="warning-banner warning-banner--compact">
            <Lock size={17} /> Officer display: badge ID only - NST-OFC-XXXX format. Name is never shown to any user.
          </div>
          <div className="form-grid form-grid--two">
            <Field label="Badge ID">
              <Input value={badgeNumber} onChange={(event) => setBadgeNumber(event.target.value)} />
            </Field>
            <Field label="Parish">
              <Input value={parish} onChange={(event) => setParish(event.target.value)} />
            </Field>
            <Field label="Coverage area" className="form-grid__full">
              <Input value={coverageArea} onChange={(event) => setCoverageArea(event.target.value)} />
            </Field>
          </div>
          <div className="toggle-row">
            <InlineLabel>
              <input checked={isActiveOffDuty} type="checkbox" onChange={(event) => setIsActiveOffDuty(event.target.checked)} />
              Active off-duty JCF
            </InlineLabel>
            <InlineLabel>
              <input checked={isRetired} type="checkbox" onChange={(event) => setIsRetired(event.target.checked)} />
              Retired
            </InlineLabel>
          </div>
          <Button type="submit">
            <BadgeCheck size={17} /> Submit onboarding
          </Button>
          {officer && (
            <div className="notice-panel">
              {officer.badgeNumber} · {officer.verificationStatus} · free badges {officer.freeBadges.join(", ") || "pending"}
            </div>
          )}
          <div className="notice-panel">
            Your NestyStay ID resets every January 1. Your privacy is protected by platform policy.
          </div>
        </form>

        <form className="management-form">
          <h2 className="section-subtitle">Evidence report</h2>
          <div className="form-grid form-grid--two">
            <Field label="Visit id">
              <Input value={visitId} onChange={(event) => setVisitId(event.target.value)} />
            </Field>
            <Field label="Officer badge">
              <Input value={badgeNumber} onChange={(event) => setBadgeNumber(event.target.value)} />
            </Field>
            <Field label="Notes" className="form-grid__full">
              <Textarea value={notes} onChange={(event) => setNotes(event.target.value)} />
            </Field>
          </div>
          <div className="wellness-upload-panel">
            <label className={buttonClassName("outline", "property-photo-picker")}>
              <Paperclip size={16} /> Report photos
              <input accept="image/jpeg,image/png,image/webp" multiple onChange={(event) => { addOfficerReportPhotos(event.currentTarget.files); event.currentTarget.value = ""; }} type="file" />
            </label>
            <WellnessReportUploadList
              uploads={reportUploads.filter((upload) => upload.visitId === visitId.trim())}
              onCancel={cancelReportPhotoUpload}
              onRemove={removeReportPhotoUpload}
              onRetry={retryReportPhotoUpload}
            />
          </div>
          <Button
            type="button"
            disabled={uploadedReportPhotoIds.length === 0}
            onClick={() =>
              void runOfficerAction(async () => {
                const result = await api.submitWellnessReport(visitId, {
                  officerBadgeNumber: badgeNumber,
                  notes,
                  photos: uploadedReportPhotoIds,
                });
                return `Report submitted. Visit is ${result.visitStatus}; payout is ${result.paymentStatus}.`;
              })
            }
          >
            <ReceiptText size={17} /> Submit report
          </Button>
          {notice && <div className="notice-panel">{notice}</div>}
          {actionError && <ErrorState message={actionError} />}
        </form>
      </section>

      <section className="product-section wellness-workflow">
        <h2 className="section-subtitle">Assigned visits</h2>
        <WellnessVisitList visits={assignedVisits} />
      </section>
    </div>
  );
}

function WellnessReportUploadList({
  uploads,
  onCancel,
  onRemove,
  onRetry,
}: {
  uploads: WellnessReportPhotoUploadItem[];
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
          <span>{upload.file.name}</span>
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
  const { properties, isLoading, error, reload } = useProperties();
  const [form, setForm] = useState({
    title: "New Tropical Studio",
    location: "Port Antonio, Portland",
    country: "Jamaica",
    nightlyRate: "155",
    currency: "USD",
    badgeLevel: "Verified",
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

  return (
    <div className="product-page">
      <PageHeader
        eyebrow="Property management"
        title="Create and manage stays."
        copy="New listings are posted to the backend and persisted by the PostgreSQL-backed store."
      />

      <section className="product-section management-layout">
        <form className="management-form" onSubmit={handleCreate}>
          <div className="form-grid form-grid--two">
            <Field label="Title">
              <Input value={form.title} onChange={(event) => update("title", event.target.value)} />
            </Field>
            <Field label="Location">
              <Input value={form.location} onChange={(event) => update("location", event.target.value)} />
            </Field>
            <Field label="Country">
              <Input value={form.country} onChange={(event) => update("country", event.target.value)} />
            </Field>
            <Field label="Nightly rate">
              <Input
                min="1"
                type="number"
                value={form.nightlyRate}
                onChange={(event) => update("nightlyRate", event.target.value)}
              />
            </Field>
            <Field label="Currency">
              <Input value={form.currency} onChange={(event) => update("currency", event.target.value.toUpperCase())} />
            </Field>
            <Field label="Badge">
              <Select value={form.badgeLevel} onChange={(event) => update("badgeLevel", event.target.value)}>
                <option value="Free">Free</option>
                <option value="Verified">Verified</option>
                <option value="Trusted">Trusted</option>
                <option value="Wellness">Wellness</option>
              </Select>
            </Field>
            <Field label="Cancellation">
              <Select
                value={form.cancellationPolicy}
                onChange={(event) => update("cancellationPolicy", event.target.value)}
              >
                <option value="Flexible">Flexible</option>
                <option value="Moderate">Moderate</option>
                <option value="Strict">Strict</option>
              </Select>
            </Field>
            <Field label="Highlights" className="form-grid__full">
              <Textarea value={form.highlights} onChange={(event) => update("highlights", event.target.value)} />
            </Field>
          </div>
          <div className="verification-toggle-card form-grid__full">
            <div>
              <strong>Enable guest identity verification for this property</strong>
              <p>NEVER AUTOMATIC - host enables per property.</p>
            </div>
            <InlineLabel>
              <input
                checked={form.guestVerificationEnabled}
                type="checkbox"
                onChange={(event) => update("guestVerificationEnabled", event.target.checked)}
              />
              Guest verification {form.guestVerificationEnabled ? "enabled" : "off"}
            </InlineLabel>
            <div className="verification-pricing">
              <span>$0.14 per booking</span>
              <span>$1.26 / 10-pack</span>
              <span>$2.99 / month</span>
              <span>$29.99 / year</span>
            </div>
          </div>
          <div className="toggle-row">
            <InlineLabel>
              <input
                checked={form.insuraGuestEnabled}
                type="checkbox"
                onChange={(event) => update("insuraGuestEnabled", event.target.checked)}
              />
              InsuraGuest
            </InlineLabel>
          </div>
          <Button type="submit">
            <Plus size={17} /> Save property
          </Button>
          {created && <div className="notice-panel">{created.title} is live in the property API.</div>}
          {submitError && <ErrorState message={submitError} />}
        </form>

        <div>
          <h2 className="section-subtitle">Your live listings</h2>
          {isLoading && <LoadingState />}
          {error && <ErrorState message={error} onRetry={reload} />}
          {!isLoading && hostProperties.length === 0 && (
            <EmptyState title="No host listings yet." copy="Your saved properties will appear here." />
          )}
          <div className="compact-list">
            {hostProperties.map((property) => {
              const uploadsForProperty = photoUploads.filter((upload) => upload.propertyId === property.id);
              return (
                <Card className="compact-list__item property-upload-card" key={property.id}>
                  <Home size={20} />
                  <div>
                    <strong>{property.title}</strong>
                    <span>{property.location}</span>
                  </div>
                  <StatusBadge value={property.badgeLevel} />
                  <div className="property-upload-actions">
                    <label className={buttonClassName("outline", "property-photo-picker")}>
                      <Paperclip size={16} /> Photos
                      <input accept="image/jpeg,image/png,image/webp" multiple onChange={(event) => { addPropertyPhotos(property.id, event.currentTarget.files); event.currentTarget.value = ""; }} type="file" />
                    </label>
                  </div>
                  {uploadsForProperty.length > 0 && (
                    <div className="property-upload-list">
                      {uploadsForProperty.map((upload) => (
                        <div className="property-upload-item" key={upload.id}>
                          <span>{upload.file.name}</span>
                          <small>{upload.status === "uploading" ? `${upload.progress}%` : upload.error ?? upload.upload?.scanStatus ?? upload.status}</small>
                          <div className="property-upload-progress"><span style={{ width: `${upload.status === "uploaded" ? 100 : upload.progress}%` }} /></div>
                          {(upload.status === "uploading" || upload.status === "queued") && <Button onClick={() => cancelPropertyPhotoUpload(upload.id)} title="Cancel upload" variant="ghost"><X size={15} /></Button>}
                          {(upload.status === "failed" || upload.status === "cancelled") && <Button onClick={() => retryPropertyPhotoUpload(upload)} title="Retry upload" variant="ghost"><RotateCcw size={15} /></Button>}
                          {upload.status !== "uploading" && <Button onClick={() => removePropertyPhotoUpload(upload.id)} title="Remove photo" variant="ghost"><X size={15} /></Button>}
                        </div>
                      ))}
                    </div>
                  )}
                </Card>
              );
            })}
          </div>
        </div>
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

export function ProfileSettingsPage({ auth }: { auth: AuthController }) {
  const [patoisEnabled, setPatoisEnabled] = useState(true);
  if (!auth.session) return <RequireAuth auth={auth} title="Profile settings need an active session." />;

  return (
    <div className="product-page">
      <PageHeader
        eyebrow="Profile"
        title="Your Nesty Stay profile."
        copy="Session identity, roles, and 2FA-backed access are synced from the milestone auth flow."
      />
      <section className="product-section settings-grid">
        <Card className="settings-card">
          <UserRound size={24} />
          <h2>{auth.session.displayName}</h2>
          <p>{auth.session.email}</p>
          <div className="highlight-list">
            {auth.session.roles.map((role) => (
              <span key={role}>{role}</span>
            ))}
          </div>
          <Button
            onClick={() => {
              auth.logout();
              navigate("/logout");
            }}
            variant="outline"
          >
            Logout
          </Button>
        </Card>
        <Card className="settings-card settings-card--toggle">
          <ToggleLeft size={24} />
          <h2>Jamaican Patois greetings</h2>
          <p>Show the island personality across your NestyStay experience.</p>
          <InlineLabel className="switch-label">
            <input checked={patoisEnabled} type="checkbox" onChange={(event) => setPatoisEnabled(event.target.checked)} />
            <span className="switch-track" aria-hidden="true" />
            <span>{patoisEnabled ? "ON" : "OFF"}</span>
          </InlineLabel>
        </Card>
        <Card className="settings-card">
          <ShieldCheck size={24} />
          <h2>Session security</h2>
          <p>{auth.session.userId}</p>
          <div className="status-grid">
            <span>2FA <StatusBadge value="Verified" /></span>
            <span>Access <StatusBadge value="Active" /></span>
            <span>Expires <strong>{new Date(auth.session.expiresAt).toLocaleString()}</strong></span>
          </div>
        </Card>
      </section>
    </div>
  );
}

export function AdminPage() {
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
  const [adminToken, setAdminToken] = useState("");
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
      api.getBadgeAssignments(),
      api.getBadgeRenewals(),
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
      const contentType = resolveWellnessReportPhotoContentType(file);
      const officerBadgeNumber = selectedWellnessVisit?.officerBadgeNumber ?? selectedWellnessOfficer?.badgeNumber ?? "ADMIN";
      const prepared = await api.prepareAdminWellnessReportPhotoUpload(targetVisitId, adminToken, {
        officerBadgeNumber,
        fileName: file.name,
        contentType,
        sizeBytes: file.size,
      });
      updateAdminReportUpload(id, { upload: prepared, progress: 5, status: "uploading", error: undefined });
      const uploaded = await api.uploadAdminWellnessReportPhotoContent(targetVisitId, prepared.id, adminToken, file, {
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

  return (
    <div className="product-page product-page--admin">
      <PageHeader
        eyebrow="Admin"
        title="Platform health, badges, pricing, and benefits."
        copy="A compact control surface for platform metadata, pricing, benefits, and wellness operations."
      />
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
        <Card className="admin-reset-card">
          <TimerReset size={22} />
          <div>
            <strong>Officer ID Reset - Next: Jan 1</strong>
            <span>{String(data.wellnessOfficers?.length || 1240)} officers enrolled. No Override and Zero Trace rules apply.</span>
          </div>
          <AppLink className={buttonClassName("outline")} href="/admin/officer-id-reset">
            View schedule
          </AppLink>
        </Card>
      </section>

      <section className="product-section management-layout wellness-workflow">
        <form className="management-form management-form--wellness">
          <h2 className="section-subtitle">Wellness operations</h2>
          <div className="form-grid form-grid--two">
            <Field label="Admin token" className="form-grid__full">
              <Input value={adminToken} onChange={(event) => setAdminToken(event.target.value)} />
            </Field>
            <Field label="Officer">
              <Select value={selectedWellnessOfficerId} onChange={(event) => setSelectedWellnessOfficerId(event.target.value)}>
                {(data.wellnessOfficers ?? []).map((officer) => (
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
          </div>
          <div className="wellness-upload-panel">
            <label className={buttonClassName("outline", "property-photo-picker")}>
              <Paperclip size={16} /> Report photos
              <input accept="image/jpeg,image/png,image/webp" multiple onChange={(event) => { addAdminReportPhotos(event.currentTarget.files); event.currentTarget.value = ""; }} type="file" />
            </label>
            <WellnessReportUploadList
              uploads={adminReportUploadsForVisit}
              onCancel={cancelAdminReportPhotoUpload}
              onRemove={removeAdminReportPhotoUpload}
              onRetry={retryAdminReportPhotoUpload}
            />
          </div>
          <div className="button-row">
            <Button
              type="button"
              variant="outline"
              disabled={!selectedWellnessOfficerId}
              onClick={() =>
                void runAction(async () => {
                  const officer = await api.approveWellnessOfficer(selectedWellnessOfficerId, adminToken, "Approved from admin dashboard.");
                  return `${officer.badgeNumber} approved.`;
                })
              }
            >
              Approve
            </Button>
            <Button
              type="button"
              variant="ghost"
              disabled={!selectedWellnessOfficerId}
              onClick={() =>
                void runAction(async () => {
                  const officer = await api.rejectWellnessOfficer(selectedWellnessOfficerId, adminToken, "Rejected from admin dashboard.");
                  return `${officer.badgeNumber} rejected.`;
                })
              }
            >
              Reject
            </Button>
            <Button
              type="button"
              variant="outline"
              disabled={!selectedWellnessVisitId || !selectedWellnessOfficerId}
              onClick={() =>
                void runAction(async () => {
                  const visit = await api.assignWellnessOfficer(selectedWellnessVisitId, selectedWellnessOfficerId, adminToken);
                  return `Visit ${visit.id.slice(0, 8)} assigned to ${visit.officerBadgeNumber}.`;
                })
              }
            >
              Assign
            </Button>
            <Button
              type="button"
              disabled={!selectedWellnessVisitId || uploadedAdminReportPhotoIds.length === 0}
              onClick={() =>
                void runAction(async () => {
                  await api.completeWellnessVisit(selectedWellnessVisitId, adminToken, {
                    officerBadgeNumber: selectedWellnessVisit?.officerBadgeNumber ?? selectedWellnessOfficer?.badgeNumber ?? "ADMIN",
                    notes: wellnessReportNotes,
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
              disabled={!selectedWellnessVisitId}
              onClick={() =>
                void runAction(async () => {
                  await api.cancelWellnessVisit(selectedWellnessVisitId, adminToken, "Cancelled from admin dashboard.");
                  return "Visit cancelled.";
                })
              }
            >
              Cancel
            </Button>
            <Button
              type="button"
              disabled={!selectedWellnessVisitId}
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
        </form>

        <div>
          <h2 className="section-subtitle">Wellness queue</h2>
          <div className="compact-list">
            {(data.wellnessOfficers ?? []).slice(0, 8).map((officer) => (
              <Card className="compact-list__item" key={officer.id}>
                <BadgeCheck size={20} />
                <div>
                  <strong>{officer.badgeNumber}</strong>
                  <span>{officer.parish} · {officer.coverageArea}</span>
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
            <Field label="Admin token" className="form-grid__full">
              <Input value={adminToken} onChange={(event) => setAdminToken(event.target.value)} />
            </Field>
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
          <Button type="submit">
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
                  const result = await api.getBadgeEligibility(buildBadgeRequest());
                  setEligibility(result);
                  return result.eligible ? `${result.level} is eligible.` : `${result.level} is not eligible.`;
                })
              }
            >
              <ShieldCheck size={17} /> Check
            </Button>
            <Button
              type="button"
              onClick={() =>
                void runAction(async () => {
                  const result = await api.purchaseBadge(buildBadgeRequest());
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
                  const result = await api.getBadgeFeatureAccess(subjectType, subjectId);
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
              disabled={!selectedAssignment}
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
              disabled={!selectedAssignment}
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
            <Button
              type="button"
              disabled={!selectedAssignment}
              onClick={() =>
                void runAction(async () => {
                  if (!selectedAssignment) throw new Error("No assignment is available.");
                  await api.payBadgeRenewal(selectedAssignment.id);
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
            <Button type="submit">
              <Plus size={17} /> Save campaign
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() =>
                void runAction(async () => {
                  const enrollment = await api.enrollCampaign(selectedCampaignKey, subjectType, subjectId);
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
            <Button
              type="button"
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
                  const result = await api.getFoundingBenefit(foundingPropertyId);
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
