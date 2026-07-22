import { useEffect, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "framer-motion";
import { ArrowUpRight, Menu, UserRound, X } from "lucide-react";
import { AppLink } from "./components/AppLink";
import Hero3D from "./components/landing/Hero3D";
import ScrollStory from "./components/landing/ScrollStory";
import FeatureCards from "./components/landing/FeatureCards";
import PropertyShowcase from "./components/landing/PropertyShowcase";
import HowItWorks from "./components/landing/HowItWorks";
import TrustSection from "./components/landing/TrustSection";
import FinalCTA from "./components/landing/FinalCTA";
import { WorkspaceFrame } from "./components/layout/WorkspaceFrame";
import { useAuth, type AuthController } from "./hooks/useAuth";
import { PatoisProvider, PatoisToggle } from "./lib/patois";
import {
  AdminPage,
  AuthPage,
  BookingManagementPage,
  CalendarPage,
  ExplorePage,
  GuestDashboardPage,
  HostDashboardPage,
  HostWellnessPage,
  OfficerWellnessPage,
  PaymentConfirmationPage,
  ProfileSettingsPage,
  PropertyDetailsPage,
  PropertyManagementPage,
} from "./pages/ProductPages";
import {
  AdminOpsSpecPage,
  AuthSpecFlowPage,
  BookingSpecStatePage,
  DirectorySpecPage,
  ExperiencesPage,
  HostProfileSpecPage,
  HostSpecPage,
  JournalPage,
  MessagesPage,
  PublicContentRoute,
  TravelerSpecPage,
} from "./pages/CompletionPages";
import {
  AccessRestrictedPage,
  AdminKpiPage,
  AdminReportsPage,
  AuthPostLoginToastPage,
  BusinessDirectoryPage,
  ComingSoonPage,
  DocumentMessagePage,
  FavoritesCollectionsPage,
  HostPropertyEditPage,
  HostReportsPage,
  InsuraGuestPage,
  InvoicesPage,
  LogoutScreenPage,
  MapSearchPage,
  NoFavoritesPage,
  NoReservationsPage,
  NotFoundPage,
  NotificationsCenterPage,
  OfficerIdResetPage,
  PendingReviewsPage,
  PoliceDirectoryPage,
  PropertyManagerGatePage,
  PropertyManagerReportsPage,
  PropertyManagerUtilitiesPage,
  PropertyManagerVerificationPage,
  ProviderDashboardPage,
  ServerErrorPage,
  SignInRequiredPage,
  TripSuggestionsPage,
  WellnessBookingPage,
} from "./pages/SpecScreens";

const navItems = [
  ["Explore", "/explore"],
  ["Guest", "/guest-dashboard"],
  ["Host", "/host-dashboard"],
  ["Wellness", "/host/wellness"],
  ["Calendar", "/calendar"],
  ["Bookings", "/bookings"],
] as const;

type Route =
  | { name: "home" }
  | { name: "explore" }
  | { name: "map-search" }
  | { name: "coming-soon" }
  | { name: "public-content"; slug: string }
  | { name: "auth-spec"; kind: string }
  | { name: "experiences"; slug?: string }
  | { name: "journal"; slug?: string }
  | { name: "booking-state"; state: string; bookingId?: string }
  | { name: "traveler-spec"; view: string }
  | { name: "messages"; conversationId?: string }
  | { name: "directory-spec"; kind?: string; slug?: string }
  | { name: "host-profile"; slug?: string; edit?: boolean }
  | { name: "host-spec"; view: string }
  | { name: "admin-ops"; view: string }
  | { name: "property"; propertyId?: string }
  | { name: "login" }
  | { name: "register" }
  | { name: "auth-post" }
  | { name: "logout" }
  | { name: "guest-dashboard" }
  | { name: "trav-favorites" }
  | { name: "trav-invoices" }
  | { name: "trav-reviews" }
  | { name: "trav-notifications" }
  | { name: "trav-suggestions" }
  | { name: "host-dashboard" }
  | { name: "host-wellness" }
  | { name: "officer-directory" }
  | { name: "wellness-booking" }
  | { name: "officer-wellness" }
  | { name: "property-management" }
  | { name: "host-property-edit" }
  | { name: "host-reports" }
  | { name: "pm-gates" }
  | { name: "pm-utilities" }
  | { name: "pm-verification" }
  | { name: "pm-reports" }
  | { name: "pm-insurance" }
  | { name: "business-directory" }
  | { name: "provider-dashboard" }
  | { name: "calendar" }
  | { name: "bookings" }
  | { name: "payment"; bookingId?: string }
  | { name: "profile" }
  | { name: "document-message" }
  | { name: "admin" }
  | { name: "admin-kpis" }
  | { name: "admin-reports" }
  | { name: "officer-id-reset" }
  | { name: "sign-in-required" }
  | { name: "access-restricted" }
  | { name: "server-error" }
  | { name: "no-favorites" }
  | { name: "no-reservations" }
  | { name: "not-found" };

function parseRoute(): Route {
  const path = window.location.pathname.replace(/\/+$/, "") || "/";
  const search = new URLSearchParams(window.location.search);

  if (path === "/") return { name: "home" };
  if (path === "/explore") return { name: "explore" };
  if (path === "/explore/map") return { name: "map-search" };
  if (path === "/coming-soon") return { name: "coming-soon" };
  if (["/about", "/trust", "/help", "/contact", "/terms", "/privacy", "/maintenance"].includes(path)) {
    return { name: "public-content", slug: path.slice(1) };
  }
  if (path.startsWith("/help/")) return { name: "public-content", slug: path.slice(1) };
  if (path === "/auth/role") return { name: "auth-spec", kind: "role" };
  if (path === "/auth/email-verification") return { name: "auth-spec", kind: "email" };
  if (path === "/auth/phone-verification") return { name: "auth-spec", kind: "phone" };
  if (path === "/auth/otp") return { name: "auth-spec", kind: "otp" };
  if (path === "/auth/forgot-password") return { name: "auth-spec", kind: "forgot" };
  if (path === "/auth/reset-password") return { name: "auth-spec", kind: "reset" };
  if (path === "/auth/2fa-setup") return { name: "auth-spec", kind: "twofa" };
  if (path === "/auth/recovery-codes") return { name: "auth-spec", kind: "recovery" };
  if (path === "/auth/social-consent") return { name: "auth-spec", kind: "social" };
  if (path === "/experiences") return { name: "experiences" };
  if (path.startsWith("/experiences/")) return { name: "experiences", slug: path.split("/")[2] };
  if (path === "/journal" || path === "/blog") return { name: "journal" };
  if (path.startsWith("/journal/") || path.startsWith("/blog/")) return { name: "journal", slug: path.split("/")[2] };
  if (path.startsWith("/booking/")) {
    const [, , bookingId, state = "review"] = path.split("/");
    return { name: "booking-state", bookingId, state };
  }
  if (path === "/traveler/reservations" || path === "/traveler/reservations/upcoming") return { name: "traveler-spec", view: "reservations-upcoming" };
  if (path === "/traveler/reservations/past") return { name: "traveler-spec", view: "reservations-past" };
  if (path === "/traveler/reservations/cancelled") return { name: "traveler-spec", view: "reservations-cancelled" };
  if (path.startsWith("/traveler/reservations/")) return { name: "traveler-spec", view: "reservation-detail" };
  if (path === "/traveler/payment-methods") return { name: "traveler-spec", view: "payment-methods" };
  if (path === "/traveler/payments") return { name: "traveler-spec", view: "payment-history" };
  if (path === "/traveler/preferences") return { name: "traveler-spec", view: "preferences" };
  if (path === "/traveler/identity") return { name: "traveler-spec", view: "identity" };
  if (path === "/traveler/reviews/given") return { name: "traveler-spec", view: "reviews-given" };
  if (path === "/traveler/reviews/pending") return { name: "traveler-spec", view: "reviews-pending" };
  if (path.startsWith("/traveler/qr")) return { name: "traveler-spec", view: "qr" };
  if (path === "/messages") return { name: "messages" };
  if (path.startsWith("/messages/") && path !== "/messages/document") return { name: "messages", conversationId: path.split("/")[2] };
  if (path === "/directory/custodians") return { name: "directory-spec", kind: "Custodian" };
  if (path === "/directory/trades") return { name: "directory-spec", kind: "Trades" };
  if (path === "/directory/businesses") return { name: "directory-spec", kind: "LocalBusiness" };
  if (path === "/directory/guest-verification") return { name: "directory-spec", kind: "Verification" };
  if (path === "/directory/provider/onboarding") return { name: "directory-spec", kind: "Provider" };
  if (path.startsWith("/directory/providers/")) return { name: "directory-spec", slug: path.split("/")[3] };
  if (path === "/hosts") return { name: "host-profile" };
  if (path === "/host/profile/edit") return { name: "host-profile", edit: true };
  if (path === "/host/profile/preview") return { name: "host-profile", slug: "my-host-profile" };
  if (path.startsWith("/hosts/")) return { name: "host-profile", slug: path.split("/")[2] };
  if (path === "/host/analytics") return { name: "host-spec", view: "analytics" };
  if (path === "/host/pricing") return { name: "host-spec", view: "pricing" };
  if (path === "/host/promotions") return { name: "host-spec", view: "promotions" };
  if (path === "/host/exports") return { name: "host-spec", view: "exports" };
  if (path === "/host/reviews") return { name: "host-spec", view: "reviews" };
  if (path === "/host/badges") return { name: "host-spec", view: "badges" };
  if (path === "/host/settings") return { name: "host-spec", view: "settings" };
  if (path === "/host/properties/archived") return { name: "host-spec", view: "archived" };
  if (path.startsWith("/admin/ops/")) return { name: "admin-ops", view: path.split("/")[3] };
  if (path.startsWith("/properties/")) return { name: "property", propertyId: path.split("/")[2] };
  if (path === "/login") return { name: "login" };
  if (path === "/register") return { name: "register" };
  if (path === "/auth/post-login-toast") return { name: "auth-post" };
  if (path === "/logout") return { name: "logout" };
  if (path === "/guest-dashboard") return { name: "guest-dashboard" };
  if (path === "/traveler/favorites" || path === "/wishlist") return { name: "trav-favorites" };
  if (path === "/traveler/invoices") return { name: "trav-invoices" };
  if (path === "/traveler/reviews") return { name: "trav-reviews" };
  if (path === "/traveler/notifications" || path === "/notifications") return { name: "trav-notifications" };
  if (path === "/traveler/suggestions") return { name: "trav-suggestions" };
  if (path === "/host-dashboard") return { name: "host-dashboard" };
  if (path === "/host/wellness") return { name: "host-wellness" };
  if (path === "/host/wellness/directory") return { name: "officer-directory" };
  if (path === "/host/wellness/book") return { name: "wellness-booking" };
  if (path === "/officer/wellness") return { name: "officer-wellness" };
  if (path === "/host/properties") return { name: "property-management" };
  if (path === "/host/properties/edit") return { name: "host-property-edit" };
  if (path === "/host/reports") return { name: "host-reports" };
  if (path === "/pm/gates") return { name: "pm-gates" };
  if (path === "/pm/utilities") return { name: "pm-utilities" };
  if (path === "/pm/verification") return { name: "pm-verification" };
  if (path === "/pm/reports") return { name: "pm-reports" };
  if (path === "/pm/insurance") return { name: "pm-insurance" };
  if (path === "/directory/provider") return { name: "directory-spec", kind: "ProviderDashboard" };
  if (path === "/calendar") return { name: "calendar" };
  if (path === "/bookings") return { name: "bookings" };
  if (path === "/payment-confirmation") {
    return { name: "payment", bookingId: search.get("bookingId") ?? undefined };
  }
  if (path === "/profile") return { name: "profile" };
  if (path === "/messages/document") return { name: "document-message" };
  if (path === "/admin") return { name: "admin" };
  if (path === "/admin/kpis") return { name: "admin-kpis" };
  if (path === "/admin/reports") return { name: "admin-reports" };
  if (path === "/admin/officer-id-reset") return { name: "officer-id-reset" };
  if (path === "/401") return { name: "sign-in-required" };
  if (path === "/403") return { name: "access-restricted" };
  if (path === "/500") return { name: "server-error" };
  if (path === "/empty/favorites") return { name: "no-favorites" };
  if (path === "/empty/reservations") return { name: "no-reservations" };
  if (path === "/404") return { name: "not-found" };
  return { name: "not-found" };
}

function useRoute() {
  const [route, setRoute] = useState<Route>(() => parseRoute());

  useEffect(() => {
    const onPopState = () => setRoute(parseRoute());
    window.addEventListener("popstate", onPopState);
    return () => window.removeEventListener("popstate", onPopState);
  }, []);

  return route;
}

function LogoMark({ className = "" }: { className?: string }) {
  return (
    <svg
      className={className}
      viewBox="560 150 930 700"
      role="img"
      aria-label="Nesty Stay"
    >
      <image
        href="/assets/nesty/Nesty-Stay.png"
        width="2048"
        height="1280"
        preserveAspectRatio="xMidYMid meet"
      />
    </svg>
  );
}

function Navbar({ auth, route }: { auth: AuthController; route: Route }) {
  const [scrolled, setScrolled] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 40);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <header
      className={`site-nav ${scrolled ? "site-nav--scrolled" : ""}`}
      aria-label="Main navigation"
    >
      <AppLink className="brand-lockup" href="/" aria-label="Nesty Stay home">
        <span className="brand-mark">
          <LogoMark />
        </span>
        <span>NESTY STAY</span>
      </AppLink>

      <nav className="desktop-nav">
        {navItems.map(([label, href]) => (
          <AppLink key={href} className={window.location.pathname === href ? "is-active" : ""} href={href}>
            {label}
          </AppLink>
        ))}
      </nav>

      <AppLink className="nav-cta" href={auth.session ? "/profile" : "/login"}>
        {auth.session ? (
          <>
            <UserRound size={16} /> {auth.session.displayName.split(" ")[0]}
          </>
        ) : (
          <>
            Explore Stays <ArrowUpRight size={16} />
          </>
        )}
      </AppLink>
      <div className="nav-patois-toggle">
        <PatoisToggle />
      </div>

      <button
        type="button"
        className="menu-button"
        aria-label={menuOpen ? "Close menu" : "Open menu"}
        aria-expanded={menuOpen}
        onClick={() => setMenuOpen((open) => !open)}
      >
        {menuOpen ? <X /> : <Menu />}
      </button>

      <AnimatePresence>
        {menuOpen && (
          <motion.nav
            className="mobile-nav"
            initial={{ opacity: 0, y: -12 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -12 }}
          >
            {navItems.map(([label, href]) => (
              <AppLink key={href} href={href} onClick={() => setMenuOpen(false)}>
                {label}
              </AppLink>
            ))}
            <AppLink href="/admin" onClick={() => setMenuOpen(false)}>
              Admin <ArrowUpRight size={16} />
            </AppLink>
            <AppLink href={auth.session ? "/profile" : "/login"} onClick={() => setMenuOpen(false)}>
              {auth.session ? "Profile" : "Login"} <ArrowUpRight size={16} />
            </AppLink>
          </motion.nav>
        )}
      </AnimatePresence>
    </header>
  );
}

