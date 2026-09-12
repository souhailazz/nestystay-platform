import type { UserRole } from "../lib/api";

export const USER_ROLES = [
  "Guest",
  "Host",
  "PropertyManager",
  "Owner",
  "ServiceProvider",
  "LocalBusiness",
  "Officer",
  "Admin",
] as const satisfies readonly UserRole[];

export type AuthMode = "public" | "authenticated" | "role";
export type ScreenType = "production" | "internal" | "error" | "preview";
export type ShellMode = "public" | "workspace" | "minimal";

type RouteData =
  | { name: "home" }
  | { name: "explore" }
  | { name: "map-search" }
  | { name: "coming-soon" }
  | { name: "public-content"; slug: string }
  | { name: "auth-spec"; kind: string }
  | { name: "owner-invitation" }
  | { name: "experiences"; slug?: string }
  | { name: "journal"; slug?: string }
  | { name: "booking-state"; state: string; bookingId?: string }
  | { name: "traveler-spec"; view: string }
  | { name: "qr-gate" }
  | { name: "messages"; conversationId?: string }
  | { name: "directory-spec"; kind?: string; slug?: string }
  | { name: "host-profile"; slug?: string; edit?: boolean }
  | { name: "host-spec"; view: string; propertyId?: string }
  | { name: "admin-ops"; view: string }
  | { name: "property"; propertyId?: string }
  | { name: "login" }
  | { name: "register" }
  | { name: "passwordless-complete" }
  | { name: "auth-post" }
  | { name: "logout" }
  | { name: "guest-dashboard" }
  | { name: "trav-suggestions" }
  | { name: "host-dashboard" }
  | { name: "host-wellness" }
  | { name: "officer-directory" }
  | { name: "wellness-booking" }
  | { name: "officer-wellness" }
  | { name: "property-management" }
  | { name: "host-property-edit" }
  | { name: "pm-gates" }
  | { name: "pm-dashboard" }
  | { name: "pm-invoices" }
  | { name: "pm-maintenance" }
  | { name: "pm-governance" }
  | { name: "pm-documents" }
  | { name: "pm-payments" }
  | { name: "pm-vendors" }
  | { name: "pm-community" }
  | { name: "pm-subscription" }
  | { name: "pm-calendar" }
  | { name: "pm-work-orders" }
  | { name: "pm-agreements" }
  | { name: "pm-approvals" }
  | { name: "pm-team" }
  | { name: "pm-inspections" }
  | { name: "pm-cleaning" }
  | { name: "owner-dashboard" }
  | { name: "pm-gate" }
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

export type Route = RouteData & {
  /** The canonical manifest ID for this URL. Design-screen routes replace this with the requested screen ID. */
  screenId: string;
  /** The canonical path template that owns the current URL. */
  canonicalPath: string;
};

type PathParams = Readonly<Record<string, string>>;
type RouteFactory = (path: string, search: URLSearchParams, params: PathParams) => RouteData;

export type ScreenDefinition = {
  readonly id: string;
  readonly canonicalPath: string;
  readonly aliases?: readonly string[];
  readonly patterns: readonly string[];
  readonly roleAccess: readonly UserRole[];
  readonly auth: AuthMode;
  readonly title: string;
  readonly productArea: string;
  readonly componentKey: Route["name"];
  readonly shell: ShellMode;
  readonly showPublicNav?: boolean;
  readonly screenType: ScreenType;
  readonly navigation?: { readonly label: string; readonly description?: string };
  readonly mobileNavigation?: { readonly label: string; readonly priority: number };
  readonly figmaRef?: string;
  readonly createRoute: RouteFactory;
};

const ALL_ROLES = [...USER_ROLES] as readonly UserRole[];
const SAMPLE_PROPERTY_ID = "22222222-2222-4222-8222-222222222222";

function publicDefinition(
  definition: Omit<ScreenDefinition, "auth" | "roleAccess" | "shell" | "screenType"> & Partial<Pick<ScreenDefinition, "screenType" | "shell">>,
): ScreenDefinition {
  return { ...definition, auth: "public", roleAccess: ALL_ROLES, shell: definition.shell ?? "public", screenType: definition.screenType ?? "production" };
}

function workspaceDefinition(
  definition: Omit<ScreenDefinition, "auth" | "roleAccess" | "shell" | "screenType"> & Pick<ScreenDefinition, "roleAccess"> & Partial<Pick<ScreenDefinition, "screenType" | "shell">>,
): ScreenDefinition {
  return { ...definition, auth: "role", shell: "workspace", screenType: definition.screenType ?? "production" };
}

function authenticatedDefinition(
  definition: Omit<ScreenDefinition, "auth" | "roleAccess" | "shell" | "screenType"> & Partial<Pick<ScreenDefinition, "screenType" | "shell">>,
): ScreenDefinition {
  return { ...definition, auth: "authenticated", roleAccess: ALL_ROLES, shell: "workspace", screenType: definition.screenType ?? "production" };
}

type ScreenMetadata = Omit<ScreenDefinition, "id" | "canonicalPath" | "patterns" | "createRoute" | "auth" | "roleAccess" | "shell" | "screenType"> & Partial<Pick<ScreenDefinition, "roleAccess" | "shell" | "screenType">>;
const screen = (id: string, canonicalPath: string, patterns: readonly string[], createRoute: RouteFactory, extra: ScreenMetadata): ScreenDefinition => {
  const { roleAccess = ALL_ROLES, shell = "public", screenType = "production", ...metadata } = extra;
  return { id, canonicalPath, patterns, createRoute, auth: "public", roleAccess, shell, screenType, ...metadata };
};

