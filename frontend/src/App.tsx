import { lazy, Suspense, useEffect, useState, type ReactNode } from "react";
import { AnimatePresence, motion, useReducedMotion } from "framer-motion";
import { Menu, Search, UserRound, X } from "lucide-react";
import { AppLink, navigate } from "./components/AppLink";
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
import { getRouteAccess, getRouteDefinition, hasPublicNav, isWorkspaceRoute, parseRoute, PUBLIC_NAVIGATION, routeForScreenId, SCREEN_MANIFEST, type Route } from "./app/routeManifest";
import { Modal } from "./components/ui/Modal";
import type { ConfirmationRequest } from "./lib/confirmation";
const AdminPage = lazy(() => import("./pages/ProductPages").then(({ AdminPage }) => ({ default: AdminPage })));
const AuthPage = lazy(() => import("./pages/ProductPages").then(({ AuthPage }) => ({ default: AuthPage })));
const PasswordlessCompletionPage = lazy(() => import("./features/auth/AuthStateContainer").then(({ PasswordlessCompletionPage }) => ({ default: PasswordlessCompletionPage })));
const BookingManagementPage = lazy(() => import("./pages/ProductPages").then(({ BookingManagementPage }) => ({ default: BookingManagementPage })));
const CalendarPage = lazy(() => import("./pages/ProductPages").then(({ CalendarPage }) => ({ default: CalendarPage })));
const ExplorePage = lazy(() => import("./pages/ProductPages").then(({ ExplorePage }) => ({ default: ExplorePage })));
const GuestDashboardPage = lazy(() => import("./pages/ProductPages").then(({ GuestDashboardPage }) => ({ default: GuestDashboardPage })));
const HostDashboardPage = lazy(() => import("./pages/ProductPages").then(({ HostDashboardPage }) => ({ default: HostDashboardPage })));
const HostWellnessPage = lazy(() => import("./pages/ProductPages").then(({ HostWellnessPage }) => ({ default: HostWellnessPage })));
const OfficerWellnessPage = lazy(() => import("./pages/ProductPages").then(({ OfficerWellnessPage }) => ({ default: OfficerWellnessPage })));
const PaymentConfirmationPage = lazy(() => import("./pages/ProductPages").then(({ PaymentConfirmationPage }) => ({ default: PaymentConfirmationPage })));
const ProfileSettingsPage = lazy(() => import("./pages/ProductPages").then(({ ProfileSettingsPage }) => ({ default: ProfileSettingsPage })));
const PropertyDetailsPage = lazy(() => import("./pages/ProductPages").then(({ PropertyDetailsPage }) => ({ default: PropertyDetailsPage })));
const PropertyManagementPage = lazy(() => import("./pages/ProductPages").then(({ PropertyManagementPage }) => ({ default: PropertyManagementPage })));

const OwnerPortalPage = lazy(() => import("./pages/PropertyManagerPages").then(({ OwnerPortalPage }) => ({ default: OwnerPortalPage })));
const PropertyManagerDashboardPage = lazy(() => import("./pages/PropertyManagerPages").then(({ PropertyManagerDashboardPage }) => ({ default: PropertyManagerDashboardPage })));
const PropertyManagerGatePage = lazy(() => import("./pages/PropertyManagerPages").then(({ PropertyManagerGatePage }) => ({ default: PropertyManagerGatePage })));
const PropertyManagerPmsPage = lazy(() => import("./pages/PropertyManagerPmsPage").then(({ PropertyManagerPmsPage }) => ({ default: PropertyManagerPmsPage })));