function LandingPage() {
  return (
    <>
      <Hero3D />
      <ScrollStory />
      <FeatureCards />
      <PropertyShowcase />
      <HowItWorks />
      <TrustSection />
      <FinalCTA />
    </>
  );
}

function isWorkspaceRoute(route: Route) {
  return [
    "guest-dashboard",
    "trav-favorites",
    "trav-invoices",
    "trav-reviews",
    "trav-notifications",
    "trav-suggestions",
    "traveler-spec",
    "host-dashboard",
    "host-spec",
    "host-profile",
    "host-wellness",
    "officer-directory",
    "wellness-booking",
    "officer-wellness",
    "property-management",
    "host-property-edit",
    "host-reports",
    "pm-gates",
    "pm-utilities",
    "pm-verification",
    "pm-reports",
    "pm-insurance",
    "business-directory",
    "directory-spec",
    "provider-dashboard",
    "calendar",
    "bookings",
    "payment",
    "profile",
    "document-message",
    "messages",
    "admin",
    "admin-ops",
    "admin-kpis",
    "admin-reports",
    "officer-id-reset",
    "no-favorites",
    "no-reservations",
  ].includes(route.name);
}

function hasPublicNav(route: Route) {
  return [
    "home",
    "explore",
    "map-search",
    "public-content",
    "auth-spec",
    "experiences",
    "journal",
    "host-profile",
    "property",
    "auth-post",
    "sign-in-required",
    "access-restricted",
    "server-error",
    "not-found",
  ].includes(route.name);
}

