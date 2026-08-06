import { useEffect, useState, type ReactNode } from "react";
import { AnimatePresence, motion, useReducedMotion } from "framer-motion";
import { Menu, UserRound, X } from "lucide-react";
import { AppLink } from "./components/AppLink";
import { EmblemRoundel } from "./components/layout/PublicShell";
import FeatureCards from "./components/landing/FeatureCards";
import FinalCTA from "./components/landing/FinalCTA";
import Hero3D from "./components/landing/Hero3D";
import HowItWorks from "./components/landing/HowItWorks";
import PropertyShowcase from "./components/landing/PropertyShowcase";
import ScrollStory from "./components/landing/ScrollStory";
import TrustSection from "./components/landing/TrustSection";
import { WorkspaceFrame } from "./components/layout/WorkspaceFrame";
import { cx } from "./lib/ui";
import { useAuth, type AuthController } from "./hooks/useAuth";
import { AdminPermissions, hasAdminPermission, isAdminSession } from "./lib/adminPermissions";
import type { AdminPermission } from "./lib/api";
import { PatoisProvider } from "./lib/patois";
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
  DesignSystemReferencePage,
  DocumentMessagePage,
  FavoritesCollectionsPage,
  HostPropertyEditPage,
  HostReportsPage,
  InsuraGuestPage,
  LoadingStatePage,
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
  ["Host", "/host-dashboard"],
  ["Wellness", "/host/wellness"],
] as const;

const mobileNavItems = [
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
  | { name: "not-found" }
  | { name: "design-system" }
  | { name: "loading-state" }
  | { name: "design-screen"; screenId: string };

function parseRoute(): Route {
  const path = window.location.pathname.replace(/\/+$/, "") || "/";
  const search = new URLSearchParams(window.location.search);

  if (path === "/") return { name: "home" };
  if (path === "/screens") return { name: "design-screen", screenId: "INDEX" };
  if (path.startsWith("/screens/")) return { name: "design-screen", screenId: path.split("/")[2] ?? "PUB-01" };
  if (path === "/design-system") return { name: "design-system" };
  if (path === "/loading") return { name: "loading-state" };
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
  if (path === "/traveler/invoices") return { name: "traveler-spec", view: "invoices" };
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

/** DS v2 floating deep pill navbar — sticky top 14px, compacts slightly on scroll. */
function Navbar({ auth, route }: { auth: AuthController; route: Route }) {
  const [scrolled, setScrolled] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const isHome = route.name === "home";
  const path = window.location.pathname;

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 40);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <div className="sticky top-3.5 z-50 px-[clamp(12px,3vw,28px)]">
      <header
        aria-label="Main navigation"
        className={cx(
          "mx-auto flex max-w-[1140px] flex-wrap items-center gap-2 rounded-pill bg-deep font-sans transition-[padding,box-shadow] duration-300",
          scrolled ? "px-1.5 py-[3px] pl-1 shadow-[0_18px_44px_rgba(4,31,31,0.5)]" : "px-2.5 py-[7px] pl-2 shadow-navbar",
        )}
      >
        <AppLink aria-label="Nesty Stay home" className="flex min-h-11 items-center gap-2.5 pl-1" href="/">
          <EmblemRoundel size={44} />
          <span className="text-[15px] font-bold tracking-[0.14em] text-sand">NESTY STAY</span>
        </AppLink>

        <div className="ml-auto flex flex-wrap items-center gap-0.5">
          <nav className="hidden items-center gap-0.5 md:flex">
            {navItems.map(([label, href]) => (
              <AppLink
                className={cx(
                  "ns-navlink flex min-h-11 items-center rounded-pill px-3.5 text-[13.5px] transition-colors",
                  path === href ? "font-bold text-yellow" : "font-semibold text-on-dark-nav hover:text-white",
                )}
                href={href}
                key={href}
              >
                {label}
              </AppLink>
            ))}
          </nav>

          {auth.session ? (
            <AppLink
              className="ml-1.5 flex min-h-11 items-center gap-2 rounded-pill bg-yellow px-5 text-[13.5px] font-bold text-deep transition-colors hover:bg-yellow-press"
              href="/profile"
            >
              <UserRound size={16} /> {auth.session.displayName.split(" ")[0]}
            </AppLink>
          ) : (
            <AppLink
              className="group ml-1.5 flex min-h-11 items-center gap-2 rounded-pill bg-yellow px-5 text-[13.5px] font-bold text-deep transition-colors hover:bg-yellow-press"
              href={isHome ? "/explore" : "/login"}
            >
              {isHome ? (
                <>
                  Explore stays{" "}
                  <span aria-hidden="true" className="inline-block transition-transform duration-200 group-hover:translate-x-1">
                    →
                  </span>
                </>
              ) : (
                "Sign in"
              )}
            </AppLink>
          )}

          <button
            aria-expanded={menuOpen}
            aria-label={menuOpen ? "Close menu" : "Open menu"}
            className="grid size-11 cursor-pointer place-items-center rounded-pill text-on-dark-nav transition-colors hover:text-white md:hidden"
            onClick={() => setMenuOpen((open) => !open)}
            type="button"
          >
            {menuOpen ? <X /> : <Menu />}
          </button>
        </div>

        <AnimatePresence>
          {menuOpen && (
            <motion.nav
              animate={{ opacity: 1, y: 0 }}
              className="flex w-full flex-col gap-0.5 border-t border-white/10 px-2 py-2 md:hidden"
              exit={{ opacity: 0, y: -12 }}
              initial={{ opacity: 0, y: -12 }}
            >
              {mobileNavItems.map(([label, href]) => (
                <AppLink
                  className={cx(
                    "flex min-h-11 items-center rounded-nav px-3.5 text-[13.5px] font-semibold",
                    path === href ? "bg-yellow/10 text-yellow" : "text-on-dark-nav hover:bg-on-dark-heading/5",
                  )}
                  href={href}
                  key={href}
                  onClick={() => setMenuOpen(false)}
                >
                  {label}
                </AppLink>
              ))}
              <AppLink
                className="flex min-h-11 items-center rounded-nav px-3.5 text-[13.5px] font-semibold text-on-dark-nav hover:bg-on-dark-heading/5"
                href={auth.session ? "/profile" : "/login"}
                onClick={() => setMenuOpen(false)}
              >
                {auth.session ? "Profile" : "Sign in"}
              </AppLink>
            </motion.nav>
          )}
        </AnimatePresence>
      </header>
    </div>
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
    "booking-state",
    "public-content",
    "auth-spec",
    "experiences",
    "journal",
    "host-profile",
    "property",
    "sign-in-required",
    "access-restricted",
    "server-error",
    "not-found",
    "design-system",
    "loading-state",
  ].includes(route.name);
}