const AdminOpsSpecPage = lazy(() => import("./pages/CompletionPages").then(({ AdminOpsSpecPage }) => ({ default: AdminOpsSpecPage })));
const AuthSpecFlowPage = lazy(() => import("./pages/CompletionPages").then(({ AuthSpecFlowPage }) => ({ default: AuthSpecFlowPage })));
const OwnerInvitationPage = lazy(() => import("./pages/CompletionPages").then(({ OwnerInvitationPage }) => ({ default: OwnerInvitationPage })));
const BookingSpecStatePage = lazy(() => import("./pages/CompletionPages").then(({ BookingSpecStatePage }) => ({ default: BookingSpecStatePage })));
const DirectorySpecPage = lazy(() => import("./pages/CompletionPages").then(({ DirectorySpecPage }) => ({ default: DirectorySpecPage })));
const ExperiencesPage = lazy(() => import("./pages/CompletionPages").then(({ ExperiencesPage }) => ({ default: ExperiencesPage })));
const HostProfileSpecPage = lazy(() => import("./pages/CompletionPages").then(({ HostProfileSpecPage }) => ({ default: HostProfileSpecPage })));
const HostSpecPage = lazy(() => import("./pages/CompletionPages").then(({ HostSpecPage }) => ({ default: HostSpecPage })));
const JournalPage = lazy(() => import("./pages/CompletionPages").then(({ JournalPage }) => ({ default: JournalPage })));
const MessagesPage = lazy(() => import("./pages/CompletionPages").then(({ MessagesPage }) => ({ default: MessagesPage })));
const PublicContentRoute = lazy(() => import("./pages/CompletionPages").then(({ PublicContentRoute }) => ({ default: PublicContentRoute })));
const QrGateValidationPage = lazy(() => import("./pages/CompletionPages").then(({ QrGateValidationPage }) => ({ default: QrGateValidationPage })));
const TravelerSpecPage = lazy(() => import("./pages/CompletionPages").then(({ TravelerSpecPage }) => ({ default: TravelerSpecPage })));