export const SCREEN_MANIFEST = [
  publicDefinition(screen("INDEX", "/screens", ["/screens"], () => ({ name: "design-screen", screenId: "INDEX" }), { title: "Validation index", productArea: "Internal", componentKey: "design-screen", shell: "minimal", screenType: "internal" })),
  publicDefinition(screen("DS-V2", "/design-system", ["/design-system"], () => ({ name: "design-system" }), { title: "Design system", productArea: "Internal", componentKey: "design-system", screenType: "internal", showPublicNav: true })),
  publicDefinition(screen("PUB-01", "/", ["/"], () => ({ name: "home" }), { title: "NestyStay home", productArea: "Public discovery", componentKey: "home", showPublicNav: true, navigation: { label: "Explore", description: "Find a stay" }, mobileNavigation: { label: "Explore", priority: 1 } })),
  publicDefinition(screen("PUB-02", "/explore", ["/explore"], () => ({ name: "explore" }), { title: "Explore stays", productArea: "Public discovery", componentKey: "explore", showPublicNav: true, navigation: { label: "Explore", description: "Search stays" } })),
  publicDefinition(screen("PUB-MAP", "/explore/map", ["/explore/map"], () => ({ name: "map-search" }), { title: "Map search", productArea: "Public discovery", componentKey: "map-search", showPublicNav: true, screenType: "preview" })),
  publicDefinition(screen("PUB-04", "/properties/:propertyId", ["/properties/:propertyId"], (_, __, params) => ({ name: "property", propertyId: params.propertyId?.startsWith(":") ? SAMPLE_PROPERTY_ID : params.propertyId }), { title: "Property detail", productArea: "Public discovery", componentKey: "property", showPublicNav: true })),
  publicDefinition(screen("PUB-SOON", "/coming-soon", ["/coming-soon"], () => ({ name: "coming-soon" }), { title: "Coming soon", productArea: "Public discovery", componentKey: "coming-soon", showPublicNav: true, screenType: "preview" })),
  publicDefinition(screen("PUB-CONTENT", "/about", ["/about", "/trust", "/help", "/contact", "/terms", "/privacy", "/maintenance", "/help/:slug"], (path) => ({ name: "public-content", slug: path.slice(1) }), { title: "Public content", productArea: "Public content", componentKey: "public-content", showPublicNav: true })),
  publicDefinition(screen("AUTH-01", "/login", ["/login", "/register"], (path) => ({ name: path === "/register" ? "register" : "login" }), { title: "Login and signup", productArea: "Authentication", componentKey: "login", shell: "minimal", showPublicNav: false })),
  publicDefinition(screen("AUTH-FLOW", "/auth/role", ["/auth/role", "/auth/email-verification", "/auth/phone-verification", "/auth/otp", "/auth/forgot-password", "/auth/reset-password", "/auth/2fa-setup", "/auth/recovery-codes", "/auth/social-consent"], (path) => ({ name: "auth-spec", kind: path.split("/").at(-1) ?? "role" }), { title: "Authentication flow", productArea: "Authentication", componentKey: "auth-spec", shell: "minimal", showPublicNav: false })),
  publicDefinition(screen("AUTH-PWLESS", "/auth/passwordless", ["/auth/passwordless"], () => ({ name: "passwordless-complete" }), { title: "Passwordless completion", productArea: "Authentication", componentKey: "passwordless-complete", shell: "minimal", showPublicNav: false })),
  publicDefinition(screen("AUTH-POST", "/auth/post-login-toast", ["/auth/post-login-toast"], () => ({ name: "auth-post" }), { title: "Post-login toast", productArea: "Authentication", componentKey: "auth-post", shell: "minimal", screenType: "internal" })),
  publicDefinition(screen("AUTH-LOGOUT", "/logout", ["/logout"], () => ({ name: "logout" }), { title: "Logout", productArea: "Authentication", componentKey: "logout", shell: "minimal" })),
  publicDefinition(screen("OWNER-INVITE", "/owner/invitation", ["/owner/invitation"], () => ({ name: "owner-invitation" }), { title: "Owner invitation", productArea: "Authentication", componentKey: "owner-invitation", shell: "minimal" })),
  publicDefinition(screen("PUB-EXP", "/experiences", ["/experiences", "/experiences/:slug"], (path, _, params) => ({ name: "experiences", slug: path === "/experiences" ? undefined : params.slug }), { title: "Experiences", productArea: "Public discovery", componentKey: "experiences", showPublicNav: true })),
  publicDefinition(screen("PUB-JOURNAL", "/journal", ["/journal", "/blog", "/journal/:slug", "/blog/:slug"], (path, _, params) => ({ name: "journal", slug: ["/journal", "/blog"].includes(path) ? undefined : params.slug }), { title: "Journal", productArea: "Public content", componentKey: "journal", showPublicNav: true })),
  publicDefinition(screen("BOOK-01", "/booking/:bookingId/review", ["/booking/:bookingId/review"], (_, __, params) => ({ name: "booking-state", bookingId: params.bookingId, state: "review" }), { title: "Booking dates and review", productArea: "Booking", componentKey: "booking-state", showPublicNav: true })),
  publicDefinition(screen("BOOK-02", "/booking/:bookingId/quote", ["/booking/:bookingId/quote"], (_, __, params) => ({ name: "booking-state", bookingId: params.bookingId, state: "quote" }), { title: "Booking quote", productArea: "Booking", componentKey: "booking-state", showPublicNav: true })),
  publicDefinition(screen("BOOK-03", "/booking/:bookingId/identity", ["/booking/:bookingId/identity"], (_, __, params) => ({ name: "booking-state", bookingId: params.bookingId, state: "identity" }), { title: "Booking identity", productArea: "Booking", componentKey: "booking-state", showPublicNav: true })),
  publicDefinition(screen("BOOK-05", "/booking/:bookingId/checkout", ["/booking/:bookingId/checkout"], (_, __, params) => ({ name: "booking-state", bookingId: params.bookingId, state: "checkout" }), { title: "Booking checkout", productArea: "Booking", componentKey: "booking-state", showPublicNav: true })),
  publicDefinition(screen("BOOK-07", "/booking/:bookingId/pending", ["/booking/:bookingId/pending"], (_, __, params) => ({ name: "booking-state", bookingId: params.bookingId, state: "pending" }), { title: "Booking pending", productArea: "Booking", componentKey: "booking-state", showPublicNav: true })),
  publicDefinition(screen("BOOK-CONF", "/booking/:bookingId/success", ["/booking/:bookingId/success", "/booking/:bookingId/failure", "/booking/:bookingId/rejected", "/booking/:bookingId/cancelled", "/booking/:bookingId/invoice", "/booking/:bookingId/receipt"], (path, _, params) => ({ name: "booking-state", bookingId: params.bookingId, state: path.split("/").at(-1) ?? "success" }), { title: "Booking outcome", productArea: "Booking", componentKey: "booking-state", showPublicNav: true })),
  publicDefinition(screen("BOOK-FLOW", "/booking/:bookingId/:state", ["/booking/:bookingId/:state", "/booking/:bookingId"], (path, _, params) => ({ name: "booking-state", bookingId: params.bookingId, state: params.state ?? "review" }), { title: "Booking state", productArea: "Booking", componentKey: "booking-state", showPublicNav: true, screenType: "internal" })),
  authenticatedDefinition(screen("TRAV-01", "/guest-dashboard", ["/guest-dashboard"], () => ({ name: "guest-dashboard" }), { title: "Traveler dashboard", productArea: "Traveler", componentKey: "guest-dashboard", navigation: { label: "Trips", description: "Upcoming and past stays" }, mobileNavigation: { label: "Trips", priority: 2 } })),
  authenticatedDefinition(screen("TRAV-RES", "/traveler/reservations", ["/traveler/reservations", "/traveler/reservations/upcoming", "/traveler/reservations/past", "/traveler/reservations/cancelled", "/traveler/reservations/:reservationId"], (path, _, params) => ({ name: "traveler-spec", view: path.endsWith("/past") ? "reservations-past" : path.endsWith("/cancelled") ? "reservations-cancelled" : path === "/traveler/reservations" || path.endsWith("/upcoming") ? "reservations-upcoming" : `reservation-detail:${params.reservationId ?? ""}` }), { title: "Traveler reservations", productArea: "Traveler", componentKey: "traveler-spec", navigation: { label: "Trips", description: "Reservations and gate passes" } })),
  authenticatedDefinition(screen("TRAV-QR", "/traveler/qr", ["/traveler/qr", "/traveler/qr/:bookingId"], () => ({ name: "traveler-spec", view: "qr" }), { title: "Traveler QR pass", productArea: "Traveler", componentKey: "traveler-spec" })),
  authenticatedDefinition(screen("TRAV-12", "/traveler/preferences", ["/traveler/preferences"], () => ({ name: "traveler-spec", view: "preferences" }), { title: "Traveler preferences", productArea: "Traveler", componentKey: "traveler-spec", navigation: { label: "Profile", description: "Profile, preferences, and security" }, mobileNavigation: { label: "Profile", priority: 5 } })),
  authenticatedDefinition(screen("TRAV-COL", "/traveler/favorites", ["/traveler/favorites", "/wishlist"], () => ({ name: "traveler-spec", view: "wishlist" }), { title: "Traveler collections", productArea: "Traveler", componentKey: "traveler-spec", navigation: { label: "Saved", description: "Saved stays and collections" }, mobileNavigation: { label: "Saved", priority: 3 } })),
  authenticatedDefinition(screen("TRAV-INV", "/traveler/invoices", ["/traveler/invoices", "/traveler/payments"], (path) => ({ name: "traveler-spec", view: path.endsWith("/payments") ? "payment-history" : "invoices" }), { title: "Traveler invoices", productArea: "Traveler", componentKey: "traveler-spec", navigation: { label: "Invoices", description: "Invoices and payment history" } })),
  authenticatedDefinition(screen("TRAV-PAY", "/traveler/payment-methods", ["/traveler/payment-methods"], () => ({ name: "traveler-spec", view: "payment-methods" }), { title: "Payment methods", productArea: "Traveler", componentKey: "traveler-spec" })),
  authenticatedDefinition(screen("TRAV-ID", "/traveler/identity", ["/traveler/identity"], () => ({ name: "traveler-spec", view: "identity" }), { title: "Identity verification", productArea: "Traveler", componentKey: "traveler-spec" })),
  authenticatedDefinition(screen("TRAV-REV", "/traveler/reviews/pending", ["/traveler/reviews/pending", "/traveler/reviews", "/traveler/reviews/given"], (path) => ({ name: "traveler-spec", view: path.endsWith("/given") ? "reviews-given" : "reviews-pending" }), { title: "Traveler reviews", productArea: "Traveler", componentKey: "traveler-spec", navigation: { label: "Reviews", description: "Pending and given reviews" } })),
  authenticatedDefinition(screen("TRAV-NOTIF", "/traveler/notifications", ["/traveler/notifications", "/notifications"], () => ({ name: "traveler-spec", view: "notifications" }), { title: "Traveler notifications", productArea: "Traveler", componentKey: "traveler-spec", navigation: { label: "Alerts", description: "Notifications and read state" }, mobileNavigation: { label: "Alerts", priority: 4 } })),
  authenticatedDefinition(screen("TRAV-SUGG", "/traveler/suggestions", ["/traveler/suggestions"], () => ({ name: "trav-suggestions" }), { title: "Trip suggestions", productArea: "Traveler", componentKey: "trav-suggestions", navigation: { label: "Suggestions", description: "Personalized stay ideas" } })),
  authenticatedDefinition(screen("MSG-DOC", "/messages/document", ["/messages/document"], () => ({ name: "messages", conversationId: undefined }), { title: "Secure document messages", productArea: "Messaging", componentKey: "messages", screenType: "production" })),
  authenticatedDefinition(screen("MSG-01", "/messages", ["/messages", "/messages/:conversationId"], (_, __, params) => ({ name: "messages", conversationId: params.conversationId }), { title: "Messages", productArea: "Messaging", componentKey: "messages", navigation: { label: "Messages", description: "Inbox and conversations" }, mobileNavigation: { label: "Messages", priority: 4 } })),
  publicDefinition(screen("DIR-02", "/directory/trades", ["/directory/trades", "/directory/custodians", "/directory/police", "/directory/guest-verification", "/directory/provider/onboarding"], (path) => {
    if (path === "/directory/custodians") return { name: "directory-spec", kind: "Custodian" };
    if (path === "/directory/police") return { name: "directory-spec", kind: "Police" };
    if (path === "/directory/guest-verification") return { name: "directory-spec", kind: "Verification" };
    if (path === "/directory/provider/onboarding") return { name: "directory-spec", kind: "Provider" };
    return { name: "directory-spec", kind: "Trades" };
  }, { title: "Directories and providers", productArea: "Directories", componentKey: "directory-spec", showPublicNav: true, navigation: { label: "Directories", description: "Verified local providers" } })),
  publicDefinition(screen("DIR-BIZ", "/directory/businesses", ["/directory/businesses"], () => ({ name: "business-directory" }), { title: "Local business directory", productArea: "Directories", componentKey: "business-directory", showPublicNav: true })),
  publicDefinition(screen("DIR-PROFILE", "/directory/providers/:slug", ["/directory/providers/:slug"], (_, __, params) => ({ name: "directory-spec", slug: params.slug }), { title: "Provider profile", productArea: "Directories", componentKey: "directory-spec", showPublicNav: true })),
  workspaceDefinition(screen("DIR-PROV", "/directory/provider", ["/directory/provider"], () => ({ name: "provider-dashboard" }), { title: "Provider dashboard", productArea: "Directories", componentKey: "provider-dashboard", roleAccess: ["ServiceProvider", "LocalBusiness"], screenType: "production" })),
  publicDefinition(screen("HOST-PROFILE", "/hosts", ["/hosts", "/hosts/:slug"], (path, _, params) => ({ name: "host-profile", slug: path === "/hosts" ? undefined : params.slug }), { title: "Host profile directory", productArea: "Host", componentKey: "host-profile", showPublicNav: true, screenType: "production" })),
  workspaceDefinition(screen("HOST-PROFILE-EDIT", "/host/profile/edit", ["/host/profile/edit", "/host/profile/preview"], (path) => ({ name: "host-profile", edit: path.endsWith("/edit"), slug: path.endsWith("/preview") ? "my-host-profile" : undefined }), { title: "Host profile editor", productArea: "Host", componentKey: "host-profile", roleAccess: ["Host"], navigation: { label: "Profile", description: "Public host profile" } })),
  workspaceDefinition(screen("HOST-01", "/host-dashboard", ["/host-dashboard"], () => ({ name: "host-dashboard" }), { title: "Host dashboard", productArea: "Host", componentKey: "host-dashboard", roleAccess: ["Host"], navigation: { label: "Home", description: "Host performance and tasks" }, mobileNavigation: { label: "Home", priority: 1 } })),
  workspaceDefinition(screen("HOST-05", "/host/properties", ["/host/properties"], () => ({ name: "property-management" }), { title: "Host properties", productArea: "Host", componentKey: "property-management", roleAccess: ["Host"], navigation: { label: "Listings", description: "Create, edit, and publish listings" }, mobileNavigation: { label: "Listings", priority: 3 } })),
  workspaceDefinition(screen("HOST-EDIT", "/host/properties/edit", ["/host/properties/edit"], (_, search) => ({ name: "host-spec", view: "properties-edit", propertyId: search.get("id") ?? undefined }), { title: "Host property editor", productArea: "Host", componentKey: "host-spec", roleAccess: ["Host"] })),
  workspaceDefinition(screen("HOST-NEW", "/host/properties/new", ["/host/properties/new"], () => ({ name: "host-spec", view: "properties-new" }), { title: "Host property wizard", productArea: "Host", componentKey: "host-spec", roleAccess: ["Host"] })),
  workspaceDefinition(screen("HOST-OPS", "/host/analytics", ["/host/analytics", "/host/pricing", "/host/promotions", "/host/exports", "/host/reviews", "/host/badges", "/host/settings", "/host/properties/archived"], (path) => ({ name: "host-spec", view: path.split("/").at(-1) === "archived" ? "archived" : path.split("/").at(-1) ?? "analytics" }), { title: "Host operations", productArea: "Host", componentKey: "host-spec", roleAccess: ["Host"], navigation: { label: "Host tools", description: "Pricing, reviews, badges, and reports" } })),
  workspaceDefinition(screen("HOST-RPT", "/host/reports", ["/host/reports"], () => ({ name: "host-spec", view: "reports" }), { title: "Host reports", productArea: "Host", componentKey: "host-spec", roleAccess: ["Host"] })),
  workspaceDefinition(screen("HOST-WELL", "/host/wellness", ["/host/wellness"], () => ({ name: "host-wellness" }), { title: "Host wellness", productArea: "Wellness", componentKey: "host-wellness", roleAccess: ["Host"], navigation: { label: "Wellness", description: "Visits and safety reports" } })),
  workspaceDefinition(screen("OFC-DIR", "/host/wellness/directory", ["/host/wellness/directory"], () => ({ name: "officer-directory" }), { title: "Wellness directory", productArea: "Wellness", componentKey: "officer-directory", roleAccess: ["Host", "Officer"] })),
  workspaceDefinition(screen("OFC-BOOK", "/host/wellness/book", ["/host/wellness/book"], () => ({ name: "wellness-booking" }), { title: "Wellness booking", productArea: "Wellness", componentKey: "wellness-booking", roleAccess: ["Host"] })),
  workspaceDefinition(screen("OFC-01", "/officer/wellness", ["/officer/wellness"], () => ({ name: "officer-wellness" }), { title: "Officer wellness workspace", productArea: "Wellness", componentKey: "officer-wellness", roleAccess: ["Officer"], navigation: { label: "Assignments", description: "Officer visits and reports" }, mobileNavigation: { label: "Assignments", priority: 1 } })),
  workspaceDefinition(screen("PM-GATE", "/pm/gates", ["/pm/gates"], () => ({ name: "pm-gates" }), { title: "Gate communications", productArea: "Property Manager", componentKey: "pm-gates", roleAccess: ["PropertyManager"], navigation: { label: "Gates", description: "Gate messages and QR access" } })),
  workspaceDefinition(screen("PM-DASH", "/pm/dashboard", ["/pm/dashboard"], () => ({ name: "pm-dashboard" }), { title: "Property manager dashboard", productArea: "Property Manager", componentKey: "pm-dashboard", roleAccess: ["PropertyManager"], navigation: { label: "Home", description: "Portfolio overview" }, mobileNavigation: { label: "Home", priority: 1 } })),
  workspaceDefinition(screen("PM-INV", "/pm/invoices", ["/pm/invoices"], () => ({ name: "pm-invoices" }), { title: "Property manager invoices", productArea: "Property Manager", componentKey: "pm-invoices", roleAccess: ["PropertyManager"], navigation: { label: "Invoices", description: "Issue and reconcile invoices" } })),
  workspaceDefinition(screen("PM-MAINT", "/pm/maintenance", ["/pm/maintenance"], () => ({ name: "pm-maintenance" }), { title: "Property manager maintenance", productArea: "Property Manager", componentKey: "pm-maintenance", roleAccess: ["PropertyManager"], navigation: { label: "Work", description: "Maintenance and work orders" }, mobileNavigation: { label: "Work", priority: 3 } })),
  workspaceDefinition(screen("PM-GOV", "/pm/governance", ["/pm/governance"], () => ({ name: "pm-governance" }), { title: "Governance", productArea: "Property Manager", componentKey: "pm-governance", roleAccess: ["PropertyManager"] })),
  workspaceDefinition(screen("PM-DOCS", "/pm/documents", ["/pm/documents"], () => ({ name: "pm-documents" }), { title: "Documents", productArea: "Property Manager", componentKey: "pm-documents", roleAccess: ["PropertyManager"] })),
  workspaceDefinition(screen("PM-PAY", "/pm/payments", ["/pm/payments"], () => ({ name: "pm-payments" }), { title: "Payments", productArea: "Property Manager", componentKey: "pm-payments", roleAccess: ["PropertyManager"] })),
  workspaceDefinition(screen("PM-VEND", "/pm/vendors", ["/pm/vendors"], () => ({ name: "pm-vendors" }), { title: "Vendors", productArea: "Property Manager", componentKey: "pm-vendors", roleAccess: ["PropertyManager"] })),
  workspaceDefinition(screen("PM-COMM", "/pm/community", ["/pm/community"], () => ({ name: "pm-community" }), { title: "Community", productArea: "Property Manager", componentKey: "pm-community", roleAccess: ["PropertyManager"] })),
  workspaceDefinition(screen("PM-SUB", "/pm/subscription", ["/pm/subscription"], () => ({ name: "pm-subscription" }), { title: "Subscription", productArea: "Property Manager", componentKey: "pm-subscription", roleAccess: ["PropertyManager"] })),
  workspaceDefinition(screen("PM-CAL", "/pm/calendar", ["/pm/calendar"], () => ({ name: "pm-calendar" }), { title: "Property manager calendar", productArea: "Property Manager", componentKey: "pm-calendar", roleAccess: ["PropertyManager"], navigation: { label: "Calendar", description: "Portfolio agenda" }, mobileNavigation: { label: "Calendar", priority: 2 } })),
  workspaceDefinition(screen("PM-WO", "/pm/work-orders", ["/pm/work-orders"], () => ({ name: "pm-work-orders" }), { title: "Work orders", productArea: "Property Manager", componentKey: "pm-work-orders", roleAccess: ["PropertyManager"] })),
  workspaceDefinition(screen("PM-AGR", "/pm/agreements", ["/pm/agreements"], () => ({ name: "pm-agreements" }), { title: "Agreements", productArea: "Property Manager", componentKey: "pm-agreements", roleAccess: ["PropertyManager"] })),
  workspaceDefinition(screen("PM-APP", "/pm/approvals", ["/pm/approvals"], () => ({ name: "pm-approvals" }), { title: "Approvals", productArea: "Property Manager", componentKey: "pm-approvals", roleAccess: ["PropertyManager"] })),
  workspaceDefinition(screen("PM-TEAM", "/pm/team", ["/pm/team"], () => ({ name: "pm-team" }), { title: "Team", productArea: "Property Manager", componentKey: "pm-team", roleAccess: ["PropertyManager"] })),
  workspaceDefinition(screen("PM-INS", "/pm/inspections", ["/pm/inspections"], () => ({ name: "pm-inspections" }), { title: "Inspections", productArea: "Property Manager", componentKey: "pm-inspections", roleAccess: ["PropertyManager"] })),
  workspaceDefinition(screen("PM-CLEAN", "/pm/cleaning", ["/pm/cleaning"], () => ({ name: "pm-cleaning" }), { title: "Cleaning", productArea: "Property Manager", componentKey: "pm-cleaning", roleAccess: ["PropertyManager"] })),
  workspaceDefinition(screen("PM-UTIL", "/pm/utilities", ["/pm/utilities"], () => ({ name: "pm-utilities" }), { title: "Utilities", productArea: "Property Manager", componentKey: "pm-utilities", roleAccess: ["PropertyManager"] })),
  workspaceDefinition(screen("PM-VERIFY", "/pm/verification", ["/pm/verification"], () => ({ name: "pm-verification" }), { title: "Tenant verification", productArea: "Property Manager", componentKey: "pm-verification", roleAccess: ["PropertyManager"] })),
  workspaceDefinition(screen("PM-RPT", "/pm/reports", ["/pm/reports"], () => ({ name: "pm-reports" }), { title: "Portfolio reports", productArea: "Property Manager", componentKey: "pm-reports", roleAccess: ["PropertyManager"], navigation: { label: "Reports", description: "Portfolio reporting" } })),
  publicDefinition(screen("PM-GUARD", "/gate", ["/gate", "/gate/qr"], (path) => path === "/gate/qr" ? ({ name: "qr-gate" }) : ({ name: "pm-gate" }), { title: "Gate validator", productArea: "Property Manager", componentKey: "pm-gate", showPublicNav: false })),
  workspaceDefinition(screen("PM-INSURANCE", "/pm/insurance", ["/pm/insurance"], () => ({ name: "pm-insurance" }), { title: "Coverage readiness", productArea: "Property Manager", componentKey: "pm-insurance", roleAccess: ["PropertyManager"], screenType: "production" })),
  workspaceDefinition(screen("OWNER-01", "/owner/dashboard", ["/owner/dashboard"], () => ({ name: "owner-dashboard" }), { title: "Owner portal", productArea: "Owner", componentKey: "owner-dashboard", roleAccess: ["Owner"], navigation: { label: "Home", description: "Assigned units and statements" }, mobileNavigation: { label: "Home", priority: 1 } })),
  workspaceDefinition(screen("CAL-01", "/calendar", ["/calendar"], () => ({ name: "calendar" }), { title: "Host calendar", productArea: "Host", componentKey: "calendar", roleAccess: ["Host"], navigation: { label: "Calendar", description: "Availability and feeds" }, mobileNavigation: { label: "Calendar", priority: 2 } })),
  workspaceDefinition(screen("BOOKINGS-01", "/bookings", ["/bookings"], () => ({ name: "bookings" }), { title: "Booking management", productArea: "Host", componentKey: "bookings", roleAccess: ["Host"], navigation: { label: "Reservations", description: "Booking review and payment" } })),
  authenticatedDefinition(screen("PAYMENT-01", "/payment-confirmation", ["/payment-confirmation"], (_, search) => ({ name: "payment", bookingId: search.get("bookingId") ?? undefined }), { title: "Payment confirmation", productArea: "Booking", componentKey: "payment", shell: "minimal" })),
  workspaceDefinition(screen("PROFILE-01", "/profile", ["/profile"], () => ({ name: "profile" }), { title: "Profile settings", productArea: "Account", componentKey: "profile", roleAccess: ALL_ROLES, navigation: { label: "Settings", description: "Account and security" } })),
  workspaceDefinition(screen("DIR-ADM", "/admin/ops/directory", ["/admin/ops/directory"], () => ({ name: "admin-ops", view: "directory" }), { title: "Directory moderation", productArea: "Admin", componentKey: "admin-ops", roleAccess: ["Admin"], screenType: "production" })),
  workspaceDefinition(screen("ADM-WELLNESS", "/admin/ops/wellness", ["/admin/ops/wellness"], () => ({ name: "admin-ops", view: "wellness" }), { title: "Wellness operations", productArea: "Admin", componentKey: "admin-ops", roleAccess: ["Admin"], screenType: "production" })),
  workspaceDefinition(screen("ADM-BADGES", "/admin/ops/badges", ["/admin/ops/badges"], () => ({ name: "admin-ops", view: "badges" }), { title: "Badge management", productArea: "Admin", componentKey: "admin-ops", roleAccess: ["Admin"], navigation: { label: "Admin", description: "Operations and controls" } })),
  workspaceDefinition(screen("ADM-01", "/admin/ops/disputes", ["/admin/ops/:view"], (path) => ({ name: "admin-ops", view: path.split("/").at(-1) ?? "disputes" }), { title: "Admin operations", componentKey: "admin-ops", productArea: "Admin", roleAccess: ["Admin"], navigation: { label: "Operations", description: "Moderation and queues" }, mobileNavigation: { label: "Queues", priority: 2 } })),
  workspaceDefinition(screen("ADM-ROOT", "/admin", ["/admin"], () => ({ name: "admin" }), { title: "Admin overview", productArea: "Admin", componentKey: "admin", roleAccess: ["Admin"], navigation: { label: "Admin", description: "System overview" }, mobileNavigation: { label: "Home", priority: 1 } })),
  workspaceDefinition(screen("ADM-KPI", "/admin/kpis", ["/admin/kpis"], () => ({ name: "admin-kpis" }), { title: "Admin KPIs", productArea: "Admin", componentKey: "admin-kpis", roleAccess: ["Admin"], screenType: "production" })),
  workspaceDefinition(screen("ADM-RPT", "/admin/reports", ["/admin/reports"], () => ({ name: "admin-reports" }), { title: "Admin reports", productArea: "Admin", componentKey: "admin-reports", roleAccess: ["Admin"], navigation: { label: "Audit", description: "Reports and activity" }, mobileNavigation: { label: "Audit", priority: 3 }, screenType: "production" })),
  workspaceDefinition(screen("ADM-RESET", "/admin/officer-id-reset", ["/admin/officer-id-reset"], () => ({ name: "officer-id-reset" }), { title: "Officer ID reset", productArea: "Admin", componentKey: "officer-id-reset", roleAccess: ["Admin"], screenType: "preview" })),
  publicDefinition(screen("ERR-401", "/401", ["/401"], () => ({ name: "sign-in-required" }), { title: "Sign in required", productArea: "System states", componentKey: "sign-in-required", shell: "minimal", screenType: "error" })),
  publicDefinition(screen("ERR-403", "/403", ["/403"], () => ({ name: "access-restricted" }), { title: "Access restricted", productArea: "System states", componentKey: "access-restricted", shell: "minimal", screenType: "error" })),
  publicDefinition(screen("ERR-404", "/404", ["/404"], () => ({ name: "not-found" }), { title: "Not found", productArea: "System states", componentKey: "not-found", shell: "minimal", screenType: "error" })),
  publicDefinition(screen("ERR-500", "/500", ["/500"], () => ({ name: "server-error" }), { title: "Server error", productArea: "System states", componentKey: "server-error", shell: "minimal", screenType: "error" })),
  publicDefinition(screen("ERR-LOAD", "/loading", ["/loading"], () => ({ name: "loading-state" }), { title: "Loading state", productArea: "System states", componentKey: "loading-state", shell: "minimal", screenType: "internal" })),
  authenticatedDefinition(screen("ERR-NOFAV", "/empty/favorites", ["/empty/favorites"], () => ({ name: "no-favorites" }), { title: "No favorites", productArea: "System states", componentKey: "no-favorites", screenType: "error" })),
  authenticatedDefinition(screen("ERR-NORES", "/empty/reservations", ["/empty/reservations"], () => ({ name: "no-reservations" }), { title: "No reservations", productArea: "System states", componentKey: "no-reservations", screenType: "error" })),
  publicDefinition(screen("SCREEN-ROUTE", "/screens/:screenId", ["/screens/:screenId"], (_, __, params) => ({ name: "design-screen", screenId: params.screenId ?? "PUB-01" }), { title: "Internal screen preview", productArea: "Internal", componentKey: "design-screen", shell: "minimal", screenType: "internal" })),
] as const satisfies readonly ScreenDefinition[];

