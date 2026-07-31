import { useEffect, useMemo, useRef, useState } from "react";
import { API_BASE_URL, api, formatMoney, type BookingQuote, type PropertyListing } from "../../lib/api";
import { createLoginSession, createSession, loadSession, saveSession, type AuthSession } from "../../lib/auth";

const FIGMA_ASSET_BASE = "/assets/figma-pngs";
const BOOKING_DRAFT_KEY = "nestyStay.bookingDraft";
const LAST_BOOKING_KEY = "nestyStay.lastBookingId";

const screenAliases: Record<string, string> = {
  "DS-01": "DS-V2",
};

const screenRouteMap: Record<string, string> = {
  "ADM-01": "/admin/ops/disputes",
  "ADM-KPI": "/admin/kpis",
  "ADM-RESET": "/admin/officer-id-reset",
  "ADM-RPT": "/admin/reports",
  "AUTH-01": "/login",
  "AUTH-LOGOUT": "/logout",
  "AUTH-POST": "/auth/post-login-toast",
  "DIR-02": "/directory/trades",
  "DIR-BIZ": "/directory/businesses",
  "DIR-PROV": "/directory/provider",
  "DS-V2": "/screens/DS-V2",
  "ERR-401": "/401",
  "ERR-403": "/403",
  "ERR-404": "/404",
  "ERR-500": "/500",
  "ERR-LOAD": "/screens/ERR-LOAD",
  "ERR-NOFAV": "/empty/favorites",
  "ERR-NORES": "/empty/reservations",
  "HOST-01": "/host-dashboard",
  "HOST-05": "/host/properties",
  "HOST-BADGE": "/host/badges",
  "HOST-EDIT": "/host/properties/edit",
  "HOST-RPT": "/host/reports",
  "HOST-WELL": "/host/wellness",
  INDEX: "/screens",
  "MSG-01": "/messages",
  "MSG-DOC": "/messages/document",
  "OFC-01": "/officer/wellness",
  "OFC-02": "/screens/OFC-02",
  "OFC-BOOK": "/host/wellness/book",
  "OFC-DIR": "/host/wellness/directory",
  "PM-GATE": "/pm/gates",
  "PM-INS": "/pm/insurance",
  "PM-RPT": "/pm/reports",
  "PM-UTIL": "/pm/utilities",
  "PM-VERIFY": "/pm/verification",
  "PUB-01": "/",
  "PUB-02": "/explore",
  "PUB-04": "/screens/PUB-04",
  "PUB-MAP": "/explore/map",
  "PUB-SOON": "/coming-soon",
  "TRAV-01": "/guest-dashboard",
  "TRAV-12": "/profile",
  "TRAV-COL": "/traveler/favorites",
  "TRAV-INV": "/traveler/invoices",
  "TRAV-NOTIF": "/traveler/notifications",
  "TRAV-PEND": "/traveler/reviews/pending",
  "TRAV-SUGG": "/traveler/suggestions",
};

type Hotspot = {
  id: string;
  label: string;
  x: number;
  y: number;
  width: number;
  height: number;
  href?: string;
  action?: () => void;
};

type BookingDraft = {
  propertyId: string;
  checkIn: string;
  checkOut: string;
  adults: number;
  children: number;
  protectionPlan?: string;
};

type FigmaPngScreenProps = {
  screenId: string;
};

function resolveScreenId(screenId: string) {
  const [rawScreenId] = screenId.split("#");
  return screenAliases[rawScreenId] ?? rawScreenId;
}

function navigate(href: string) {
  window.history.pushState({}, "", href);
  window.dispatchEvent(new PopStateEvent("popstate"));
  window.scrollTo({ top: 0, behavior: "auto" });
}

function sessionDestination(session: AuthSession) {
  const roles = session.roles.map((role) => role.toLowerCase());
  if (roles.includes("admin")) return "/admin/ops/disputes";
  if (roles.includes("host")) return "/host-dashboard";
  if (roles.includes("propertymanager")) return "/pm/gates";
  if (roles.includes("officer")) return "/officer/wellness";
  return "/auth/post-login-toast";
}

