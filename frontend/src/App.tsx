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
import { useAuth, type AuthController } from "./hooks/useAuth";
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
  | { name: "property"; propertyId?: string }
  | { name: "login" }
  | { name: "register" }
  | { name: "guest-dashboard" }
  | { name: "host-dashboard" }
  | { name: "host-wellness" }
  | { name: "officer-wellness" }
  | { name: "property-management" }
  | { name: "calendar" }
  | { name: "bookings" }
  | { name: "payment"; bookingId?: string }
  | { name: "profile" }
  | { name: "admin" };

function parseRoute(): Route {
  const path = window.location.pathname.replace(/\/+$/, "") || "/";
  const search = new URLSearchParams(window.location.search);

  if (path === "/") return { name: "home" };
  if (path === "/explore") return { name: "explore" };
  if (path.startsWith("/properties/")) return { name: "property", propertyId: path.split("/")[2] };
  if (path === "/login") return { name: "login" };
  if (path === "/register") return { name: "register" };
  if (path === "/guest-dashboard") return { name: "guest-dashboard" };
  if (path === "/host-dashboard") return { name: "host-dashboard" };
  if (path === "/host/wellness") return { name: "host-wellness" };
  if (path === "/officer/wellness") return { name: "officer-wellness" };
  if (path === "/host/properties") return { name: "property-management" };
  if (path === "/calendar") return { name: "calendar" };
  if (path === "/bookings") return { name: "bookings" };
  if (path === "/payment-confirmation") {
    return { name: "payment", bookingId: search.get("bookingId") ?? undefined };
  }
  if (path === "/profile") return { name: "profile" };
  if (path === "/admin") return { name: "admin" };
  return { name: "home" };
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

function CurrentPage({ auth, route }: { auth: AuthController; route: Route }) {
  switch (route.name) {
    case "explore":
      return <ExplorePage auth={auth} />;
    case "property":
      return <PropertyDetailsPage auth={auth} propertyId={route.propertyId} />;
    case "login":
      return <AuthPage auth={auth} mode="login" />;
    case "register":
      return <AuthPage auth={auth} mode="register" />;
    case "guest-dashboard":
      return <GuestDashboardPage auth={auth} />;
    case "host-dashboard":
      return <HostDashboardPage auth={auth} />;
    case "host-wellness":
      return <HostWellnessPage auth={auth} />;
    case "officer-wellness":
      return <OfficerWellnessPage />;
    case "property-management":
      return <PropertyManagementPage auth={auth} />;
    case "calendar":
      return <CalendarPage />;
    case "bookings":
      return <BookingManagementPage />;
    case "payment":
      return <PaymentConfirmationPage bookingId={route.bookingId} />;
    case "profile":
      return <ProfileSettingsPage auth={auth} />;
    case "admin":
      return <AdminPage />;
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
    <div className={`app-shell route-${route.name}`}>
      <Navbar auth={auth} route={route} />
      <main>
        <CurrentPage auth={auth} route={route} />
      </main>
    </div>
  );
}