export type ScreenId = typeof SCREEN_MANIFEST[number]["id"];

const byId = new Map(SCREEN_MANIFEST.map((definition) => [definition.id, definition]));

function normalizePath(path: string) {
  return path.replace(/\/+$/, "") || "/";
}

function matchPattern(pattern: string, path: string): PathParams | null {
  const patternParts = normalizePath(pattern).split("/").filter(Boolean);
  const pathParts = normalizePath(path).split("/").filter(Boolean);
  const wildcard = patternParts.at(-1) === "*";
  if ((!wildcard && patternParts.length !== pathParts.length) || (wildcard && pathParts.length < patternParts.length - 1)) return null;

  const params: Record<string, string> = {};
  for (let index = 0; index < patternParts.length; index += 1) {
    const expected = patternParts[index];
    if (expected === "*") break;
    const actual = pathParts[index];
    if (expected.startsWith(":")) {
      if (!actual) return null;
      params[expected.slice(1)] = decodeURIComponent(actual);
    } else if (expected !== actual) {
      return null;
    }
  }
  return params;
}

function decorate(definition: ScreenDefinition, data: RouteData): Route {
  const screenId = data.name === "design-screen" ? data.screenId : definition.id;
  return { ...data, screenId, canonicalPath: definition.canonicalPath } as Route;
}