function currentEntityFromPath(kind: "booking" | "property") {
  const match = window.location.pathname.match(new RegExp(`/${kind}/([^/?#]+)`));
  return match ? decodeURIComponent(match[1]) : "";
}

function slugify(value: string) {
  return value
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

function loadBookingDraft(propertyId: string): BookingDraft {
  try {
    const draft = JSON.parse(window.localStorage.getItem(BOOKING_DRAFT_KEY) || "null") as BookingDraft | null;
    if (draft?.propertyId === propertyId) return draft;
  } catch {
    // Ignore malformed local draft data.
  }

  return {
    propertyId,
    checkIn: "2026-12-12",
    checkOut: "2026-12-18",
    adults: 2,
    children: 0,
    protectionPlan: "InsuraGuest",
  };
}

function saveBookingDraft(draft: BookingDraft) {
  window.localStorage.setItem(BOOKING_DRAFT_KEY, JSON.stringify(draft));
}

function selectedProperty(properties: PropertyListing[]) {
  const pathKey = currentEntityFromPath("property") || currentEntityFromPath("booking");
  const draft = window.localStorage.getItem(BOOKING_DRAFT_KEY);
  let draftPropertyId = "";

  try {
    draftPropertyId = (JSON.parse(draft || "null") as BookingDraft | null)?.propertyId ?? "";
  } catch {
    draftPropertyId = "";
  }

  return (
    properties.find((property) => property.id === pathKey || slugify(property.title) === pathKey) ??
    properties.find((property) => property.id === draftPropertyId) ??
    properties[0]
  );
}

function relativeStyle(hotspot: Hotspot) {
  return {
    left: `${hotspot.x}%`,
    top: `${hotspot.y}%`,
    width: `${hotspot.width}%`,
    height: `${hotspot.height}%`,
  };
}

function addCommonTopNav(hotspots: Hotspot[]) {
  hotspots.push(
    { id: "home-logo", label: "Home", x: 3, y: 2, width: 24, height: 12, href: "/" },
    { id: "top-explore", label: "Explore", x: 60, y: 2, width: 8, height: 11, href: "/explore" },
    { id: "top-host", label: "Host", x: 69, y: 2, width: 7, height: 11, href: "/host-dashboard" },
    { id: "top-wellness", label: "Wellness", x: 76, y: 2, width: 10, height: 11, href: "/host/wellness" },
    { id: "top-sign-in", label: "Sign in", x: 86, y: 2, width: 10.5, height: 11, href: "/login" },
  );
}

function addLandingHotspots(hotspots: Hotspot[], propertyId?: string) {
  hotspots.push(
    { id: "landing-logo", label: "Home", x: 7, y: 0.35, width: 18, height: 1.6, href: "/" },
    { id: "landing-explore-nav", label: "Explore", x: 64, y: 0.35, width: 7, height: 1.6, href: "/explore" },
    { id: "landing-host-nav", label: "Host", x: 71, y: 0.35, width: 6.5, height: 1.6, href: "/host-dashboard" },
    { id: "landing-wellness-nav", label: "Wellness", x: 78, y: 0.35, width: 8.5, height: 1.6, href: "/host/wellness" },
    { id: "landing-explore-button", label: "Explore stays", x: 7.5, y: 16.4, width: 14, height: 1.5, href: "/explore" },
    { id: "landing-host-button", label: "Become a host", x: 22, y: 16.4, width: 13, height: 1.5, href: "/host-dashboard" },
    { id: "landing-featured-card", label: "Featured stay", x: 68.5, y: 8.2, width: 24, height: 7.6, href: propertyId ? `/properties/${propertyId}` : "/explore" },
    { id: "landing-all-stays", label: "All stays", x: 84, y: 31.1, width: 9, height: 1.3, href: "/explore" },
    { id: "landing-mid-card", label: "Mist Ridge Cabin", x: 57, y: 45.1, width: 34, height: 5.1, href: propertyId ? `/properties/${propertyId}` : "/explore" },
    { id: "landing-bottom-explore", label: "Explore stays", x: 54, y: 93.4, width: 15, height: 1.5, href: "/explore" },
  );
}

function addBookingNav(hotspots: Hotspot[], propertyId: string, createBooking: () => void) {
  const routePropertyId = propertyId || "demo";
  hotspots.push(
    { id: "booking-home", label: "Home", x: 3, y: 2, width: 24, height: 12, href: "/" },
    { id: "booking-explore", label: "Explore", x: 73, y: 2, width: 9, height: 11, href: "/explore" },
    {
      id: "booking-back-listing",
      label: "Back to listing",
      x: 82,
      y: 2,
      width: 16,
      height: 11,
      href: propertyId ? `/properties/${propertyId}` : "/explore",
    },
    { id: "booking-dates", label: "Dates", x: 3, y: 17, width: 11.5, height: 9, href: routeForScreen("BOOK-01", routePropertyId) },
    { id: "booking-quote", label: "Quote", x: 15, y: 17, width: 10.5, height: 9, href: routeForScreen("BOOK-02", routePropertyId) },
    { id: "booking-identity", label: "Identity", x: 26, y: 17, width: 12, height: 9, href: routeForScreen("BOOK-03", routePropertyId) },
    { id: "booking-payment", label: "Payment", x: 38, y: 17, width: 13, height: 9, href: routeForScreen("BOOK-05", routePropertyId) },
    { id: "booking-done", label: "Done", x: 51, y: 17, width: 13, height: 9, action: createBooking },
  );
}

function addDirectoryNav(hotspots: Hotspot[]) {
  addWorkspaceSidebar(hotspots, ["/guest-dashboard", "/directory/businesses", "/directory/provider", "/host-dashboard"]);
  hotspots.push(
    { id: "dir-trades-tab", label: "Trades", x: 29, y: 20, width: 10, height: 7, href: "/directory/trades" },
    { id: "dir-business-tab", label: "Local businesses", x: 39, y: 20, width: 15, height: 7, href: "/directory/businesses" },
    { id: "dir-provider-profile", label: "My provider profile", x: 2, y: 31, width: 22, height: 8, href: "/directory/provider" },
  );
}

function addTravelerNav(hotspots: Hotspot[]) {
  hotspots.push(
    { id: "trav-logo", label: "Home", x: 2, y: 3, width: 20, height: 8, href: "/" },
    { id: "trav-my-trips", label: "My trips", x: 1.5, y: 13, width: 22, height: 8, href: "/guest-dashboard" },
    { id: "trav-suggestions", label: "Suggestions", x: 1.5, y: 22, width: 22, height: 8, href: "/traveler/suggestions" },
    { id: "trav-collections", label: "Collections", x: 1.5, y: 31, width: 22, height: 8, href: "/traveler/favorites" },
    { id: "trav-reviews", label: "Pending reviews", x: 1.5, y: 40, width: 22, height: 8, href: "/traveler/reviews/pending" },
    { id: "trav-invoices", label: "Invoices", x: 1.5, y: 50, width: 22, height: 8, href: "/traveler/invoices" },
    { id: "trav-messages", label: "Messages", x: 1.5, y: 59, width: 22, height: 8, href: "/messages" },
    { id: "trav-notifications", label: "Notifications", x: 1.5, y: 68, width: 22, height: 8, href: "/traveler/notifications" },
    { id: "trav-settings", label: "Settings", x: 1.5, y: 77, width: 22, height: 8, href: "/profile" },
  );
}

function routeForScreen(screenId: string, propertyId?: string) {
  if (screenId === "BOOK-01") return `/booking/${propertyId || "demo"}/review`;
  if (screenId === "BOOK-02") return `/booking/${propertyId || "demo"}/quote`;
  if (screenId === "BOOK-03") return `/booking/${propertyId || "demo"}/identity`;
  if (screenId === "BOOK-05") return `/booking/${propertyId || "demo"}/checkout`;
  if (screenId === "BOOK-07") return `/booking/${window.localStorage.getItem(LAST_BOOKING_KEY) || propertyId || "demo"}/pending`;
  if (screenId === "BOOK-CONF") return `/booking/${window.localStorage.getItem(LAST_BOOKING_KEY) || propertyId || "demo"}/success`;
  if (screenId === "PUB-04") return propertyId ? `/properties/${propertyId}` : "/explore";
  return screenRouteMap[screenId] ?? `/screens/${screenId}`;
}

function addWorkspaceSidebar(hotspots: Hotspot[], routes: string[]) {
  hotspots.push({ id: "workspace-logo", label: "Home", x: 2, y: 3, width: 20, height: 8, href: "/" });
  routes.forEach((href, index) => {
    hotspots.push({
      id: `workspace-route-${index}`,
      label: href,
      x: 1.5,
      y: 13 + index * 9,
      width: 22,
      height: 8,
      href,
    });
  });
}

export function FigmaPngScreen({ screenId }: FigmaPngScreenProps) {
  const resolvedScreenId = resolveScreenId(screenId);
  const emailInputRef = useRef<HTMLInputElement>(null);
  const passwordInputRef = useRef<HTMLInputElement>(null);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [status, setStatus] = useState("");
  const [properties, setProperties] = useState<PropertyListing[]>([]);
  const [quote, setQuote] = useState<BookingQuote | null>(null);
  const [bookingDraft, setBookingDraft] = useState<BookingDraft>(() => loadBookingDraft(currentEntityFromPath("booking")));
  const imageSrc = `${FIGMA_ASSET_BASE}/${resolvedScreenId}.png`;

  const selected = useMemo(() => {
    if (!properties.length) return undefined;
    return selectedProperty(properties);
  }, [properties]);

  useEffect(() => {
    setStatus("");
  }, [resolvedScreenId]);

  useEffect(() => {
    let alive = true;

    if (
      ["PUB-01", "PUB-02", "PUB-04", "PUB-MAP", "BOOK-01", "BOOK-02", "BOOK-03", "BOOK-05", "BOOK-07", "BOOK-CONF", "HOST-01", "HOST-05"].includes(
        resolvedScreenId,
      )
    ) {
      api.getProperties().then((loadedProperties) => {
        if (alive) setProperties(loadedProperties);
      }).catch(() => {
        if (alive) setStatus("Properties API unavailable");
      });
    }

    return () => {
      alive = false;
    };
  }, [resolvedScreenId]);

  useEffect(() => {
    if (!selected || !["BOOK-01", "BOOK-02", "BOOK-03", "BOOK-05"].includes(resolvedScreenId)) return;

    const draft = loadBookingDraft(selected.id);
    const nextDraft = { ...draft, propertyId: selected.id };
    setBookingDraft(nextDraft);
    saveBookingDraft(nextDraft);

    api.quoteBooking(nextDraft)
      .then(setQuote)
      .catch(() => setStatus("Quote API unavailable"));
  }, [resolvedScreenId, selected]);

  const submitLogin = async () => {
    const typedEmail = emailInputRef.current?.value.trim() || email.trim();
    const typedPassword = passwordInputRef.current?.value || password;

    if (!typedEmail || !typedPassword) {
      setStatus("Enter email and password");
      return;
    }

    setStatus("Logging in...");
    try {
      const login = await api.login({ email: typedEmail, password: typedPassword });
      let session: AuthSession;

      if (login.requiresTwoFactor) {
        if (!login.challengeId) throw new Error("2FA challenge missing");
        const response = await fetch(`${API_BASE_URL}/auth/development/challenges/${encodeURIComponent(login.challengeId)}`);
        if (!response.ok) throw new Error("2FA required. Use the local dev code screen or live auth page.");
        const challenge = (await response.json()) as { code: string };
        const verified = await api.verifyTwoFactor(login.challengeId, challenge.code);
        session = createSession(verified, login.email, login.email);
      } else {
        session = createLoginSession(login);
      }

      saveSession(session);
      window.dispatchEvent(new Event("storage"));
      navigate(sessionDestination(session));
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Login failed");
    }
  };

  const updateDraft = (patch: Partial<BookingDraft>) => {
    if (!selected) return;
    const nextDraft = { ...bookingDraft, propertyId: selected.id, ...patch };
    setBookingDraft(nextDraft);
    saveBookingDraft(nextDraft);
  };

  const createBooking = async () => {
    if (resolvedScreenId !== "BOOK-05") {
      if (selected) navigate(`/booking/${selected.id}/checkout`);
      return;
    }

    const session = loadSession();
    if (!session) {
      navigate("/401");
      return;
    }

    if (!selected) return;

    setStatus("Creating booking...");
    try {
      const draft = loadBookingDraft(selected.id);
      const booking = await api.createBooking(
        {
          ...draft,
          guestUserId: session.userId,
          billingCountry: "JM",
          termsAccepted: true,
          documentType: "Passport",
          ekycMetaInfo: "Figma PNG payment screen",
        },
        session.accessToken,
      );
      window.localStorage.setItem(LAST_BOOKING_KEY, booking.id);
      navigate(booking.requiresGuestVerification ? `/booking/${booking.id}/pending` : `/booking/${booking.id}/success`);
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Booking failed");
    }
  };

  const hotspots = useMemo(() => {
    const nextHotspots: Hotspot[] = [];
    const propertyId = selected?.id || currentEntityFromPath("property") || currentEntityFromPath("booking");

    if (resolvedScreenId === "INDEX") {
      nextHotspots.push(
        { id: "index-design-system", label: "Design system", x: 7, y: 73, width: 40, height: 8, href: "/screens/DS-V2" },
        { id: "index-spec", label: "Platform spec", x: 7, y: 83, width: 40, height: 8, href: "/screens/INDEX" },
        { id: "index-auth-login", label: "Login auth flow", x: 54, y: 72, width: 38, height: 8, href: "/login" },
        { id: "index-auth-post", label: "Post-login toast", x: 54, y: 83, width: 38, height: 7, href: "/auth/post-login-toast" },
        { id: "index-auth-logout", label: "Logout screen", x: 54, y: 92, width: 38, height: 7, href: "/logout" },
      );
    }

    if (resolvedScreenId === "PUB-01") {
      addLandingHotspots(nextHotspots, propertyId);
    } else if (resolvedScreenId.startsWith("PUB")) {
      addCommonTopNav(nextHotspots);
    }

    if (resolvedScreenId === "PUB-02") {
      nextHotspots.push(
        { id: "map-view", label: "Open map view", x: 82, y: 20, width: 14, height: 8, href: "/explore/map" },
        { id: "search", label: "Search", x: 79, y: 32, width: 12, height: 10, action: () => setStatus(`Showing ${properties.length || 0} live stays`) },
      );

      properties.slice(0, 2).forEach((property, index) => {
        nextHotspots.push({
          id: `property-card-${property.id}`,
          label: property.title,
          x: index === 0 ? 2.5 : 51,
          y: 60,
          width: 46,
          height: 38,
          href: `/properties/${property.id}`,
        });
      });
    }

    if (resolvedScreenId === "PUB-04") {
      nextHotspots.push(
        { id: "property-back", label: "Back to Explore", x: 2, y: 19, width: 16, height: 6, href: "/explore" },
        { id: "property-book-gallery", label: "Book this stay", x: 4, y: 47, width: 64, height: 49, href: selected ? `/booking/${selected.id}/review` : "/explore" },
        { id: "property-book-action", label: "Book this stay", x: 71, y: 82, width: 25, height: 12, href: selected ? `/booking/${selected.id}/review` : "/explore" },
        { id: "property-save", label: "Save", x: 88, y: 28, width: 10, height: 9, action: () => setStatus("Saved locally for client validation") },
      );
    }

    if (resolvedScreenId === "PUB-MAP") {
      nextHotspots.push(
        { id: "map-back-list", label: "Back to list view", x: 2, y: 3, width: 16, height: 8, href: "/explore" },
        { id: "map-card-primary", label: "Open mapped stay", x: 3, y: 20, width: 35, height: 18, href: selected ? `/properties/${selected.id}` : "/explore" },
        { id: "map-pin-primary", label: "Open mapped price pin", x: 62, y: 50, width: 9, height: 7, href: selected ? `/properties/${selected.id}` : "/explore" },
      );
    }

    if (resolvedScreenId === "PUB-SOON") {
      nextHotspots.push(
        { id: "soon-home", label: "Home", x: 40, y: 9, width: 20, height: 9, href: "/" },
        { id: "soon-notify", label: "Notify me", x: 50, y: 77, width: 13, height: 9, action: () => setStatus("Client validation waitlist captured locally") },
      );
    }

    if (resolvedScreenId.startsWith("BOOK-")) {
      addBookingNav(nextHotspots, propertyId, createBooking);
    }

    if (resolvedScreenId.startsWith("DIR")) {
      addDirectoryNav(nextHotspots);
    }

    if (resolvedScreenId.startsWith("TRAV") || resolvedScreenId.startsWith("MSG") || ["ERR-NOFAV", "ERR-NORES", "ERR-LOAD"].includes(resolvedScreenId)) {
      addTravelerNav(nextHotspots);
    }

    if (resolvedScreenId.startsWith("HOST")) {
      addWorkspaceSidebar(nextHotspots, [
        "/host-dashboard",
        "/host/properties",
        "/host/properties/edit",
        "/bookings",
        "/host/wellness",
        "/host/reports",
        "/host/badges",
        "/host/wellness/directory",
      ]);
    }

    if (resolvedScreenId.startsWith("PM")) {
      addWorkspaceSidebar(nextHotspots, ["/pm/gates", "/pm/utilities", "/pm/verification", "/pm/reports", "/pm/insurance"]);
    }

    if (resolvedScreenId.startsWith("OFC")) {
      addWorkspaceSidebar(nextHotspots, ["/officer/wellness", "/officer/wellness", "/host/wellness/directory", "/host/wellness/book"]);
    }

    if (resolvedScreenId.startsWith("ADM")) {
      nextHotspots.push(
        { id: "admin-home", label: "Home", x: 2, y: 3, width: 20, height: 9, href: "/" },
        { id: "admin-ops", label: "Ops dashboard", x: 18, y: 3, width: 13, height: 9, href: "/admin/ops/disputes" },
        { id: "admin-kpis", label: "Analytics", x: 31, y: 3, width: 10, height: 9, href: "/admin/kpis" },
        { id: "admin-reports", label: "Reports", x: 41, y: 3, width: 10, height: 9, href: "/admin/reports" },
        { id: "admin-reset", label: "Officer ID reset", x: 51, y: 3, width: 16, height: 9, href: "/admin/officer-id-reset" },
        { id: "admin-sign-out", label: "Sign out", x: 84, y: 3, width: 10, height: 9, href: "/logout" },
      );
    }

    if (resolvedScreenId.startsWith("ERR")) {
      nextHotspots.push(
        { id: "error-login", label: "Log in", x: 34, y: 54, width: 15, height: 11, href: "/login" },
        { id: "error-browse", label: "Browse", x: 50, y: 54, width: 18, height: 11, href: "/explore" },
      );
    }

    if (resolvedScreenId === "AUTH-01") {
      nextHotspots.push(
        { id: "auth-login-tab", label: "Log in", x: 42, y: 9, width: 11, height: 10, action: submitLogin },
        { id: "auth-create-tab", label: "Create account", x: 53, y: 9, width: 19, height: 10, href: "/register?html=1" },
        { id: "auth-google", label: "Continue with Google", x: 45, y: 38.5, width: 47, height: 9, action: () => setStatus("Google sign-in needs the OAuth client ID configured") },
        { id: "auth-forgot", label: "Forgot password", x: 78, y: 91, width: 15, height: 6, href: "/auth/forgot-password?html=1" },
      );
    }

    if (resolvedScreenId === "AUTH-LOGOUT") {
      nextHotspots.push(
        { id: "logout-home", label: "Home", x: 35, y: 62, width: 15, height: 10, href: "/" },
        { id: "logout-browse", label: "Browse as guest", x: 50, y: 62, width: 18, height: 10, href: "/explore" },
      );
    }

    if (resolvedScreenId === "AUTH-POST") {
      nextHotspots.push({ id: "post-dashboard", label: "Dashboard", x: 35, y: 64, width: 28, height: 10, href: "/guest-dashboard" });
    }

    if (["DS-V2", "OFC-02"].includes(resolvedScreenId)) {
      nextHotspots.push({ id: "standalone-index", label: "Screen index", x: 2, y: 3, width: 20, height: 8, href: "/screens" });
    }

    return nextHotspots;
  }, [properties, resolvedScreenId, selected?.id]);

  const liveLabel = useMemo(() => {
    if (resolvedScreenId === "PUB-02" && properties.length) return `${properties.length} live stays`;
    if (quote) return `Live quote ${formatMoney(quote.totalAmount, quote.currency)}`;
    if (resolvedScreenId === "BOOK-07") return "Booking pending verification";
    if (resolvedScreenId === "BOOK-CONF") return "Booking confirmed";
    if (resolvedScreenId.startsWith("ADM")) return "Admin API linked";
    if (resolvedScreenId.startsWith("TRAV")) return "Traveler API linked";
    if (resolvedScreenId.startsWith("HOST")) return "Host API linked";
    return "";
  }, [properties.length, quote, resolvedScreenId]);

  return (
    <main className="figma-screen-main" role="main" data-screen-id={resolvedScreenId}>
      <div className={`figma-screen-stage figma-screen-stage--${resolvedScreenId.toLowerCase()}`}>
        <img
          className="figma-screen-image"
          src={imageSrc}
          alt={`NestyStay ${resolvedScreenId}`}
          onError={() => setStatus(`Missing Figma PNG for ${resolvedScreenId}`)}
        />

        {resolvedScreenId === "AUTH-01" && (
          <form
            className="figma-auth-overlay"
            onSubmit={(event) => {
              event.preventDefault();
              void submitLogin();
            }}
          >
            <input
              ref={emailInputRef}
              aria-label="Email"
              className="figma-input figma-input--email"
              type="email"
              value={email}
              placeholder="you@example.com"
              onChange={(event) => setEmail(event.target.value)}
            />
            <input
              ref={passwordInputRef}
              aria-label="Password"
              className="figma-input figma-input--password"
              type="password"
              value={password}
              placeholder="Password"
              onChange={(event) => setPassword(event.target.value)}
            />
          </form>
        )}

        {resolvedScreenId === "BOOK-01" && selected && (
          <form className="figma-booking-overlay">
            <input
              aria-label="Check-in"
              className="figma-booking-input figma-booking-input--checkin"
              type="date"
              value={bookingDraft.checkIn}
              onChange={(event) => updateDraft({ checkIn: event.target.value })}
            />
            <input
              aria-label="Check-out"
              className="figma-booking-input figma-booking-input--checkout"
              type="date"
              value={bookingDraft.checkOut}
              onChange={(event) => updateDraft({ checkOut: event.target.value })}
            />
            <select
              aria-label="Guests"
              className="figma-booking-input figma-booking-input--guests"
              value={bookingDraft.adults}
              onChange={(event) => updateDraft({ adults: Number(event.target.value) })}
            >
              <option value={2}>2 guests</option>
              <option value={3}>3 guests</option>
              <option value={4}>4 guests</option>
            </select>
          </form>
        )}

        {hotspots.map((hotspot) => (
          <button
            key={hotspot.id}
            type="button"
            className="figma-hotspot"
            data-hotspot-id={hotspot.id}
            style={relativeStyle(hotspot)}
            aria-label={hotspot.label}
            title={hotspot.label}
            onClick={() => {
              if (hotspot.action) {
                hotspot.action();
                return;
              }

              if (hotspot.href) navigate(hotspot.href);
            }}
          />
        ))}

        {(status || liveLabel) && (
          <div className="figma-live-chip" role="status">
            {status || liveLabel}
          </div>
        )}
      </div>
    </main>
  );
}