function LogoutRoute({ auth }: { auth: AuthController }) {
  useEffect(() => {
    auth.logout();
  }, [auth.logout]);

  return <LogoutScreenPage />;
}

function AdminRoute({
  auth,
  permission,
  children,
}: {
  auth: AuthController;
  permission: AdminPermission;
  children: ReactNode;
}) {
  if (!auth.session) {
    return <AuthPage auth={auth} mode="login" />;
  }

  if (!isAdminSession(auth.session) || !hasAdminPermission(auth.session, permission)) {
    return <AccessRestrictedPage />;
  }

  return <>{children}</>;
}

function adminOpsPermission(view: string): AdminPermission {
  if (view === "audit" || view === "logs") return AdminPermissions.auditLogAccess;
  if (view === "payments" || view === "refunds" || view === "reports") return AdminPermissions.financialReporting;
  return AdminPermissions.userManagement;
}

const implementedScreens = [
  ["INDEX", "Validation index", "/screens"],
  ["DS-V2", "Design system", "/design-system"],
  ["PUB-01", "Landing", "/"],
  ["PUB-02", "Explore stays", "/explore"],
  ["PUB-04", "Property detail", "/properties/22222222-2222-4222-8222-222222222222"],
  ["PUB-MAP", "Map search", "/explore/map"],
  ["PUB-SOON", "Coming soon", "/coming-soon"],
  ["AUTH-01", "Login and signup", "/login"],
  ["AUTH-POST", "Post-login toast", "/auth/post-login-toast"],
  ["AUTH-LOGOUT", "Logout", "/logout"],
  ["BOOK-01", "Booking dates/review", "/booking/review"],
  ["BOOK-02", "Booking quote", "/booking/quote"],
  ["BOOK-03", "Booking identity", "/booking/identity"],
  ["BOOK-05", "Booking checkout", "/booking/checkout"],
  ["BOOK-07", "Booking pending", "/booking/pending"],
  ["BOOK-CONF", "Booking confirmation", "/booking/success"],
  ["TRAV-01", "Traveler dashboard", "/guest-dashboard"],
  ["TRAV-12", "Traveler settings", "/profile"],
  ["TRAV-COL", "Traveler collections", "/traveler/favorites"],
  ["TRAV-INV", "Traveler invoices", "/traveler/invoices"],
  ["TRAV-NOTIF", "Traveler notifications", "/traveler/notifications"],
  ["TRAV-PEND", "Pending reviews", "/traveler/reviews/pending"],
  ["TRAV-SUGG", "Trip suggestions", "/traveler/suggestions"],
  ["MSG-01", "Messages", "/messages"],
  ["MSG-DOC", "Secure document message", "/messages/document"],
  ["DIR-02", "Trades directory", "/directory/trades"],
  ["DIR-BIZ", "Business directory", "/directory/businesses"],
  ["DIR-PROV", "Provider profile", "/directory/provider"],
  ["HOST-01", "Host dashboard", "/host-dashboard"],
  ["HOST-05", "Host properties", "/host/properties"],
  ["HOST-EDIT", "Host property edit", "/host/properties/edit"],
  ["HOST-RPT", "Host reports", "/host/reports"],
  ["HOST-WELL", "Host wellness", "/host/wellness"],
  ["HOST-BADGE", "Host badges", "/host/badges"],
  ["OFC-01", "Officer onboarding", "/officer/wellness"],
  ["OFC-02", "Officer visits", "/officer/wellness"],
  ["OFC-DIR", "Officer directory", "/host/wellness/directory"],
  ["OFC-BOOK", "Wellness booking", "/host/wellness/book"],
  ["PM-GATE", "Gate communications", "/pm/gates"],
  ["PM-UTIL", "Utility proofing", "/pm/utilities"],
  ["PM-VERIFY", "Tenant verification", "/pm/verification"],
  ["PM-RPT", "Portfolio reports", "/pm/reports"],
  ["PM-INS", "Insurance", "/pm/insurance"],
  ["ADM-01", "Admin operations", "/admin/ops/disputes"],
  ["ADM-KPI", "Admin KPIs", "/admin/kpis"],
  ["ADM-RESET", "Officer ID reset", "/admin/officer-id-reset"],
  ["ADM-RPT", "Admin reports", "/admin/reports"],
  ["ERR-401", "Sign-in required", "/401"],
  ["ERR-403", "Access restricted", "/403"],
  ["ERR-404", "Not found", "/404"],
  ["ERR-500", "Server error", "/500"],
  ["ERR-LOAD", "Loading state", "/loading"],
  ["ERR-NOFAV", "No favorites", "/empty/favorites"],
  ["ERR-NORES", "No reservations", "/empty/reservations"],
] as const;

