import {
  BadgeCheck,
  CalendarDays,
  CreditCard,
  Home,
  LayoutDashboard,
  ListChecks,
  MessageSquare,
  Settings,
  ShieldCheck,
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
  { label: "Overview", href: "/guest-dashboard", icon: <LayoutDashboard size={17} />, routes: ["guest-dashboard"] },
  { label: "Host", href: "/host-dashboard", icon: <Home size={17} />, routes: ["host-dashboard"] },
  { label: "Properties", href: "/host/properties", icon: <BadgeCheck size={17} />, routes: ["property-management"] },
  { label: "Reservations", href: "/bookings", icon: <ListChecks size={17} />, routes: ["bookings", "payment"] },
  { label: "Calendar", href: "/calendar", icon: <CalendarDays size={17} />, routes: ["calendar"] },
  { label: "Wellness", href: "/host/wellness", icon: <ShieldCheck size={17} />, routes: ["host-wellness", "officer-wellness"] },
  { label: "Messages", href: "/profile", icon: <MessageSquare size={17} />, routes: ["profile"] },
  { label: "Payments", href: "/payment-confirmation", icon: <CreditCard size={17} />, routes: [] },
  { label: "Admin", href: "/admin", icon: <Settings size={17} />, routes: ["admin"] },
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