export function parseRoute(pathname = window.location.pathname, search = new URLSearchParams(window.location.search)): Route {
  const path = normalizePath(pathname);
  for (const definition of SCREEN_MANIFEST) {
    for (const pattern of definition.patterns) {
      const params = matchPattern(pattern, path);
      if (params) return decorate(definition, definition.createRoute(path, search, params));
    }
  }
  const notFound = byId.get("ERR-404")!;
  return decorate(notFound, { name: "not-found" });
}

export function routeForScreenId(screenId: string): Route | undefined {
  const definition = byId.get(screenId);
  if (!definition) return undefined;
  const params = matchPattern(definition.patterns[0], definition.canonicalPath) ?? {};
  return decorate(definition, definition.createRoute(definition.canonicalPath, new URLSearchParams(), params));
}

export function getScreenDefinition(screenId: string) {
  return byId.get(screenId);
}

export function getRouteDefinition(route: Route) {
  if (route.name === "design-screen") return SCREEN_MANIFEST.find((definition) => definition.canonicalPath === route.canonicalPath);
  return byId.get(route.screenId) ?? SCREEN_MANIFEST.find((definition) => definition.componentKey === route.name);
}

export function isWorkspaceRoute(route: Route) {
  return getRouteDefinition(route)?.shell === "workspace";
}