function componentRouteForScreen(screenId: string): Route | undefined {
  switch (screenId) {
    case "DS-V2":
      return { name: "design-system" };
    case "PUB-01":
      return { name: "home" };
    case "PUB-02":
      return { name: "explore" };
    case "PUB-04":
      return { name: "property", propertyId: "22222222-2222-4222-8222-222222222222" };
    case "PUB-MAP":
      return { name: "map-search" };
    case "PUB-SOON":
      return { name: "coming-soon" };
    case "AUTH-01":
      return { name: "login" };
    case "AUTH-POST":
      return { name: "auth-post" };
    case "AUTH-LOGOUT":
      return { name: "logout" };
    case "BOOK-01":
      return { name: "booking-state", state: "review" };
    case "BOOK-02":
      return { name: "booking-state", state: "quote" };
    case "BOOK-03":
      return { name: "booking-state", state: "identity" };
    case "BOOK-05":
      return { name: "booking-state", state: "checkout" };
    case "BOOK-07":
      return { name: "booking-state", state: "pending" };
    case "BOOK-CONF":
      return { name: "booking-state", state: "success" };
    case "TRAV-01":
      return { name: "guest-dashboard" };
    case "TRAV-12":
      return { name: "profile" };
    case "TRAV-COL":
      return { name: "trav-favorites" };
    case "TRAV-INV":
      return { name: "traveler-spec", view: "invoices" };
    case "TRAV-NOTIF":
      return { name: "trav-notifications" };
    case "TRAV-PEND":
      return { name: "traveler-spec", view: "reviews-pending" };
    case "TRAV-SUGG":
      return { name: "trav-suggestions" };
    case "MSG-01":
      return { name: "messages" };
    case "MSG-DOC":
      return { name: "document-message" };
    case "DIR-02":
      return { name: "directory-spec", kind: "Trades" };
    case "DIR-BIZ":
      return { name: "directory-spec", kind: "LocalBusiness" };
    case "DIR-PROV":
      return { name: "provider-dashboard" };
    case "HOST-01":
      return { name: "host-dashboard" };
    case "HOST-05":
      return { name: "property-management" };
    case "HOST-EDIT":
      return { name: "host-property-edit" };
    case "HOST-RPT":
      return { name: "host-reports" };
    case "HOST-WELL":
      return { name: "host-wellness" };
    case "HOST-BADGE":
      return { name: "host-spec", view: "badges" };
    case "OFC-01":
    case "OFC-02":
      return { name: "officer-wellness" };
    case "OFC-DIR":
      return { name: "officer-directory" };
    case "OFC-BOOK":
      return { name: "wellness-booking" };
    case "PM-GATE":
      return { name: "pm-gates" };
    case "PM-UTIL":
      return { name: "pm-utilities" };
    case "PM-VERIFY":
      return { name: "pm-verification" };
    case "PM-RPT":
      return { name: "pm-reports" };
    case "PM-INS":
      return { name: "pm-insurance" };
    case "ADM-01":
      return { name: "admin-ops", view: "disputes" };
    case "ADM-KPI":
      return { name: "admin-kpis" };
    case "ADM-RESET":
      return { name: "officer-id-reset" };
    case "ADM-RPT":
      return { name: "admin-reports" };
    case "ERR-401":
      return { name: "sign-in-required" };
    case "ERR-403":
      return { name: "access-restricted" };
    case "ERR-404":
      return { name: "not-found" };
    case "ERR-500":
      return { name: "server-error" };
    case "ERR-LOAD":
      return { name: "loading-state" };
    case "ERR-NOFAV":
      return { name: "no-favorites" };
    case "ERR-NORES":
      return { name: "no-reservations" };
    default:
      return undefined;
  }
}