function LogoutRoute({ auth }: { auth: AuthController }) {
  useEffect(() => {
    auth.logout();
  }, [auth.logout]);

  return <LogoutScreenPage />;
}

function CurrentPage({ auth, route }: { auth: AuthController; route: Route }) {
  switch (route.name) {
    case "public-content":
      return <PublicContentRoute slug={route.slug} />;
    case "auth-spec":
      return <AuthSpecFlowPage auth={auth} kind={route.kind} />;
    case "experiences":
      return <ExperiencesPage slug={route.slug} />;
    case "journal":
      return <JournalPage slug={route.slug} />;
    case "booking-state":
      return <BookingSpecStatePage auth={auth} bookingId={route.bookingId} state={route.state} />;
    case "traveler-spec":
      return <TravelerSpecPage auth={auth} view={route.view} />;
    case "messages":
      return <MessagesPage auth={auth} conversationId={route.conversationId} />;
    case "directory-spec":
      return <DirectorySpecPage auth={auth} kind={route.kind} slug={route.slug} />;
    case "host-profile":
      return <HostProfileSpecPage auth={auth} edit={route.edit} slug={route.slug} />;
    case "host-spec":
      return <HostSpecPage auth={auth} view={route.view} />;
    case "admin-ops":
      return <AdminOpsSpecPage view={route.view} />;
    case "explore":
      return <ExplorePage auth={auth} />;
    case "map-search":
      return <MapSearchPage />;
    case "coming-soon":
      return <ComingSoonPage />;
    case "property":
      return <PropertyDetailsPage auth={auth} propertyId={route.propertyId} />;
    case "login":
      return <AuthPage auth={auth} mode="login" />;
    case "register":
      return <AuthPage auth={auth} mode="register" />;
    case "auth-post":
      return <AuthPostLoginToastPage />;
    case "logout":
      return <LogoutRoute auth={auth} />;
    case "guest-dashboard":
      return <GuestDashboardPage auth={auth} />;
    case "trav-favorites":
      return <FavoritesCollectionsPage />;
    case "trav-invoices":
      return <InvoicesPage />;
    case "trav-reviews":
      return <PendingReviewsPage />;
    case "trav-notifications":
      return <NotificationsCenterPage />;
    case "trav-suggestions":
      return <TripSuggestionsPage />;
    case "host-dashboard":
      return <HostDashboardPage auth={auth} />;
    case "host-wellness":
      return <HostWellnessPage auth={auth} />;
    case "officer-directory":
      return <PoliceDirectoryPage />;
    case "wellness-booking":
      return <WellnessBookingPage />;
    case "officer-wellness":
      return <OfficerWellnessPage />;
    case "property-management":
      return <PropertyManagementPage auth={auth} />;
    case "host-property-edit":
      return <HostPropertyEditPage />;
    case "host-reports":
      return <HostReportsPage />;
    case "pm-gates":
      return <PropertyManagerGatePage />;
    case "pm-utilities":
      return <PropertyManagerUtilitiesPage />;
    case "pm-verification":
      return <PropertyManagerVerificationPage />;
    case "pm-reports":
      return <PropertyManagerReportsPage />;
    case "pm-insurance":
      return <InsuraGuestPage />;
    case "business-directory":
      return <BusinessDirectoryPage />;
    case "provider-dashboard":
      return <ProviderDashboardPage />;
    case "calendar":
      return <CalendarPage />;
    case "bookings":
      return <BookingManagementPage />;
    case "payment":
      return <PaymentConfirmationPage bookingId={route.bookingId} />;
    case "profile":
      return <ProfileSettingsPage auth={auth} />;
    case "document-message":
      return <DocumentMessagePage />;
    case "admin":
      return <AdminPage />;
    case "admin-kpis":
      return <AdminKpiPage />;
    case "admin-reports":
      return <AdminReportsPage />;
    case "officer-id-reset":
      return <OfficerIdResetPage />;
    case "sign-in-required":
      return <SignInRequiredPage />;
    case "access-restricted":
      return <AccessRestrictedPage />;
    case "server-error":
      return <ServerErrorPage />;
    case "no-favorites":
      return <NoFavoritesPage />;
    case "no-reservations":
      return <NoReservationsPage />;
    case "not-found":
      return <NotFoundPage />;
    default:
      return <LandingPage />;
  }
}

export default function App() {
  const reduceMotion = useReducedMotion();
  const auth = useAuth();
  const route = useRoute();

  useEffect(() => {
    document.documentElement.style.scrollBehavior = reduceMotion ? "auto" : "smooth";
  }, [reduceMotion]);

  return (
    <PatoisProvider>
      <div className={`app-shell route-${route.name} ${isWorkspaceRoute(route) ? "app-shell--workspace" : ""}`}>
        {hasPublicNav(route) && <Navbar auth={auth} route={route} />}
        {isWorkspaceRoute(route) ? (
          <WorkspaceFrame routeName={route.name}>
            <CurrentPage auth={auth} route={route} />
          </WorkspaceFrame>
        ) : (
          <main>
            <CurrentPage auth={auth} route={route} />
          </main>
        )}
      </div>
    </PatoisProvider>
  );
}