export function hasPublicNav(route: Route) {
  return getRouteDefinition(route)?.showPublicNav === true;
}

export type RouteAccess = { kind: "allowed" } | { kind: "auth-required"; returnTo: string } | { kind: "forbidden"; requiredRoles: readonly UserRole[] };

export function getRouteAccess(route: Route, session: { roles?: readonly UserRole[] } | null | undefined): RouteAccess {
  const definition = getRouteDefinition(route);
  if (!definition || definition.auth === "public") return { kind: "allowed" };
  if (!session) return { kind: "auth-required", returnTo: `${window.location.pathname}${window.location.search}${window.location.hash}` };
  if (definition.auth === "role" && !definition.roleAccess.some((role) => session.roles?.includes(role))) {
    return { kind: "forbidden", requiredRoles: definition.roleAccess };
  }
  return { kind: "allowed" };
}

export type NavigationItem = {
  readonly label: string;
  readonly href: string;
  readonly screenId: string;
  readonly description?: string;
  readonly roles: readonly UserRole[];
  readonly mobile?: boolean;
  readonly mobilePriority?: number;
  readonly more?: boolean;
};

const navigationItem = (screenId: string, label: string, href: string, roles: readonly UserRole[], options: Omit<NavigationItem, "screenId" | "label" | "href" | "roles"> = {}): NavigationItem => ({ screenId, label, href, roles, ...options });

