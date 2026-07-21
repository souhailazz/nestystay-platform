import {
  BadgeCheck,
  BarChart3,
  Bell,
  CalendarDays,
  CreditCard,
  FileText,
  Home,
  LayoutDashboard,
  ListChecks,
  Map,
  MessageSquare,
  Settings,
  ShieldCheck,
  Wrench,
} from "lucide-react";
import type { ReactNode } from "react";
import { AppLink } from "../AppLink";

type WorkspaceItem = {
  label: string;
  href: string;
  icon: ReactNode;
  routes: string[];
};

const workspaceItems: WorkspaceItem[] = [
  {
    label: "Overview",
    href: "/guest-dashboard",
    icon: <LayoutDashboard size={17} />,
    routes: ["guest-dashboard", "trav-suggestions"],
  },
  {
    label: "Wishlist",
    href: "/traveler/favorites",
    icon: <BadgeCheck size={17} />,
    routes: ["trav-favorites", "no-favorites"],
  },
  {
    label: "Payments",
    href: "/traveler/invoices",
    icon: <CreditCard size={17} />,
    routes: ["trav-invoices", "payment"],
  },
  {
    label: "Reviews",
    href: "/traveler/reviews",
    icon: <FileText size={17} />,
    routes: ["trav-reviews"],
  },
  {
    label: "Alerts",
    href: "/traveler/notifications",
    icon: <Bell size={17} />,
    routes: ["trav-notifications"],
  },
  { label: "Host", href: "/host-dashboard", icon: <Home size={17} />, routes: ["host-dashboard", "host-reports"] },
  {
    label: "Properties",
    href: "/host/properties",
    icon: <BadgeCheck size={17} />,
    routes: ["property-management", "host-property-edit"],
  },
  {
    label: "Reservations",
    href: "/bookings",
    icon: <ListChecks size={17} />,
    routes: ["bookings", "no-reservations"],
  },
  { label: "Calendar", href: "/calendar", icon: <CalendarDays size={17} />, routes: ["calendar"] },
  {
    label: "Wellness",
    href: "/host/wellness",
    icon: <ShieldCheck size={17} />,
    routes: ["host-wellness", "officer-wellness", "officer-directory", "wellness-booking"],
  },
  {
    label: "PM",
    href: "/pm/gates",
    icon: <Wrench size={17} />,
    routes: ["pm-gates", "pm-utilities", "pm-verification", "pm-reports", "pm-insurance"],
  },
  {
    label: "Directory",
    href: "/directory/businesses",
    icon: <Map size={17} />,
    routes: ["business-directory", "provider-dashboard"],
  },
  { label: "Messages", href: "/messages/document", icon: <MessageSquare size={17} />, routes: ["profile", "document-message"] },
  {
    label: "Admin",
    href: "/admin",
    icon: <Settings size={17} />,
    routes: ["admin", "admin-kpis", "admin-reports", "officer-id-reset"],
  },
  { label: "Reports", href: "/admin/kpis", icon: <BarChart3 size={17} />, routes: ["admin-kpis", "admin-reports"] },
];

export function WorkspaceFrame({ routeName, children }: { routeName: string; children: ReactNode }) {
  return (
    <div className="workspace-frame">
      <aside className="workspace-sidebar" aria-label="Workspace navigation">
        <AppLink className="workspace-wordmark" href="/">
          <img src="/assets/reference/nestystay-logo.png" alt="" />
          <span>NestyStay</span>
        </AppLink>
        <nav className="workspace-nav">
          {workspaceItems.map((item) => (
            <AppLink
              key={item.label}
              className={item.routes.includes(routeName) ? "workspace-nav-item is-active" : "workspace-nav-item"}
              href={item.href}
            >
              {item.icon}
              <span>{item.label}</span>
            </AppLink>
          ))}
        </nav>
      </aside>
      <main className="workspace-content">{children}</main>
    </div>
  );
}