function ScreenImplementationIndex() {
  return (
    <main className="screen-index-page">
      <section className="screen-index-hero">
        <span className="badge badge-sun">Component implementation map</span>
        <h1>Client screens are implemented as React routes.</h1>
        <p>
          Each client screen ID points to a component route backed by typed DTOs and the NestyStay API client.
        </p>
      </section>
      <section className="screen-index-grid">
        {implementedScreens.map(([screenId, title, href]) => (
          <AppLink className="screen-index-card" href={`/screens/${screenId}`} key={screenId}>
            <span>{screenId}</span>
            <strong>{title}</strong>
            <small>{href}</small>
          </AppLink>
        ))}
      </section>
    </main>
  );
}

function CurrentPage({ auth, route }: { auth: AuthController; route: Route }) {
  switch (route.name) {
    case "design-screen": {
      if (route.screenId === "INDEX") return <ScreenImplementationIndex />;
      const componentRoute = componentRouteForScreen(route.screenId);
      return componentRoute ? <CurrentPage auth={auth} route={componentRoute} /> : <ScreenImplementationIndex />;
    }
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
      return (
        <AdminRoute auth={auth} permission={adminOpsPermission(route.view)}>
          <AdminOpsSpecPage auth={auth} view={route.view} />
        </AdminRoute>
      );
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
      return <CalendarPage auth={auth} />;
    case "bookings":
      return <BookingManagementPage auth={auth} />;
    case "payment":
      return <PaymentConfirmationPage auth={auth} bookingId={route.bookingId} />;
    case "profile":
      return <ProfileSettingsPage auth={auth} />;
    case "document-message":
      return <DocumentMessagePage />;
    case "admin":
      return (
        <AdminRoute auth={auth} permission={AdminPermissions.superAdministration}>
          <AdminPage auth={auth} />
        </AdminRoute>
      );
    case "admin-kpis":
      return (
        <AdminRoute auth={auth} permission={AdminPermissions.financialReporting}>
          <AdminKpiPage />
        </AdminRoute>
      );
    case "admin-reports":
      return (
        <AdminRoute auth={auth} permission={AdminPermissions.financialReporting}>
          <AdminReportsPage />
        </AdminRoute>
      );
    case "officer-id-reset":
      return (
        <AdminRoute auth={auth} permission={AdminPermissions.officerManagement}>
          <OfficerIdResetPage />
        </AdminRoute>
      );
    case "sign-in-required":
      return <SignInRequiredPage />;
    case "access-restricted":
      return <AccessRestrictedPage />;
    case "server-error":
      return <ServerErrorPage />;
    case "design-system":
      return <DesignSystemReferencePage />;
    case "loading-state":
      return <LoadingStatePage />;
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

  useEffect(() => {
    if (route.name === "logout") {
      auth.logout();
    }
  }, [auth.logout, route.name]);

  return (
    <PatoisProvider>
      <div
        className={`app-shell route-${route.name} ${isWorkspaceRoute(route) ? "app-shell--workspace" : ""}`}
      >
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