export const PUBLIC_NAVIGATION: readonly NavigationItem[] = [
  navigationItem("PUB-02", "Explore", "/explore", ALL_ROLES),
  navigationItem("HOST-01", "Host", "/host-dashboard", ALL_ROLES),
  navigationItem("HOST-WELL", "Wellness", "/host/wellness", ALL_ROLES),
];

export const ROLE_NAVIGATION: Record<UserRole, { desktop: readonly NavigationItem[]; mobile: readonly NavigationItem[] }> = {
  Guest: {
    desktop: [
      navigationItem("TRAV-01", "Trips", "/guest-dashboard", ["Guest"]),
      navigationItem("TRAV-SUGG", "Suggestions", "/traveler/suggestions", ["Guest"]),
      navigationItem("TRAV-COL", "Saved", "/traveler/favorites", ["Guest"]),
      navigationItem("TRAV-INV", "Invoices", "/traveler/invoices", ["Guest"]),
      navigationItem("MSG-01", "Messages", "/messages", ["Guest"]),
      navigationItem("TRAV-NOTIF", "Alerts", "/traveler/notifications", ["Guest"]),
      navigationItem("PROFILE-01", "Settings", "/profile", ["Guest"]),
      navigationItem("DIR-02", "Directories", "/directory/trades", ["Guest"]),
    ],
    mobile: [
      navigationItem("PUB-01", "Explore", "/explore", ["Guest"], { mobile: true, mobilePriority: 1 }),
      navigationItem("TRAV-01", "Trips", "/guest-dashboard", ["Guest"], { mobile: true, mobilePriority: 2 }),
      navigationItem("TRAV-COL", "Saved", "/traveler/favorites", ["Guest"], { mobile: true, mobilePriority: 3 }),
      navigationItem("MSG-01", "Messages", "/messages", ["Guest"], { mobile: true, mobilePriority: 4 }),
      navigationItem("PROFILE-01", "Profile", "/profile", ["Guest"], { mobile: true, mobilePriority: 5 }),
    ],
  },
  Host: {
    desktop: [
      navigationItem("HOST-01", "Home", "/host-dashboard", ["Host"]),
      navigationItem("HOST-05", "Listings", "/host/properties", ["Host"]),
      navigationItem("BOOKINGS-01", "Reservations", "/bookings", ["Host"]),
      navigationItem("CAL-01", "Calendar", "/calendar", ["Host"]),
      navigationItem("HOST-WELL", "Wellness", "/host/wellness", ["Host"]),
      navigationItem("MSG-01", "Messages", "/messages", ["Host"]),
      navigationItem("TRAV-NOTIF", "Alerts", "/traveler/notifications", ["Host"]),
      navigationItem("DIR-02", "Directories", "/directory/trades", ["Host"]),
      navigationItem("PROFILE-01", "Settings", "/profile", ["Host"]),
    ],
    mobile: [
      navigationItem("HOST-01", "Home", "/host-dashboard", ["Host"], { mobile: true, mobilePriority: 1 }),
      navigationItem("CAL-01", "Calendar", "/calendar", ["Host"], { mobile: true, mobilePriority: 2 }),
      navigationItem("HOST-05", "Listings", "/host/properties", ["Host"], { mobile: true, mobilePriority: 3 }),
      navigationItem("MSG-01", "Inbox", "/messages", ["Host"], { mobile: true, mobilePriority: 4 }),
      navigationItem("HOST-OPS", "More", "/host-dashboard", ["Host"], { mobile: true, mobilePriority: 5, more: true }),
    ],
  },
  PropertyManager: {
    desktop: [
      navigationItem("PM-DASH", "Home", "/pm/dashboard", ["PropertyManager"]),
      navigationItem("PM-CAL", "Calendar", "/pm/calendar", ["PropertyManager"]),
      navigationItem("PM-MAINT", "Work", "/pm/maintenance", ["PropertyManager"]),
      navigationItem("PM-INV", "Invoices", "/pm/invoices", ["PropertyManager"]),
      navigationItem("PM-PAY", "Payments", "/pm/payments", ["PropertyManager"]),
      navigationItem("PM-GATE", "Gates", "/pm/gates", ["PropertyManager"]),
      navigationItem("PM-RPT", "Reports", "/pm/reports", ["PropertyManager"]),
      navigationItem("TRAV-NOTIF", "Alerts", "/traveler/notifications", ["PropertyManager"]),
      navigationItem("PROFILE-01", "Settings", "/profile", ["PropertyManager"]),
    ],
    mobile: [
      navigationItem("PM-DASH", "Home", "/pm/dashboard", ["PropertyManager"], { mobile: true, mobilePriority: 1 }),
      navigationItem("PM-CAL", "Calendar", "/pm/calendar", ["PropertyManager"], { mobile: true, mobilePriority: 2 }),
      navigationItem("PM-MAINT", "Work", "/pm/maintenance", ["PropertyManager"], { mobile: true, mobilePriority: 3 }),
      navigationItem("TRAV-NOTIF", "Alerts", "/traveler/notifications", ["PropertyManager"], { mobile: true, mobilePriority: 4 }),
      navigationItem("PM-RPT", "More", "/pm/dashboard", ["PropertyManager"], { mobile: true, mobilePriority: 5, more: true }),
    ],
  },
  Owner: {
    desktop: [
      navigationItem("OWNER-01", "Home", "/owner/dashboard", ["Owner"]),
      navigationItem("TRAV-INV", "Statements", "/traveler/invoices", ["Owner"]),
      navigationItem("MSG-01", "Messages", "/messages", ["Owner"]),
      navigationItem("TRAV-NOTIF", "Alerts", "/traveler/notifications", ["Owner"]),
      navigationItem("PROFILE-01", "Settings", "/profile", ["Owner"]),
    ],
    mobile: [
      navigationItem("OWNER-01", "Home", "/owner/dashboard", ["Owner"], { mobile: true, mobilePriority: 1 }),
      navigationItem("TRAV-INV", "Statements", "/traveler/invoices", ["Owner"], { mobile: true, mobilePriority: 2 }),
      navigationItem("TRAV-REV", "Reviews", "/traveler/reviews", ["Owner"], { mobile: true, mobilePriority: 3 }),
      navigationItem("TRAV-NOTIF", "Alerts", "/traveler/notifications", ["Owner"], { mobile: true, mobilePriority: 4 }),
      navigationItem("PROFILE-01", "More", "/profile", ["Owner"], { mobile: true, mobilePriority: 5, more: true }),
    ],
  },
  ServiceProvider: {
    desktop: [
      navigationItem("DIR-PROV", "Provider home", "/directory/provider", ["ServiceProvider"]),
      navigationItem("DIR-02", "Directories", "/directory/trades", ["ServiceProvider"]),
      navigationItem("MSG-01", "Messages", "/messages", ["ServiceProvider"]),
      navigationItem("TRAV-NOTIF", "Alerts", "/traveler/notifications", ["ServiceProvider"]),
      navigationItem("PROFILE-01", "Settings", "/profile", ["ServiceProvider"]),
    ],
    mobile: [
      navigationItem("DIR-PROV", "Home", "/directory/provider", ["ServiceProvider"], { mobile: true, mobilePriority: 1 }),
      navigationItem("DIR-02", "Directory", "/directory/trades", ["ServiceProvider"], { mobile: true, mobilePriority: 2 }),
      navigationItem("MSG-01", "Messages", "/messages", ["ServiceProvider"], { mobile: true, mobilePriority: 3 }),
      navigationItem("TRAV-NOTIF", "Alerts", "/traveler/notifications", ["ServiceProvider"], { mobile: true, mobilePriority: 4 }),
      navigationItem("PROFILE-01", "More", "/profile", ["ServiceProvider"], { mobile: true, mobilePriority: 5, more: true }),
    ],
  },
  LocalBusiness: {
    desktop: [
      navigationItem("DIR-PROV", "Business home", "/directory/provider", ["LocalBusiness"]),
      navigationItem("DIR-BIZ", "Directory", "/directory/businesses", ["LocalBusiness"]),
      navigationItem("MSG-01", "Messages", "/messages", ["LocalBusiness"]),
      navigationItem("TRAV-NOTIF", "Alerts", "/traveler/notifications", ["LocalBusiness"]),
      navigationItem("PROFILE-01", "Settings", "/profile", ["LocalBusiness"]),
    ],
    mobile: [
      navigationItem("DIR-PROV", "Home", "/directory/provider", ["LocalBusiness"], { mobile: true, mobilePriority: 1 }),
      navigationItem("DIR-BIZ", "Directory", "/directory/businesses", ["LocalBusiness"], { mobile: true, mobilePriority: 2 }),
      navigationItem("MSG-01", "Messages", "/messages", ["LocalBusiness"], { mobile: true, mobilePriority: 3 }),
      navigationItem("TRAV-NOTIF", "Alerts", "/traveler/notifications", ["LocalBusiness"], { mobile: true, mobilePriority: 4 }),
      navigationItem("PROFILE-01", "More", "/profile", ["LocalBusiness"], { mobile: true, mobilePriority: 5, more: true }),
    ],
  },
  Officer: {
    desktop: [
      navigationItem("OFC-01", "Assignments", "/officer/wellness", ["Officer"]),
      navigationItem("OFC-DIR", "Directory", "/host/wellness/directory", ["Officer"]),
      navigationItem("MSG-01", "Messages", "/messages", ["Officer"]),
      navigationItem("TRAV-NOTIF", "Alerts", "/traveler/notifications", ["Officer"]),
      navigationItem("PROFILE-01", "Settings", "/profile", ["Officer"]),
    ],
    mobile: [
      navigationItem("OFC-01", "Home", "/officer/wellness", ["Officer"], { mobile: true, mobilePriority: 1 }),
      navigationItem("OFC-DIR", "Assignments", "/host/wellness/directory", ["Officer"], { mobile: true, mobilePriority: 2 }),
      navigationItem("MSG-01", "Messages", "/messages", ["Officer"], { mobile: true, mobilePriority: 3 }),
      navigationItem("TRAV-NOTIF", "Alerts", "/traveler/notifications", ["Officer"], { mobile: true, mobilePriority: 4 }),
      navigationItem("PROFILE-01", "More", "/profile", ["Officer"], { mobile: true, mobilePriority: 5, more: true }),
    ],
  },
  Admin: {
    desktop: [
      navigationItem("ADM-ROOT", "Home", "/admin", ["Admin"]),
      navigationItem("ADM-01", "Queues", "/admin/ops/disputes", ["Admin"]),
      navigationItem("ADM-RPT", "Audit", "/admin/reports", ["Admin"]),
      navigationItem("ADM-BADGES", "Configuration", "/admin/ops/badges", ["Admin"]),
      navigationItem("TRAV-NOTIF", "Alerts", "/traveler/notifications", ["Admin"]),
      navigationItem("PROFILE-01", "Settings", "/profile", ["Admin"]),
    ],
    mobile: [
      navigationItem("ADM-ROOT", "Home", "/admin", ["Admin"], { mobile: true, mobilePriority: 1 }),
      navigationItem("ADM-01", "Queues", "/admin/ops/disputes", ["Admin"], { mobile: true, mobilePriority: 2 }),
      navigationItem("ADM-RPT", "Audit", "/admin/reports", ["Admin"], { mobile: true, mobilePriority: 3 }),
      navigationItem("TRAV-NOTIF", "Alerts", "/traveler/notifications", ["Admin"], { mobile: true, mobilePriority: 4 }),
      navigationItem("ADM-BADGES", "More", "/admin", ["Admin"], { mobile: true, mobilePriority: 5, more: true }),
    ],
  },
};

export function navigationForRole(role: UserRole, mobile = false) {
  return ROLE_NAVIGATION[role]?.[mobile ? "mobile" : "desktop"] ?? ROLE_NAVIGATION.Guest[mobile ? "mobile" : "desktop"];
}

export function manifestCanonicalPaths() {
  return SCREEN_MANIFEST.map((definition) => definition.canonicalPath);
}

export function manifestAliases() {
  return SCREEN_MANIFEST.flatMap((definition) => definition.aliases ?? definition.patterns.slice(1));
}

const TEST_VALUE_BY_PARAM: Record<string, string> = {
  bookingId: "booking-00000000-0000-4000-8000-000000000000",
  propertyId: SAMPLE_PROPERTY_ID,
  reservationId: "reservation-00000000-0000-4000-8000-000000000000",
  screenId: "PUB-01",
  slug: "sample",
  state: "review",
};

export function manifestTestPaths() {
  return SCREEN_MANIFEST.flatMap((definition) => definition.patterns.map((pattern) => pattern.replace(/:([A-Za-z]+)/g, (_, key: string) => TEST_VALUE_BY_PARAM[key] ?? "sample")));
}