const AccessRestrictedPage = lazy(() => import("./pages/SpecScreens").then(({ AccessRestrictedPage }) => ({ default: AccessRestrictedPage })));
const AdminInsightsPage = lazy(() => import("./features/admin/AdminInsights").then(({ AdminInsights }) => ({ default: AdminInsights })));
const AuthPostLoginToastPage = lazy(() => import("./pages/SpecScreens").then(({ AuthPostLoginToastPage }) => ({ default: AuthPostLoginToastPage })));
const ComingSoonPage = lazy(() => import("./pages/SpecScreens").then(({ ComingSoonPage }) => ({ default: ComingSoonPage })));
const DesignSystemReferencePage = lazy(() => import("./pages/SpecScreens").then(({ DesignSystemReferencePage }) => ({ default: DesignSystemReferencePage })));
const LoadingStatePage = lazy(() => import("./pages/SpecScreens").then(({ LoadingStatePage }) => ({ default: LoadingStatePage })));
const LogoutScreenPage = lazy(() => import("./pages/SpecScreens").then(({ LogoutScreenPage }) => ({ default: LogoutScreenPage })));
const MapSearchPage = lazy(() => import("./pages/SpecScreens").then(({ MapSearchPage }) => ({ default: MapSearchPage })));
const NoFavoritesPage = lazy(() => import("./pages/SpecScreens").then(({ NoFavoritesPage }) => ({ default: NoFavoritesPage })));
const NoReservationsPage = lazy(() => import("./pages/SpecScreens").then(({ NoReservationsPage }) => ({ default: NoReservationsPage })));
const NotFoundPage = lazy(() => import("./pages/SpecScreens").then(({ NotFoundPage }) => ({ default: NotFoundPage })));
const OfficerIdResetPage = lazy(() => import("./pages/SpecScreens").then(({ OfficerIdResetPage }) => ({ default: OfficerIdResetPage })));
const ServerErrorPage = lazy(() => import("./pages/SpecScreens").then(({ ServerErrorPage }) => ({ default: ServerErrorPage })));
const SignInRequiredPage = lazy(() => import("./pages/SpecScreens").then(({ SignInRequiredPage }) => ({ default: SignInRequiredPage })));
const TripSuggestionsPage = lazy(() => import("./pages/SpecScreens").then(({ TripSuggestionsPage }) => ({ default: TripSuggestionsPage })));

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
  const [searchQuery, setSearchQuery] = useState("");
  const isHome = route.name === "home";
  const path = window.location.pathname;
  const visibleNavItems = auth.session?.roles?.includes("Admin")
    ? [...PUBLIC_NAVIGATION, { label: "Admin", href: "/admin/ops/badges", screenId: "ADM-BADGES", roles: ["Admin"] as const }]
    : PUBLIC_NAVIGATION;

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
          <form
            aria-label="Global search"
            className="mr-1 hidden min-h-11 items-center gap-2 rounded-pill border border-white/15 bg-white/5 px-3 transition-colors focus-within:border-yellow lg:flex"
            onSubmit={(event) => {
              event.preventDefault();
              const query = searchQuery.trim();
              navigate(query ? `/explore?search=${encodeURIComponent(query)}` : "/explore");
            }}
          >
            <Search aria-hidden="true" className="text-on-dark-faint" size={15} />
            <input
              aria-label="Search stays and workspaces"
              className="w-36 border-none bg-transparent text-[13px] text-white outline-none placeholder:text-on-dark-faint"
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Search stays…"
              type="search"
              value={searchQuery}
            />
            <kbd className="rounded border border-white/15 px-1.5 py-0.5 text-[10px] text-on-dark-faint">/</kbd>
          </form>
          <nav className="hidden items-center gap-0.5 md:flex">
            {visibleNavItems.map((item) => (
              <AppLink
                className={cx(
                  "ns-navlink flex min-h-11 items-center rounded-pill px-3.5 text-[13.5px] transition-colors",
                  path === item.href ? "font-bold text-yellow" : "font-semibold text-on-dark-nav hover:text-white",
                )}
                href={item.href}
                key={item.screenId}
              >
                {item.label}
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
              <form
                aria-label="Global search"
                className="mb-1 flex min-h-11 items-center gap-2 rounded-nav border border-white/15 bg-white/5 px-3 focus-within:border-yellow"
                onSubmit={(event) => {
                  event.preventDefault();
                  const query = searchQuery.trim();
                  setMenuOpen(false);
                  navigate(query ? `/explore?search=${encodeURIComponent(query)}` : "/explore");
                }}
              >
                <Search aria-hidden="true" className="text-on-dark-faint" size={15} />
                <input
                  aria-label="Search stays and workspaces"
                  className="min-h-10 min-w-0 flex-1 border-none bg-transparent text-[13px] text-white outline-none placeholder:text-on-dark-faint"
                  onChange={(event) => setSearchQuery(event.target.value)}
                  placeholder="Search stays or workspaces…"
                  type="search"
                  value={searchQuery}
                />
              </form>
              {visibleNavItems.map((item) => (
                <AppLink
                  className={cx(
                    "flex min-h-11 items-center rounded-nav px-3.5 text-[13.5px] font-semibold",
                    path === item.href ? "bg-yellow/10 text-yellow" : "text-on-dark-nav hover:bg-on-dark-heading/5",
                  )}
                  href={item.href}
                  key={item.screenId}
                  onClick={() => setMenuOpen(false)}
                >
                  {item.label}
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

function LogoutRoute({ auth }: { auth: AuthController }) {
  useEffect(() => {
    void auth.logout();
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
  if (view === "directory" || view === "directories" || view === "providers") return AdminPermissions.propertyModeration;
  if (view === "payments" || view === "refunds" || view === "reports" || view === "wellness") return AdminPermissions.financialReporting;
  if (view === "badges" || view === "badge-management" || view === "badge-assignments" || view === "pricebook" || view === "campaigns" || view === "fees" || view === "integrations") return AdminPermissions.systemConfiguration;
  return AdminPermissions.userManagement;
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
        {SCREEN_MANIFEST.map((definition) => (
          <AppLink className="screen-index-card" href={`/screens/${definition.id}`} key={definition.id}>
            <span>{definition.id}</span>
            <strong>{definition.title}</strong>
            <small>{definition.canonicalPath}</small>
          </AppLink>
        ))}
      </section>
    </main>
  );
}

type PendingConfirmation = ConfirmationRequest & { resolve: (accepted: boolean) => void };

function ConfirmationHost() {
  const [pending, setPending] = useState<PendingConfirmation | null>(null);

  useEffect(() => {
    const onConfirm = (event: Event) => {
      const detail = (event as CustomEvent<{ request?: ConfirmationRequest; resolve?: (accepted: boolean) => void }>).detail;
      if (!detail?.request || !detail.resolve) return;
      setPending({ ...detail.request, resolve: detail.resolve });
    };
    window.addEventListener("nesty:confirm", onConfirm);
    return () => window.removeEventListener("nesty:confirm", onConfirm);
  }, []);

  const close = (accepted: boolean) => {
    pending?.resolve(accepted);
    setPending(null);
  };

  return (
    <Modal open={Boolean(pending)} title={pending?.title ?? "Please confirm"} onClose={() => close(false)} variant="sheet">
      <p className="m-0 text-sm leading-6 text-sand-700">{pending?.message}</p>
      <div className="mt-6 flex flex-wrap justify-end gap-2">
        <button className="min-h-11 rounded-pill border border-sand-input bg-transparent px-4 text-sm font-semibold text-ink" onClick={() => close(false)} type="button">
          {pending?.cancelLabel ?? "Cancel"}
        </button>
        <button className="min-h-11 rounded-pill bg-deep px-4 text-sm font-semibold text-on-dark-heading" onClick={() => close(true)} type="button">
          {pending?.confirmLabel ?? "Confirm"}
        </button>
      </div>
    </Modal>
  );
}

function CurrentPage({ auth, route }: { auth: AuthController; route: Route }) {
  switch (route.name) {
    case "design-screen": {
      if (route.screenId === "INDEX") return <ScreenImplementationIndex />;
      const componentRoute = routeForScreenId(route.screenId);
      return componentRoute ? <CurrentPage auth={auth} route={componentRoute} /> : <ScreenImplementationIndex />;
    }
    case "public-content":
      return <PublicContentRoute slug={route.slug} />;
    case "auth-spec":
      return <AuthSpecFlowPage auth={auth} kind={route.kind} />;
    case "owner-invitation":
      return <OwnerInvitationPage />;
    case "experiences":
      return <ExperiencesPage slug={route.slug} />;
    case "journal":
      return <JournalPage slug={route.slug} />;
    case "booking-state":
      return <BookingSpecStatePage auth={auth} bookingId={route.bookingId} state={route.state} />;
    case "traveler-spec":
      return <TravelerSpecPage auth={auth} view={route.view} />;
    case "qr-gate":
      return <QrGateValidationPage />;
    case "messages":
      return <MessagesPage auth={auth} conversationId={route.conversationId} />;
    case "directory-spec":
      return <DirectorySpecPage auth={auth} kind={route.kind} slug={route.slug} />;
    case "host-profile":
      return <HostProfileSpecPage auth={auth} edit={route.edit} slug={route.slug} />;
    case "host-spec":
      return <HostSpecPage auth={auth} view={route.view} propertyId={route.propertyId} />;
    case "admin-ops":
      return (
        <AdminRoute auth={auth} permission={adminOpsPermission(route.view)}>
          {route.view === "wellness" ? <AdminPage auth={auth} /> : <AdminOpsSpecPage auth={auth} view={route.view} />}
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
    case "passwordless-complete":
      return <PasswordlessCompletionPage auth={auth} />;
    case "auth-post":
      return <AuthPostLoginToastPage />;
    case "logout":
      return <LogoutRoute auth={auth} />;
    case "guest-dashboard":
      return <GuestDashboardPage auth={auth} />;
    case "trav-suggestions":
      return <TripSuggestionsPage auth={auth} />;
    case "host-dashboard":
      return <HostDashboardPage auth={auth} />;
    case "host-wellness":
      return <HostWellnessPage auth={auth} />;
    case "officer-directory":
      return <DirectorySpecPage auth={auth} kind="Police" />;
    case "wellness-booking":
      return <HostWellnessPage auth={auth} />;
    case "officer-wellness":
      return <OfficerWellnessPage auth={auth} />;
    case "property-management":
      return <PropertyManagementPage auth={auth} />;
    case "host-property-edit":
      return <HostSpecPage auth={auth} view="properties-edit" />;
    case "pm-gates":
      return <PropertyManagerPmsPage auth={auth} module="gates" />;
    case "pm-dashboard":
      return <PropertyManagerDashboardPage auth={auth} />;
    case "pm-invoices":
      return <PropertyManagerPmsPage auth={auth} module="invoices" />;
    case "pm-maintenance":
      return <PropertyManagerPmsPage auth={auth} module="maintenance" />;
    case "pm-governance":
      return <PropertyManagerPmsPage auth={auth} module="governance" />;
    case "pm-documents":
      return <PropertyManagerPmsPage auth={auth} module="documents" />;
    case "owner-dashboard":
      return <OwnerPortalPage auth={auth} />;
    case "pm-gate":
      return <PropertyManagerGatePage auth={auth} />;
    case "pm-utilities":
      return <PropertyManagerPmsPage auth={auth} module="utilities" />;
    case "pm-verification":
      return <PropertyManagerPmsPage auth={auth} module="verification" />;
    case "pm-reports":
      return <PropertyManagerPmsPage auth={auth} module="reports" />;
    case "pm-payments":
      return <PropertyManagerPmsPage auth={auth} module="payments" />;
    case "pm-vendors":
      return <PropertyManagerPmsPage auth={auth} module="vendors" />;
    case "pm-community":
      return <PropertyManagerPmsPage auth={auth} module="community" />;
    case "pm-subscription":
      return <PropertyManagerPmsPage auth={auth} module="subscription" />;
    case "pm-calendar":
      return <PropertyManagerPmsPage auth={auth} module="calendar" />;
    case "pm-work-orders":
      return <PropertyManagerPmsPage auth={auth} module="work-orders" />;
    case "pm-agreements":
      return <PropertyManagerPmsPage auth={auth} module="agreements" />;
    case "pm-approvals":
      return <PropertyManagerPmsPage auth={auth} module="approvals" />;
    case "pm-team":
      return <PropertyManagerPmsPage auth={auth} module="team" />;
    case "pm-inspections":
      return <PropertyManagerPmsPage auth={auth} module="inspections" />;
    case "pm-cleaning":
      return <PropertyManagerPmsPage auth={auth} module="cleaning" />;
    case "pm-insurance":
      return <PropertyManagerPmsPage auth={auth} module="insurance" />;
    case "business-directory":
      return <DirectorySpecPage auth={auth} kind="LocalBusiness" />;
    case "provider-dashboard":
      return <DirectorySpecPage auth={auth} kind="ProviderDashboard" />;
    case "calendar":
      return <CalendarPage auth={auth} />;
    case "bookings":
      return <BookingManagementPage auth={auth} />;
    case "payment":
      return <PaymentConfirmationPage auth={auth} bookingId={route.bookingId} />;
    case "profile":
      return <ProfileSettingsPage auth={auth} />;
    case "admin":
      return (
        <AdminRoute auth={auth} permission={AdminPermissions.superAdministration}>
          <AdminPage auth={auth} />
        </AdminRoute>
      );
    case "admin-kpis":
      return (
        <AdminRoute auth={auth} permission={AdminPermissions.financialReporting}>
          <AdminInsightsPage token={auth.session?.accessToken ?? ""} view="kpis" />
        </AdminRoute>
      );
    case "admin-reports":
      return (
        <AdminRoute auth={auth} permission={AdminPermissions.financialReporting}>
          <AdminInsightsPage token={auth.session?.accessToken ?? ""} view="reports" />
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
  const access = getRouteAccess(route, auth.session);
  const canRenderWorkspace = access.kind === "allowed" && isWorkspaceRoute(route);

  useEffect(() => {
    document.documentElement.style.scrollBehavior = reduceMotion ? "auto" : "smooth";
  }, [reduceMotion]);

  useEffect(() => {
    if (route.name === "logout") {
      void auth.logout();
    }
  }, [auth.logout, route.name]);

  useEffect(() => {
    const definition = getRouteDefinition(route);
    document.title = definition ? `${definition.title} · NestyStay` : "NestyStay";
  }, [route]);

  return (
    <PatoisProvider>
      <ConfirmationHost />
      <div
        className={`app-shell route-${route.name} ${canRenderWorkspace ? "app-shell--workspace" : ""}`}
      >
        {access.kind === "allowed" && hasPublicNav(route) && <Navbar auth={auth} route={route} />}
        <Suspense fallback={<div aria-live="polite" className="min-h-[50vh] p-8" role="status">Loading this workspace…</div>}>
          {access.kind === "auth-required" ? (
            <SignInRequiredPage returnTo={access.returnTo} />
          ) : access.kind === "forbidden" ? (
            <AccessRestrictedPage />
          ) : canRenderWorkspace ? (
            <WorkspaceFrame routeName={route.name} screenId={route.screenId}>
              <CurrentPage auth={auth} route={route} />
            </WorkspaceFrame>
          ) : (
            <main id="route-main" tabIndex={-1}>
              <CurrentPage auth={auth} route={route} />
            </main>
          )}
        </Suspense>
      </div>
    </PatoisProvider>
  );
}
